# MultiQuery

Execute SQL queries against multiple PostgreSQL databases with real-time streaming results and bulletproof security.

## Overview

MultiQuery is a command-line tool designed for DevOps and database administrators that allows you to run SQL queries against multiple PostgreSQL databases with real-time streaming results. It features dual-layer security with query validation and read-only transactions, supports CSV output, and provides detailed connection diagnostics with immediate feedback as each database completes.

## Key Features

- **Real-Time Streaming Results**: See results immediately as each database completes - no waiting for slow databases
- **Automatic Path Resolution**: Files resolved relative to current working directory with full backward compatibility
- **Dual-Layer Security**: Query validation + read-only transactions prevent any data modifications
- **Multi-Database Execution**: Run queries across multiple PostgreSQL environments with immediate feedback
- **Flexible Output**: Table format (default) or CSV export for data analysis with streaming support
- **DevOps Friendly**: Perfect for monitoring, reporting, and script generation with responsive feedback
- **Comprehensive Diagnostics**: Detailed connection testing and verbose error reporting
- **Fast & Efficient**: Optimized output formatting with StringBuilder for large result sets

## Installation

For detailed installation instructions to add MultiQuery to your system PATH, see [INSTALLATION.md](INSTALLATION.md).

### Quick Installation Summary
1. Build the application using `publish-all.bat`
2. Copy the executable to a directory in your PATH
3. Run `multiquery --help` to verify installation

## Usage

MultiQuery now supports **automatic path resolution** for both query files and environment files:

```bash
# Basic usage - files resolved relative to current directory
multiquery query.sql

# With custom environments file (also resolved relatively)
multiquery query.sql -e custom-environments.json

# Absolute paths still work for full compatibility
multiquery /full/path/to/query.sql -e /full/path/to/environments.json

# Mix relative and absolute paths
multiquery query.sql -e /full/path/to/environments.json

# With additional options
multiquery query.sql --csv --verbose
```

### Path Resolution Features

- **Relative Path Support**: Files are automatically resolved relative to your current working directory
- **Environments File Fallback**: If `environments.json` isn't found in current directory, automatically checks the application install directory
- **Backward Compatibility**: Absolute paths continue to work exactly as before
- **Verbose Debugging**: Use `--verbose` to see detailed path resolution information including fallback search locations
- **Better Error Messages**: Clear feedback when files aren't found with helpful tips

### Example Workflow
```bash
# Navigate to your project directory
cd /path/to/my/sql/project

# Your files are in the current directory
ls
# query.sql  environments.json

# Run MultiQuery - it finds files automatically
multiquery query.sql
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

The application uses a JSON configuration file to define database connections. By default, it looks for `environments.json` using this search order:

1. **Current working directory** (for project-specific configurations)
2. **Application install directory** (for global default configuration)

This allows you to have a global default `environments.json` in your MultiQuery install directory, while still supporting project-specific configurations that override the global defaults.

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

- `environments.json` (default, with fallback to install directory)
- Custom files via `-e` or `--environments-file` option (also support fallback behavior)
- Files matching `*-environments.json` pattern (e.g., `prod-environments.json`, `test-environments.json`)

### Environment File Search Behavior

**For relative paths** (including default `environments.json`):
1. First checks your current working directory
2. If not found, checks the application install directory
3. Uses the first file found, or shows error if neither exists

**For absolute paths**: Uses the exact path specified (no fallback)

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

## Building and Publishing

### Development Build

```bash
# Build the project for development
dotnet build

# Run from build output
.\MultiQuery\bin\Debug\net8.0\multiquery.exe query.sql
```

### Production Publishing

MultiQuery includes automated publishing scripts that create self-contained executables for both Windows and Linux platforms. These executables include the .NET runtime and require no additional dependencies on target machines.

#### Quick Publishing (Recommended)

```bash
# Build for both Windows and Linux platforms
.\publish-all.bat
```

This creates optimized, self-contained executables:
- **Windows**: `publish/windows-x64/multiquery.exe` (~16 MB)
- **Linux**: `publish/linux-x64/multiquery` (~18 MB)

#### Manual Publishing

```bash
# Windows x64 self-contained
dotnet publish MultiQuery/MultiQuery.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o publish/windows-x64

# Linux x64 self-contained (cross-compiled from Windows)
dotnet publish MultiQuery/MultiQuery.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o publish/linux-x64
```

#### Publishing Features

- ✅ **Self-Contained**: No .NET runtime required on target machines
- ✅ **Single File**: Everything packaged into one executable
- ✅ **Cross-Platform**: Build Linux binaries from Windows
- ✅ **Optimized**: Code trimming and ReadyToRun for performance
- ✅ **Small Size**: ~16-18 MB per platform

#### Distribution

**Windows Distribution:**
```bash
# Copy to target Windows machine
copy publish\windows-x64\multiquery.exe \\target-machine\tools\
```

**Linux Distribution:**
```bash
# Copy to target Linux machine and make executable
scp publish/linux-x64/multiquery user@linux-server:/usr/local/bin/
ssh user@linux-server "chmod +x /usr/local/bin/multiquery"
```

#### Cleanup

```bash
# Remove all published files
.\clean-publish.bat
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
│   ├── PathResolver.cs     # Path resolution service (NEW)
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

## Streaming Results

MultiQuery features **real-time streaming results** that provide immediate feedback as each database completes its query execution. This eliminates waiting for slow databases and provides a much more responsive user experience.

### How Streaming Works

- **Sequential Execution**: Queries are executed one database at a time to ensure perfect result grouping
- **Immediate Display**: Results appear instantly when each database completes
- **Progress Indication**: Shows execution progress with "Executing against X database(s)..." message
- **Perfect Grouping**: Each database's results stay perfectly organized and grouped together

### Streaming Benefits

✅ **Immediate Feedback** - See results as soon as each database completes
✅ **No Waiting** - Fast databases don't wait for slow ones
✅ **Better UX** - Responsive feedback perfect for monitoring and DevOps
✅ **Same Functionality** - All existing features work identically
✅ **Perfect Grouping** - Results maintain exact same organization

### Example Streaming Output

```
=== Query Results: monitoring-query.sql ===
Executing against 3 database(s)...

[fast-db] ✓ 5 rows (12ms)        ← Appears immediately
service_name | status  | last_check
-------------|---------|--------------------
web-api      | running | 2024-01-15 14:30:00
database     | running | 2024-01-15 14:29:55

[medium-db] ✓ 3 rows (156ms)     ← Appears when complete
service_name | status  | last_check
-------------|---------|--------------------
web-api      | running | 2024-01-15 14:30:00
cache        | running | 2024-01-15 14:29:50

[slow-db] ✓ 8 rows (2340ms)      ← Appears last
service_name | status  | last_check
-------------|---------|--------------------
web-api      | running | 2024-01-15 14:30:00
database     | running | 2024-01-15 14:29:55
cache        | stopped | 2024-01-15 14:25:10

Query execution complete!
```

## Output Formats

### Table Format (Default)
```
=== Query Results: monitoring-query.sql ===
Executing against 2 database(s)...

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

Query execution complete!
```

### CSV Format (with Streaming)
```bash
# Export to CSV for analysis with streaming results
multiquery report-query.sql --csv > results.csv
```

CSV output streams data as each database completes, with headers written once at the beginning:

```csv
client_id,service_name,status,last_check
prod-db,web-api,running,2024-01-15 14:30:00
prod-db,database,running,2024-01-15 14:29:55
prod-db,cache,stopped,2024-01-15 14:25:10
staging-db,web-api,running,2024-01-15 14:30:00
staging-db,database,running,2024-01-15 14:29:55
staging-db,cache,running,2024-01-15 14:29:50
```

**Note**: In CSV mode, you'll see the progress header followed by streaming data rows as each database completes.

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
- **Query Files**: Resolved relative to your current working directory only
- **Environment Files**: Support fallback behavior - checked in current directory first, then application install directory
- Use `--verbose` to see detailed path resolution information including:
  - Original paths provided
  - Whether paths are relative or absolute
  - Current working directory
  - Search locations tried (for environment files)
  - Resolved absolute paths
  - File existence confirmation
- **Tip**: Place `environments.json` in your application install directory for a global default configuration

### Performance Issues
- Large result sets are handled efficiently with optimized formatting
- **Streaming results** provide immediate feedback - no waiting for slow databases
- Use `--csv` format for better performance with very large datasets
- Consider adding `LIMIT` clauses for exploratory queries
- **Real-time feedback** makes MultiQuery perfect for monitoring and DevOps scenarios