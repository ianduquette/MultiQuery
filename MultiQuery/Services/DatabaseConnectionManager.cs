using Npgsql;
using MultiQuery.Models;

namespace MultiQuery.Services;

/// <summary>
/// Service for managing database connections and testing connectivity.
/// </summary>
public class DatabaseConnectionManager {
    private const int DefaultConnectionTimeoutSeconds = 30;
    private const int DefaultCommandTimeoutSeconds = 60;

    /// <summary>
    /// Builds a PostgreSQL connection string from environment configuration.
    /// </summary>
    /// <param name="environment">Database environment configuration.</param>
    /// <returns>PostgreSQL connection string.</returns>
    public string BuildConnectionString(DatabaseEnvironment environment) {
        var builder = new NpgsqlConnectionStringBuilder {
            Host = environment.Hostname,
            Port = environment.Port,
            Database = environment.Database,
            Username = environment.Username,
            Password = environment.Password,
            Timeout = DefaultConnectionTimeoutSeconds,
            CommandTimeout = DefaultCommandTimeoutSeconds,
            Pooling = true,
            MinPoolSize = 1,
            MaxPoolSize = 5,
            // Security settings
            SslMode = SslMode.Prefer
        };

        return builder.ToString();
    }

    /// <summary>
    /// Tests connectivity to a database environment.
    /// </summary>
    /// <param name="environment">Database environment to test.</param>
    /// <returns>Connection test result.</returns>
    public async Task<ConnectionTestResult> TestConnectionAsync(DatabaseEnvironment environment) {
        var result = new ConnectionTestResult {
            ClientId = environment.ClientId,
            StartTime = DateTime.UtcNow
        };

        try {
            var connectionString = BuildConnectionString(environment);

            using var connection = new NpgsqlConnection(connectionString);

            // Test connection
            await connection.OpenAsync();

            // Test basic query execution
            using var command = new NpgsqlCommand("SELECT 1 as test_value, version() as pg_version", connection);
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync()) {
                result.PostgreSqlVersion = reader.GetString(1); // pg_version is the second column
                result.Success = true;
                result.Message = "Connection successful";
            } else {
                result.Success = false;
                result.Message = "Failed to execute test query";
            }
        } catch (NpgsqlException ex) {
            result.Success = false;
            result.Message = $"PostgreSQL Error: {ex.Message}";
            result.ErrorCode = ex.SqlState;
        } catch (TimeoutException ex) {
            result.Success = false;
            result.Message = $"Connection timeout: {ex.Message}";
        } catch (Exception ex) {
            result.Success = false;
            result.Message = $"Unexpected error: {ex.Message}";
        } finally {
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
        }

        return result;
    }

    /// <summary>
    /// Tests connectivity to all database environments.
    /// </summary>
    /// <param name="environments">List of database environments to test.</param>
    /// <param name="maxConcurrency">Maximum number of concurrent connection tests.</param>
    /// <returns>List of connection test results.</returns>
    public async Task<List<ConnectionTestResult>> TestAllConnectionsAsync(
        IEnumerable<DatabaseEnvironment> environments,
        int maxConcurrency = 5) {
        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = environments.Select(async env => {
            await semaphore.WaitAsync();
            try {
                return await TestConnectionAsync(env);
            } finally {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    /// <summary>
    /// Creates a new database connection for query execution.
    /// </summary>
    /// <param name="environment">Database environment configuration.</param>
    /// <returns>Configured NpgsqlConnection.</returns>
    public NpgsqlConnection CreateConnection(DatabaseEnvironment environment) {
        var connectionString = BuildConnectionString(environment);
        return new NpgsqlConnection(connectionString);
    }

    /// <summary>
    /// Displays connection test results to console.
    /// </summary>
    /// <param name="results">Connection test results to display.</param>
    /// <param name="verbose">Whether to show detailed information.</param>
    public void DisplayConnectionResults(List<ConnectionTestResult> results, bool verbose = false) {
        var successful = results.Count(r => r.Success);
        var failed = results.Count - successful;

        Console.WriteLine($"=== Database Connection Test Results ===");
        Console.WriteLine($"Total: {results.Count}, Successful: {successful}, Failed: {failed}");
        Console.WriteLine();

        foreach (var result in results.OrderBy(r => r.ClientId)) {
            var status = result.Success ? "✓" : "✗";
            var duration = $"{result.Duration.TotalMilliseconds:F0}ms";

            Console.WriteLine($"{status} {result.ClientId,-15} ({duration,6}) - {result.Message}");

            if (verbose && result.Success && !string.IsNullOrEmpty(result.PostgreSqlVersion)) {
                // Extract just the version number for cleaner output
                var versionParts = result.PostgreSqlVersion.Split(' ');
                var version = versionParts.Length > 1 ? versionParts[1] : "Unknown";
                Console.WriteLine($"    PostgreSQL Version: {version}");
            }

            if (verbose && !result.Success && !string.IsNullOrEmpty(result.ErrorCode)) {
                Console.WriteLine($"    Error Code: {result.ErrorCode}");
            }
        }

        Console.WriteLine();
    }
}

/// <summary>
/// Represents the result of a database connection test.
/// </summary>
public class ConnectionTestResult {
    public required string ClientId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? PostgreSqlVersion { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
}