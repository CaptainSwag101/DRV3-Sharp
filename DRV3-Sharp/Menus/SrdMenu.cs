using System;
using System.Collections.Generic;
using System.IO;
using DRV3_Sharp_Library.Formats.Resource.SRD;

namespace DRV3_Sharp.Menus;

internal sealed class SrdMenu : IMenu
{
    private (string srdPath, SrdData data)? loadedData = null;
    public string HeaderText => "You can choose from the following options:";
    public int FocusedEntry { get; set; }

    public MenuEntry[] AvailableEntries
    {
        get
        {
            List<MenuEntry> entries = new()
            {
                // Add always-available entries
                new("Load", "Load an SRD file, pulling data from associated SRDI and SRDV binary files.", Load),
                new("Help", "View descriptions of currently-available operations.", Help),
                new("Back", "Return to the previous menu.", Program.PopMenu)
            };

            if (loadedData is not null)
            {
                // Add loaded-data specific entries
                
            }

            return entries.ToArray();
        }
    }

    private void Load()
    {
        var paths = Utils.ParsePathsFromConsole("Type the file you wish to load, or drag-and-drop it onto this window: ", true, false);

        if (paths?[0] is not FileInfo fileInfo) return;
        
        // TODO: Check with the user if there are existing loaded files before clearing the list
        loadedData = null;
        
        // Determine the expected paths of the accompanying SRDI and SRDV files.
        int lengthNoExtension = (fileInfo.FullName.Length - fileInfo.Extension.Length);
        string noExtension = fileInfo.FullName[..lengthNoExtension];
        FileInfo srdiInfo = new(noExtension + "srdi");
        FileInfo srdvInfo = new(noExtension + "srdv");
        
        // Initialize appropriate FileStreams based on which files exist.
        FileStream fs = new(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        FileStream? srdi = null;
        if (srdiInfo.Exists) srdi = new FileStream(srdiInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        FileStream? srdv = null;
        if (srdvInfo.Exists) srdv = new FileStream(srdvInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        
        // Deserialize the SRD data with available resource streams
        SrdSerializer.Deserialize(fs, srdi, srdv, out SrdData data);
        loadedData = (fileInfo.FullName, data);
            
        Console.WriteLine($"Loaded the SRD file successfully. Press ENTER to continue...");
        Console.ReadLine();
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}