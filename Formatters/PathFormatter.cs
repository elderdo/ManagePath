namespace ManagePath.Formatters;

/// <summary>
/// Formats and displays PATH entries to the console.
/// </summary>
public class PathFormatter
{
    /// <summary>
    /// Displays PATH directories with optional numbering and validation results.
    /// </summary>
    /// <param name="entries">Collection of PathEntry objects to display</param>
    /// <param name="showNumbers">Whether to show line numbers</param>
    /// <param name="showValidation">Whether to show validation results</param>
    public void Display(IEnumerable<PathEntry> entries, bool showNumbers, bool showValidation)
    {
        var entryList = entries.ToList();
        int cnt = 0;
        int invalidCnt = 0;

        foreach (var entry in entryList)
        {
            cnt++;

            if (showNumbers)
            {
                Console.Write($"{cnt}: ");
            }

            Console.WriteLine(entry.Directory);

            if (showValidation)
            {
                if (!entry.Exists)
                {
                    invalidCnt++;
                    Console.WriteLine("    [Invalid: Directory does not exist]");
                }
                else if (!entry.HasExecutables)
                {
                    invalidCnt++;
                    Console.WriteLine("    [Invalid: No executable files found]");
                }
                else
                {
                    Console.WriteLine("    [Valid]");
                }
            }
        }

        // Summary
        if (showValidation && invalidCnt > 0)
        {
            Console.WriteLine($"Total invalid directories: {invalidCnt}");
        }

        if (cnt == 0)
        {
            Console.WriteLine("The PATH environment variable is empty.");
        }
    }

    /// <summary>
    /// Displays simple directory list without validation.
    /// </summary>
    /// <param name="directories">Collection of directory paths</param>
    /// <param name="showNumbers">Whether to show line numbers</param>
    public void DisplaySimple(IEnumerable<string> directories, bool showNumbers)
    {
        var entries = directories.Select(d => new PathEntry(d, false, false));
        Display(entries, showNumbers, showValidation: false);
    }
}
