using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DRV3_Sharp_Library.Formats.Archive.SPC;

namespace DRV3_Sharp.Menus;

internal sealed class SpcRootMenu : IMenu
{
    public MenuEntry[] AvailableEntries => new MenuEntry[]
    {
        // Add always-available entries
        new("Quick Extract", "Load and extract the contents of one or more SPC archives.", QuickExtract),
        new("Detailed Operations", "Load a single SPC file to operate on more precisely.", DetailedOperations),
        new("Help", "View descriptions of currently-available operations.", Help),
        new("Back", "Return to the previous menu.", Program.PopMenu)
    };
    
    public int FocusedEntry { get; set; }

    private async void QuickExtract()
    {
        var paths = Utils.ParsePathsFromConsole("Type the files/directories of SPC archives you want to extract, or drag-and-drop them onto this window, separated by spaces and/or quotes: ", true, true);
        if (paths is null) return;

        // Load data
        List<(string name, SpcData data)> loadedData = new();
        foreach (var info in paths)
        {
            // If the path is a directory, load all SPC files within it
            if (info is DirectoryInfo directoryInfo)
            {
                var contents = directoryInfo.GetFiles("*.spc");

                foreach (var file in contents)
                {
                    using FileStream fs = new(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    SpcSerializer.Deserialize(fs, out SpcData data);
                    loadedData.Add((file.FullName, data));
                }
            }
            else if (info is FileInfo)
            {
                using FileStream fs = new(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                SpcSerializer.Deserialize(fs, out SpcData data);
                loadedData.Add((info.FullName, data));
            }
        }
        
        // Print an error if we didn't actually find any valid SPC data from the provided paths.
        if (loadedData.Count == 0)
        {
            Console.WriteLine("Unable to load any valid SPC data from the paths provided. Please ensure the files/directories exist.\nPress ENTER to continue...");
            Console.ReadLine();
            return;
        }
        
        // Extract data
        foreach (var (name, data) in loadedData)
        {
            foreach (var file in data.Files)
            {
                string outputDir = name.Remove(name.Length - (".SPC".Length));
                
                // Create output directory if it does not exist
                Directory.CreateDirectory(outputDir);
                await using BinaryWriter writer = new(new FileStream(Path.Combine(outputDir, file.Name), FileMode.Create, FileAccess.ReadWrite, FileShare.Read));

                var fileContents = file.Data;
                if (file.IsCompressed)
                {
                    fileContents = SpcCompressor.Decompress(fileContents);
                }
            
                writer.Write(fileContents);
                writer.Close();
            }
        }
        
        Console.WriteLine($"Extracted contents of {loadedData.Count} SPC file(s). Press ENTER to continue...");
        Console.ReadLine();
    }

    private void DetailedOperations()
    {
        var paths = Utils.ParsePathsFromConsole("Type the file you wish to load, or drag-and-drop it onto this window, separated by spaces and/or quotes: ", true, true);
        if (paths is null) return;

        // Load first non-directory path, then stop and load the detailed menu.
        foreach (var info in paths)
        {
            if (info is FileInfo)
            {
                using FileStream fs = new(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                SpcSerializer.Deserialize(fs, out SpcData data);
                Program.PushMenu(new SpcDetailedOperationsMenu((info.FullName, data)));
                return;
            }
        }
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}