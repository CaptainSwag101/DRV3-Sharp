using System;
using System.Collections.Generic;
using System.Text;
using DRV3_Sharp_Library.Formats.Archive.SPC;
using Microsoft.Extensions.Primitives;

namespace DRV3_Sharp.Menus;

internal sealed class SpcDetailedOperationsMenu : ISelectableMenu
{
    public string HeaderText => "You can choose from the following options:";
    private (string Name, SpcData Data) loadedData { get; }
    public int FocusedEntry { get; set; }
    public SortedSet<int> SelectedEntries { get; }

    public SpcDetailedOperationsMenu((string Name, SpcData Data) incomingFile)
    {
        loadedData = incomingFile;
        SelectedEntries = new();
    }

    public MenuEntry[] AvailableEntries => new MenuEntry[]
    {
        new("List Files", "List information about the contents of this SPC archive.", ListFiles),
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

    private void ManipulateFiles()
    {
        Program.PushMenu(new SpcFileSelectionMenu(loadedData.Data));
    }

    private void Save()
    {
        
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}