namespace ManagePath.Services;

/// <summary>
/// Validates PATH directories by checking existence and executable file presence.
/// </summary>
public class PathValidator
{
    private readonly string[] _executableExtensions;

    /// <summary>
    /// Creates a new PathValidator with optional custom executable extensions.
    /// </summary>
    /// <param name="customExtensions">Custom file extensions to treat as executable (e.g., ".pl", ".py")</param>
    public PathValidator(string[]? customExtensions = null)
    {
        _executableExtensions = customExtensions ?? GetDefaultExecutableExtensions();
    }

    /// <summary>
    /// Validates a directory path.
    /// </summary>
    /// <param name="directory">The directory path to validate</param>
    /// <returns>PathEntry with validation results</returns>
    public PathEntry Validate(string directory)
    {
        bool exists = Directory.Exists(directory);
        bool hasExecutables = exists && HasExecutableFiles(directory);

        return new PathEntry(directory, exists, hasExecutables);
    }

    /// <summary>
    /// Validates multiple directories.
    /// </summary>
    /// <remarks>
    /// This method uses LINQ's Select() projection to transform each directory path into a PathEntry.
    /// 
    /// The syntax "directories.Select(Validate)" is a method group conversion, which is shorthand for:
    /// directories.Select(directory => Validate(directory))
    /// 
    /// Key concepts:
    /// - Select() projects each element through a transform function (Validate in this case)
    /// - Method group syntax allows passing the method name directly without lambda syntax
    /// - Returns IEnumerable&lt;PathEntry&gt; with deferred execution (evaluated when enumerated)
    /// - Functionally equivalent to a foreach loop creating a new collection, but more concise
    /// 
    /// Deferred execution means the validation only occurs when the result is enumerated
    /// (e.g., via ToArray(), ToList(), foreach, or other enumeration operations).
    /// </remarks>
    /// <param name="directories">Collection of directory paths to validate</param>
    /// <returns>Collection of PathEntry objects with validation results</returns>
    public IEnumerable<PathEntry> ValidateMany(IEnumerable<string> directories)
    {
        // LINQ Select projection: transforms each string directory path into a PathEntry object
        // Method group syntax: "Validate" is shorthand for "directory => Validate(directory)"
        // This is a functional programming approach to mapping/transforming collections
        return directories.Select(Validate);
    }

    /// <summary>
    /// Checks if a directory contains executable files using platform-specific detection logic.
    /// </summary>
    /// <remarks>
    /// Windows: Checks for files matching known executable extensions (from PATHEXT).
    /// Unix/Linux/macOS: Checks for files with the execute permission bit set.
    /// 
    /// Uses LINQ's Any() method which returns true if at least one element satisfies the condition,
    /// short-circuiting as soon as a match is found for optimal performance.
    /// </remarks>
    /// <param name="dir">The directory path to check</param>
    /// <returns>True if the directory contains at least one executable file; otherwise, false</returns>
    private bool HasExecutableFiles(string dir)
    {
        if (OperatingSystem.IsWindows())
        {
            // LINQ Any() with lambda: Tests if ANY extension has matching files in the directory
            // Short-circuits on first match, avoiding unnecessary file system queries
            // Lambda parameter: ext = current extension being tested (e.g., ".exe", ".bat")
            // String interpolation: $"*{ext}" creates wildcard pattern like "*.exe"
            // Directory.GetFiles() returns array of matching files; Length > 0 means files found
            return _executableExtensions.Any(ext =>
                Directory.GetFiles(dir, $"*{ext}").Length > 0);
        }
        else // Linux/macOS
        {
            try
            {
                // Get all files in directory, then check execute permissions using LINQ
                // LINQ Any() with lambda: Tests if ANY file has execute permission
                // Lambda parameter: file = full path to current file being tested
                // File.GetUnixFileMode() returns UnixFileMode flags for file permissions
                // Bitwise AND (&) checks if UserExecute flag is set in the permission bits
                // UnixFileMode.UserExecute = 0x0100 (owner execute permission bit)
                // Result != 0 means the execute bit is set, making it an executable file
                return Directory.GetFiles(dir)
                    .Any(file => (File.GetUnixFileMode(file) & UnixFileMode.UserExecute) != 0);
            }
            catch
            {
                // Catch permission errors or other I/O exceptions
                // Returns false if we can't read directory contents or file permissions
                return false;
            }
        }
    }

    private static string[] GetDefaultExecutableExtensions()
    {
        if (OperatingSystem.IsWindows())
        {
            string? pathExt = Environment.GetEnvironmentVariable("PATHEXT");
            if (pathExt != null)
            {
                var extensions = pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries);
                // Add custom extensions not typically in PATHEXT
                return extensions.Concat(new[] { ".pl", ".PL" }).Distinct().ToArray();
            }

            // Fallback if PATHEXT is not set
            return new[] { ".exe", ".bat", ".cmd", ".com", ".ps1", ".pl" };
        }

        // Unix-like systems don't use extensions
        return Array.Empty<string>();
    }
}
