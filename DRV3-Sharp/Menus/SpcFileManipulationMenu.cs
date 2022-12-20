using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DRV3_Sharp_Library.Formats.Archive.SPC;

namespace DRV3_Sharp.Menus;

internal sealed class SpcFileManipulationMenu : IMenu
{
    public string HeaderText => $"Currently manipulating file: {fileQueue.Peek().Name}, {fileQueue.Count - 1} remaining.";
    private SpcData spcReference { get; }
    private Queue<ArchivedFile> fileQueue { get; }
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
            Console.WriteLine("The specified name is invalid. Press ENTER to continue...");
            Console.ReadLine();
            return;
        }

        int index = spcReference.Files.IndexOf(file);
        spcReference.Files[index] = file with { Name = newName };
    }

    private void ReplaceData()
    {
        
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