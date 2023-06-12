namespace DRV3_Sharp.Menus;

internal sealed class SelectFileTypeMenu : IMenu
{
    public string HeaderText => "You can choose from the following file types:";
    public int FocusedEntry { get; set; }
    public MenuEntry[] AvailableEntries => new MenuEntry[]
    {
        new("SPC", "The primary data archive format, used to store most files in the game.", SPC),
        new("CPK", "The outer layer archive format, which stores all the game data.", CPK),
        new("SRD", "The primary resource container format, used to store images, 3D models, fonts, and more.", SRD),
        new("STX", "The primary text format, used to store dialogue.", STX),
        new("WRD", "The primary script format, used to control game behavior, map transitions, text displays, and more.", WRD),
        new("Help", "View descriptions of currently-available operations.", Help),
        new("Back", "Return to the previous menu.", Program.PopMenu)
    };

    private static void SPC()
    {
        Program.PushMenu(new SpcRootMenu());
    }

    private static void CPK()
    {
        Program.PushMenu(new CpkMenu());
    }

    private static void SRD()
    {
        Program.PushMenu(new SrdMenu());
    }

    private static void STX()
    {
        Program.PushMenu(new StxMenu());
    }

    private static void WRD()
    {
        Program.PushMenu(new WrdMenu());
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}