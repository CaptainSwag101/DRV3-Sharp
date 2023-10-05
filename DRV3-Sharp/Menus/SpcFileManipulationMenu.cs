using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DRV3_Sharp_Library.Formats.Archive.SPC;

namespace DRV3_Sharp.Menus;

internal sealed class SpcFileManipulationMenu : IMenu
{
    private SpcData spcReference { get; }
    private Queue<ArchivedFile> fileQueue { get; }
    public string HeaderText => $"Currently manipulating file: {fileQueue.Peek().Name}, {fileQueue.Count - 1} remaining.";
    public int FocusedEntry { get; set; }

    public SpcFileManipulationMenu(SpcData spc, Queue<ArchivedFile> filesToManipulate)
    {
        spcReference = spc;
        fileQueue = filesToManipulate;
    }
    
    public MenuEntry[] AvailableEntries => new MenuEntry[]
    {
        new("Done", "Finish performing actions on the selected file.", DequeueFile),
        new("Rename", "Change the name of the current file.", Rename),
        new("Replace Data", "Overwrite the data for the current file with something else.", ReplaceData),
        new("Delete", "Remove the file from the archive.", Delete),
    };

    private void DequeueFile()
    {
        fileQueue.Dequeue();
        
        // If we've finished manipulating files, pop this menu and the file-selection menu
        // off the program's menu stack.
        if (fileQueue.Count == 0)
        {
            Program.PopMenu();
            Program.PopMenu();
        }
    }

    private void Rename()
    {
        var file = fileQueue.Peek();
        Console.WriteLine($"Enter the new name of the file (Original name: {file.Name})");
        var newName = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(newName) || newName is "." or "..") return;
        
        // Check for invalid path characters
        var invalidChars = Path.GetInvalidFileNameChars();
        if (invalidChars.Any(c => newName.Contains(c)))
        {
            Console.Write("The specified name is invalid.");
            Utils.PromptForEnterKey(false);
            return;
        }

        int index = spcReference.Files.IndexOf(file);
        spcReference.Files[index].Name = newName;
    }

    private void ReplaceData()
    {
        var paths = Utils.ParsePathsFromConsole("Type the file you wish to use as replacement data, or drag-and-drop it onto this window: ", true, false);
        if (paths is null || paths.Length == 0)
        {
            Console.Write("Unable to load any data from the provided path. Please ensure the file exists.");
            Utils.PromptForEnterKey();
            return;
        }

        // Load new data and replace the old
        var info = paths[0];
        byte[] newData = File.ReadAllBytes(info.FullName);
        var file = fileQueue.Peek();
        int index = spcReference.Files.IndexOf(file);
        spcReference.Files[index].Data = newData;   // This auto-compresses the data if possible
    }

    private void Delete()
    {
        Console.Write("Are you sure you want to delete this? (y/N): ");
        var response = Console.ReadLine();
        if (response!.ToLowerInvariant() == "y")
        {
            spcReference.Files.Remove(fileQueue.Peek());
            DequeueFile();
        }
    }
}