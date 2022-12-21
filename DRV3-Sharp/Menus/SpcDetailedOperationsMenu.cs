using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DRV3_Sharp_Library.Formats.Archive.SPC;
using Microsoft.Extensions.Primitives;

namespace DRV3_Sharp.Menus;

internal sealed class SpcDetailedOperationsMenu : ISelectableMenu
{
    public string HeaderText => "You can choose from the following options:";
    private (string Path, SpcData Data) loadedData { get; }
    public int FocusedEntry { get; set; }
    public SortedSet<int> SelectedEntries { get; }

    public SpcDetailedOperationsMenu((string Path, SpcData Data) incomingFile)
    {
        loadedData = incomingFile;
        SelectedEntries = new();
    }

    public MenuEntry[] AvailableEntries => new MenuEntry[]
    {
        new("List Files", "List information about the contents of this SPC archive.", ListFiles),
        new("Add Files", "Add new files to the SPC archive.", AddFiles),
        new("Manipulate Files", "Select one or more archived files to manipulate in more detail.", ManipulateFiles),
        new("Save", "Saves the SPC archive to a file. If one does not exist, it will be created.", Save),
        new("Help", "View descriptions of currently-available operations.", Help),
        new("Back", "Return to the previous menu.", Program.PopMenu)
    };
    
    private void ListFiles()
    {
        Console.Clear();
        
        foreach (var file in loadedData.Data.Files)
        {
            var sb = new StringBuilder();
            sb.Append($"{file.Name}, ".PadRight(52));
            sb.Append($"{(decimal)file.OriginalSize / 1000} KB, ".PadRight(20));
            sb.Append($"{file.UnknownFlag}, ");
            sb.Append($"{(file.IsCompressed ? "C" : "U")}");
            string truncatedFileInfo = sb.ToString();
            truncatedFileInfo = truncatedFileInfo[..Math.Min(Console.WindowWidth, truncatedFileInfo.Length)];
            Console.WriteLine(truncatedFileInfo);
        }
        
        Console.WriteLine("Press ENTER to continue...");
        Console.ReadLine();
    }

    private void AddFiles()
    {
        var files = Utils.ParsePathsFromConsole("Type the files you wish to load, or drag-and-drop them onto this window, separated by spaces and/or quotes: ", true, false);
        if (files is null || files.Length == 0)
        {
            Console.WriteLine("Unable to load any files from the provided path(s). Please ensure the files exist.\nPress ENTER to continue...");
            Console.ReadLine();
            return;
        }

        bool someSuccess = false;
        foreach (var file in files)
        {
            // Verify that a file with the same name does not already exist in the archive.
            if (loadedData.Data.Files.Any(f => f.Name == file.Name))
            {
                Console.WriteLine($"Skipping file {file.Name}: A file with the same name already exists. If you wish to replace its data, please manipulate the existing file.");
                continue;
            }
            
            // Load the data but do not compress it, that will be done when saving to save on performance.
            var data = File.ReadAllBytes(file.FullName);
            loadedData.Data.Files.Add(new(file.Name, data, 4, false, data.Length));
            someSuccess = true;
        }
        
        if (someSuccess) Console.Write($"Added files to the archive, not yet saved or compressed.");
        Console.WriteLine("Press ENTER to continue...");
        Console.ReadLine();
    }

    private void ManipulateFiles()
    {
        Program.PushMenu(new SpcFileSelectionMenu(loadedData.Data));
    }

    private void Save()
    {
        Console.WriteLine("Compressing data and saving, please wait...");

        for (int i = 0; i < loadedData.Data.Files.Count; ++i)
        {
            var originalFile = loadedData.Data.Files[i];
            
            // Attempt to compress the file; see if it makes the data smaller.
            if (!originalFile.IsCompressed)
            {
                byte[] compressedData = SpcCompressor.Compress(originalFile.Data);
                if (compressedData.Length < originalFile.Data.Length)
                {
                    loadedData.Data.Files[i] = originalFile with { Data = compressedData, IsCompressed = true };
                }
            }
        }

        using FileStream outStream = new(loadedData.Path, FileMode.Create, FileAccess.Write, FileShare.Read);
        SpcSerializer.Serialize(loadedData.Data, outStream);
        outStream.Flush();
        outStream.Dispose();
        
        Console.WriteLine("Done! Press ENTER to continue...");
        Console.ReadLine();
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}