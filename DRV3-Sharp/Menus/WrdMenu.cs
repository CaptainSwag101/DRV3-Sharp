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
    
    public int FocusedEntry { get; set; }

    private void Load()
    {
        var paths = Utils.ParsePathsFromConsole("Type the file you wish to load, or drag-and-drop it onto this window: ", true, true);

        if (paths is null) return;

        if (paths[0] is not FileInfo fileInfo) return;
        
        // TODO: Check with the user if there are existing loaded files before clearing the list
        loadedData = null;
        
        using FileStream fs = new(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        WrdSerializer.Deserialize(fs, out WrdData data);
        loadedData = data;
            
        Console.WriteLine($"Loaded the WRD file successfully.");
        foreach (var command in loadedData.Commands)
        {
            Console.WriteLine(command);
        }
        Console.WriteLine("Press ENTER to continue...");
        Console.ReadLine();
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}