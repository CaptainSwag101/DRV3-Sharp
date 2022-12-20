using System.Collections.Generic;
using DRV3_Sharp_Library.Formats.Archive.SPC;

namespace DRV3_Sharp.Menus;

internal sealed class SpcFileManipulationMenu : IMenu
{
    public string HeaderText => $"Currently manipulating file: {fileQueue.Peek().Name}, {fileQueue.Count - 1} remaining.";
    private SpcData spcReference { get; }
    private Queue<ArchivedFile> fileQueue { get; }
    public int FocusedEntry { get; set; }

    public SpcFileManipulationMenu(SpcData spc, Queue<ArchivedFile> filesToManipulate)
    {
        spcReference = spc;
        fileQueue = filesToManipulate;
    }
    
    public MenuEntry[] AvailableEntries => new MenuEntry[]
    {
        new("Do Nothing", "Perform no action on the selected file.", DequeueFile),
    };

    private void DequeueFile()
    {
        fileQueue.Dequeue();
        
        // If we've finished manipulating files, pop this menu and the file-selection menu
        // off the program's menu stack.
        if (fileQueue.Count == 0)
        {
            Program.PopMenu();
            Program.PopMenu();
        }
    }
}