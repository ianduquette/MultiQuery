using System.CommandLine;
using MultiQuery.Models;
using MultiQuery.Services;

namespace MultiQuery;

class Program {
    static async Task<int> Main(string[] args) {
        var rootCommand = CreateRootCommand();
        return await rootCommand.InvokeAsync(args);
    }

    private static RootCommand CreateRootCommand() {
        var queryFileArgument = new Argument<string>(
            name: "queryFile",
            description: "Path to the SQL query file to execute");

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
            // Phase 1 Output: Display parsed arguments
            Console.WriteLine("=== MultiQuery - Parsed Arguments ===");
            Console.WriteLine($"Query File: {options.QueryFile}");
            Console.WriteLine($"Environments File: {options.EnvironmentsFile}");
            Console.WriteLine($"CSV Output: {options.CsvOutput}");
            Console.WriteLine($"Verbose Mode: {options.Verbose}");
            Console.WriteLine();

            // Validate file existence
            if (!File.Exists(options.QueryFile)) {
                Console.Error.WriteLine($"Error: Query file '{options.QueryFile}' not found.");
                Environment.Exit(1);
            }

            if (!File.Exists(options.EnvironmentsFile)) {
                Console.Error.WriteLine($"Error: Environments file '{options.EnvironmentsFile}' not found.");
                Environment.Exit(1);
            }

            Console.WriteLine("✓ All required files exist");
            Console.WriteLine();

            // Phase 2: Load environments
            var environmentLoader = new EnvironmentLoader();
            var environmentConfig = await environmentLoader.LoadAsync(options.EnvironmentsFile);

            // Phase 2 Output: Display loaded environments
            environmentLoader.DisplayEnvironments(environmentConfig, options.Verbose);

            // Phase 3: Test database connections
            var connectionManager = new DatabaseConnectionManager();
            var connectionResults = await connectionManager.TestAllConnectionsAsync(environmentConfig.Environments);

            // Phase 3 Output: Display connection test results
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

            // Phase 4: Read and validate query file
            var queryFileReader = new QueryFileReader();
            var queryContent = await queryFileReader.ReadQueryFileAsync(options.QueryFile);

            // Phase 4 Output: Display query content
            queryFileReader.DisplayQueryContent(queryContent, options.QueryFile, options.Verbose);

            var queryValidator = new QueryValidator();
            var validationResult = queryValidator.ValidateQuery(queryContent);

            // Phase 4 Output: Display validation results
            queryValidator.DisplayValidationResult(validationResult, options.Verbose);

            if (!validationResult.IsValid) {
                Console.Error.WriteLine("Query validation failed. Only SELECT statements are allowed.");
                Environment.Exit(1);
            }

            // Phase 5: Execute queries
            var queryExecutionService = new QueryExecutionService();
            var queryResults = await queryExecutionService.ExecuteQueryAsync(
                queryContent,
                connectionResults.Where(r => r.Success).Select(r => environmentConfig.Environments.First(e => e.ClientId == r.ClientId)).ToList(),
                options.QueryFile);

            // Phase 6: Format and display results
            var resultsFormatter = new ResultsFormatterService();
            resultsFormatter.DisplayResults(queryResults, options.CsvOutput, options.Verbose);

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