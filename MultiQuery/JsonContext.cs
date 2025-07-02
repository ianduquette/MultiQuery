using System.Text.Json;
using System.Text.Json.Serialization;
using MultiQuery.Models;

namespace MultiQuery;

/// <summary>
/// JSON serialization context for MultiQuery models using source generators.
/// This provides AOT-compatible, trimming-safe JSON serialization.
/// </summary>
[JsonSerializable(typeof(EnvironmentConfig))]
[JsonSerializable(typeof(DatabaseEnvironment))]
[JsonSerializable(typeof(List<DatabaseEnvironment>))]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    AllowTrailingCommas = true,
    WriteIndented = false
)]
public partial class MultiQueryJsonContext : JsonSerializerContext {
}