using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp
{
    internal static class Utils
    {
        public static string? GetPathFromUser(string? promptMessage, bool fileMustExist)
        {
            if (promptMessage is not null)
                Console.WriteLine(promptMessage);

            string? path = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine("The specified path is null.");
                Console.WriteLine("Press any key to continue...");
                _ = Console.ReadKey(true);
                return null;
            }

            // Trim leading and trailing quotation marks (which are often added during drag-and-drop)
            path = path.Trim('"');

            FileInfo fi = new(path);

            if (!fi.Exists && fileMustExist)
            {
                Console.WriteLine("The specified path does not exist.");
                return null;
            }

            // Ensure the path isn't a directory, if it exists
            if (fi.Exists && fi.Attributes.HasFlag(FileAttributes.Directory))
            {
                Console.WriteLine("The specified path is a directory.");
                Console.WriteLine("Press any key to continue...");
                _ = Console.ReadKey(true);
                return null;
            }

            return path;
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
            foreach (var entry in entries)
            {
                // Preserve original foreground color in the case of a custom-themed terminal
                ConsoleColor origForeground = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(entry.Name);
                Console.ForegroundColor = origForeground;
                Console.WriteLine($"\t{entry.Description}");
            }
        }
    }
}
