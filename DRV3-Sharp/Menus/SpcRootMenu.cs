using System;
using System.Collections.Generic;
using System.IO;
using DRV3_Sharp_Library.Formats.Archive.SPC;

namespace DRV3_Sharp.Menus;

internal sealed class SpcRootMenu : IMenu
{
    public string HeaderText => "You can choose from the following options:";
    public int FocusedEntry { get; set; }
    
    public MenuEntry[] AvailableEntries => new MenuEntry[]
    {
        new("Quick Extract", "Load and extract the contents of one or more SPC archives.", QuickExtract),
        new("Detailed Operations", "Load a single SPC file to operate on more precisely.", DetailedOperations),
        new("Help", "View descriptions of currently-available operations.", Help),
        new("Back", "Return to the previous menu.", Program.PopMenu)
    };

    private static async void QuickExtract()
    {
        var paths = Utils.ParsePathsFromConsole("Type the files/directories of SPC archives you want to extract, or drag-and-drop them onto this window, separated by spaces and/or quotes: ", true, true);
        if (paths is null)
        {
            Console.WriteLine("Unable to find the path(s) specified.");
            Utils.PromptForEnterKey(false);
            return;
        }

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
            Console.WriteLine("Unable to load any valid SPC data from the paths provided. Please ensure the files/directories exist.");
            Utils.PromptForEnterKey();
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
        
        Console.Write($"Extracted contents of {loadedData.Count} SPC file(s).");
        Utils.PromptForEnterKey(false);
    }

    private void DetailedOperations()
    {
        var paths = Utils.ParsePathsFromConsole("Type the file/directory you wish to load, or drag-and-drop it onto this window: ", true, true);
        if (paths?.Length == 0)
        {
            Console.Write("Unable to find the path specified.");
            Utils.PromptForEnterKey(false);
            return;
        }

        // Load first path, then stop and load the detailed menu.
        var info = paths?[0];
        if (info is FileInfo)
        {
            // If we're loading a file, open its SPC data.
            using FileStream fs = new(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            SpcSerializer.Deserialize(fs, out SpcData data);
            Console.Write($"Loaded SPC archive {info.Name}.");
            Utils.PromptForEnterKey(false);
            
            Program.PushMenu(new SpcDetailedOperationsMenu((info.FullName, data)));
        }
        else if (info is DirectoryInfo dir)
        {
            // If we're loading a directory, generate a new SPC archive based on its contents.
            List<ArchivedFile> archivedFiles = new();
            var files = dir.GetFiles("*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                // Load the data but do not compress it, that will be done when saving to save on performance.
                var data = File.ReadAllBytes(file.FullName);
                string shortenedName = file.FullName.Replace(dir.FullName + Path.DirectorySeparatorChar, "");
                archivedFiles.Add(new(shortenedName, data, 4, false, data.Length));
            }
            Console.Write($"Loaded the directory {info.Name} as a new SPC archive, not yet saved.");
            Utils.PromptForEnterKey(false);
            
            Program.PushMenu(new SpcDetailedOperationsMenu((dir.FullName + ".spc", new SpcData(0, archivedFiles))));
        }
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}