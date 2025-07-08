using Npgsql;
using MultiQuery.Models;

namespace MultiQuery.Services;

/// <summary>
/// Service for executing SQL queries against multiple database environments.
/// </summary>
public class QueryExecutionService {
    private readonly DatabaseConnectionManager _connectionManager;

    public QueryExecutionService() {
        _connectionManager = new DatabaseConnectionManager();
    }

    /// <summary>
    /// Executes a query against multiple database environments.
    /// </summary>
    /// <param name="queryContent">The SQL query to execute.</param>
    /// <param name="environments">List of database environments to query.</param>
    /// <param name="queryFile">Path to the query file (for reporting).</param>
    /// <returns>Results from all database environments.</returns>
    public async Task<MultiQueryResult> ExecuteQueryAsync(
        string queryContent,
        List<DatabaseEnvironment> environments,
        string queryFile) {
        var result = new MultiQueryResult {
            QueryFile = queryFile
        };

        // Execute query against each environment
        foreach (var environment in environments) {
            var queryResult = await ExecuteQueryOnEnvironmentAsync(queryContent, environment);
            result.Results.Add(queryResult);
        }

        return result;
    }

    /// <summary>
    /// Executes a query against multiple database environments with streaming results.
    /// </summary>
    /// <param name="queryContent">The SQL query to execute.</param>
    /// <param name="environments">List of database environments to query.</param>
    /// <param name="queryFile">Path to the query file (for reporting).</param>
    /// <param name="onResultCompleted">Callback function called when each database completes.</param>
    public async Task ExecuteQueryStreamingAsync(
        string queryContent,
        List<DatabaseEnvironment> environments,
        string queryFile,
        Func<QueryResult, Task> onResultCompleted) {

        foreach (var environment in environments) {
            var queryResult = await ExecuteQueryOnEnvironmentAsync(queryContent, environment);
            await onResultCompleted(queryResult);
        }
    }

    /// <summary>
    /// Executes a query against a single database environment.
    /// </summary>
    /// <param name="queryContent">The SQL query to execute.</param>
    /// <param name="environment">Database environment to query.</param>
    /// <returns>Query execution result.</returns>
    private async Task<QueryResult> ExecuteQueryOnEnvironmentAsync(string queryContent, DatabaseEnvironment environment) {
        var result = new QueryResult {
            ClientId = environment.ClientId
        };

        var startTime = DateTime.UtcNow;

        try {
            using var connection = _connectionManager.CreateConnection(environment);
            await connection.OpenAsync();

            // Start read-only transaction for security
            using var transaction = await connection.BeginTransactionAsync();
            using var setReadOnlyCommand = new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction);
            await setReadOnlyCommand.ExecuteNonQueryAsync();

            using var command = new NpgsqlCommand(queryContent, connection, transaction);
            using var reader = await command.ExecuteReaderAsync();

            // Get column names
            for (int i = 0; i < reader.FieldCount; i++) {
                result.ColumnNames.Add(reader.GetName(i));
            }

            // Read all rows
            while (await reader.ReadAsync()) {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++) {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[columnName] = value;
                }
                result.Rows.Add(row);
            }

            result.Success = true;
        } catch (Exception ex) {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        } finally {
            result.ExecutionTime = DateTime.UtcNow - startTime;
        }

        return result;
    }
}