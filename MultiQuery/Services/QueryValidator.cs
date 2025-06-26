using System.Text.RegularExpressions;

namespace MultiQuery.Services;

/// <summary>
/// Service for validating SQL queries to ensure only SELECT statements are allowed.
/// </summary>
public class QueryValidator {
    // Regex patterns for detecting different SQL statement types
    private static readonly Regex SelectPattern = new(@"^\s*SELECT\s+", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private static readonly Regex DmlPattern = new(@"^\s*(INSERT|UPDATE|DELETE|MERGE)\s+", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private static readonly Regex DdlPattern = new(@"^\s*(CREATE|ALTER|DROP|TRUNCATE|RENAME)\s+", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private static readonly Regex TransactionPattern = new(@"^\s*(BEGIN|COMMIT|ROLLBACK|START\s+TRANSACTION)\s*", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private static readonly Regex ProcedurePattern = new(@"^\s*(CALL|EXEC|EXECUTE)\s+", RegexOptions.IgnoreCase | RegexOptions.Multiline);

    // Pattern to identify SQL comments and remove them for analysis
    private static readonly Regex CommentPattern = new(@"(--.*?(?=\r?\n|$)|/\*.*?\*/)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    /// <summary>
    /// Validates that a SQL query contains only SELECT statements.
    /// </summary>
    /// <param name="queryContent">The SQL query content to validate.</param>
    /// <returns>Validation result with details about any issues found.</returns>
    public QueryValidationResult ValidateQuery(string queryContent) {
        var result = new QueryValidationResult {
            IsValid = true,
            OriginalQuery = queryContent
        };

        try {
            // Remove comments for analysis
            var cleanedQuery = RemoveComments(queryContent);
            result.CleanedQuery = cleanedQuery;

            if (string.IsNullOrWhiteSpace(cleanedQuery)) {
                result.IsValid = false;
                result.ErrorMessage = "Query contains only comments or whitespace";
                return result;
            }

            // Split into individual statements (separated by semicolons)
            var statements = SplitStatements(cleanedQuery);
            result.StatementCount = statements.Count;

            // Validate each statement
            for (int i = 0; i < statements.Count; i++) {
                var statement = statements[i].Trim();
                if (string.IsNullOrWhiteSpace(statement))
                    continue;

                var statementResult = ValidateStatement(statement, i + 1);
                result.StatementResults.Add(statementResult);

                if (!statementResult.IsValid) {
                    result.IsValid = false;
                    if (string.IsNullOrEmpty(result.ErrorMessage)) {
                        result.ErrorMessage = statementResult.ErrorMessage;
                    }
                }
            }

            // Ensure we have at least one valid SELECT statement
            if (result.IsValid && !result.StatementResults.Any(s => s.IsValid && s.StatementType == SqlStatementType.Select)) {
                result.IsValid = false;
                result.ErrorMessage = "No valid SELECT statements found in query";
            }
        } catch (Exception ex) {
            result.IsValid = false;
            result.ErrorMessage = $"Error validating query: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Validates a single SQL statement.
    /// </summary>
    /// <param name="statement">The SQL statement to validate.</param>
    /// <param name="statementNumber">The statement number for error reporting.</param>
    /// <returns>Validation result for the statement.</returns>
    private StatementValidationResult ValidateStatement(string statement, int statementNumber) {
        var result = new StatementValidationResult {
            StatementNumber = statementNumber,
            Statement = statement
        };

        // Check for SELECT statements (allowed)
        if (SelectPattern.IsMatch(statement)) {
            result.IsValid = true;
            result.StatementType = SqlStatementType.Select;
            return result;
        }

        // Check for DML statements (not allowed)
        if (DmlPattern.IsMatch(statement)) {
            result.IsValid = false;
            result.StatementType = SqlStatementType.Dml;
            result.ErrorMessage = $"Statement {statementNumber}: DML operations (INSERT, UPDATE, DELETE, MERGE) are not allowed";
            return result;
        }

        // Check for DDL statements (not allowed)
        if (DdlPattern.IsMatch(statement)) {
            result.IsValid = false;
            result.StatementType = SqlStatementType.Ddl;
            result.ErrorMessage = $"Statement {statementNumber}: DDL operations (CREATE, ALTER, DROP, TRUNCATE, RENAME) are not allowed";
            return result;
        }

        // Check for transaction control statements (not allowed)
        if (TransactionPattern.IsMatch(statement)) {
            result.IsValid = false;
            result.StatementType = SqlStatementType.Transaction;
            result.ErrorMessage = $"Statement {statementNumber}: Transaction control statements are not allowed";
            return result;
        }

        // Check for procedure calls (not allowed)
        if (ProcedurePattern.IsMatch(statement)) {
            result.IsValid = false;
            result.StatementType = SqlStatementType.Procedure;
            result.ErrorMessage = $"Statement {statementNumber}: Procedure calls are not allowed";
            return result;
        }

        // Unknown statement type
        result.IsValid = false;
        result.StatementType = SqlStatementType.Unknown;
        result.ErrorMessage = $"Statement {statementNumber}: Unknown or unsupported SQL statement type";
        return result;
    }

    /// <summary>
    /// Removes SQL comments from the query content.
    /// </summary>
    /// <param name="queryContent">The SQL query content.</param>
    /// <returns>Query content with comments removed.</returns>
    private static string RemoveComments(string queryContent) {
        return CommentPattern.Replace(queryContent, " ");
    }

    /// <summary>
    /// Splits SQL content into individual statements.
    /// </summary>
    /// <param name="queryContent">The SQL query content.</param>
    /// <returns>List of individual SQL statements.</returns>
    private static List<string> SplitStatements(string queryContent) {
        // Simple split by semicolon - could be enhanced for more complex cases
        return queryContent.Split(';', StringSplitOptions.RemoveEmptyEntries)
                          .Select(s => s.Trim())
                          .Where(s => !string.IsNullOrWhiteSpace(s))
                          .ToList();
    }

    /// <summary>
    /// Displays query validation results to console.
    /// </summary>
    /// <param name="result">The validation result to display.</param>
    /// <param name="verbose">Whether to show detailed information.</param>
    public void DisplayValidationResult(QueryValidationResult result, bool verbose = false) {
        Console.WriteLine("=== Query Validation Results ===");
        Console.WriteLine($"Valid: {(result.IsValid ? "✓ Yes" : "✗ No")}");
        Console.WriteLine($"Statements Found: {result.StatementCount}");

        if (!result.IsValid) {
            Console.WriteLine($"Error: {result.ErrorMessage}");
        }

        if (verbose && result.StatementResults.Count > 0) {
            Console.WriteLine("\nStatement Details:");
            foreach (var stmt in result.StatementResults) {
                var status = stmt.IsValid ? "✓" : "✗";
                Console.WriteLine($"  {status} Statement {stmt.StatementNumber}: {stmt.StatementType}");
                if (!stmt.IsValid) {
                    Console.WriteLine($"    Error: {stmt.ErrorMessage}");
                }
                if (verbose) {
                    var preview = stmt.Statement.Length > 50
                        ? stmt.Statement.Substring(0, 50) + "..."
                        : stmt.Statement;
                    Console.WriteLine($"    Preview: {preview.Replace('\n', ' ').Replace('\r', ' ')}");
                }
            }
        }

        Console.WriteLine();
    }
}

/// <summary>
/// Represents the result of query validation.
/// </summary>
public class QueryValidationResult {
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string OriginalQuery { get; set; } = string.Empty;
    public string CleanedQuery { get; set; } = string.Empty;
    public int StatementCount { get; set; }
    public List<StatementValidationResult> StatementResults { get; set; } = new();
}

/// <summary>
/// Represents the result of validating a single SQL statement.
/// </summary>
public class StatementValidationResult {
    public int StatementNumber { get; set; }
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string Statement { get; set; } = string.Empty;
    public SqlStatementType StatementType { get; set; }
}

/// <summary>
/// Enumeration of SQL statement types.
/// </summary>
public enum SqlStatementType {
    Unknown,
    Select,
    Dml,        // INSERT, UPDATE, DELETE, MERGE
    Ddl,        // CREATE, ALTER, DROP, TRUNCATE, RENAME
    Transaction, // BEGIN, COMMIT, ROLLBACK
    Procedure   // CALL, EXEC, EXECUTE
}