using System.IO;
using CriFsV2Lib;

namespace DRV3_Sharp.Menus;

internal sealed class CpkMenu : IMenu
{
    public string HeaderText => "You can choose from the following options:";
    public int FocusedEntry { get; set; }

    public MenuEntry[] AvailableEntries => new MenuEntry[]
    {
        new("Extract CPK", "Extract the entire contents of a specified CPK archive.", Extract),
        new("Help", "View descriptions of currently-available operations.", Help),
        new("Back", "Return to the previous menu.", Program.PopMenu)
    };

    private void Extract()
    {
        var cpkPaths = Utils.ParsePathsFromConsole("Type the paths of the CPK files you want to extract, or drag-and-drop them onto this window, separated by spaces and/or quotes: ", true, false);
        if (cpkPaths is null) return;

        // For each CPK provided, extract its entire contents.
        foreach (var path in cpkPaths)
        {
            if (path is not FileInfo cpkInfo || cpkInfo.Extension.ToLowerInvariant() != ".cpk") continue;

            using FileStream cpkStream = new(cpkInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var cpkReader = CriFsLib.Instance.CreateCpkReader(cpkStream, false);
            
            // Get the contents of the CPK and extract them.
            var innerFiles = cpkReader.GetFiles();
            foreach (var file in innerFiles)
            {
                // Place the file in the same location as the parent CPK.
                string outputPath = cpkInfo.DirectoryName!;
                if (file.Directory is not null)
                {
                    outputPath = Path.Combine(outputPath, file.Directory);
                    Directory.CreateDirectory(outputPath);
                }
                outputPath = Path.Combine(outputPath, file.FileName);
                
                // Write out the file data to the appropriate path.
                var data = cpkReader.ExtractFile(file);
                FileStream outStream = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                outStream.Write(data.Span);
                outStream.Flush();
                outStream.Close();
            }
        }
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}