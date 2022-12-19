using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
        // For a breakdown of this regex: https://regex101.com/r/zy7rY2/1
        Regex pathRegex = new(@"""(?<quoted>.*?)""|(?<unquoted>\w.*)", RegexOptions.Multiline);
        var matches = pathRegex.Matches(input);
        
        // Iterate through all matches and process based on quoted or unquoted
        List<FileSystemInfo> foundPaths = new();
        foreach (Match m in matches)
        {
            if (string.IsNullOrWhiteSpace(m.Value)) continue;
            
            // Trim the whitespace if it's a quoted string
            string path = m.Value;
            if (m.Groups["quoted"].Success)
            {
                path = path.Trim('"');
            }

            FileInfo fi = new(path);
            DirectoryInfo di = new(path);

            if (fi.Exists || !mustExist)
            {
                foundPaths.Add(fi);
            }
            else if (canBeDirectory && di.Exists)
            {
                foundPaths.Add(di);
            }
        }

        return foundPaths.ToArray();
    }

    public static string? GetEnclosingDirectory(string filePath, bool bypassExistenceCheck = false)
    {
        FileInfo fi = new(filePath);

        if (!bypassExistenceCheck && !fi.Exists)
        {
            Console.WriteLine("The specified file does not exist; unable to get enclosing directory path.");
            return null;
        }

        return fi.DirectoryName;
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
