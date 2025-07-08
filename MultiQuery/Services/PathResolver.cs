using System.IO;
using System.Reflection;

namespace MultiQuery.Services;

/// <summary>
/// Service for resolving file paths relative to the current working directory with fallback support for environments files.
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
    /// Resolves an environments file path with fallback to the application install directory.
    /// Search order: 1) Current working directory, 2) Application install directory
    /// </summary>
    /// <param name="filePath">The environments file path to resolve.</param>
    /// <returns>The resolved absolute file path.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file is not found in either location.</exception>
    /// <exception cref="ArgumentException">Thrown when the file path is null or empty.</exception>
    public string ResolveEnvironmentsFile(string filePath) {
        if (string.IsNullOrWhiteSpace(filePath)) {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        // If the path is already absolute, use standard resolution
        if (Path.IsPathRooted(filePath)) {
            var absolutePath = Path.GetFullPath(filePath);
            if (!File.Exists(absolutePath)) {
                throw new FileNotFoundException(
                    $"The environments file '{filePath}' was not found.\n" +
                    $"Resolved path: {absolutePath}\n" +
                    $"Tip: Ensure the file exists at the specified absolute path.");
            }
            return absolutePath;
        }

        // For relative paths, try current directory first
        var currentDirectory = Directory.GetCurrentDirectory();
        var currentDirPath = Path.Combine(currentDirectory, filePath);

        if (File.Exists(currentDirPath)) {
            return Path.GetFullPath(currentDirPath);
        }

        // Fallback: try application install directory
        var installDirectory = GetApplicationInstallDirectory();
        var installDirPath = Path.Combine(installDirectory, filePath);

        if (File.Exists(installDirPath)) {
            return Path.GetFullPath(installDirPath);
        }

        // File not found in either location
        throw new FileNotFoundException(
            $"The environments file '{filePath}' was not found.\n" +
            $"Searched locations:\n" +
            $"  1. Current working directory: {currentDirPath}\n" +
            $"  2. Application install directory: {installDirPath}\n" +
            $"Tip: Place the file in your current directory for project-specific configs, " +
            $"or in the application install directory for global defaults.");
    }

    /// <summary>
    /// Gets the current working directory.
    /// </summary>
    /// <returns>The current working directory path.</returns>
    public string GetCurrentDirectory() {
        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Gets the application install directory (where the executable is located).
    /// </summary>
    /// <returns>The application install directory path.</returns>
    public string GetApplicationInstallDirectory() {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyLocation = assembly.Location;

        // Handle single-file deployment where Location might be empty
        if (string.IsNullOrEmpty(assemblyLocation)) {
            assemblyLocation = Environment.ProcessPath ?? AppContext.BaseDirectory;
        }

        return Path.GetDirectoryName(assemblyLocation) ?? AppContext.BaseDirectory;
    }

    /// <summary>
    /// Displays path resolution information for debugging purposes.
    /// </summary>
    /// <param name="originalPath">The original path provided by the user.</param>
    /// <param name="resolvedPath">The resolved absolute path.</param>
    /// <param name="fileDescription">Description of the file type.</param>
    /// <param name="verbose">Whether to show detailed information.</param>
    /// <param name="isEnvironmentsFile">Whether this is an environments file (shows fallback info).</param>
    public void DisplayPathResolution(string originalPath, string resolvedPath, string fileDescription, bool verbose = false, bool isEnvironmentsFile = false) {
        if (verbose) {
            var currentDirectory = Directory.GetCurrentDirectory();
            var isRelative = !Path.IsPathRooted(originalPath);

            Console.WriteLine($"=== Path Resolution: {fileDescription} ===");
            Console.WriteLine($"Original path: {originalPath}");
            Console.WriteLine($"Path type: {(isRelative ? "Relative" : "Absolute")}");
            Console.WriteLine($"Current working directory: {currentDirectory}");

            if (isEnvironmentsFile && isRelative) {
                var installDirectory = GetApplicationInstallDirectory();
                var currentDirPath = Path.Combine(currentDirectory, originalPath);
                var installDirPath = Path.Combine(installDirectory, originalPath);

                Console.WriteLine($"Search locations:");
                Console.WriteLine($"  1. Current directory: {currentDirPath} {(File.Exists(currentDirPath) ? "✓" : "✗")}");
                Console.WriteLine($"  2. Install directory: {installDirPath} {(File.Exists(installDirPath) ? "✓" : "✗")}");
                Console.WriteLine($"Application install directory: {installDirectory}");
            }

            Console.WriteLine($"Resolved path: {resolvedPath}");
            Console.WriteLine($"File exists: {File.Exists(resolvedPath)}");
            Console.WriteLine();
        }
    }
}