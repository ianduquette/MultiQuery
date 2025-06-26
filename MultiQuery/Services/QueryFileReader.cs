using System.Text;

namespace MultiQuery.Services;

/// <summary>
/// Service for reading SQL query files with encoding detection.
/// </summary>
public class QueryFileReader {
    /// <summary>
    /// Reads the contents of a SQL query file.
    /// </summary>
    /// <param name="filePath">Path to the SQL file.</param>
    /// <returns>The SQL query content.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the file cannot be read.</exception>
    public async Task<string> ReadQueryFileAsync(string filePath) {
        if (!File.Exists(filePath)) {
            throw new FileNotFoundException($"Query file not found: {filePath}");
        }

        try {
            // Try to detect encoding, default to UTF-8
            var encoding = DetectEncoding(filePath) ?? Encoding.UTF8;

            var content = await File.ReadAllTextAsync(filePath, encoding);

            if (string.IsNullOrWhiteSpace(content)) {
                throw new InvalidOperationException($"Query file '{filePath}' is empty or contains only whitespace");
            }

            return content.Trim();
        } catch (Exception ex) when (ex is not FileNotFoundException && ex is not InvalidOperationException) {
            throw new InvalidOperationException($"Error reading query file '{filePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Attempts to detect the encoding of a text file.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>Detected encoding or null if detection fails.</returns>
    private static Encoding? DetectEncoding(string filePath) {
        try {
            // Read the first few bytes to check for BOM
            var buffer = new byte[4];
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var bytesRead = fileStream.Read(buffer, 0, 4);

            if (bytesRead >= 3) {
                // UTF-8 BOM
                if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                    return Encoding.UTF8;

                // UTF-16 LE BOM
                if (buffer[0] == 0xFF && buffer[1] == 0xFE)
                    return Encoding.Unicode;

                // UTF-16 BE BOM
                if (buffer[0] == 0xFE && buffer[1] == 0xFF)
                    return Encoding.BigEndianUnicode;
            }

            if (bytesRead >= 4) {
                // UTF-32 LE BOM
                if (buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0x00 && buffer[3] == 0x00)
                    return Encoding.UTF32;

                // UTF-32 BE BOM
                if (buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xFE && buffer[3] == 0xFF)
                    return new UTF32Encoding(true, true);
            }

            // No BOM detected, assume UTF-8
            return null;
        } catch {
            // If encoding detection fails, return null to use default
            return null;
        }
    }

    /// <summary>
    /// Displays the loaded query content to console.
    /// </summary>
    /// <param name="queryContent">The SQL query content.</param>
    /// <param name="filePath">Path to the query file.</param>
    /// <param name="verbose">Whether to show the full query content.</param>
    public void DisplayQueryContent(string queryContent, string filePath, bool verbose = false) {
        var lines = queryContent.Split('\n', StringSplitOptions.None);
        var nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).Count();

        Console.WriteLine($"=== SQL Query File: {Path.GetFileName(filePath)} ===");
        Console.WriteLine($"File Size: {new FileInfo(filePath).Length} bytes");
        Console.WriteLine($"Total Lines: {lines.Length}, Non-empty Lines: {nonEmptyLines}");

        if (verbose) {
            Console.WriteLine("Query Content:");
            Console.WriteLine(new string('-', 50));

            // Display with line numbers for better readability
            for (int i = 0; i < lines.Length; i++) {
                Console.WriteLine($"{i + 1,3}: {lines[i]}");
            }

            Console.WriteLine(new string('-', 50));
        } else {
            // Show just the first few lines as a preview
            var previewLines = lines.Take(3).Where(line => !string.IsNullOrWhiteSpace(line));
            if (previewLines.Any()) {
                Console.WriteLine("Preview:");
                foreach (var line in previewLines) {
                    Console.WriteLine($"  {line.Trim()}");
                }
                if (lines.Length > 3) {
                    Console.WriteLine("  ... (use --verbose to see full content)");
                }
            }
        }

        Console.WriteLine();
    }
}