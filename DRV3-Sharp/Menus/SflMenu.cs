using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DRV3_Sharp_Library.Formats.Data.SFL;

namespace DRV3_Sharp.Menus;

internal sealed class SflMenu : IMenu
{
    public string HeaderText => "You can choose from the following options:";
    public int FocusedEntry { get; set; }

    public MenuEntry[] AvailableEntries => new MenuEntry[]
    {
        new("SFL to JSON", "Convert SFL file(s) to JSON format for editing.", ToJson),
        new("(Unfinished) JSON to SFL", "Experimental! Convert JSON file(s) to SFL format for use in the game.", ToSfl),
        new("Help", "View descriptions of currently-available operations.", Help),
        new("Back", "Return to the previous menu.", Program.PopMenu)
    };

    private static void ToJson()
    {
        var paths = Utils.ParsePathsFromConsole("Type the files/directories of SFL files you want to convert, or drag-and-drop them onto this window, separated by spaces and/or quotes: ", true, true);
        if (paths is null) return;

        // Load data
        List<(string name, SflData data)> loadedData = new();
        foreach (var info in paths)
        {
            // If the path is a directory, load all SPC files within it
            if (info is DirectoryInfo directoryInfo)
            {
                var contents = directoryInfo.GetFiles("*.sfl");

                foreach (var file in contents)
                {
                    using FileStream fs = new(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    
                    SflData output;
                    try
                    {
                        SflSerializer.Deserialize(fs, out output);
                    }
                    catch (InvalidDataException)
                    {
                        continue;
                    }
                
                    loadedData.Add((file.FullName, output));
                }
            }
            else if (info is FileInfo)
            {
                using FileStream fs = new(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                
                SflData output;
                try
                {
                    SflSerializer.Deserialize(fs, out output);
                }
                catch (InvalidDataException)
                {
                    continue;
                }
                
                loadedData.Add((info.FullName, output));
            }
        }
        
        // Print an error if we didn't actually find any valid STX data from the provided paths.
        if (loadedData.Count == 0)
        {
            Console.Write("Unable to load any valid STX data from the paths provided. Please ensure the files/directories exist.");
            Utils.PromptForEnterKey();
            return;
        }
        
        
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        foreach ((string name, SflData stx) in loadedData)
        {
            string output = JsonSerializer.Serialize(stx, options);

            using StreamWriter writer = new(new FileStream(name + ".json", FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8);
            writer.Write(output);
            writer.Flush();
            writer.Dispose();
        }
        
        Console.Write($"Converted {loadedData.Count} SFL file(s) to JSON.");
        Utils.PromptForEnterKey(false);
    }
    
    private static void ToSfl()
    {
        
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}