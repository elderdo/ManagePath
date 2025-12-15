namespace ManagePath;

/// <summary>
/// Represents a single directory entry from the PATH environment variable
/// with its validation state.
/// </summary>
/// <param name="Directory">The directory path</param>
/// <param name="Exists">Whether the directory exists on the file system</param>
/// <param name="HasExecutables">Whether the directory contains executable files</param>
public record PathEntry(string Directory, bool Exists, bool HasExecutables)
{
    /// <summary>
    /// Gets whether this PATH entry is valid (exists and contains executables).
    /// </summary>
    public bool IsValid => Exists && HasExecutables;
}
