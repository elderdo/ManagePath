namespace ManagePath.Services;

// This service checks whether PATH folders are useful.
//
// For each folder, it answers two simple questions:
// - Does the folder exist?
//
// - Does it contain executable files?
//
// Windows and Unix-like systems detect executables differently,
// so this class uses different logic depending on the platform.

/// <summary>
/// Validates PATH folders.
/// </summary>
public class PathValidator
{
    private readonly string[] _executableExtensions;

    /// <summary>
    /// Creates a validator.
    /// </summary>
    /// <param name="customExtensions">
    /// Optional file extensions to treat as executable on Windows.
    /// </param>
    public PathValidator(string[]? customExtensions = null)
    {
        _executableExtensions = customExtensions ?? GetDefaultExecutableExtensions();
    }

    /// <summary>
    /// Checks one directory and returns the results.
    /// </summary>
    /// <param name="directory">The directory to check.</param>
    /// <returns>A <see cref="PathEntry"/> containing the results.</returns>
    public PathEntry Validate(string directory)
    {
        bool exists = Directory.Exists(directory);
        bool hasExecutables = exists && HasExecutableFiles(directory);

        return new PathEntry(directory, exists, hasExecutables);
    }

    /// <summary>
    /// Checks many directories.
    /// </summary>
    /// <param name="directories">The directories to check.</param>
    /// <returns>A sequence of validation results.</returns>
    public IEnumerable<PathEntry> ValidateMany(IEnumerable<string> directories)
    {
        return directories.Select(Validate);
    }

    /// <summary>
    /// Checks whether a directory contains at least one executable file.
    /// </summary>
    private bool HasExecutableFiles(string dir)
    {
        if (OperatingSystem.IsWindows())
        {
            return _executableExtensions.Any(ext =>
                Directory.GetFiles(dir, $"*{ext}").Length > 0);
        }

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

    /// <summary>
    /// Gets the default executable extensions for the current platform.
    /// </summary>
    private static string[] GetDefaultExecutableExtensions()
    {
        if (OperatingSystem.IsWindows())
        {
            string? pathExt = Environment.GetEnvironmentVariable("PATHEXT");
            if (pathExt != null)
            {
                var extensions = pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries);
                return extensions.Concat(new[] { ".pl", ".PL" }).Distinct().ToArray();
            }

            return new[] { ".exe", ".bat", ".cmd", ".com", ".ps1", ".pl" };
        }

        return Array.Empty<string>();
    }
}
