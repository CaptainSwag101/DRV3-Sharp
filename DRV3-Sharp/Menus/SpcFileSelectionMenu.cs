using System;
using System.Collections.Generic;
using DRV3_Sharp_Library.Formats.Archive.SPC;

namespace DRV3_Sharp.Menus;

internal sealed class SpcFileSelectionMenu : ISelectableMenu
{
    public string HeaderText => "You can choose from the following options:";
    private SpcData spcReference { get; }
    public int FocusedEntry { get; set; }
    public SortedSet<int> SelectedEntries { get; }

    public SpcFileSelectionMenu(SpcData spc)
    {
        spcReference = spc;
        SelectedEntries = new();
    }

    public MenuEntry[] AvailableEntries
    {
        get
        {
            List<MenuEntry> entries = new();
            entries.Add(new("Back", "", Program.PopMenu));

            // Add entries for each file
            foreach (var file in spcReference.Files)
            {
                string truncatedFileInfo = $"{file.Name}, True Size: {(decimal)file.OriginalSize / 1000} KB, Compressed: {file.IsCompressed}";
                truncatedFileInfo = truncatedFileInfo.Substring(0, Math.Min(Console.WindowWidth - 1, truncatedFileInfo.Length));
                entries.Add(new($"{truncatedFileInfo}", "", ManipulateSelectedFiles));
            }
            
            return entries.ToArray();
        }
    }

    private void ManipulateSelectedFiles()
    {
        Queue<ArchivedFile> filesToManipulate = new();
        foreach (int selection in SelectedEntries)
        {
            // The "Back" option doesn't count, so ignore it and offset all selection indexes by -1.
            if (selection == 0) continue;
            
            filesToManipulate.Enqueue(spcReference.Files[selection - 1]);
        }
        Program.PushMenu(new SpcFileManipulationMenu(spcReference, filesToManipulate));
    }
}