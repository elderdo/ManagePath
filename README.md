# ManagePath

A cross-platform command-line tool for managing and analyzing the PATH environment variable on Windows, Linux, and macOS.

## Features

- 📋 **List PATH directories** from different environment variable scopes (Process, User, Machine, or Effective)
- ✅ **Validate directories** - Check if directories exist and contain executable files
- 🔢 **Numbered output** - Display directories with line numbers for easy reference
- 🔍 **Smart executable detection** - Uses `PATHEXT` on Windows and file permissions on Unix-like systems
- 🎯 **Custom extensions** - Supports additional executable extensions (e.g., `.pl`, `.py`)
- 🌐 **Cross-platform** - Works on Windows, Linux, and macOS

## Installation

### Prerequisites 1 of 2

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download) or later

### Build from Source

```bash
git clone https://github.com/elderdo/ManagePath.git
cd ManagePath
dotnet build
```

### Run

```bash
dotnet run -- path list [options]
```

Or publish as a self-contained executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Usage

### Basic Commands

```bash
# Show help
dotnet run -- -h
dotnet run -- path list -h

# List all PATH directories (effective PATH)
dotnet run -- path list --effective

# List PATH directories from User environment variable
dotnet run -- path list --target User

# List PATH directories from Machine environment variable
dotnet run -- path list --target Machine

# List with validation
dotnet run -- path list --effective --validate

# List with line numbers
dotnet run -- path list --effective --number

# Combine options
dotnet run -- path list -e -v -n
```

### Options

#### `--target, -t <Process|User|Machine>`

Specify which environment variable scope to read:

- `Process` - Process-level PATH (default)
- `User` - User-level PATH (HKEY_CURRENT_USER)
- `Machine` - System-level PATH (HKEY_LOCAL_MACHINE)

#### `--effective, -e`

Show the effective PATH (merged view of Machine + User + Process). This represents what the current process actually sees. Overrides `--target` option.

#### `--validate, -v`

Validate each directory:

- Checks if the directory exists
- Checks if it contains executable files
- Shows validation status for each entry

#### `--number, -n`

Display line numbers for each directory entry.

## Examples

### Example 1: List effective PATH with validation and numbers

```bash
dotnet run -- path list -e -v -n
```

**Output:**

```text
1: C:\Windows\system32
    [Valid]
2: C:\Windows
    [Valid]
3: C:\Program Files\Git\cmd
    [Valid]
4: C:\missing\directory
    [Invalid: Directory does not exist]
5: C:\empty\folder
    [Invalid: No executable files found]
Total invalid directories: 2
```

### Example 2: List User PATH only

```bash
dotnet run -- path list --target User -n
```

**Output:**

```text
1: C:\Users\YourName\AppData\Local\Programs\Python\Python39
2: C:\Users\YourName\bin
3: C:\Users\YourName\.dotnet\tools
```

### Example 3: Validate Machine PATH

```bash
dotnet run -- path list --target Machine --validate
```

## Architecture

The project is organized into a clean, maintainable structure:

```text
ManagePath/
├── ManagePath/                # Main application project
│   ├── Models/
│   │   └── PathEntry.cs       # Domain model for PATH entries
│   ├── Services/
│   │   ├── PathService.cs     # PATH reading and parsing
│   │   └── PathValidator.cs   # Directory validation logic
│   ├── Formatters/
│   │   └── PathFormatter.cs   # Console output formatting
│   ├── GlobalUsings.cs        # Global using directives
│   └── Program.cs             # CLI entry point
└── ManagePath.Tests/          # Unit test project
    ├── Services/
    │   └── PathServiceTests.cs    # Tests for PathService
    └── ManagePath.Tests.csproj    # Test project file
```
├── GlobalUsings.cs           # Global using directives
└── Program.cs                # CLI entry point
```

### Key Components

- **PathEntry** - Immutable record representing a PATH directory with validation state
- **PathService** - Reads PATH from different environment variable targets
- **PathValidator** - Validates directories (existence, executable detection)
- **PathFormatter** - Handles console output with formatting options

## Executable Detection

### Windows Part 1

Uses the `PATHEXT` environment variable to determine executable extensions:

- `.COM`, `.EXE`, `.BAT`, `.CMD`, `.VBS`, `.JS`, `.WSF`, `.PS1`, etc.
- Custom extensions like `.PL` are also included

#### Configuring .PL (Perl) File Associations on Windows

For `.pl` files to be executable from the command line, you need to configure Windows file associations. ManagePath will **detect** `.pl` files as potential executables, but Windows needs to know how to **run** them.

##### Prerequisites 2 of 2

- Perl must be installed (e.g., [Strawberry Perl](https://strawberryperl.com/) or [ActivePerl](https://www.activestate.com/products/perl/))

##### Method 1: Using Command Prompt (Recommended - requires Administrator)

```cmd
# Associate .pl extension with PerlScript file type
assoc .pl=PerlScript

# Tell Windows how to execute PerlScript files
# Replace C:\Perl64\bin\perl.exe with your actual Perl path
ftype PerlScript="C:\Perl64\bin\perl.exe" "%1" %*

# Add .PL to PATHEXT so you can run scripts without typing the extension
setx PATHEXT "%PATHEXT%;.PL"
```

##### Method 2: Using Registry Editor (requires Administrator)

1. Open Registry Editor (`regedit`)
2. Navigate to `HKEY_CLASSES_ROOT\.pl`
3. Set the default value to `PerlScript`
4. Create a new key: `HKEY_CLASSES_ROOT\PerlScript\shell\open\command`
5. Set its default value to: `"C:\Perl64\bin\perl.exe" "%1" %*`
6. Add `.PL` to `PATHEXT` in:
   - `HKEY_CURRENT_USER\Environment\PATHEXT` (User-level), or
   - `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment\PATHEXT` (System-level)

##### Method 3: Using PowerShell (requires Administrator)

```powershell
# Set file association
cmd /c assoc .pl=PerlScript
cmd /c ftype PerlScript="""C:\Perl64\bin\perl.exe"" ""%1"" %*"

# Add to PATHEXT
[Environment]::SetEnvironmentVariable(
    "PATHEXT",
    [Environment]::GetEnvironmentVariable("PATHEXT", "Machine") + ";.PL",
    "Machine"
)
```

##### Verification

```cmd
# Check file association
assoc .pl
ftype PerlScript

# Check PATHEXT
echo %PATHEXT%

# Test by running a Perl script (without .pl extension if PATHEXT is configured)
cd path\to\script
myscript.pl
# or just:
myscript
```

##### Note

After setting PATHEXT, you may need to restart your command prompt or terminal for changes to take effect.

### Linux/macOS

Checks file permissions for execute bits (`UnixFileMode.UserExecute`)

#### Making scripts executable on Unix

```bash
# Add execute permission
chmod +x script.pl

# Verify permissions
ls -l script.pl
# Output should show: -rwxr-xr-x (x = executable)
```

## Building for Distribution

### Windows Part 2

```bash
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true
```

### Linux

```bash
dotnet publish -c Release -r linux-x64 --self-contained /p:PublishSingleFile=true
```

### macOS

```bash
dotnet publish -c Release -r osx-x64 --self-contained /p:PublishSingleFile=true
```

## Testing

The project includes comprehensive unit tests to ensure reliability across different platforms and scenarios.

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests for specific project
dotnet test ManagePath.Tests/ManagePath.Tests.csproj

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Structure

The test project (`ManagePath.Tests`) uses xUnit as the testing framework and includes:

**PathServiceTests** - Tests for PATH environment variable reading and parsing:
- `GetDirectories_WithNullTarget_ReturnsEffectivePath()` - Tests effective PATH retrieval
- `GetDirectories_WithUserTarget_ReturnsUserPath()` - Tests user-level PATH reading
- `GetDirectories_WithMachineTarget_ReturnsMachinePath()` - Tests system-level PATH reading
- `GetDirectories_WithProcessTarget_ReturnsProcessPath()` - Tests process-level PATH reading
- `GetDirectories_SplitsByPathSeparator()` - Tests correct parsing with platform-specific separators
- `GetDirectories_HandlesEmptyPath()` - Tests edge cases with empty PATH values
- `GetTargetDisplayName_ReturnsCorrectNames()` - Tests display name mapping

### Writing New Tests

To add new tests:

1. Create test files in the appropriate folder under `ManagePath.Tests/`
2. Use the `[Fact]` attribute for simple tests
3. Use the `[Theory]` and `[InlineData]` attributes for parameterized tests
4. Follow the Arrange-Act-Assert pattern

Example:

```csharp
[Fact]
public void MyTest_WithCondition_ReturnsExpectedResult()
{
    // Arrange: Set up test data
    var service = new MyService();
    
    // Act: Execute the method being tested
    var result = service.MyMethod();
    
    // Assert: Verify the result
    Assert.NotNull(result);
    Assert.Equal(expectedValue, result);
}
```

### Test Coverage

The tests cover:
- ✅ Environment variable reading from different targets (Process, User, Machine)
- ✅ PATH parsing with platform-specific separators
- ✅ Edge cases (empty PATH, whitespace handling)
- ✅ Display name formatting
- 🔄 More tests planned for PathValidator and PathFormatter (see Roadmap)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Setup

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with [System.CommandLine](https://github.com/dotnet/command-line-api) for command-line parsing
- Uses .NET 9.0 features for cross-platform compatibility

## Roadmap

- [ ] Add ability to modify PATH (add/remove directories)
- [ ] Export PATH to different formats (JSON, CSV, XML)
- [ ] Compare PATH across different targets
- [ ] Detect duplicate entries
- [ ] Suggest PATH cleanup and optimization

## Support

If you encounter any issues or have questions, please [open an issue](https://github.com/elderdo/ManagePath/issues) on GitHub.

## Author

### Douglas Elder

- GitHub: [@elderdo](https://github.com/elderdo)
