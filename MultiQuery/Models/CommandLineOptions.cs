namespace MultiQuery.Models;

/// <summary>
/// Represents the command-line options for the MultiQuery application.
/// </summary>
public class CommandLineOptions {
    /// <summary>
    /// Path to the SQL query file to execute.
    /// </summary>
    public required string QueryFile { get; set; }

    /// <summary>
    /// Path to the JSON file containing database environment configurations.
    /// </summary>
    public required string EnvironmentsFile { get; set; }

    /// <summary>
    /// Whether to output results in CSV format instead of formatted tables.
    /// </summary>
    public bool CsvOutput { get; set; } = false;

    /// <summary>
    /// Whether to enable verbose output with additional diagnostic information.
    /// </summary>
    public bool Verbose { get; set; } = false;
}