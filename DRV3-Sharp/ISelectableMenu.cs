using System.Collections.Generic;

namespace DRV3_Sharp;

internal interface ISelectableMenu : IMenu
{
    public SortedSet<int> SelectedEntries { get; }
}