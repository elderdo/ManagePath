using ManagePath.Models;

namespace ManagePath.Tests.Models;

/// <summary>
/// Unit tests for PathEntry record type.
/// Tests immutable data model, computed properties, and value-based equality semantics.
/// </summary>
public class PathEntryTests
{
    /// <summary>
    /// Tests that IsValid returns true when directory exists and has executables.
    /// </summary>
    /// <remarks>
    /// The IsValid property is a computed property that uses a logical AND (&&) expression:
    /// IsValid => Exists && HasExecutables
    /// 
    /// This is shorthand for:
    /// public bool IsValid 
    /// { 
    ///     get { return Exists && HasExecutables; } 
    /// }
    /// 
    /// The => syntax is called an "expression-bodied member" in C#.
    /// It's a concise way to define read-only properties with a single expression.
    /// 
    /// Logical AND (&&) behavior:
    /// - Returns true only if BOTH operands are true
    /// - Short-circuits: if Exists is false, HasExecutables is never evaluated
    /// - Truth table:
    ///   Exists=true,  HasExecutables=true  → IsValid=true  ✓
    ///   Exists=true,  HasExecutables=false → IsValid=false
    ///   Exists=false, HasExecutables=true  → IsValid=false
    ///   Exists=false, HasExecutables=false → IsValid=false
    /// 
    /// This test verifies the first case in the truth table.
    /// </remarks>
    [Fact]
    public void IsValid_WithExistingDirectoryAndExecutables_ReturnsTrue()
    {
        // Arrange: Create a PathEntry with both conditions satisfied
        // Records use positional parameters in the constructor
        // The order must match: (Directory, Exists, HasExecutables)
        var entry = new PathEntry(
            Directory: "/usr/bin",     // Parameter 1: Directory path
            Exists: true,              // Parameter 2: Directory exists
            HasExecutables: true       // Parameter 3: Contains executables
        );

        // Act: Access the computed IsValid property
        // This evaluates: true && true
        bool isValid = entry.IsValid;

        // Assert: Should return true because both conditions are met
        Assert.True(isValid);

        // Alternative assertion style (more explicit):
        Assert.True(entry.IsValid, "Entry should be valid when directory exists and has executables");
    }

    /// <summary>
    /// Tests that IsValid returns false when directory does not exist.
    /// </summary>
    /// <remarks>
    /// This tests the second condition in the logical AND truth table.
    /// Even if HasExecutables=true, IsValid should be false because Exists=false.
    /// 
    /// Real-world scenario:
    /// A directory might theoretically contain executable files, but if the
    /// directory itself doesn't exist on the filesystem, it can't be a valid
    /// PATH entry. The operating system won't search non-existent directories.
    /// 
    /// Logical AND short-circuit evaluation:
    /// Since Exists=false, the && operator short-circuits and doesn't even
    /// evaluate HasExecutables. The result is immediately false.
    /// 
    /// This demonstrates defensive validation: a PATH entry must satisfy
    /// ALL criteria to be considered valid, not just some of them.
    /// </remarks>
    [Fact]
    public void IsValid_WithNonExistingDirectory_ReturnsFalse()
    {
        // Arrange: Create entry where directory doesn't exist
        // Note: HasExecutables=true is irrelevant because directory doesn't exist
        // This represents an orphaned PATH entry pointing to a removed directory
        var entry = new PathEntry(
            Directory: "/nonexistent/path",
            Exists: false,             // Directory doesn't exist
            HasExecutables: true       // Hypothetical: would have executables if it existed
        );

        // Act: Evaluate IsValid
        // Evaluates: false && true → false (short-circuits at false)
        bool isValid = entry.IsValid;

        // Assert: Should return false
        Assert.False(isValid);

        // More explicit assertion with message
        Assert.False(entry.IsValid, "Entry should be invalid when directory does not exist, regardless of HasExecutables value");
    }

    /// <summary>
    /// Tests that IsValid returns false when directory exists but has no executables.
    /// </summary>
    /// <remarks>
    /// This tests the third case in the logical AND truth table.
    /// Exists=true but HasExecutables=false → IsValid=false
    /// 
    /// Real-world scenario:
    /// A directory might exist on the filesystem but contain only data files,
    /// configuration files, or libraries - no actual executable programs.
    /// Such directories don't contribute to PATH functionality.
    /// 
    /// Examples of directories that exist but have no executables:
    /// - C:\Users\YourName\Documents (exists, but no .exe files)
    /// - C:\Windows\Fonts (exists, but only .ttf/.otf font files)
    /// - Empty directories
    /// 
    /// PATH semantics:
    /// The PATH environment variable is meant to help the OS find executable
    /// programs. If a directory doesn't contain executables, including it in
    /// PATH serves no purpose and may even slow down command resolution.
    /// 
    /// This validation helps identify PATH pollution - directories that shouldn't
    /// be in PATH because they don't contain runnable programs.
    /// </remarks>
    [Fact]
    public void IsValid_WithExistingDirectoryNoExecutables_ReturnsFalse()
    {
        // Arrange: Create entry where directory exists but has no executable files
        // This could represent a data directory mistakenly added to PATH
        var entry = new PathEntry(
            Directory: "/home/user/documents",
            Exists: true,              // Directory exists on filesystem
            HasExecutables: false      // But contains no executable files
        );

        // Act: Evaluate IsValid
        // Evaluates: true && false → false
        bool isValid = entry.IsValid;

        // Assert: Should return false
        Assert.False(isValid);

        // Alternative assertion checking the property directly
        Assert.False(entry.IsValid, "Entry should be invalid when directory exists but contains no executables");
    }

    /// <summary>
    /// Tests that two PathEntry records with identical values are considered equal.
    /// </summary>
    /// <remarks>
    /// This test validates the value-based equality semantics of C# records.
    /// 
    /// RECORD EQUALITY vs CLASS EQUALITY:
    /// 
    /// Records (value-based equality):
    /// - Two record instances are equal if all their property values match
    /// - Comparison is structural/value-based, not reference-based
    /// - Compiler auto-generates Equals(), GetHashCode(), and == operator
    /// - Perfect for DTOs, value objects, and immutable data
    /// 
    /// Classes (reference-based equality):
    /// - Two class instances are equal only if they reference the SAME object
    /// - Different instances with identical values are NOT equal by default
    /// - Must manually override Equals() and GetHashCode() for value equality
    /// 
    /// Example demonstrating the difference:
    /// 
    /// // With a class:
    /// var class1 = new PathEntryClass("/usr/bin", true, true);
    /// var class2 = new PathEntryClass("/usr/bin", true, true);
    /// class1 == class2  → FALSE (different references)
    /// 
    /// // With a record:
    /// var record1 = new PathEntry("/usr/bin", true, true);
    /// var record2 = new PathEntry("/usr/bin", true, true);
    /// record1 == record2  → TRUE (same values)
    /// 
    /// Why this matters:
    /// - Collections: Can use records as dictionary keys or HashSet members
    /// - Comparison: Natural equality for unit tests and assertions
    /// - Immutability: Records encourage immutable value semantics
    /// 
    /// Under the hood:
    /// The C# compiler generates an Equals method that compares each property:
    /// public virtual bool Equals(PathEntry? other)
    /// {
    ///     return other != null &&
    ///            Directory == other.Directory &&
    ///            Exists == other.Exists &&
    ///            HasExecutables == other.HasExecutables;
    /// }
    /// </remarks>
    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange: Create two separate PathEntry instances with identical values
        // These are TWO DIFFERENT OBJECTS in memory (different references)
        var entry1 = new PathEntry("/usr/bin", true, true);
        var entry2 = new PathEntry("/usr/bin", true, true);

        // Act & Assert: Test multiple forms of equality

        // Test 1: Equals() method
        // This uses the compiler-generated Equals method for records
        Assert.True(entry1.Equals(entry2));

        // Test 2: == operator
        // Records override the == operator to use value equality
        Assert.True(entry1 == entry2);

        // Test 3: xUnit's Assert.Equal
        // Assert.Equal uses Equals() internally, perfect for records
        Assert.Equal(entry1, entry2);

        // Test 4: GetHashCode equality
        // Equal objects MUST have equal hash codes (contract requirement)
        // This is crucial for using records in HashSet or as Dictionary keys
        Assert.Equal(entry1.GetHashCode(), entry2.GetHashCode());

        // Additional verification: Test that reference equality is different
        // ReferenceEquals checks if they're the SAME object (which they're not)
        Assert.False(ReferenceEquals(entry1, entry2));
        // ↑ This proves they're different objects but still considered equal

        // Test 5: Inequality operator (should be false for equal values)
        Assert.False(entry1 != entry2);
    }

    /// <summary>
    /// Additional test: Verifies that records with different values are NOT equal.
    /// </summary>
    /// <remarks>
    /// This is the complement to the equality test above.
    /// It ensures that records with ANY differing property value are not considered equal.
    /// 
    /// This validates that the auto-generated Equals method correctly compares
    /// ALL properties, not just some of them.
    /// </remarks>
    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange: Create entries that differ in various ways
        var entry1 = new PathEntry("/usr/bin", true, true);

        // Different directory path
        var entry2 = new PathEntry("/usr/local/bin", true, true);

        // Different Exists value
        var entry3 = new PathEntry("/usr/bin", false, true);

        // Different HasExecutables value
        var entry4 = new PathEntry("/usr/bin", true, false);

        // Assert: None of these should be equal to entry1
        Assert.NotEqual(entry1, entry2);  // Different directory
        Assert.NotEqual(entry1, entry3);  // Different exists flag
        Assert.NotEqual(entry1, entry4);  // Different executables flag

        // Test with operators
        Assert.True(entry1 != entry2);
        Assert.False(entry1 == entry3);
    }

    /// <summary>
    /// Tests record deconstruction feature.
    /// </summary>
    /// <remarks>
    /// Records support deconstruction, allowing you to extract property values
    /// into separate variables using pattern matching syntax.
    /// 
    /// This is syntactic sugar that makes working with records more functional.
    /// Behind the scenes, the compiler generates a Deconstruct method.
    /// </remarks>
    [Fact]
    public void Deconstruction_ExtractsPropertyValues()
    {
        // Arrange
        var entry = new PathEntry("/usr/bin", true, true);

        // Act: Deconstruct the record into individual variables
        // The order matches the constructor parameter order
        var (directory, exists, hasExecutables) = entry;

        // Assert: Verify each deconstructed value
        Assert.Equal("/usr/bin", directory);
        Assert.True(exists);
        Assert.True(hasExecutables);

        // This is equivalent to:
        // string directory = entry.Directory;
        // bool exists = entry.Exists;
        // bool hasExecutables = entry.HasExecutables;
    }
}
