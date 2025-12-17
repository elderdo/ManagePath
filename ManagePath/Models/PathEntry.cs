namespace ManagePath.Models;

/// <summary>
/// Represents a single directory entry from the PATH environment variable
/// with its validation state.
/// </summary>
/// <remarks>
/// This type is implemented as a record rather than a class for the following reasons:
/// 
/// Pros of using a record:
/// - Value-based equality: Two PathEntry instances with the same values are considered equal,
///   which is semantically correct for immutable data transfer objects (DTOs).
/// - Immutability by default: Records use init-only properties, preventing accidental modification
///   after creation, which is appropriate for snapshot data like validation results.
/// - Concise syntax: Primary constructor parameters automatically become properties, reducing
///   boilerplate code while maintaining readability.
/// - Built-in deconstruction: Records support pattern matching and deconstruction, making them
///   convenient for functional-style programming patterns.
/// - ToString() override: Automatic implementation provides meaningful string representation
///   for debugging and logging.
/// 
/// Cons of using a class instead:
/// - Reference equality: Classes use reference equality by default, requiring manual Equals()
///   and GetHashCode() overrides to achieve value-based comparison, adding complexity.
/// - Mutability concerns: Classes allow mutable properties by default, increasing the risk of
///   unintended side effects when sharing instances across methods.
/// - More verbose: Classes require explicit property declarations, constructors, and potentially
///   equality members, resulting in more code for the same functionality.
/// - Less expressive: Classes don't convey the intent of being immutable value objects as clearly
///   as records do, making the code less self-documenting.
/// </remarks>
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
