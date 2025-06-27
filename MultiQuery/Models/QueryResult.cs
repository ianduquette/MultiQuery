namespace MultiQuery.Models;

/// <summary>
/// Result of running a query against one database.
/// </summary>
public class QueryResult {
    public required string ClientId { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    // The data (only populated if successful)
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public List<string> ColumnNames { get; set; } = new();

    public TimeSpan ExecutionTime { get; set; }
}

/// <summary>
/// Results from all databases.
/// </summary>
public class MultiQueryResult {
    public required string QueryFile { get; set; }
    public List<QueryResult> Results { get; set; } = new();
}