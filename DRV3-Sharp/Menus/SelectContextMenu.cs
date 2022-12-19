using System;
using System.Collections.Generic;

namespace DRV3_Sharp.Menus;

internal sealed class SelectContextMenu : IMenu
{
    public MenuEntry[] AvailableEntries => new MenuEntry[]
    {
        new("SPC", "The primary data archive format, used to store most files in the game.", SPC),
        new("WRD", "The primary script format, used to control game behavior, map transitions, text displays, and more.", WRD),
        new("Selectable Menu Test", "Opens a test menu for debugging selectable menu entries.", SelectableMenu),
        new("Help", "View descriptions of currently-available operations.", Help),
        new("Back", "Return to the previous menu.", Program.PopMenu)
    };
    
    public int FocusedEntry { get; set; }

    private void SPC()
    {
        Program.PushMenu(new SpcMenu());
    }

    private void WRD()
    {
        Program.PushMenu(new WrdMenu());
    }

    private void SelectableMenu()
    {
        Program.PushMenu(new SelectableMenuTest());
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}