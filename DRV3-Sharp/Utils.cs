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

        public static void DisplayDescriptiveList(List<(string Name, string Description)> list)
        {
            foreach (var (Name, Description) in list)
            {
                // Preserve original foreground color in the case of a custom-themed terminal
                ConsoleColor origForeground = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(Name);
                Console.ForegroundColor = origForeground;
                Console.WriteLine(Description);
            }
        }
    }
}
