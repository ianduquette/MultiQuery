using System.Text.Json;
using MultiQuery.Models;

namespace MultiQuery.Services;

/// <summary>
/// Service for loading and validating database environment configurations from JSON files.
/// Uses source generators for AOT-compatible, trimming-safe JSON serialization.
/// </summary>
public class EnvironmentLoader {

    /// <summary>
    /// Loads database environments from a JSON file.
    /// </summary>
    /// <param name="filePath">Path to the JSON configuration file.</param>
    /// <returns>Loaded and validated environment configuration.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is malformed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
    public async Task<EnvironmentConfig> LoadAsync(string filePath) {
        if (!File.Exists(filePath)) {
            throw new FileNotFoundException($"Environment file not found: {filePath}");
        }

        try {
            var jsonContent = await File.ReadAllTextAsync(filePath);

            if (string.IsNullOrWhiteSpace(jsonContent)) {
                throw new InvalidOperationException("Environment file is empty");
            }

            var config = JsonSerializer.Deserialize(jsonContent, MultiQueryJsonContext.Default.EnvironmentConfig);

            if (config == null) {
                throw new InvalidOperationException("Failed to deserialize environment configuration");
            }

            // Validate the configuration
            var validationErrors = config.Validate();
            if (validationErrors.Count > 0) {
                var errorMessage = "Environment configuration validation failed:\n" +
                                 string.Join("\n", validationErrors);
                throw new InvalidOperationException(errorMessage);
            }

            return config;
        } catch (JsonException ex) {
            throw new JsonException($"Invalid JSON in environment file '{filePath}': {ex.Message}", ex);
        } catch (Exception ex) when (ex is not FileNotFoundException && ex is not JsonException && ex is not InvalidOperationException) {
            throw new InvalidOperationException($"Error loading environment file '{filePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Displays loaded environments to console with masked passwords.
    /// </summary>
    /// <param name="config">Environment configuration to display.</param>
    /// <param name="verbose">Whether to show detailed information.</param>
    public void DisplayEnvironments(EnvironmentConfig config, bool verbose = false) {
        Console.WriteLine($"=== Loaded {config.Count} Database Environment(s) ===");

        for (int i = 0; i < config.Environments.Count; i++) {
            var env = config.Environments[i];
            Console.WriteLine($"{i + 1:D2}. {env}");

            if (verbose) {
                Console.WriteLine($"    Connection: postgresql://{env.Username}:***@{env.Hostname}:{env.Port}/{env.Database}");
            }
        }

        Console.WriteLine();
    }
}