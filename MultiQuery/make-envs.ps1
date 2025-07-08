param(
    [Parameter(Mandatory=$true)]
    [string]$ContextName,
    
    [Parameter(Mandatory=$true)]
    [string]$Namespace,
    
    [Parameter(Mandatory=$false)]
    [string]$EnvironmentPostfix = "test"
)

# Check if required arguments are provided
if (-not $ContextName -or -not $Namespace) {
    Write-Host "Usage: .\make-envs.ps1 -ContextName <context_name> -Namespace <namespace> [-EnvironmentPostfix <environment_postfix>]"
    Write-Host "Example: .\make-envs.ps1 -ContextName mycontext -Namespace mynamespace -EnvironmentPostfix test"
    exit 1
}

$CtxPrefix = "--context $ContextName -n $Namespace"
$OutputFile = "$EnvironmentPostfix-environments.json"

Write-Host "Generating $OutputFile file for $EnvironmentPostfix environment"

# Start the JSON file
Set-Content -Path $OutputFile -Value "{"
Add-Content -Path $OutputFile -Value "  `"environments`": ["

$FirstEntry = $true

# Get all clusters matching the pattern
try {
    $ClusterNames = kubectl get cluster -n $Namespace --context $ContextName -o custom-columns=NAME:.metadata.name --no-headers | Where-Object { $_ -match "-$EnvironmentPostfix$" }
    
    foreach ($ClusterName in $ClusterNames) {
        # Extract client ID by removing gis- prefix and -${ENV_POSTFIX} suffix
        $ClientId = $ClusterName -replace "^gis-", "" -replace "-$EnvironmentPostfix$", ""
        
        Write-Host "Processing client: $ClientId"
        
        # Get PostgreSQL password from secret
        $PostgresPassword = $null
        try {
            $PostgresPassword = kubectl get secrets $CtxPrefix.Split() gis-$ClientId-$EnvironmentPostfix-app --template='{{.data.password | base64decode}}' 2>$null
        }
        catch {
            Write-Host "    Warning: Could not retrieve password for $ClientId, skipping..."
            continue
        }
        
        if (-not $PostgresPassword) {
            Write-Host "    Warning: Could not retrieve password for $ClientId, skipping..."
            continue
        }
        
        # Get load balancer IP
        $PgLbIp = $null
        try {
            $PgLbIp = kubectl get service $CtxPrefix.Split() gis-$ClientId-$EnvironmentPostfix-rw-lb -o custom-columns=IP:.status.loadBalancer.ingress[0].ip --no-headers 2>$null
        }
        catch {
            Write-Host "    Warning: Could not retrieve load balancer IP for $ClientId, skipping..."
            continue
        }
        
        if (-not $PgLbIp -or $PgLbIp -eq "<none>") {
            Write-Host "    Warning: Could not retrieve load balancer IP for $ClientId, skipping..."
            continue
        }
        
        # Get PostgreSQL port from service
        $PgPort = $null
        try {
            $PgPort = kubectl get service $CtxPrefix.Split() gis-$ClientId-$EnvironmentPostfix-rw-lb -o custom-columns=PORT:.spec.ports[0].port --no-headers 2>$null
        }
        catch {
            Write-Host "    Warning: Could not retrieve port for $ClientId, using default 5432"
            $PgPort = 5432
        }
        
        if (-not $PgPort -or $PgPort -eq "<none>") {
            Write-Host "    Warning: Could not retrieve port for $ClientId, using default 5432"
            $PgPort = 5432
        }
        
        Write-Host "    Retrieved connection details: $PgLbIp`:$PgPort"
        
        # Add comma if not first entry
        if (-not $FirstEntry) {
            Add-Content -Path $OutputFile -Value ","
        }
        
        # Add JSON entry
        $JsonEntry = @"
    {
      "clientId": "$ClientId",
      "hostname": "$PgLbIp",
      "port": $PgPort,
      "database": "gis_$ClientId",
      "username": "gis_$ClientId",
      "password": "$PostgresPassword"
    }
"@
        
        Add-Content -Path $OutputFile -Value $JsonEntry -NoNewline
        
        $FirstEntry = $false
        Write-Host "    Added $ClientId to $OutputFile"
    }
}
catch {
    Write-Host "Error: Failed to get clusters - $($_.Exception.Message)"
    exit 1
}

# Close the JSON file
Add-Content -Path $OutputFile -Value ""
Add-Content -Path $OutputFile -Value "  ]"
Add-Content -Path $OutputFile -Value "}"

Write-Host "Done! Created $OutputFile"