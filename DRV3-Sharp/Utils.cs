using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DRV3_Sharp;

internal static class Utils
{
    public static FileSystemInfo[]? ParsePathsFromConsole(string? promptMessage, bool mustExist, bool canBeDirectory)
    {
        if (promptMessage is not null) Console.WriteLine(promptMessage);

        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input)) return null;
        
        // Use regex to match quoted paths and then split any unquoted paths by space.
        // For a breakdown of this regex: https://regex101.com/r/tqKFpz/2
        Regex pathRegex = new(@"""(.*?)""|'(.*?)'", RegexOptions.Singleline);
        var matches = pathRegex.Matches(input);
        
        // Add the quoted matches to the list of strings
        List<string> foundPaths = new();
        foreach (Match m in matches)
        {
            if (string.IsNullOrWhiteSpace(m.Value)) continue;
            foundPaths.Add(m.Value);
        }
        
        // Now that we've found all quoted matches, remove them from the string
        // to isolate any remaining unquoted paths.
        foreach (string s in foundPaths)
        {
            input = input.Replace(s, "");
        }
        
        // Trim any leading or trailing whitespace, then add
        // all remaining string segments, split by spaces.
        input = input.Trim(' ');
        foundPaths.AddRange(input.Split());
        
        // Iterate through all matches
        List<FileSystemInfo> results = new();
        foreach (string s in foundPaths)
        {
            // Trim single- or double-quotation marks, if any
            string path = s.Trim('"').Trim('\'');
            
            if (string.IsNullOrWhiteSpace(s)) continue;

            try
            {
                FileInfo fi = new(path);
                DirectoryInfo di = new(path);

                // If the file or directory matches our requirements, add it to the results
                if (fi.Exists || !mustExist) results.Add(fi);
                else if (canBeDirectory && di.Exists) results.Add(di);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while trying to parse one of the path(s) provided: {ex}");
                PromptForEnterKey();
            }
        }

        // Return null if we ended up with zero results, otherwise return the results
        return results.Count == 0 ? null : results.ToArray();
    }

    public static void PromptForEnterKey(bool startOnNewLine = true)
    {
        if (startOnNewLine) Console.WriteLine();
        else Console.Write(' ');
        
        Console.WriteLine("Press ENTER to continue...");
        Console.ReadLine();
    }

    public static void PrintMenuDescriptions(IEnumerable<MenuEntry> entries)
    {
        Console.Clear();
        foreach (var entry in entries)
        {
            // Preserve original foreground color in the case of a custom-themed terminal
            var origForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(entry.Name);
            Console.ForegroundColor = origForeground;
            Console.WriteLine($"\t{entry.Description}");
        }
        
        PromptForEnterKey();
    }
}
