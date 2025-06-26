using System.Text.Json.Serialization;

namespace MultiQuery.Models;

/// <summary>
/// Root configuration object containing all database environments.
/// </summary>
public class EnvironmentConfig {
    /// <summary>
    /// List of database environment configurations.
    /// </summary>
    [JsonPropertyName("environments")]
    public List<DatabaseEnvironment> Environments { get; set; } = new();

    /// <summary>
    /// Returns the number of environments configured.
    /// </summary>
    public int Count => Environments.Count;

    /// <summary>
    /// Validates that all environments have required fields populated.
    /// </summary>
    /// <returns>List of validation errors, empty if valid.</returns>
    public List<string> Validate() {
        var errors = new List<string>();

        if (Environments.Count == 0) {
            errors.Add("No environments found in configuration");
            return errors;
        }

        for (int i = 0; i < Environments.Count; i++) {
            var env = Environments[i];
            var prefix = $"Environment {i + 1}";

            if (string.IsNullOrWhiteSpace(env.ClientId))
                errors.Add($"{prefix}: ClientId is required");

            if (string.IsNullOrWhiteSpace(env.Hostname))
                errors.Add($"{prefix}: Hostname is required");

            if (env.Port <= 0 || env.Port > 65535)
                errors.Add($"{prefix}: Port must be between 1 and 65535");

            if (string.IsNullOrWhiteSpace(env.Database))
                errors.Add($"{prefix}: Database is required");

            if (string.IsNullOrWhiteSpace(env.Username))
                errors.Add($"{prefix}: Username is required");

            if (string.IsNullOrWhiteSpace(env.Password))
                errors.Add($"{prefix}: Password is required");
        }

        return errors;
    }
}