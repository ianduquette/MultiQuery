# MultiQuery

Run a read-only SQL query against many PostgreSQL databases and stream the results.

- **SELECT only.** Queries run inside a read-only transaction — no writes possible.
- **Streaming.** Each database's results print as soon as it finishes.
- **Table or CSV** output.

## 1. Build

```bash
dotnet build -c Release
```

Or produce self-contained binaries for distribution:

```bash
.\publish-all.bat        # outputs to publish/windows-x64 and publish/linux-x64
```

See [INSTALLATION.md](INSTALLATION.md) to add `multiquery` to your PATH.

## 2. Create `environments.json`

The app needs a JSON file listing the databases to query. Copy the template and fill it in:

```bash
cp environments.json.template environments.json
```

```json
{
  "environments": [
    {
      "clientId": "prod-db-1",
      "hostname": "10.0.0.5",
      "port": 5432,
      "database": "appdb",
      "username": "readonly",
      "password": "..."
    }
  ]
}
```

`environments.json` is gitignored. Don't commit real credentials.

### Optional: auto-generate from Kubernetes

If your databases live in a k8s cluster with a specific secret/service layout, `generate-environments.ps1` can build `environments.json` for you. **Read the comment at the top of the script — it's tailored to one cluster's naming convention and you will likely need to tweak it.**

```powershell
.\generate-environments.ps1 -EnvType test     # or -EnvType prod
```

## 3. Run

The first argument can be **either a `.sql` file or an inline SQL string**:

```bash
multiquery query.sql                      # run a .sql file
multiquery "SELECT now();"                # run an inline query
multiquery query.sql --csv > results.csv  # CSV output
multiquery query.sql --verbose            # see connection + path resolution details
multiquery query.sql -e other-envs.json   # use a different environments file
```

Detection rule: if the argument matches an existing file, it's read as a file. Otherwise it's executed as SQL. **Exception:** if the argument ends in `.sql` but the file doesn't exist, the run fails fast (no silent fallback to "treat as SQL") to catch typos.

Either way, the query is validated as read-only before it runs.

Files (query and environments) are resolved relative to your current directory. If the environments file isn't there, the app also checks the directory where `multiquery` is installed — useful for a shared default.

## Options

| Flag | Description |
|------|-------------|
| `<query>` | Path to a `.sql` file **or** an inline SQL string (positional, required) |
| `-e, --environments-file` | Path to environments JSON. Default: `environments.json` |
| `-c, --csv` | Output CSV instead of a table |
| `-v, --verbose` | Show connection diagnostics and path resolution |
| `-h, --help` | Show help |

## Query rules

Only `SELECT` is allowed. The validator rejects `INSERT`/`UPDATE`/`DELETE`/`MERGE`, DDL, and transaction control statements; the connection then runs in a PostgreSQL read-only transaction as a second line of defense. CTEs, joins, window functions, and read-only function calls all work.

Generating a script *as text* is fine:

```sql
SELECT 'DELETE FROM logs WHERE created < ''' || (current_date - 90)::text || ''';' AS script
FROM tables_to_clean;
```

## Troubleshooting

- **Connection fails for one DB** — run with `--verbose` for the underlying error. The other DBs still run.
- **"DML operations are not allowed"** — your query has a write statement. Use `SELECT` (or generate the write as text, as above).
- **File not found** — `--verbose` prints exactly where it looked.
- **Garbled `✓` characters on Windows** — fixed; the app now forces UTF-8 console output.

## Project layout

```
MultiQuery/
  Program.cs              entry point
  Models/                 DTOs
  Services/               connection, validation, execution, formatting
generate-environments.ps1 optional k8s helper (cluster-specific)
environments.json.template committed template
environments.json         your local, gitignored config
```

Built on .NET 8, Npgsql, and System.CommandLine.
