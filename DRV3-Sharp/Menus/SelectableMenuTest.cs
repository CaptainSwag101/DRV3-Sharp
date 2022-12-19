using System.Collections.Generic;

namespace DRV3_Sharp.Menus;

internal sealed class SelectableMenuTest : ISelectableMenu
{
    public SelectableMenuTest()
    {
        SelectedEntries = new();
    }
    
    public MenuEntry[] AvailableEntries => new MenuEntry[]
    {
#pragma warning disable CS8625
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
        new("Test", "", null),
#pragma warning restore CS8625
    };
    
    public int FocusedEntry { get; set; }
    
    public SortedSet<int> SelectedEntries { get; }
}