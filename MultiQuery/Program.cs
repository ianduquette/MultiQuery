using System.CommandLine;
using MultiQuery.Models;
using MultiQuery.Services;

namespace MultiQuery;

class Program {
    static async Task<int> Main(string[] args) {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var rootCommand = CreateRootCommand();
        return await rootCommand.InvokeAsync(args);
    }

    private static RootCommand CreateRootCommand() {
        var queryFileArgument = new Argument<string>(
            name: "query",
            description: "Path to a .sql file, or an inline SQL query string");

        var environmentsFileOption = new Option<string>(
            aliases: ["--environments-file", "-e"],
            description: "Path to the JSON file containing database environment configurations",
            getDefaultValue: () => "environments.json");

        var csvOption = new Option<bool>(
            aliases: ["--csv", "-c"],
            description: "Output results in CSV format");

        var verboseOption = new Option<bool>(
            aliases: ["--verbose", "-v"],
            description: "Enable verbose output with additional diagnostic information");

        var rootCommand = new RootCommand("MultiQuery - Execute SQL queries against multiple PostgreSQL databases") {
            queryFileArgument,
            environmentsFileOption,
            csvOption,
            verboseOption
        };

        rootCommand.SetHandler(async (queryFile, environmentsFile, csvOutput, verbose) => {
            var options = new CommandLineOptions {
                QueryFile = queryFile,
                EnvironmentsFile = environmentsFile,
                CsvOutput = csvOutput,
                Verbose = verbose
            };

            await ExecuteMultiQuery(options);
        }, queryFileArgument, environmentsFileOption, csvOption, verboseOption);

        return rootCommand;
    }

    private static async Task ExecuteMultiQuery(CommandLineOptions options) {
        try {
            var pathResolver = new PathResolver();

            // Phase 1: Determine if the query arg is a file path or an inline SQL string.
            // Rule: if it resolves to an existing file, treat as file. If it ends in .sql but
            // doesn't exist, fail. Otherwise, treat the string itself as the query.
            string? resolvedQueryFile = null;
            try {
                var attempted = pathResolver.ResolvePath(options.QueryFile);
                if (File.Exists(attempted)) {
                    resolvedQueryFile = attempted;
                } else if (options.QueryFile.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)) {
                    Console.Error.WriteLine($"Error: Query file '{options.QueryFile}' not found.");
                    Console.Error.WriteLine($"Resolved path: {attempted}");
                    Environment.Exit(1);
                    return;
                }
            } catch (ArgumentException) {
                // Input contains characters that aren't valid in a path → treat as inline.
            }

            bool isInlineQuery = resolvedQueryFile == null;
            string queryDisplayName = isInlineQuery ? "inline query" : Path.GetFileName(resolvedQueryFile!);

            string resolvedEnvironmentsFile;
            try {
                resolvedEnvironmentsFile = pathResolver.ResolveEnvironmentsFile(options.EnvironmentsFile);
            } catch (FileNotFoundException ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
                return;
            }

            // Phase 1 Output: Display parsed arguments
            Console.WriteLine("=== MultiQuery - Parsed Arguments ===");
            Console.WriteLine($"Query: {(isInlineQuery ? "(inline SQL)" : options.QueryFile)}");
            Console.WriteLine($"Environments File: {options.EnvironmentsFile}");
            Console.WriteLine($"CSV Output: {options.CsvOutput}");
            Console.WriteLine($"Verbose Mode: {options.Verbose}");
            Console.WriteLine();

            if (!isInlineQuery) {
                pathResolver.DisplayPathResolution(options.QueryFile, resolvedQueryFile!, "Query File", options.Verbose);
            }
            pathResolver.DisplayPathResolution(options.EnvironmentsFile, resolvedEnvironmentsFile, "Environments File", options.Verbose, isEnvironmentsFile: true);

            Console.WriteLine("✓ Resolved");
            Console.WriteLine();

            options.EnvironmentsFile = resolvedEnvironmentsFile;

            // Phase 2: Load environments
            var environmentLoader = new EnvironmentLoader();
            var environmentConfig = await environmentLoader.LoadAsync(options.EnvironmentsFile);

            // Phase 2 Output: Display loaded environments
            environmentLoader.DisplayEnvironments(environmentConfig, options.Verbose);

            // Phase 3: Load and validate query (before connecting — fail fast on bad queries)
            string queryContent;
            if (isInlineQuery) {
                queryContent = options.QueryFile.Trim();
                if (string.IsNullOrWhiteSpace(queryContent)) {
                    Console.Error.WriteLine("Error: Query is empty.");
                    Environment.Exit(1);
                    return;
                }
            } else {
                var queryFileReader = new QueryFileReader();
                queryContent = await queryFileReader.ReadQueryFileAsync(resolvedQueryFile!);
                queryFileReader.DisplayQueryContent(queryContent, resolvedQueryFile!, options.Verbose);
            }

            var queryValidator = new QueryValidator();
            var validationResult = queryValidator.ValidateQuery(queryContent);

            // Phase 3 Output: Display validation results
            queryValidator.DisplayValidationResult(validationResult, options.Verbose);

            if (!validationResult.IsValid) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Query validation failed. Only SELECT statements are allowed.");
                Console.ResetColor();
                Environment.Exit(1);
            }

            // Phase 4: Test database connections
            var connectionManager = new DatabaseConnectionManager();
            var connectionResults = await connectionManager.TestAllConnectionsAsync(environmentConfig.Environments);

            // Phase 4 Output: Display connection test results
            connectionManager.DisplayConnectionResults(connectionResults, options.Verbose);

            // Check if any connections failed
            var failedConnections = connectionResults.Where(r => !r.Success).ToList();
            if (failedConnections.Count > 0) {
                Console.WriteLine($"Warning: {failedConnections.Count} database connection(s) failed.");
                if (!options.Verbose) {
                    Console.WriteLine("Use --verbose flag to see detailed error information.");
                }
                Console.WriteLine("Proceeding with available connections...");
                Console.WriteLine();
            }

            // Phase 5: Execute queries with streaming
            var queryExecutionService = new QueryExecutionService();
            var resultsFormatter = new ResultsFormatterService();
            var successfulEnvironments = connectionResults.Where(r => r.Success)
                .Select(r => environmentConfig.Environments.First(e => e.ClientId == r.ClientId))
                .ToList();
            var isFirstResult = true;

            // Display streaming header
            Console.WriteLine($"=== Query Results: {queryDisplayName} ===");
            Console.WriteLine($"Executing against {successfulEnvironments.Count} database(s)...");
            Console.WriteLine();

            await queryExecutionService.ExecuteQueryStreamingAsync(
                queryContent,
                successfulEnvironments,
                queryDisplayName,
                async result => {
                    resultsFormatter.DisplaySingleResult(result, options.CsvOutput, isFirstResult);
                    isFirstResult = false;
                });

            Console.WriteLine("Query execution complete!");
        } catch (Exception ex) {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (options.Verbose) {
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            Environment.Exit(1);
        }
    }
}