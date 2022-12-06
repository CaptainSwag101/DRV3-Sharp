using System;
using System.Collections.Generic;
using System.IO;
using DRV3_Sharp_Library.Formats.Script.WRD;

namespace DRV3_Sharp.Menus;

internal sealed class WrdMenu : IMenu
{
    private WrdData? loadedData = null;
    
    public MenuEntry[] AvailableEntries
    {
        get
        {
            List<MenuEntry> entries = new(){
                // Add always-available entries
                new("Load", "Load a WRD script.", Load),
                new("Help", "View descriptions of currently-available operations.", Help),
                new("Back", "Return to the previous menu.", Program.PopMenu)
            };

            return entries.ToArray();
        }
    }

    private void Load()
    {
        FileInfo? info = Utils.GetPathFromUser("Type the file/directory you wish to load, or drag-and-drop it onto this window: ", true, true);

        if (info is null) return;
        
        // TODO: Check with the user if there are existing loaded files before clearing the list
        loadedData = null;
        
        using FileStream fs = new(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        WrdSerializer.Deserialize(fs, out WrdData data);
        loadedData = data;
            
        Console.WriteLine($"Loaded the WRD file successfully.\n{data}\nPress ENTER to continue...");
        Console.ReadLine();
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}