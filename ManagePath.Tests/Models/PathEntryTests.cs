using ManagePath.Models;

namespace ManagePath.Tests.Models;

// These tests check the small rules used by PathEntry.
// They help confirm that IsValid and record equality work as expected.

/// <summary>
/// Tests for <see cref="PathEntry"/>.
/// </summary>
public class PathEntryTests
{
    /// <summary>
    /// IsValid should be true when the folder exists and has executables.
    /// </summary>
    [Fact]
    public void IsValid_WithExistingDirectoryAndExecutables_ReturnsTrue()
    {
        var entry = new PathEntry("/usr/bin", true, true);

        Assert.True(entry.IsValid);
    }

    /// <summary>
    /// IsValid should be false when the folder does not exist.
    /// </summary>
    [Fact]
    public void IsValid_WithNonExistingDirectory_ReturnsFalse()
    {
        var entry = new PathEntry("/nonexistent/path", false, true);

        Assert.False(entry.IsValid);
    }

    /// <summary>
    /// IsValid should be false when the folder exists but has no executables.
    /// </summary>
    [Fact]
    public void IsValid_WithExistingDirectoryNoExecutables_ReturnsFalse()
    {
        var entry = new PathEntry("/home/user/documents", true, false);

        Assert.False(entry.IsValid);
    }

    /// <summary>
    /// Two records with the same values should be equal.
    /// </summary>
    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var entry1 = new PathEntry("/usr/bin", true, true);
        var entry2 = new PathEntry("/usr/bin", true, true);

        Assert.Equal(entry1, entry2);
        Assert.True(entry1 == entry2);
    }

    /// <summary>
    /// Records with different values should not be equal.
    /// </summary>
    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var entry1 = new PathEntry("/usr/bin", true, true);
        var entry2 = new PathEntry("/usr/local/bin", true, true);
        var entry3 = new PathEntry("/usr/bin", false, true);
        var entry4 = new PathEntry("/usr/bin", true, false);

        Assert.NotEqual(entry1, entry2);
        Assert.NotEqual(entry1, entry3);
        Assert.NotEqual(entry1, entry4);
    }

    /// <summary>
    /// A record can be deconstructed into separate values.
    /// </summary>
    [Fact]
    public void Deconstruction_ExtractsPropertyValues()
    {
        var entry = new PathEntry("/usr/bin", true, true);

        var (directory, exists, hasExecutables) = entry;

        Assert.Equal("/usr/bin", directory);
        Assert.True(exists);
        Assert.True(hasExecutables);
    }
}
