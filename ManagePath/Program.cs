// Program.cs uses global usings defined in GlobalUsings.cs

// This file is the starting point of the application.
//
// This app is a command-line tool that helps inspect the PATH environment variable.
// PATH is a list of folders that the operating system searches when you type a command
// such as `dotnet`, `git`, or `pwsh`.
//
// The app can:
// - list PATH folders
// - choose which PATH scope to read
// - show the effective PATH seen by the current process
// - number the results
// - validate whether folders exist and contain executable files

// Create the top-level command for the app.
var rootCommand = new RootCommand("Manage PATH environment variable");

// Create a command group named "path".
// Users will run commands like: `path list`
var pathCommand = new Command("path", "Manage the PATH environment variable");

// Let the user choose which PATH scope to read from.
// Process = current app only
// User    = current signed-in user
// Machine = whole computer
var targetOption = new Option<EnvironmentVariableTarget>("--target", "-t")
{
    Description = "Specify the target environment variable to manage (Process, User, Machine).",
    DefaultValueFactory = parseResult => EnvironmentVariableTarget.Process
};

// Show numbers next to each directory.
var numberOption = new Option<bool>("--number", "-n")
{
    Description = "Directories are numbered in the output.",
    DefaultValueFactory = parseResult => false
};

// Check whether each directory exists and contains executable files.
var validateOption = new Option<bool>("--validate", "-v")
{
    Description = "Validate that each directory exists and contains at least one executable file.",
    DefaultValueFactory = parseResult => false
};

// Show the combined PATH that the current process actually sees.
// If this is used, it takes priority over --target.
var effectiveOption = new Option<bool>("--effective", "-e")
{
    Description = "Show the effective PATH considering all environment variable levels: Process, User, and Machine. This option overrides the --target option.",
    DefaultValueFactory = parseResult => false
};

// Register the "path" command under the root command.
rootCommand.Add(pathCommand);

// Create the `path list` command and attach its options.
var listCommand = new Command("list", "List the directories in the PATH environment variable")
{
    targetOption,
    effectiveOption,
    numberOption,
    validateOption
};

// Register `list` under `path`.
pathCommand.Add(listCommand);

// Define what happens when the user runs `path list`.
listCommand.SetAction(parseResult =>
{
    // Read the option values chosen by the user.
    bool effective = parseResult.GetValue(effectiveOption);
    bool showNumbers = parseResult.GetValue(numberOption);
    bool showValidation = parseResult.GetValue(validateOption);

    // If --effective is used, pass null so the service reads the merged PATH.
    EnvironmentVariableTarget? target = effective ? null : parseResult.GetValue(targetOption);

    // PathService reads PATH entries.
    // PathFormatter prints results in a readable way.
    var pathService = new PathService();
    var formatter = new PathFormatter();

    // Get the PATH directories as strings.
    string[] directories = pathService.GetDirectories(target);

    if (showValidation)
    {
        // PathValidator checks whether each directory is useful.
        var validator = new PathValidator();

        // Convert plain strings into PathEntry objects that include validation results.
        var validatedEntries = validator.ValidateMany(directories);

        // Print the detailed output.
        formatter.Display(validatedEntries, showNumbers, showValidation: true);
    }
    else
    {
        // Print a simple list if validation is not requested.
        formatter.DisplaySimple(directories, showNumbers);
    }
});

// Parse the command-line arguments.
var parseResult = rootCommand.Parse(args);

// Run the selected command and return its exit code.
return parseResult.Invoke();