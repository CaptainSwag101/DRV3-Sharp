using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DRV3_Sharp_Library.Formats.Script.WRD;

namespace DRV3_Sharp.Menus;

internal sealed class WrdMenu : IMenu
{
    private (string Path, WrdData Data)? loadedData = null;
    public string HeaderText => "You can choose from the following options:";
    public int FocusedEntry { get; set; }
    
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
        var paths = Utils.ParsePathsFromConsole("Type the file you wish to load, or drag-and-drop it onto this window: ", true, false);
        if (paths?[0] is not FileInfo fileInfo)
        {
            Console.WriteLine("Unable to find the path specified. Press ENTER to continue...");
            Console.ReadLine();
            return;
        }
        
        // TODO: Check with the user if there are existing loaded files before clearing the list
        loadedData = null;
        
        using FileStream fs = new(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        WrdSerializer.Deserialize(fs, out WrdData data);
        loadedData = (fileInfo.FullName, data);
            
        Console.WriteLine($"Loaded the WRD file successfully.");
        foreach (var command in data.Commands)
        {
            StringBuilder argText = new();
            argText.AppendJoin(' ', command.Arguments);
            Console.WriteLine($"{command.Opcode}:\t{argText}");
        }
        Console.WriteLine("Press ENTER to continue...");
        Console.ReadLine();
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}