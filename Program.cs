using System.CommandLine;
using ManagePath;
6
\\
var rootCommand = new RootCommand("Manage PATH environment variable");
var pathCommand = new Command("path", "Manage the PATH environment variable");

var targetOption = new Option<EnvironmentVariableTarget>("--target", "-t")
{
    Description = "Specify the target environment variable to manage (Process, User, Machine).",
    DefaultValueFactory = parseResult => EnvironmentVariableTarget.Process
};

var numberOption = new Option<bool>("--number", "-n")
{
    Description = "Directories are numbered in the output.",
    DefaultValueFactory = parseResult => false
};

var validateOption = new Option<bool>("--validate", "-v")
{
    Description = "Validate that each directory exists and contains at least one executable file.",
    DefaultValueFactory = parseResult => false
};

var effectiveOption = new Option<bool>("--effective", "-e")
{
    Description = "Show the effective PATH considering all environment variable levels: Process, User, and Machine. This option overrides the --target option.",
    DefaultValueFactory = parseResult => false
};

rootCommand.Add(pathCommand);

var listCommand = new Command("list", "List the directories in the PATH environment variable")
{
    targetOption,
    effectiveOption,
    numberOption,
    validateOption
};

pathCommand.Add(listCommand);

listCommand.SetAction(parseResult =>
{
    bool effective = parseResult.GetValue(effectiveOption);
    bool showNumbers = parseResult.GetValue(numberOption);
    bool showValidation = parseResult.GetValue(validateOption);
    EnvironmentVariableTarget? target = effective ? null : parseResult.GetValue(targetOption);

    var pathService = new PathService();
    var formatter = new PathFormatter();

    string[] directories = pathService.GetDirectories(target);

    if (showValidation)
    {
        var validator = new PathValidator();
        var validatedEntries = validator.ValidateMany(directories);
        formatter.Display(validatedEntries, showNumbers, showValidation: true);
    }
    else
    {
        formatter.DisplaySimple(directories, showNumbers);
    }
});

var parseResult = rootCommand.Parse(args);
return parseResult.Invoke();