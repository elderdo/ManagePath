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

### Prerequisites

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
```
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
```
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

```
ManagePath/
├── Models/
│   └── PathEntry.cs          # Domain model for PATH entries
├── Services/
│   ├── PathService.cs        # PATH reading and parsing
│   └── PathValidator.cs      # Directory validation logic
├── Formatters/
│   └── PathFormatter.cs      # Console output formatting
├── GlobalUsings.cs           # Global using directives
└── Program.cs                # CLI entry point
```

### Key Components

- **PathEntry** - Immutable record representing a PATH directory with validation state
- **PathService** - Reads PATH from different environment variable targets
- **PathValidator** - Validates directories (existence, executable detection)
- **PathFormatter** - Handles console output with formatting options

## Executable Detection

### Windows
Uses the `PATHEXT` environment variable to determine executable extensions:
- `.COM`, `.EXE`, `.BAT`, `.CMD`, `.VBS`, `.JS`, `.WSF`, `.PS1`, etc.
- Custom extensions like `.PL` are also included

### Linux/macOS
Checks file permissions for execute bits (`UnixFileMode.UserExecute`)

## Building for Distribution

### Windows

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

**Douglas Elder**
- GitHub: [@elderdo](https://github.com/elderdo)
