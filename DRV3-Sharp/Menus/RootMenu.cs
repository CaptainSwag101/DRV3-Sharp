using System;
using System.Collections.Generic;

namespace DRV3_Sharp.Menus;

internal sealed class RootMenu : IMenu
{
    public MenuEntry[] AvailableEntries => new MenuEntry[]
    {
        new("Select Initial Context", "Select which kind of file you wish to work with.", SelectContext),
        new("Help", "View descriptions of currently-available operations.", Help),
        new("Exit", "Exits the program.", Program.PopMenu)
    };
    
    public int HighlightedEntry { get; set; }

    private void SelectContext()
    {
        Program.PushMenu(new SelectContextMenu());
    }
    
    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}