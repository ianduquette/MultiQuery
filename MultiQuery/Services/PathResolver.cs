using System.IO;

namespace MultiQuery.Services;

/// <summary>
/// Service for resolving file paths relative to the current working directory.
/// </summary>
public class PathResolver {
    /// <summary>
    /// Resolves a file path, converting relative paths to absolute paths based on the current working directory.
    /// </summary>
    /// <param name="filePath">The file path to resolve (can be absolute or relative).</param>
    /// <returns>The resolved absolute file path.</returns>
    /// <exception cref="ArgumentException">Thrown when the file path is null or empty.</exception>
    public string ResolvePath(string filePath) {
        if (string.IsNullOrWhiteSpace(filePath)) {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        // If the path is already absolute, return it as-is
        if (Path.IsPathRooted(filePath)) {
            return Path.GetFullPath(filePath);
        }

        // For relative paths, resolve them relative to the current working directory
        var currentDirectory = Directory.GetCurrentDirectory();
        var resolvedPath = Path.Combine(currentDirectory, filePath);

        return Path.GetFullPath(resolvedPath);
    }

    /// <summary>
    /// Resolves a file path and validates that the file exists.
    /// </summary>
    /// <param name="filePath">The file path to resolve and validate.</param>
    /// <param name="fileDescription">Description of the file type for error messages (e.g., "query file", "environments file").</param>
    /// <returns>The resolved absolute file path.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the resolved file does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when the file path is null or empty.</exception>
    public string ResolveAndValidatePath(string filePath, string fileDescription = "file") {
        var resolvedPath = ResolvePath(filePath);

        if (!File.Exists(resolvedPath)) {
            var currentDirectory = Directory.GetCurrentDirectory();
            throw new FileNotFoundException(
                $"The {fileDescription} '{filePath}' was not found.\n" +
                $"Resolved path: {resolvedPath}\n" +
                $"Current working directory: {currentDirectory}\n" +
                $"Tip: Ensure the file exists in the current directory or provide an absolute path.");
        }

        return resolvedPath;
    }

    /// <summary>
    /// Gets the current working directory.
    /// </summary>
    /// <returns>The current working directory path.</returns>
    public string GetCurrentDirectory() {
        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Displays path resolution information for debugging purposes.
    /// </summary>
    /// <param name="originalPath">The original path provided by the user.</param>
    /// <param name="resolvedPath">The resolved absolute path.</param>
    /// <param name="fileDescription">Description of the file type.</param>
    /// <param name="verbose">Whether to show detailed information.</param>
    public void DisplayPathResolution(string originalPath, string resolvedPath, string fileDescription, bool verbose = false) {
        if (verbose) {
            var currentDirectory = Directory.GetCurrentDirectory();
            var isRelative = !Path.IsPathRooted(originalPath);

            Console.WriteLine($"=== Path Resolution: {fileDescription} ===");
            Console.WriteLine($"Original path: {originalPath}");
            Console.WriteLine($"Path type: {(isRelative ? "Relative" : "Absolute")}");
            Console.WriteLine($"Current working directory: {currentDirectory}");
            Console.WriteLine($"Resolved path: {resolvedPath}");
            Console.WriteLine($"File exists: {File.Exists(resolvedPath)}");
            Console.WriteLine();
        }
    }
}