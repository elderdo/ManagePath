using ManagePath.Models;
using ManagePath.Services;

namespace ManagePath.Tests.Services;

/// <summary>
/// Unit tests for PathValidator class.
/// Tests directory validation and executable file detection across different platforms.
/// </summary>
public class PathValidatorTests
{
    /// <summary>
    /// Tests that Validate correctly identifies an existing directory.
    /// </summary>
    /// <remarks>
    /// This test validates the directory existence check using Directory.Exists().
    /// 
    /// Directory.Exists() behavior:
    /// - Returns true if the path points to an existing directory
    /// - Returns false if directory doesn't exist, path is null, or path is a file
    /// - On Windows: case-insensitive (C:\Windows == c:\windows)
    /// - On Unix: case-sensitive (/usr/bin != /usr/Bin)
    /// 
    /// We use a well-known system directory that exists on all platforms:
    /// - Windows: C:\Windows (or equivalent system directory)
    /// - Unix: /usr or /bin
    /// 
    /// The test uses Environment.SystemDirectory which provides a guaranteed
    /// existing directory on all platforms (e.g., C:\Windows\System32 on Windows).
    /// </remarks>
    [Fact]
    public void DirectoryExists_ExistingDirectory_ReturnsTrue()
    {
        // Arrange: Create validator and use a system directory that definitely exists
        var validator = new PathValidator();

        // Environment.SystemDirectory returns a well-known system directory
        // Windows: C:\Windows\System32
        // Unix: Varies, but always valid
        string existingDirectory = Environment.SystemDirectory;

        // Act: Validate the directory
        PathEntry result = validator.Validate(existingDirectory);

        // Assert: The Exists property should be true
        Assert.True(result.Exists, $"Directory {existingDirectory} should exist");

        // Additional verification: directory should match
        Assert.Equal(existingDirectory, result.Directory);
    }

    /// <summary>
    /// Tests that Validate correctly identifies a non-existing directory.
    /// </summary>
    /// <remarks>
    /// This tests the negative case - what happens when Directory.Exists() returns false.
    /// 
    /// We construct a path that is highly unlikely to exist by using a GUID
    /// (Globally Unique Identifier) which generates a random string like:
    /// "c:\nonexistent-3f2504e0-4f89-11d3-9a0c-0305e82c3301"
    /// 
    /// GUID properties:
    /// - Statistically unique (1 in 2^122 chance of collision)
    /// - Generated at runtime, so no hardcoded path assumptions
    /// - Safe to use across platforms
    /// 
    /// Expected behavior when directory doesn't exist:
    /// - Exists = false
    /// - HasExecutables = false (can't have executables if dir doesn't exist)
    /// - IsValid = false (fails validation)
    /// </remarks>
    [Fact]
    public void DirectoryExists_NonExistingDirectory_ReturnsFalse()
    {
        // Arrange: Create validator and a path that definitely doesn't exist
        var validator = new PathValidator();

        // Use GUID to generate a unique non-existent path
        string nonExistentDirectory = Path.Combine(
            Path.GetTempPath(),
            $"nonexistent-{Guid.NewGuid()}"
        );

        // Verify our assumption that it doesn't exist
        Assert.False(Directory.Exists(nonExistentDirectory), "Test directory should not exist");

        // Act: Validate the non-existent directory
        PathEntry result = validator.Validate(nonExistentDirectory);

        // Assert: Should indicate directory doesn't exist
        Assert.False(result.Exists, "Non-existent directory should return Exists=false");

        // HasExecutables should also be false (can't detect executables in non-existent dir)
        Assert.False(result.HasExecutables);

        // IsValid should be false
        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Tests that Windows executable file detection works for common Windows executables.
    /// </summary>
    /// <remarks>
    /// This test is platform-specific and only runs on Windows.
    /// We use the [Fact] attribute with a runtime check instead of [Fact(Skip = "...")] 
    /// to allow the test to run conditionally.
    /// 
    /// Windows executable detection strategy:
    /// - Uses PATHEXT environment variable (e.g., .COM;.EXE;.BAT;.CMD;.PS1)
    /// - Checks if directory contains files matching these extensions
    /// - File association determines if they're actually executable
    /// 
    /// Test approach:
    /// - Create a temporary directory
    /// - Create test files with Windows executable extensions (.exe, .bat, .cmd)
    /// - Verify PathValidator detects them
    /// - Clean up afterwards
    /// 
    /// Note: We create empty files - they don't need to be valid executables,
    /// just have the right extension. PathValidator only checks extension,
    /// not whether the file is a valid PE (Portable Executable) binary.
    /// </remarks>
    [Fact]
    public void HasExecutableFiles_WithWindowsExecutables_ReturnsTrue()
    {
        // Skip test on non-Windows platforms
        if (!OperatingSystem.IsWindows())
        {
            // Use Skip.If pattern - test passes but is skipped
            return;
        }

        // Arrange: Create a temporary directory with Windows executable files
        string tempDir = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create test files with Windows executable extensions
            // These don't need to be real executables, just have the right extension
            File.WriteAllText(Path.Combine(tempDir, "test.exe"), "dummy");
            File.WriteAllText(Path.Combine(tempDir, "script.bat"), "@echo off");
            File.WriteAllText(Path.Combine(tempDir, "command.cmd"), "rem test");

            // Create validator with default Windows extensions
            var validator = new PathValidator();

            // Act: Validate the directory
            PathEntry result = validator.Validate(tempDir);

            // Assert: Should detect executables on Windows
            Assert.True(result.Exists, "Temp directory should exist");
            Assert.True(result.HasExecutables, "Should detect .exe, .bat, .cmd files as executables on Windows");
            Assert.True(result.IsValid, "Directory with executables should be valid");
        }
        finally
        {
            // Cleanup: Delete temporary directory and files
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Tests that Unix executable file detection works based on file permissions.
    /// </summary>
    /// <remarks>
    /// This test is platform-specific and only runs on Unix-like systems (Linux/macOS).
    /// 
    /// Unix executable detection strategy:
    /// - Ignores file extensions (Unix doesn't use them for executability)
    /// - Checks file permission bits using File.GetUnixFileMode()
    /// - Specifically checks UnixFileMode.UserExecute (owner execute permission)
    /// 
    /// File permissions on Unix (octal notation):
    /// - 0755 = rwxr-xr-x (owner: read+write+execute, group/others: read+execute)
    /// - 0644 = rw-r--r-- (owner: read+write, group/others: read only)
    /// - 0700 = rwx------ (owner: full access, no access for others)
    /// 
    /// UnixFileMode flags (C# enum):
    /// - UserRead = 0x0100 (owner read permission)
    /// - UserWrite = 0x0080 (owner write permission)  
    /// - UserExecute = 0x0040 (owner execute permission) ← What we check
    /// 
    /// The test creates a file and sets the execute bit using File.SetUnixFileMode.
    /// </remarks>
    [Fact]
    public void HasExecutableFiles_WithUnixExecutables_ReturnsTrue()
    {
        // Skip test on Windows
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange: Create a temporary directory with executable files
        string tempDir = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a test file and make it executable
            string scriptPath = Path.Combine(tempDir, "test-script");
            File.WriteAllText(scriptPath, "#!/bin/bash\necho test");

            // Set execute permission using UnixFileMode
            // UserRead | UserWrite | UserExecute = 0700 (rwx------)
            File.SetUnixFileMode(scriptPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

            // Verify execute permission is set
            var mode = File.GetUnixFileMode(scriptPath);
            Assert.True((mode & UnixFileMode.UserExecute) != 0, "Execute bit should be set");

            // Create validator
            var validator = new PathValidator();

            // Act: Validate the directory
            PathEntry result = validator.Validate(tempDir);

            // Assert: Should detect executable based on permissions
            Assert.True(result.Exists, "Temp directory should exist");
            Assert.True(result.HasExecutables, "Should detect files with execute permission on Unix");
            Assert.True(result.IsValid, "Directory with executables should be valid");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Tests that an empty directory is correctly identified as having no executables.
    /// </summary>
    /// <remarks>
    /// This is an important edge case test.
    /// 
    /// Expected behavior for empty directory:
    /// - Exists = true (directory exists)
    /// - HasExecutables = false (no files = no executables)
    /// - IsValid = false (exists but not useful for PATH)
    /// 
    /// Real-world scenario:
    /// Users sometimes add empty directories to PATH by mistake, or directories
    /// that had executables but were later cleaned out. These should be flagged
    /// as invalid because they don't contribute to PATH functionality.
    /// 
    /// LINQ Any() behavior with empty collection:
    /// - Any() returns false when called on an empty sequence
    /// - This is exactly what we want: no files → no executables
    /// </remarks>
    [Fact]
    public void HasExecutableFiles_EmptyDirectory_ReturnsFalse()
    {
        // Arrange: Create an empty temporary directory
        string tempDir = Path.Combine(Path.GetTempPath(), $"empty-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Verify directory is actually empty
            Assert.Empty(Directory.GetFiles(tempDir));

            var validator = new PathValidator();

            // Act: Validate the empty directory
            PathEntry result = validator.Validate(tempDir);

            // Assert: Should exist but have no executables
            Assert.True(result.Exists, "Empty directory should exist");
            Assert.False(result.HasExecutables, "Empty directory should have no executables");
            Assert.False(result.IsValid, "Empty directory should be invalid for PATH purposes");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Tests that directories containing only non-executable files are correctly identified.
    /// </summary>
    /// <remarks>
    /// This tests another important edge case: directories with files, but no executables.
    /// 
    /// Scenarios tested:
    /// - Windows: Files without executable extensions (.txt, .dat, .config)
    /// - Unix: Files without execute permission bit set
    /// 
    /// Real-world examples:
    /// - C:\Windows\Fonts (has .ttf, .otf files - not executables)
    /// - C:\Users\Name\Documents (has .docx, .pdf - not executables)
    /// - /etc/config (has config files - not executable even on Unix)
    /// 
    /// This validates that PathValidator doesn't just check "has files?",
    /// but specifically checks for executable files according to platform rules.
    /// </remarks>
    [Fact]
    public void HasExecutableFiles_OnlyNonExecutables_ReturnsFalse()
    {
        // Arrange: Create temporary directory with non-executable files
        string tempDir = Path.Combine(Path.GetTempPath(), $"nonexec-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            if (OperatingSystem.IsWindows())
            {
                // Windows: Create files with non-executable extensions
                File.WriteAllText(Path.Combine(tempDir, "readme.txt"), "test");
                File.WriteAllText(Path.Combine(tempDir, "config.json"), "{}");
                File.WriteAllText(Path.Combine(tempDir, "data.dat"), "data");
            }
            else
            {
                // Unix: Create files without execute permission
                string file1 = Path.Combine(tempDir, "readme.txt");
                string file2 = Path.Combine(tempDir, "config.json");

                File.WriteAllText(file1, "test");
                File.WriteAllText(file2, "{}");

                // Set read/write only (no execute) - 0644 (rw-r--r--)
                File.SetUnixFileMode(file1,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite |
                    UnixFileMode.GroupRead | UnixFileMode.OtherRead);
                File.SetUnixFileMode(file2,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            var validator = new PathValidator();

            // Act: Validate the directory
            PathEntry result = validator.Validate(tempDir);

            // Assert: Should exist but have no executables
            Assert.True(result.Exists, "Directory should exist");
            Assert.False(result.HasExecutables, "Should not detect non-executable files as executables");
            Assert.False(result.IsValid, "Directory with only non-executable files should be invalid");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Tests that custom extensions including .pl are recognized.
    /// </summary>
    /// <remarks>
    /// This test verifies that PathValidator adds .pl extension support.
    /// 
    /// The GetDefaultExecutableExtensions() method should:
    /// - On Windows: Read PATHEXT and add .pl/.PL extensions
    /// - On Unix: Return empty array (doesn't use extensions)
    /// 
    /// Why test .pl specifically?
    /// - It's a custom extension added to the standard PATHEXT
    /// - Demonstrates extensibility of the PathValidator
    /// - Important for Perl developers
    /// 
    /// We test this indirectly by creating a .pl file and verifying
    /// it's detected as an executable on Windows.
    /// 
    /// Note: This only tests DETECTION, not actual executability.
    /// For .pl files to RUN, file association must be configured separately.
    /// </remarks>
    [Fact]
    public void GetDefaultExecutableExtensions_ContainsPlExtension()
    {
        // Skip on non-Windows (Unix doesn't use extensions)
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange: Create temporary directory with a .pl file
        string tempDir = Path.Combine(Path.GetTempPath(), $"pl-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a .pl file (Perl script)
            File.WriteAllText(Path.Combine(tempDir, "script.pl"), "#!/usr/bin/perl\nprint 'test';");

            var validator = new PathValidator();

            // Act: Validate directory with .pl file
            PathEntry result = validator.Validate(tempDir);

            // Assert: Should detect .pl file as executable on Windows
            Assert.True(result.Exists);
            Assert.True(result.HasExecutables, ".pl files should be detected as executables");

            // Also test with uppercase extension
            File.WriteAllText(Path.Combine(tempDir, "SCRIPT.PL"), "test");
            result = validator.Validate(tempDir);
            Assert.True(result.HasExecutables, ".PL (uppercase) should also be detected");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Tests that PathValidator reads and respects the PATHEXT environment variable on Windows.
    /// </summary>
    /// <remarks>
    /// PATHEXT is a Windows-specific environment variable that lists file extensions
    /// the OS considers executable when typed without extension at the command line.
    /// 
    /// Typical PATHEXT value:
    /// .COM;.EXE;.BAT;.CMD;.VBS;.VBE;.JS;.JSE;.WSF;.WSH;.MSC;.PS1
    /// 
    /// PathValidator should:
    /// 1. Read the PATHEXT environment variable
    /// 2. Split it by semicolon
    /// 3. Use these extensions for executable detection
    /// 4. Add custom extensions like .pl
    /// 
    /// This test verifies that standard PATHEXT extensions like .exe are recognized.
    /// We create files with common Windows extensions and verify detection.
    /// </remarks>
    [Fact]
    public void GetDefaultExecutableExtensions_OnWindows_ContainsPathext()
    {
        // Skip on non-Windows platforms
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange: Get actual PATHEXT value from environment
        string? pathExt = Environment.GetEnvironmentVariable("PATHEXT");
        Assert.NotNull(pathExt); // PATHEXT should exist on Windows

        // Parse extensions from PATHEXT
        var expectedExtensions = pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries);
        Assert.NotEmpty(expectedExtensions);

        // Create temporary directory
        string tempDir = Path.Combine(Path.GetTempPath(), $"pathext-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Test common Windows extensions from PATHEXT
            var testExtensions = new[] { ".exe", ".bat", ".cmd", ".com" };

            foreach (var ext in testExtensions)
            {
                // Only test if extension is in PATHEXT
                if (expectedExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    // Create file with this extension
                    string fileName = $"test{ext}";
                    File.WriteAllText(Path.Combine(tempDir, fileName), "dummy");

                    var validator = new PathValidator();
                    PathEntry result = validator.Validate(tempDir);

                    // Assert: Should detect file as executable
                    Assert.True(result.HasExecutables,
                        $"Should detect {ext} files (from PATHEXT) as executables");

                    // Cleanup for next iteration
                    File.Delete(Path.Combine(tempDir, fileName));
                }
            }
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Tests that the Validate method creates PathEntry with correct property values.
    /// </summary>
    /// <remarks>
    /// This is an integration test that verifies the complete Validate() workflow:
    /// 
    /// Workflow steps:
    /// 1. Check if directory exists using Directory.Exists()
    /// 2. If exists, check for executables using HasExecutableFiles()
    /// 3. Create PathEntry with:
    ///    - Directory = input path
    ///    - Exists = directory existence result
    ///    - HasExecutables = executable detection result
    /// 
    /// We test multiple scenarios:
    /// - Valid directory with executables → all properties correct
    /// - Existing directory without executables → Exists=true, HasExecutables=false
    /// - Non-existent directory → Exists=false, HasExecutables=false
    /// 
    /// This ensures the PathEntry returned matches the actual filesystem state.
    /// </remarks>
    [Fact]
    public void Validate_CreatesCorrectPathEntry()
    {
        // Arrange: Create test directory with an executable file
        string tempDir = Path.Combine(Path.GetTempPath(), $"validate-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create an executable file based on platform
            if (OperatingSystem.IsWindows())
            {
                File.WriteAllText(Path.Combine(tempDir, "test.exe"), "dummy");
            }
            else
            {
                string scriptPath = Path.Combine(tempDir, "test-script");
                File.WriteAllText(scriptPath, "#!/bin/bash\necho test");
                File.SetUnixFileMode(scriptPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }

            var validator = new PathValidator();

            // Act: Validate the directory
            PathEntry result = validator.Validate(tempDir);

            // Assert: Verify all PathEntry properties are correct

            // 1. Directory path should match input
            Assert.Equal(tempDir, result.Directory);

            // 2. Exists should be true (we created it)
            Assert.True(result.Exists, "Created directory should exist");

            // 3. HasExecutables should be true (we added an executable)
            Assert.True(result.HasExecutables, "Directory should contain executables");

            // 4. IsValid should be true (exists AND has executables)
            Assert.True(result.IsValid, "Valid directory should have IsValid=true");

            // Test scenario 2: Non-existent directory
            string nonExistent = Path.Combine(Path.GetTempPath(), $"nonexist-{Guid.NewGuid()}");
            result = validator.Validate(nonExistent);

            Assert.Equal(nonExistent, result.Directory);
            Assert.False(result.Exists);
            Assert.False(result.HasExecutables);
            Assert.False(result.IsValid);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Tests that custom extensions passed to constructor are used for detection.
    /// </summary>
    /// <remarks>
    /// PathValidator constructor accepts customExtensions parameter to override
    /// default extensions. This is useful for:
    /// - Testing specific file types
    /// - Supporting custom scripting languages
    /// - Platform-specific extension needs
    /// 
    /// When custom extensions are provided, they completely replace the defaults
    /// (not added to them). This gives full control over what's considered executable.
    /// </remarks>
    [Fact]
    public void CustomExtensions_AreUsedForDetection()
    {
        // Skip on non-Windows (custom extensions only apply to Windows detection)
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange: Create validator with custom extensions
        var customExtensions = new[] { ".custom", ".test" };
        var validator = new PathValidator(customExtensions);

        string tempDir = Path.Combine(Path.GetTempPath(), $"custom-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create file with custom extension
            File.WriteAllText(Path.Combine(tempDir, "app.custom"), "test");

            // Act
            PathEntry result = validator.Validate(tempDir);

            // Assert: Should detect custom extension
            Assert.True(result.HasExecutables, "Should detect custom extensions");

            // Verify standard .exe is NOT detected (custom extensions replace defaults)
            File.Delete(Path.Combine(tempDir, "app.custom"));
            File.WriteAllText(Path.Combine(tempDir, "app.exe"), "test");

            result = validator.Validate(tempDir);
            Assert.False(result.HasExecutables,
                "Standard .exe should not be detected when custom extensions are specified");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
