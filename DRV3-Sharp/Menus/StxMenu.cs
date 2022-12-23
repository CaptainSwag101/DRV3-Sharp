using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DRV3_Sharp_Library.Formats.Text.STX;

namespace DRV3_Sharp.Menus;

internal sealed class StxMenu : IMenu
{
    public string HeaderText => "You can choose from the following options:";
    public int FocusedEntry { get; set; }
    
    public MenuEntry[] AvailableEntries => new MenuEntry[]
    {
        new("STX to JSON", "Convert STX file(s) to JSON format for editing.", ToJson),
        new("JSON to STX", "Convert JSON file(s) to STX format for use in the game.", ToStx),
        new("Help", "View descriptions of currently-available operations.", Help),
        new("Back", "Return to the previous menu.", Program.PopMenu)
    };

    private void ToJson()
    {
        var paths = Utils.ParsePathsFromConsole("Type the files/directories of STX files you want to convert, or drag-and-drop them onto this window, separated by spaces and/or quotes: ", true, true);
        if (paths is null) return;

        // Load data
        List<(string name, StxData data)> loadedData = new();
        foreach (var info in paths)
        {
            // If the path is a directory, load all SPC files within it
            if (info is DirectoryInfo directoryInfo)
            {
                var contents = directoryInfo.GetFiles("*.stx");

                foreach (var file in contents)
                {
                    using FileStream fs = new(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    
                    StxData output;
                    try
                    {
                        StxSerializer.Deserialize(fs, out output);
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
                
                StxData output;
                try
                {
                    StxSerializer.Deserialize(fs, out output);
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
            Console.WriteLine("Unable to load any valid STX data from the paths provided. Please ensure the files/directories exist.\nPress ENTER to continue...");
            Console.ReadLine();
            return;
        }
        
        
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        foreach ((string name, StxData stx) in loadedData)
        {
            string output = JsonSerializer.Serialize(stx, options);

            using StreamWriter writer = new(new FileStream(name + ".json", FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8);
            writer.Write(output);
            writer.Flush();
            writer.Dispose();
        }
        
        Console.WriteLine($"Converted {loadedData.Count} STX file(s) to JSON. Press ENTER to continue...");
        Console.ReadLine();
    }
    
    private void ToStx()
    {
        var paths = Utils.ParsePathsFromConsole("Type the files/directories of JSON files you want to convert, or drag-and-drop them onto this window, separated by spaces and/or quotes: ", true, true);
        if (paths is null) return;

        // Load data
        List<(string name, StxData data)> loadedData = new();
        foreach (var info in paths)
        {
            // If the path is a directory, load all SPC files within it
            if (info is DirectoryInfo directoryInfo)
            {
                var contents = directoryInfo.GetFiles("*.json");

                foreach (var file in contents)
                {
                    using FileStream fs = new(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var output = JsonSerializer.Deserialize<StxData>(fs);

                    if (output is null) continue;
                
                    loadedData.Add((file.FullName.Replace(".json", ""), output));
                }
            }
            else if (info is FileInfo)
            {
                using FileStream fs = new(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                var output = JsonSerializer.Deserialize<StxData>(fs);

                if (output is null) continue;
                
                loadedData.Add((info.FullName.Replace(".json", ""), output));
            }
        }
        
        // Print an error if we didn't actually find any valid JSON data from the provided paths.
        if (loadedData.Count == 0)
        {
            Console.WriteLine("Unable to load any valid JSON data from the paths provided. Please ensure the files/directories exist.\nPress ENTER to continue...");
            Console.ReadLine();
            return;
        }

        foreach ((string name, StxData stx) in loadedData)
        {
            using FileStream fs = new(name, FileMode.Create, FileAccess.Write, FileShare.Read);
            
            StxSerializer.Serialize(stx, fs);
            
            fs.Flush();
        }
        
        Console.WriteLine($"Converted {loadedData.Count} JSON file(s) to STX. Press ENTER to continue...");
        Console.ReadLine();
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}