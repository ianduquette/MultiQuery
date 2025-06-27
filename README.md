# MultiQuery

Execute SQL queries against multiple PostgreSQL databases simultaneously with bulletproof security.

## Overview

MultiQuery is a command-line tool designed for DevOps and database administrators that allows you to run SQL queries against multiple PostgreSQL databases and view the results in a unified format. It features dual-layer security with query validation and read-only transactions, supports CSV output, and provides detailed connection diagnostics.

## Key Features

- **Dual-Layer Security**: Query validation + read-only transactions prevent any data modifications
- **Multi-Database Execution**: Run queries across multiple PostgreSQL environments simultaneously
- **Flexible Output**: Table format (default) or CSV export for data analysis
- **DevOps Friendly**: Perfect for monitoring, reporting, and script generation
- **Comprehensive Diagnostics**: Detailed connection testing and verbose error reporting
- **Fast & Efficient**: Optimized output formatting with StringBuilder for large result sets

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

MultiQuery uses **dual-layer security** to ensure only read operations are performed:

1. **Query Validation**: Only `SELECT` statements pass initial validation
2. **Read-Only Transactions**: Database-level protection prevents any modifications

### Supported Query Types
- `SELECT` statements (all variants: joins, subqueries, CTEs, window functions)
- Read-only functions (`now()`, `version()`, `generate_series()`, etc.)
- Script generation queries: `SELECT 'DELETE FROM...' as cleanup_script`

### File Requirements
- Files should use `.sql` extension
- Comments and whitespace are supported
- Multiple statements separated by semicolons

Example query file:
```sql
-- DevOps monitoring query
SELECT
    client_name,
    database_size,
    active_connections,
    last_backup_date
FROM monitoring_view
WHERE last_checked > now() - interval '1 hour'
ORDER BY database_size DESC;
```

Example script generation:
```sql
-- Generate cleanup scripts for review
SELECT
    'DELETE FROM ' || table_name || ' WHERE created_date < ''' ||
    (current_date - interval '90 days')::date || ''';' as cleanup_script
FROM table_stats
WHERE row_count > 1000000;
```

## Building

```bash
# Build the project
dotnet build

# Run from build output
.\MultiQuery\bin\Debug\net8.0\multiquery.exe query.sql
```

## Security Features

### Dual-Layer Security Architecture

MultiQuery implements **defense in depth** with two security layers:

1. **Query Validation Layer**
   - Blocks DML operations (`INSERT`, `UPDATE`, `DELETE`, `MERGE`)
   - Blocks DDL operations (`CREATE`, `ALTER`, `DROP`, `TRUNCATE`)
   - Blocks transaction control (`BEGIN`, `COMMIT`, `ROLLBACK`)
   - Provides fast feedback with clear error messages

2. **Read-Only Transaction Layer**
   - **Database-level enforcement** via PostgreSQL read-only transactions
   - **Bulletproof protection** - no SQL statement can bypass this
   - Blocks any function calls that attempt data modification
   - Prevents any extension-based bypasses

### Security Benefits

- ✅ **Zero Risk of Data Modification**: Impossible to alter, delete, or create data
- ✅ **Function-Safe**: Even dangerous functions cannot modify data in read-only mode
- ✅ **Extension-Safe**: No PostgreSQL extension can bypass read-only transactions
- ✅ **Audit-Friendly**: Clear transaction logs show read-only access patterns

### Credential Management Best Practices

- **Never commit real credentials** to version control
- Use environment-specific configuration files
- Store production credentials securely (Azure Key Vault, AWS Secrets Manager, etc.)
- Use the principle of least privilege for database users
- Consider using read-only database users for additional security

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
│   ├── EnvironmentConfig.cs
│   └── QueryResult.cs      # Query execution result models
├── Services/               # Business logic services
│   ├── DatabaseConnectionManager.cs
│   ├── EnvironmentLoader.cs
│   ├── QueryFileReader.cs
│   ├── QueryValidator.cs
│   ├── QueryExecutionService.cs    # Multi-database query execution
│   └── ResultsFormatterService.cs  # Output formatting (table/CSV)
└── bin/Debug/net8.0/      # Build output
```

### Dependencies

- .NET 8.0
- System.CommandLine (command-line parsing)
- Npgsql (PostgreSQL connectivity)

## Output Formats

### Table Format (Default)
```
=== Query Results: monitoring-query.sql ===

[prod-db] ✓ 3 rows (45ms)
service_name | status  | last_check
-------------|---------|--------------------
web-api      | running | 2024-01-15 14:30:00
database     | running | 2024-01-15 14:29:55
cache        | stopped | 2024-01-15 14:25:10

[staging-db] ✓ 3 rows (52ms)
service_name | status  | last_check
-------------|---------|--------------------
web-api      | running | 2024-01-15 14:30:00
database     | running | 2024-01-15 14:29:55
cache        | running | 2024-01-15 14:29:50
```

### CSV Format
```bash
# Export to CSV for analysis
multiquery report-query.sql --csv > results.csv
```
```csv
client_id,service_name,status,last_check
prod-db,web-api,running,2024-01-15 14:30:00
prod-db,database,running,2024-01-15 14:29:55
prod-db,cache,stopped,2024-01-15 14:25:10
staging-db,web-api,running,2024-01-15 14:30:00
staging-db,database,running,2024-01-15 14:29:55
staging-db,cache,running,2024-01-15 14:29:50
```

## Examples

### DevOps Monitoring
```bash
# Check service health across environments
multiquery health-check.sql --verbose

# Generate maintenance scripts
multiquery cleanup-generator.sql --csv > cleanup-scripts.csv
```

### Database Analysis
```bash
# Compare table sizes across environments
multiquery "SELECT schemaname, tablename, pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size FROM pg_tables WHERE schemaname = 'public' ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC LIMIT 10;"

# Monitor connection counts
multiquery "SELECT datname, numbackends, xact_commit, xact_rollback FROM pg_stat_database WHERE datname NOT IN ('template0', 'template1', 'postgres');"
```

### Environment-Specific Execution
```bash
# Production environments only
multiquery query.sql -e prod-environments.json

# Development environments
multiquery query.sql -e dev-environments.json

# All environments with detailed output
multiquery query.sql --verbose
```

## Troubleshooting

### Connection Issues
- Verify database credentials in your environments file
- Check network connectivity to database servers
- Ensure PostgreSQL server is accepting connections
- Use `--verbose` flag for detailed error information
- Check if database user has CONNECT privileges

### Query Security Errors

#### "DML operations are not allowed"
- **Cause**: Query contains `INSERT`, `UPDATE`, `DELETE`, or `MERGE` statements
- **Solution**: Use only `SELECT` statements for data retrieval
- **DevOps Tip**: Generate scripts with `SELECT 'DELETE FROM...' as script` instead

#### "cannot execute DELETE in a read-only transaction"
- **Cause**: A modification statement somehow bypassed validation (rare)
- **Solution**: This is the read-only transaction protection working correctly
- **Action**: Review your query to ensure it only contains SELECT statements

#### "Transaction control statements are not allowed"
- **Cause**: Query contains `BEGIN`, `COMMIT`, `ROLLBACK`, etc.
- **Solution**: Remove transaction control statements - MultiQuery manages transactions automatically

### Query Validation Errors
- Only SELECT statements are allowed for security
- Ensure your SQL file contains valid SQL syntax
- Check for proper statement termination with semicolons
- Use `--verbose` for detailed validation information

### File Not Found Errors
- Verify the query file path is correct
- Ensure the environments file exists (default: `environments.json`)
- Use absolute paths if relative paths are not working

### Performance Issues
- Large result sets are handled efficiently with optimized formatting
- Use `--csv` format for better performance with very large datasets
- Consider adding `LIMIT` clauses for exploratory queries