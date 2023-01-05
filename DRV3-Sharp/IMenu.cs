using System;

namespace DRV3_Sharp;

internal interface IMenu
{
    public string HeaderText { get; }   // The text displayed in the top bar of the menu above the menu entries
    public MenuEntry[] AvailableEntries { get; }    // This is dynamic based on the current state of the current context
    public int FocusedEntry { get; set; }   // Which entry on the list is currently the center of attention?
}

internal sealed record MenuEntry(string Name, string Description, Action Operation);
