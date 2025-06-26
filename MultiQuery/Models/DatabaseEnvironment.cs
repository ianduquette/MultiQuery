using System.Text.Json.Serialization;

namespace MultiQuery.Models;

/// <summary>
/// Represents a database environment configuration.
/// </summary>
public class DatabaseEnvironment {
    /// <summary>
    /// Unique identifier for the client/environment.
    /// </summary>
    [JsonPropertyName("clientId")]
    public required string ClientId { get; set; }

    /// <summary>
    /// Database server hostname or IP address.
    /// </summary>
    [JsonPropertyName("hostname")]
    public required string Hostname { get; set; }

    /// <summary>
    /// Database server port number.
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; }

    /// <summary>
    /// Database name to connect to.
    /// </summary>
    [JsonPropertyName("database")]
    public required string Database { get; set; }

    /// <summary>
    /// Username for database authentication.
    /// </summary>
    [JsonPropertyName("username")]
    public required string Username { get; set; }

    /// <summary>
    /// Password for database authentication.
    /// </summary>
    [JsonPropertyName("password")]
    public required string Password { get; set; }

    /// <summary>
    /// Returns a string representation with masked password for logging.
    /// </summary>
    public override string ToString() {
        return $"ClientId: {ClientId}, Host: {Hostname}:{Port}, Database: {Database}, User: {Username}, Password: ***";
    }
}