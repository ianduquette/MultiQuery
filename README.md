# MultiQuery

Execute SQL queries against multiple PostgreSQL databases simultaneously.

## Overview

MultiQuery is a command-line tool that allows you to run SQL queries against multiple PostgreSQL databases and view the results in a unified format. It supports CSV output and provides detailed connection diagnostics.

## Usage

```bash
# Basic usage with default environments.json
multiquery query.sql

# With custom environments file
multiquery query.sql -e custom-environments.json

# With additional options
multiquery query.sql --csv --verbose
```

### Command Line Options

```
Usage:
  multiquery <queryFile> [options]

Arguments:
  <queryFile>  Path to the SQL query file to execute

Options:
  -e, --environments-file <environments-file>  Path to the JSON file containing database environment configurations [default: environments.json]
  -c, --csv                                    Output results in CSV format
  -v, --verbose                                Enable verbose output with additional diagnostic information
  --version                                    Show version information
  -?, -h, --help                               Show help and usage information
```

## Configuration

### Environment Configuration File

The application uses a JSON configuration file to define database connections. By default, it looks for `environments.json` in the current directory.

#### Template Structure

```json
{
  "environments": [
    {
      "clientId": "unique-client-identifier",
      "hostname": "database-server.example.com",
      "port": 5432,
      "database": "database_name",
      "username": "database_user",
      "password": "database_password"
    }
  ]
}
```

#### Setting Up Your Environment File

1. **Copy the template**: The included `environments.json` contains example configurations
2. **Update credentials**: Replace the example values with your actual database credentials
3. **Secure your file**: Ensure your actual credentials file is not committed to version control

**⚠️ Security Note**: The included `environments.json` file contains random/example credentials for demonstration purposes only. Replace these with your actual database credentials.

### Supported Environment File Patterns

- `environments.json` (default)
- Custom files via `-e` or `--environments-file` option
- Files matching `*-environments.json` pattern (e.g., `prod-environments.json`, `test-environments.json`)

## Query Files

- Only `SELECT` statements are allowed for security
- Comments and whitespace are supported
- Files should use `.sql` extension

Example query file:
```sql
-- Get user count by status
SELECT 
    status,
    COUNT(*) as user_count
FROM users 
GROUP BY status
ORDER BY user_count DESC;
```

## Building

```bash
# Build the project
dotnet build

# Run from build output
.\MultiQuery\bin\Debug\net8.0\multiquery.exe query.sql
```

## Security Best Practices

### Credential Management

- **Never commit real credentials** to version control
- Use environment-specific configuration files
- Store production credentials securely (Azure Key Vault, AWS Secrets Manager, etc.)
- Use the principle of least privilege for database users

### Files Excluded from Version Control

The following files are automatically excluded via `.gitignore`:
- `*-environments.json` (environment-specific credential files)
- `make-envs.sh` (credential generation scripts)
- `*.env` files
- Production configuration files

## Development

### Project Structure

```
MultiQuery/
├── Program.cs              # Main application entry point
├── Models/                 # Data models
│   ├── CommandLineOptions.cs
│   ├── DatabaseEnvironment.cs
│   └── EnvironmentConfig.cs
├── Services/               # Business logic services
│   ├── DatabaseConnectionManager.cs
│   ├── EnvironmentLoader.cs
│   ├── QueryFileReader.cs
│   └── QueryValidator.cs
└── bin/Debug/net8.0/      # Build output
```

### Dependencies

- .NET 8.0
- System.CommandLine (command-line parsing)
- Npgsql (PostgreSQL connectivity)

## Examples

### Basic Query Execution
```bash
# Execute query against default environments
multiquery "SELECT version();"
```

### Multiple Environment Testing
```bash
# Test with verbose output
multiquery health-check.sql --verbose

# Export results to CSV
multiquery report-query.sql --csv > results.csv
```

### Environment-Specific Execution
```bash
# Production environments
multiquery query.sql -e prod-environments.json

# Development environments  
multiquery query.sql -e dev-environments.json
```

## Troubleshooting

### Connection Issues
- Verify database credentials in your environments file
- Check network connectivity to database servers
- Ensure PostgreSQL server is accepting connections
- Use `--verbose` flag for detailed error information

### Query Validation Errors
- Only SELECT statements are allowed
- Ensure your SQL file contains valid SQL syntax
- Check for proper statement termination with semicolons

### File Not Found Errors
- Verify the query file path is correct
- Ensure the environments file exists (default: `environments.json`)
- Use absolute paths if relative paths are not working