# Generates environments.json from Kubernetes secrets.
#
# This script is tailored to a specific cluster layout:
#   - Per-client app secrets named "<client>-<env>-app" (e.g. acme-test-app)
#   - A matching load-balancer service "<client>-<env>-rw-lb" exposing the DB IP
#   - Secret data keys: port, dbname, username, password (base64-encoded)
#
# If your cluster uses different naming, secret keys, or a different way of
# exposing the DB host, edit the filter/regex and the field mappings below.
# You can also skip this script entirely and hand-write environments.json — the
# app only cares about the final JSON shape (see environments.json.template).

param(
    [Parameter(Mandatory=$true)]
    [string]$Namespace,
    [ValidateSet("test","prod")]
    [string]$EnvType = "test",
    [string]$OutputPath = "environments.json"
)

function Decode([string]$b64) {
    [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($b64))
}

$secrets = kubectl get secrets -n $Namespace -o name |
    Where-Object { $_ -match "-$EnvType-app$" -and $_ -notmatch "^secret/gis-" } |
    ForEach-Object { $_ -replace '^secret/','' }

$environments = foreach ($secret in $secrets) {
    $client  = $secret -replace "-$EnvType-app$",""
    $svcName = "$client-$EnvType-rw-lb"

    $ip   = kubectl get svc $svcName -n $Namespace -o jsonpath='{.status.loadBalancer.ingress[0].ip}'
    $data = (kubectl get secret $secret -n $Namespace -o json | ConvertFrom-Json).data

    [pscustomobject]@{
        clientId = "$client-$EnvType"
        hostname = $ip
        port     = [int](Decode $data.port)
        database = Decode $data.dbname
        username = Decode $data.username
        password = Decode $data.password
    }
}

@{ environments = $environments } |
    ConvertTo-Json -Depth 5 |
    Set-Content -Path $OutputPath -Encoding utf8

Write-Host "Wrote $($environments.Count) environment(s) to $OutputPath"
