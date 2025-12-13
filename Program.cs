using System;
using System.CommandLine;


RootCommand rootCommand = new("Manage PATH environment variable");
Command pathCommand = new("path", "Manage the PATH environment variable");

Option<EnvironmentVariableTarget> targetOption = new("--target", "-t")
{
    Description = "Specify the target environment variable to manage (Process, User, Machine).",
    DefaultValueFactory = parseResult => EnvironmentVariableTarget.Process
};

Option<bool> numberOption = new("--number", "-n")
{
    Description = "Directories are numbered in the output.",
    DefaultValueFactory = parseResult => false
};
Option<bool> validateOption = new("--validate", "-v")
{
    Description = "Validate that each directory exists and contains at least one executable file.",
    DefaultValueFactory = parseResult => false
};



rootCommand.Subcommands.Add(pathCommand);
Command listCommand = new("list", "List the directories in the PATH environment variable")
{
    targetOption,
    numberOption,
    validateOption
};

pathCommand.Subcommands.Add(listCommand);
listCommand.SetAction(parseResult =>
{
    string[] dirs = ReadPathVariable(
        parseResult.GetValue(targetOption));
    bool number = parseResult.GetValue(numberOption);
    bool validate = parseResult.GetValue(validateOption);
    int cnt = 0;
    int invalidCnt = 0;
    foreach (string dir in dirs)
    {
        if (string.IsNullOrWhiteSpace(dir))
        {
            continue;
        }
        cnt++;
        if (number)
        {
            Console.Write($"{cnt}: ");
        }
        Console.WriteLine(dir);
        if (validate)
        {
            if (System.IO.Directory.Exists(dir))
            {
                string[] exeFiles = System.IO.Directory.GetFiles(dir, "*.exe");
                if (exeFiles.Length > 0)
                {
                    Console.WriteLine("    [Valid]");
                }
                else
                {
                    ++invalidCnt;
                    Console.WriteLine("    [Invalid: No executable files found]");
                }
            }
            else
            {
                ++invalidCnt;
                Console.WriteLine("    [Invalid: Directory does not exist]");
            }
        }
    }
    if (invalidCnt > 0)
    {
        Console.WriteLine($"Total invalid directories: {invalidCnt}");
    }
    if (cnt == 0)
    {
        Console.WriteLine("The PATH environment variable is empty.");
    }
});

ParseResult parseResult = rootCommand.Parse(args);
return parseResult.Invoke();

static string[] ReadPathVariable(EnvironmentVariableTarget? target)
{
    // The name of the environment variable is case-insensitive on Windows,
    // but case-sensitive on Linux/macOS. 
    // Using "PATH" (uppercase) is a common convention across platforms.
    string[] dirs = Array.Empty<string>();

    string? pathVariable = Environment.GetEnvironmentVariable("PATH", target ?? EnvironmentVariableTarget.Process);

    if (pathVariable is null)
    {
        Console.WriteLine($"Unable to get the {target} PATH environment variable");
    }
    else
    {
        dirs = pathVariable.Split(System.IO.Path.PathSeparator);
    }
    return dirs;
}