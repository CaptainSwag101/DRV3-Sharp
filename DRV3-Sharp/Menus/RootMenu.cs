using System;
using System.Collections.Generic;

namespace DRV3_Sharp.Menus;

internal sealed class RootMenu : IMenu
{
    public List<MenuEntry> AvailableEntries =>
        new()
        {
            new("Help", "View descriptions of currently-available operations.", Help),
            new("Exit", "Exits the program.", Exit)
        };

    private void Help()
    {
        Console.Clear();
        Utils.PrintMenuDescriptions(AvailableEntries);
        Console.WriteLine("Press ENTER to return to the menu...");
        Console.ReadLine();
    }
    
    private void Exit()
    {
        Program.PopMenu();
    }
}