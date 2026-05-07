using ManagePath.Models;
using ManagePath.Services;

namespace ManagePath.Tests.Services;

// These tests verify that PathValidator checks PATH folders correctly.
// They cover missing folders, valid folders, and platform-specific executable detection.

/// <summary>
/// Tests for <see cref="PathValidator"/>.
/// </summary>
public class PathValidatorTests
{
    /// <summary>
    /// Checks that an existing directory is reported as existing.
    /// </summary>
    [Fact]
    public void DirectoryExists_ExistingDirectory_ReturnsTrue()
    {
        var validator = new PathValidator();
        string existingDirectory = Environment.SystemDirectory;

        PathEntry result = validator.Validate(existingDirectory);

        Assert.True(result.Exists, $"Directory {existingDirectory} should exist");
        Assert.Equal(existingDirectory, result.Directory);
    }

    /// <summary>
    /// Checks that a missing directory is reported as missing.
    /// </summary>
    [Fact]
    public void DirectoryExists_NonExistingDirectory_ReturnsFalse()
    {
        var validator = new PathValidator();

        string nonExistentDirectory = Path.Combine(
            Path.GetTempPath(),
            $"nonexistent-{Guid.NewGuid()}"
        );

        Assert.False(Directory.Exists(nonExistentDirectory), "Test directory should not exist");

        PathEntry result = validator.Validate(nonExistentDirectory);

        Assert.False(result.Exists, "Non-existent directory should return Exists=false");
        Assert.False(result.HasExecutables);
        Assert.False(result.IsValid);
    }

    /// <summary>
    /// On Windows, common executable extensions should be detected.
    /// </summary>
    [Fact]
    public void HasExecutableFiles_WithWindowsExecutables_ReturnsTrue()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "test.exe"), "dummy");
            File.WriteAllText(Path.Combine(tempDir, "script.bat"), "@echo off");
            File.WriteAllText(Path.Combine(tempDir, "command.cmd"), "rem test");

            var validator = new PathValidator();

            PathEntry result = validator.Validate(tempDir);

            Assert.True(result.Exists, "Temp directory should exist");
            Assert.True(result.HasExecutables, "Should detect .exe, .bat, .cmd files as executables on Windows");
            Assert.True(result.IsValid, "Directory with executables should be valid");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// On Unix-like systems, execute permission should be detected.
    /// </summary>
    [Fact]
    public void HasExecutableFiles_WithUnixExecutables_ReturnsTrue()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            string scriptPath = Path.Combine(tempDir, "test-script");
            File.WriteAllText(scriptPath, "#!/bin/bash\necho test");

            File.SetUnixFileMode(
                scriptPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

            var mode = File.GetUnixFileMode(scriptPath);
            Assert.True((mode & UnixFileMode.UserExecute) != 0, "Execute bit should be set");

            var validator = new PathValidator();

            PathEntry result = validator.Validate(tempDir);

            Assert.True(result.Exists, "Temp directory should exist");
            Assert.True(result.HasExecutables, "Should detect files with execute permission on Unix");
            Assert.True(result.IsValid, "Directory with executables should be valid");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// An empty directory should not be treated as valid for PATH.
    /// </summary>
    [Fact]
    public void HasExecutableFiles_EmptyDirectory_ReturnsFalse()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"empty-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            Assert.Empty(Directory.GetFiles(tempDir));

            var validator = new PathValidator();

            PathEntry result = validator.Validate(tempDir);

            Assert.True(result.Exists, "Empty directory should exist");
            Assert.False(result.HasExecutables, "Empty directory should have no executables");
            Assert.False(result.IsValid, "Empty directory should be invalid for PATH purposes");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// A directory with only normal files should not be treated as executable.
    /// </summary>
    [Fact]
    public void HasExecutableFiles_OnlyNonExecutables_ReturnsFalse()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"nonexec-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            if (OperatingSystem.IsWindows())
            {
                File.WriteAllText(Path.Combine(tempDir, "readme.txt"), "test");
                File.WriteAllText(Path.Combine(tempDir, "config.json"), "{}");
                File.WriteAllText(Path.Combine(tempDir, "data.dat"), "data");
            }
            else
            {
                string file1 = Path.Combine(tempDir, "readme.txt");
                string file2 = Path.Combine(tempDir, "config.json");

                File.WriteAllText(file1, "test");
                File.WriteAllText(file2, "{}");

                File.SetUnixFileMode(
                    file1,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite |
                    UnixFileMode.GroupRead | UnixFileMode.OtherRead);

                File.SetUnixFileMode(
                    file2,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            var validator = new PathValidator();

            PathEntry result = validator.Validate(tempDir);

            Assert.True(result.Exists, "Directory should exist");
            Assert.False(result.HasExecutables, "Should not detect non-executable files as executables");
            Assert.False(result.IsValid, "Directory with only non-executable files should be invalid");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// On Windows, .pl files should be included in detection.
    /// </summary>
    [Fact]
    public void GetDefaultExecutableExtensions_ContainsPlExtension()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), $"pl-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "script.pl"), "#!/usr/bin/perl\nprint 'test';");

            var validator = new PathValidator();

            PathEntry result = validator.Validate(tempDir);

            Assert.True(result.Exists);
            Assert.True(result.HasExecutables, ".pl files should be detected as executables");

            File.WriteAllText(Path.Combine(tempDir, "SCRIPT.PL"), "test");
            result = validator.Validate(tempDir);

            Assert.True(result.HasExecutables, ".PL (uppercase) should also be detected");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// On Windows, PATHEXT entries should be used for detection.
    /// </summary>
    [Fact]
    public void GetDefaultExecutableExtensions_OnWindows_ContainsPathext()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string? pathExt = Environment.GetEnvironmentVariable("PATHEXT");
        Assert.NotNull(pathExt);

        var expectedExtensions = pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries);
        Assert.NotEmpty(expectedExtensions);

        string tempDir = Path.Combine(Path.GetTempPath(), $"pathext-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var testExtensions = new[] { ".exe", ".bat", ".cmd", ".com" };

            foreach (var ext in testExtensions)
            {
                if (expectedExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    string fileName = $"test{ext}";
                    File.WriteAllText(Path.Combine(tempDir, fileName), "dummy");

                    var validator = new PathValidator();
                    PathEntry result = validator.Validate(tempDir);

                    Assert.True(result.HasExecutables,
                        $"Should detect {ext} files (from PATHEXT) as executables");

                    File.Delete(Path.Combine(tempDir, fileName));
                }
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Validate should fill PathEntry properties correctly.
    /// </summary>
    [Fact]
    public void Validate_CreatesCorrectPathEntry()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"validate-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            if (OperatingSystem.IsWindows())
            {
                File.WriteAllText(Path.Combine(tempDir, "test.exe"), "dummy");
            }
            else
            {
                string scriptPath = Path.Combine(tempDir, "test-script");
                File.WriteAllText(scriptPath, "#!/bin/bash\necho test");
                File.SetUnixFileMode(
                    scriptPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }

            var validator = new PathValidator();

            PathEntry result = validator.Validate(tempDir);

            Assert.Equal(tempDir, result.Directory);
            Assert.True(result.Exists, "Created directory should exist");
            Assert.True(result.HasExecutables, "Directory should contain executables");
            Assert.True(result.IsValid, "Valid directory should have IsValid=true");

            string nonExistent = Path.Combine(Path.GetTempPath(), $"nonexist-{Guid.NewGuid()}");
            result = validator.Validate(nonExistent);

            Assert.Equal(nonExistent, result.Directory);
            Assert.False(result.Exists);
            Assert.False(result.HasExecutables);
            Assert.False(result.IsValid);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Custom Windows extensions should replace the defaults.
    /// </summary>
    [Fact]
    public void CustomExtensions_AreUsedForDetection()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var customExtensions = new[] { ".custom", ".test" };
        var validator = new PathValidator(customExtensions);

        string tempDir = Path.Combine(Path.GetTempPath(), $"custom-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "app.custom"), "test");

            PathEntry result = validator.Validate(tempDir);

            Assert.True(result.HasExecutables, "Should detect custom extensions");

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
