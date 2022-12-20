using System;
using System.Collections.Generic;
using DRV3_Sharp_Library.Formats.Archive.SPC;

namespace DRV3_Sharp.Menus;

internal sealed class SpcDetailedOperationsMenu : ISelectableMenu
{
    private (string Name, SpcData Data) loadedData;

    public SpcDetailedOperationsMenu((string Name, SpcData Data) incomingFile)
    {
        loadedData = incomingFile;
        SelectedEntries = new();
    }

    public MenuEntry[] AvailableEntries => new MenuEntry[]
    {
        new("List Files", "List information about the contents of this SPC archive.", ListFiles),
        new("Help", "View descriptions of currently-available operations.", Help),
        new("Back", "Return to the previous menu.", Program.PopMenu)
    };
    
    public int FocusedEntry { get; set; }
    
    public SortedSet<int> SelectedEntries { get; }
    
    private void ListFiles()
    {
        Console.Clear();
        
        Console.WriteLine($"{loadedData.Name}:");
        foreach (var file in loadedData.Data.Files)
        {
            Console.WriteLine($"\t{file}");
        }
        
        Console.WriteLine("Press ENTER to continue...");
        Console.ReadLine();
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}