namespace ManagePath.Services;

/// <summary>
/// Service for reading and parsing PATH environment variable.
/// </summary>
public class PathService
{
    /// <summary>
    /// Retrieves directories from the PATH environment variable.
    /// </summary>
    /// <param name="target">The environment variable target (Process, User, Machine), or null for effective PATH</param>
    /// <returns>Array of directory paths from PATH</returns>
    public string[] GetDirectories(EnvironmentVariableTarget? target = null)
    {
        string? pathVariable = target.HasValue
            ? Environment.GetEnvironmentVariable("PATH", target.Value)
            : Environment.GetEnvironmentVariable("PATH");

        if (pathVariable is null)
        {
            return Array.Empty<string>();
        }

        return pathVariable
            .Split(Path.PathSeparator)
            .Where(dir => !string.IsNullOrWhiteSpace(dir))
            .ToArray();
    }

    /// <summary>
    /// Gets the formatted name of the PATH source for display purposes.
    /// </summary>
    /// <param name="target">The environment variable target, or null for effective PATH</param>
    /// <returns>Display name for the PATH source</returns>
    public static string GetTargetDisplayName(EnvironmentVariableTarget? target)
    {
        return target switch
        {
            EnvironmentVariableTarget.Machine => "Machine",
            EnvironmentVariableTarget.User => "User",
            EnvironmentVariableTarget.Process => "Process",
            null => "Effective",
            _ => "Unknown"
        };
    }
}
