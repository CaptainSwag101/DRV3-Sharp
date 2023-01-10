using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DRV3_Sharp_Library.Formats.Data.SRD;
using DRV3_Sharp_Library.Formats.Data.SRD.Resources;
using SixLabors.ImageSharp.Formats.Bmp;

namespace DRV3_Sharp.Menus;

internal sealed class SrdMenu : IMenu
{
    private SrdData? loadedData = null;
    private FileInfo? loadedDataInfo = null;
    public string HeaderText => "You can choose from the following options:";
    public int FocusedEntry { get; set; }

    public MenuEntry[] AvailableEntries
    {
        get
        {
            List<MenuEntry> entries = new()
            {
                // Add always-available entries
                new("Load", "Load an SRD file, pulling data from associated SRDI and SRDV binary files.", Load),
                new("Help", "View descriptions of currently-available operations.", Help),
                new("Back", "Return to the previous menu.", Program.PopMenu)
            };

            if (loadedData is not null)
            {
                // Add loaded-data specific entries
                entries.Insert(1, new("Export Textures", "Export all texture resources within the resource data.", ExtractTextures));
            }

            return entries.ToArray();
        }
    }

    private void Load()
    {
        var paths = Utils.ParsePathsFromConsole("Type the file you wish to load, or drag-and-drop it onto this window: ", true, false);
        if (paths?[0] is not FileInfo fileInfo)
        {
            Console.WriteLine("Unable to find the path specified. Press ENTER to continue...");
            Console.ReadLine();
            return;
        }
        
        // TODO: Check with the user if there are existing loaded files before clearing the list
        loadedData = null;
        loadedDataInfo = null;
        
        // Determine the expected paths of the accompanying SRDI and SRDV files.
        int lengthNoExtension = (fileInfo.FullName.Length - fileInfo.Extension.Length);
        string noExtension = fileInfo.FullName[..lengthNoExtension];
        FileInfo srdiInfo = new(noExtension + ".srdi");
        FileInfo srdvInfo = new(noExtension + ".srdv");
        
        // Initialize appropriate FileStreams based on which files exist.
        FileStream fs = new(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        FileStream? srdi = null;
        if (srdiInfo.Exists) srdi = new FileStream(srdiInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        FileStream? srdv = null;
        if (srdvInfo.Exists) srdv = new FileStream(srdvInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        
        // Deserialize the SRD data with available resource streams
        SrdSerializer.Deserialize(fs, srdi, srdv, out SrdData data);
        loadedData = data;
        loadedDataInfo = fileInfo;
            
        Console.WriteLine($"Loaded the SRD file successfully.");
        Utils.PromptForEnterKey(false);
    }

    private async void ExtractTextures()
    {
        if (loadedData is null || loadedDataInfo is null) return;

        var successfulExports = 0;
        foreach (var resource in loadedData!.Resources)
        {
            if (resource is not TextureResource texture) continue;
            
            string outputPath = Path.Combine(loadedDataInfo.DirectoryName!, texture.Name.Replace(".tga", ".bmp"));
            await using FileStream fs = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            await texture.ImageMipmaps[0].SaveAsync(fs, new BmpEncoder());
            ++successfulExports;
        }
        
        Console.WriteLine($"Exported {successfulExports} textures successfully.");
        Utils.PromptForEnterKey(false);
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}