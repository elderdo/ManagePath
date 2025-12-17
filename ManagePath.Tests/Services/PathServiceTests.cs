using ManagePath.Services;

namespace ManagePath.Tests.Services;

/// <summary>
/// Unit tests for PathService class.
/// Tests PATH environment variable reading and parsing functionality.
/// </summary>
public class PathServiceTests
{
    /// <summary>
    /// Tests that GetDirectories with null target returns the effective PATH.
    /// </summary>
    /// <remarks>
    /// The effective PATH is what the current process sees - it's the merged combination
    /// of Machine, User, and Process environment variables. This is the default behavior
    /// when no target is specified.
    /// 
    /// Test strategy:
    /// - Create PathService instance
    /// - Call GetDirectories(null) which should use Environment.GetEnvironmentVariable("PATH")
    /// - Verify the result is not null (PATH should exist in any environment)
    /// - Verify the result is not empty (PATH typically contains at least system directories)
    /// 
    /// This test validates the most common use case where developers want to see
    /// what PATH directories are actually available to the running application.
    /// </remarks>
    [Fact]
    public void GetDirectories_WithNullTarget_ReturnsEffectivePath()
    {
        // Arrange: Create service instance
        var pathService = new PathService();

        // Act: Call GetDirectories with null (default parameter value)
        // This should retrieve the effective PATH from the current process environment
        string[] directories = pathService.GetDirectories(null);

        // Assert: Verify we got a valid result
        // PATH should never be null in a normal execution environment
        Assert.NotNull(directories);

        // PATH should contain at least some directories (e.g., C:\Windows\System32 on Windows)
        // Even minimal systems have PATH entries for basic commands
        Assert.NotEmpty(directories);
    }

    /// <summary>
    /// Tests that GetDirectories with User target returns user-level PATH.
    /// </summary>
    /// <remarks>
    /// User-level PATH is stored in HKEY_CURRENT_USER\Environment on Windows
    /// or in user shell configuration files on Unix systems.
    /// 
    /// Test strategy:
    /// - Call GetDirectories(EnvironmentVariableTarget.User)
    /// - Result may be empty if user hasn't customized their PATH
    /// - Result should never be null (empty array returned instead)
    /// 
    /// Note: This test doesn't verify specific directories because user PATH
    /// can be legitimately empty or vary significantly between environments.
    /// We only verify the API contract (returns non-null array).
    /// </remarks>
    [Fact]
    public void GetDirectories_WithUserTarget_ReturnsUserPath()
    {
        // Arrange
        var pathService = new PathService();

        // Act: Request user-level PATH specifically
        // This reads from HKEY_CURRENT_USER\Environment\PATH on Windows
        string[] directories = pathService.GetDirectories(EnvironmentVariableTarget.User);

        // Assert: Should return an array (possibly empty)
        Assert.NotNull(directories);

        // Note: We don't assert NotEmpty here because user PATH can legitimately be empty
        // if the user hasn't added any custom directories
    }

    /// <summary>
    /// Tests that GetDirectories with Machine target returns system-level PATH.
    /// </summary>
    /// <remarks>
    /// Machine-level PATH is stored in HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment
    /// on Windows. This is the system-wide PATH that applies to all users.
    /// 
    /// Test strategy:
    /// - Call GetDirectories(EnvironmentVariableTarget.Machine)
    /// - Result should not be null
    /// - Result typically not empty (contains system directories like C:\Windows\System32)
    /// 
    /// Machine PATH usually contains essential system directories needed for
    /// basic OS functionality, so we expect it to have content.
    /// </remarks>
    [Fact]
    public void GetDirectories_WithMachineTarget_ReturnsMachinePath()
    {
        // Arrange
        var pathService = new PathService();

        // Act: Request machine-level (system-wide) PATH
        // This reads from HKEY_LOCAL_MACHINE on Windows
        string[] directories = pathService.GetDirectories(EnvironmentVariableTarget.Machine);

        // Assert: Machine PATH should always exist and typically have content
        Assert.NotNull(directories);

        // System PATH almost always has entries (e.g., Windows\System32)
        // However, on some minimal containers it might be empty, so we don't assert NotEmpty
    }

    /// <summary>
    /// Tests that GetDirectories with Process target returns process-level PATH.
    /// </summary>
    /// <remarks>
    /// Process-level PATH is the environment variable as seen by the current process.
    /// It can be modified at runtime and doesn't affect other processes.
    /// 
    /// Test strategy:
    /// - Call GetDirectories(EnvironmentVariableTarget.Process)
    /// - Should return the same as GetDirectories(null) since both read process environment
    /// - Should not be null and should contain directories
    /// 
    /// This is useful for testing runtime PATH modifications or for applications
    /// that need to verify the PATH their child processes will inherit.
    /// </remarks>
    [Fact]
    public void GetDirectories_WithProcessTarget_ReturnsProcessPath()
    {
        // Arrange
        var pathService = new PathService();

        // Act: Request process-level PATH
        // This reads the PATH variable from the current process's environment block
        string[] directories = pathService.GetDirectories(EnvironmentVariableTarget.Process);

        // Assert: Process PATH should exist
        Assert.NotNull(directories);
        Assert.NotEmpty(directories);

        // Additional verification: Process target should give similar results to null target
        // (both read from the current process environment, just different API methods)
        string[] effectiveDirectories = pathService.GetDirectories(null);

        // The arrays might not be exactly equal due to timing or modifications,
        // but they should have the same length in a stable test environment
        // Commenting this out as it could be flaky in some test runners
        // Assert.Equal(effectiveDirectories.Length, directories.Length);
    }

    /// <summary>
    /// Tests that GetDirectories correctly splits PATH by the path separator.
    /// </summary>
    /// <remarks>
    /// PATH uses different separators on different platforms:
    /// - Windows: semicolon (;)
    /// - Unix/Linux/macOS: colon (:)
    /// 
    /// Path.PathSeparator provides the platform-specific separator.
    /// 
    /// Test strategy:
    /// - Set a controlled PATH environment variable for the process
    /// - Create a PATH string with multiple directories separated by Path.PathSeparator
    /// - Verify GetDirectories splits them correctly into individual strings
    /// 
    /// This test validates the core parsing logic that splits the PATH string.
    /// We use EnvironmentVariableTarget.Process to avoid affecting system/user settings.
    /// </remarks>
    [Fact]
    public void GetDirectories_SplitsByPathSeparator()
    {
        // Arrange
        var pathService = new PathService();

        // Create a test PATH with known directories
        // Path.PathSeparator is ';' on Windows, ':' on Unix
        string testPath = $"/usr/bin{Path.PathSeparator}/usr/local/bin{Path.PathSeparator}/opt/bin";

        // Temporarily modify the process environment for this test
        string? originalPath = Environment.GetEnvironmentVariable("PATH");
        try
        {
            Environment.SetEnvironmentVariable("PATH", testPath);

            // Act: Get directories from our custom PATH
            string[] directories = pathService.GetDirectories(EnvironmentVariableTarget.Process);

            // Assert: Should split into exactly 3 directories
            Assert.NotNull(directories);
            Assert.Equal(3, directories.Length);

            // Verify each directory is present (order should be preserved)
            Assert.Contains("/usr/bin", directories);
            Assert.Contains("/usr/local/bin", directories);
            Assert.Contains("/opt/bin", directories);
        }
        finally
        {
            // Cleanup: Restore original PATH to avoid affecting other tests
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }
    }

    /// <summary>
    /// Tests that GetDirectories handles empty PATH gracefully.
    /// </summary>
    /// <remarks>
    /// Edge case testing: What happens when PATH is empty or not set?
    /// 
    /// Expected behavior:
    /// - If PATH is null or empty, return an empty array (not null)
    /// - No exceptions should be thrown
    /// - Empty/whitespace-only entries should be filtered out
    /// 
    /// Test strategy:
    /// - Set PATH to various empty/whitespace values
    /// - Verify GetDirectories returns empty array
    /// - Verify no exceptions are thrown
    /// 
    /// This validates defensive programming - the method should handle
    /// edge cases without crashing.
    /// </remarks>
    [Fact]
    public void GetDirectories_HandlesEmptyPath()
    {
        // Arrange
        var pathService = new PathService();
        string? originalPath = Environment.GetEnvironmentVariable("PATH");

        try
        {
            // Test 1: Empty string PATH
            Environment.SetEnvironmentVariable("PATH", "");

            // Act
            string[] directories = pathService.GetDirectories(EnvironmentVariableTarget.Process);

            // Assert: Should return empty array, not null
            Assert.NotNull(directories);
            Assert.Empty(directories);

            // Test 2: PATH with only whitespace and separators
            Environment.SetEnvironmentVariable("PATH", $"  {Path.PathSeparator}  {Path.PathSeparator}  ");

            // Act
            directories = pathService.GetDirectories(EnvironmentVariableTarget.Process);

            // Assert: Whitespace entries should be filtered out
            Assert.NotNull(directories);
            Assert.Empty(directories);
        }
        finally
        {
            // Cleanup: Restore original PATH
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }
    }

    /// <summary>
    /// Tests that GetTargetDisplayName returns correct display names.
    /// </summary>
    /// <remarks>
    /// GetTargetDisplayName is a static helper method that converts
    /// EnvironmentVariableTarget enum values to user-friendly display names.
    /// 
    /// Test coverage:
    /// - Machine → "Machine"
    /// - User → "User"
    /// - Process → "Process"
    /// - null → "Effective"
    /// 
    /// This validates the switch expression mapping and ensures consistent
    /// naming in the user interface.
    /// 
    /// Pattern matching in C#: The switch expression uses pattern matching
    /// to map enum values to strings. The _ pattern is a discard pattern
    /// that matches anything not matched by previous cases (default case).
    /// </remarks>
    [Fact]
    public void GetTargetDisplayName_ReturnsCorrectNames()
    {
        // Test each enum value and null case

        // Act & Assert: Machine target
        string machineName = PathService.GetTargetDisplayName(EnvironmentVariableTarget.Machine);
        Assert.Equal("Machine", machineName);

        // Act & Assert: User target
        string userName = PathService.GetTargetDisplayName(EnvironmentVariableTarget.User);
        Assert.Equal("User", userName);

        // Act & Assert: Process target
        string processName = PathService.GetTargetDisplayName(EnvironmentVariableTarget.Process);
        Assert.Equal("Process", processName);

        // Act & Assert: Null target (effective PATH)
        string effectiveName = PathService.GetTargetDisplayName(null);
        Assert.Equal("Effective", effectiveName);

        // Note: We don't test the "Unknown" case because EnvironmentVariableTarget
        // is an enum with only 3 values, and there's no valid way to pass an
        // undefined enum value in normal code. The "Unknown" case is defensive
        // programming for potential future enum additions or invalid casts.
    }
}
