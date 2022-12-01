using System;
using System.Collections.Generic;
using System.IO;
using DRV3_Sharp_Library.Formats.Archive.SPC;
using Microsoft.VisualBasic;

namespace DRV3_Sharp.Menus;

internal sealed class SpcMenu : IMenu
{
    private List<SpcData> loadedData = new();

    public MenuEntry[] AvailableEntries
    {
        get
        {
            var entries = new List<MenuEntry>()
            {
                // Add always-available entries
                new("Load", "Load an SPC file, or whole folder of SPC files.", Load),
                new("Help", "View descriptions of currently-available operations.", Help),
                new("Back", "Return to the previous menu.", Program.PopMenu)
            };
            
            // Add context-sensitive entries, if applicable
            if (loadedData.Count > 0)
            {
                entries.Insert(2, new("Save", "Save the currently-loaded SPC file(s).", Save));
            }

            return entries.ToArray();
        }
    }

    private void Load()
    {
        FileInfo? info = Utils.GetPathFromUser("Type the file/directory you wish to load, or drag-and-drop it onto this window: ", true, true);

        if (info is null) return;
        
        // If the path is a directory, load all SPC files within it
        if (info.Attributes.HasFlag(FileAttribute.Directory))
        {
            string[] contents = Directory.GetFiles(info.FullName, "*.spc");

            foreach (var file in contents)
            {
                
            }
        }
        else
        {
            
        }
    }

    private void Save()
    {
        
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}