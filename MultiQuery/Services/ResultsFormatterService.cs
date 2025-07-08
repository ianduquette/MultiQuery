using System.Text;
using MultiQuery.Models;

namespace MultiQuery.Services;

/// <summary>
/// Service for formatting and displaying query results.
/// </summary>
public class ResultsFormatterService {
    private bool csvHeadersWritten = false;

    /// <summary>
    /// Displays query results to console.
    /// </summary>
    /// <param name="results">The query results to display.</param>
    /// <param name="csvOutput">Whether to output in CSV format.</param>
    /// <param name="verbose">Whether to show verbose output.</param>
    public void DisplayResults(MultiQueryResult results, bool csvOutput = false, bool verbose = false) {
        if (csvOutput) {
            DisplayCsvFormat(results);
        } else {
            DisplayTableFormat(results, verbose);
        }
    }

    /// <summary>
    /// Displays a single query result immediately (for streaming).
    /// </summary>
    /// <param name="result">The query result to display.</param>
    /// <param name="csvOutput">Whether to output in CSV format.</param>
    /// <param name="isFirstResult">Whether this is the first result (for CSV headers).</param>
    public void DisplaySingleResult(QueryResult result, bool csvOutput, bool isFirstResult) {
        if (csvOutput) {
            DisplaySingleResultCsv(result, isFirstResult);
        } else {
            DisplaySingleResultTable(result);
        }
    }

    /// <summary>
    /// Displays a single result in table format.
    /// </summary>
    private void DisplaySingleResultTable(QueryResult result) {
        var sb = new StringBuilder();

        if (result.Success) {
            var rowText = result.Rows.Count == 1 ? "row" : "rows";
            sb.AppendLine($"[{result.ClientId}] ✓ {result.Rows.Count} {rowText} ({result.ExecutionTime.TotalMilliseconds:F0}ms)");

            if (result.Rows.Count > 0) {
                DisplayTable(sb, result.ColumnNames, result.Rows);
            }
        } else {
            sb.AppendLine($"[{result.ClientId}] ✗ {result.ErrorMessage}");
        }

        sb.AppendLine();
        Console.Write(sb.ToString());
    }

    /// <summary>
    /// Displays a single result in CSV format.
    /// </summary>
    private void DisplaySingleResultCsv(QueryResult result, bool isFirstResult) {
        var sb = new StringBuilder();

        // Write headers only once for first successful result
        if (!csvHeadersWritten && result.Success && result.Rows.Count > 0) {
            sb.AppendLine("client_id," + string.Join(",", result.ColumnNames));
            csvHeadersWritten = true;
        }

        // Write data rows
        if (result.Success) {
            foreach (var row in result.Rows) {
                var values = new List<string> { result.ClientId };
                foreach (var column in result.ColumnNames) {
                    var value = row.ContainsKey(column) ? row[column] : null;
                    values.Add(FormatCsvValue(value));
                }
                sb.AppendLine(string.Join(",", values));
            }
        }

        Console.Write(sb.ToString());
    }

    /// <summary>
    /// Displays results in table format.
    /// </summary>
    private void DisplayTableFormat(MultiQueryResult results, bool verbose) {
        var sb = new StringBuilder();
        sb.AppendLine($"=== Query Results: {Path.GetFileName(results.QueryFile)} ===");
        sb.AppendLine();

        foreach (var result in results.Results) {
            if (result.Success) {
                var rowText = result.Rows.Count == 1 ? "row" : "rows";
                sb.AppendLine($"[{result.ClientId}] ✓ {result.Rows.Count} {rowText} ({result.ExecutionTime.TotalMilliseconds:F0}ms)");

                if (result.Rows.Count > 0) {
                    DisplayTable(sb, result.ColumnNames, result.Rows);
                }
            } else {
                sb.AppendLine($"[{result.ClientId}] ✗ {result.ErrorMessage}");
            }

            sb.AppendLine();
        }

        Console.Write(sb.ToString());
    }

    /// <summary>
    /// Displays results in CSV format.
    /// </summary>
    private void DisplayCsvFormat(MultiQueryResult results) {
        var sb = new StringBuilder();
        var allColumns = new HashSet<string>();
        var successfulResults = results.Results.Where(r => r.Success).ToList();

        // Collect all unique column names
        foreach (var result in successfulResults) {
            foreach (var column in result.ColumnNames) {
                allColumns.Add(column);
            }
        }

        var columnList = allColumns.ToList();

        // Add client_id as first column
        sb.AppendLine("client_id," + string.Join(",", columnList));

        // Output data rows
        foreach (var result in successfulResults) {
            foreach (var row in result.Rows) {
                var values = new List<string> { result.ClientId };

                foreach (var column in columnList) {
                    var value = row.ContainsKey(column) ? row[column] : null;
                    values.Add(FormatCsvValue(value));
                }

                sb.AppendLine(string.Join(",", values));
            }
        }

        Console.Write(sb.ToString());
    }

    /// <summary>
    /// Displays data in a simple table format using StringBuilder.
    /// </summary>
    private void DisplayTable(StringBuilder sb, List<string> columnNames, List<Dictionary<string, object?>> rows) {
        if (columnNames.Count == 0 || rows.Count == 0) return;

        // Calculate column widths
        var widths = new Dictionary<string, int>();
        foreach (var column in columnNames) {
            widths[column] = column.Length;
        }

        // Check data widths
        foreach (var row in rows) {
            foreach (var column in columnNames) {
                if (row.ContainsKey(column)) {
                    var valueStr = FormatValue(row[column]);
                    widths[column] = Math.Max(widths[column], valueStr.Length);
                }
            }
        }

        // Build header
        var headerParts = columnNames.Select(col => col.PadRight(widths[col])).ToArray();
        sb.AppendLine(string.Join(" | ", headerParts));

        // Build separator
        var separatorParts = columnNames.Select(col => new string('-', widths[col])).ToArray();
        sb.AppendLine(string.Join("-|-", separatorParts));

        // Build data rows
        foreach (var row in rows) {
            var rowParts = columnNames.Select(col => {
                var value = row.ContainsKey(col) ? row[col] : null;
                return FormatValue(value).PadRight(widths[col]);
            }).ToArray();
            sb.AppendLine(string.Join(" | ", rowParts));
        }
    }

    /// <summary>
    /// Formats a value for display.
    /// </summary>
    private string FormatValue(object? value) {
        return value switch {
            null => "NULL",
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            bool b => b.ToString().ToLower(),
            _ => value.ToString() ?? "NULL"
        };
    }

    /// <summary>
    /// Formats a value for CSV output.
    /// </summary>
    private string FormatCsvValue(object? value) {
        var formatted = FormatValue(value);

        // Escape CSV values that contain commas, quotes, or newlines
        if (formatted.Contains(',') || formatted.Contains('"') || formatted.Contains('\n')) {
            formatted = "\"" + formatted.Replace("\"", "\"\"") + "\"";
        }

        return formatted;
    }
}