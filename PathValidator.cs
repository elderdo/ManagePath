namespace ManagePath;

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
    /// <param name="directories">Collection of directory paths to validate</param>
    /// <returns>Collection of PathEntry objects with validation results</returns>
    public IEnumerable<PathEntry> ValidateMany(IEnumerable<string> directories)
    {
        return directories.Select(Validate);
    }

    private bool HasExecutableFiles(string dir)
    {
        if (OperatingSystem.IsWindows())
        {
            return _executableExtensions.Any(ext =>
                Directory.GetFiles(dir, $"*{ext}").Length > 0);
        }
        else // Linux/macOS
        {
            try
            {
                return Directory.GetFiles(dir)
                    .Any(file => (File.GetUnixFileMode(file) & UnixFileMode.UserExecute) != 0);
            }
            catch
            {
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
