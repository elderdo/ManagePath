namespace ManagePath.Models;

// This record represents one folder from the PATH variable.
//
// It stores:
// - the folder path
// - whether the folder exists
// - whether it contains executable files
//
// The app uses this type when it wants to keep both the folder name
// and the validation results together.


/// <summary>
/// Represents one PATH folder and the results of checking it.
/// </summary>
/// <param name="Directory">The folder path.</param>
/// <param name="Exists">True if the folder exists.</param>
/// <param name="HasExecutables">True if the folder contains executable files.</param>
public record PathEntry(string Directory, bool Exists, bool HasExecutables)
{
    /// <summary>
    /// Gets whether this PATH entry is valid.
    /// A valid entry exists and has executable files.
    /// </summary>
    public bool IsValid => Exists && HasExecutables;
}
