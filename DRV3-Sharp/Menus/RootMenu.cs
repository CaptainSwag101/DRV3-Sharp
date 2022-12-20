using System;
using System.Collections.Generic;
using System.IO;

namespace DRV3_Sharp.Menus;

internal sealed class RootMenu : IMenu
{
    public MenuEntry[] AvailableEntries => new MenuEntry[]
    {
        new("Select Initial Context", "Select which kind of file you wish to work with.", SelectContext),
        new("Help", "View descriptions of currently-available operations.", Help),
        new("About", "Displays information about DRV3-Sharp.", About),
        new("Exit", "Exits the program.", Program.PopMenu)
    };
    
    public int FocusedEntry { get; set; }

    private void SelectContext()
    {
        Program.PushMenu(new SelectContextMenu());
    }
    
    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }

    private void About()
    {
        Console.WriteLine("DRV3-Sharp, by CaptainSwag101");
        try
        {
            var versionInfo = File.ReadAllLines("VERSION_INFO_FOR_DEBUGGING.txt");
            foreach (string s in versionInfo)
            {
                Console.WriteLine(s);
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("The version/build info file that should be included with this software was missing. Consider re-downloading the software or re-building it from source.");
        }
        
        Console.WriteLine("Press ENTER to continue...");
        Console.ReadLine();
    }
}