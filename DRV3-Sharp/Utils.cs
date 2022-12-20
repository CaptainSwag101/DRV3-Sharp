using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DRV3_Sharp;

internal static class Utils
{
    public static FileSystemInfo[]? ParsePathsFromConsole(string? promptMessage, bool mustExist, bool canBeDirectory)
    {
        if (promptMessage is not null)
            Console.WriteLine(promptMessage);

        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("The specified path is null.");
            Console.WriteLine("Press any key to continue...");
            _ = Console.ReadKey(true);
            return null;
        }
        
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
        
        // Now that we've found all quoted matches, remove them from the string to isolate any remaining unquoted paths.
        foreach (string s in foundPaths)
        {
            input = input.Replace(s, "");
        }
        
        // Trim any leading or trailing whitespace, then add all remaining string segments, split by spaces.
        input.Trim(' ');
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

                if (fi.Exists || !mustExist)
                {
                    results.Add(fi);
                }
                else if (canBeDirectory && di.Exists)
                {
                    results.Add(di);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while trying to parse one of the path(s) provided: {ex}");
            }
        }

        return results.ToArray();
    }

    [Obsolete]
    public static DirectoryInfo? GetEnclosingDirectory(string filePath, bool mustExist = true)
    {
        FileInfo fi = new(filePath);

        if (!fi.Exists && mustExist)
        {
            Console.WriteLine("The specified file does not exist; unable to get enclosing directory path.");
            return null;
        }

        return fi.Directory;
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
        Console.WriteLine("Press ENTER to return to the menu...");
        Console.ReadLine();
    }
}
