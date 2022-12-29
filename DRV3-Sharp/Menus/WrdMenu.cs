using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DRV3_Sharp_Library.Formats.Archive.SPC;
using DRV3_Sharp_Library.Formats.Script.WRD;
using DRV3_Sharp_Library.Formats.Text.STX;

namespace DRV3_Sharp.Menus;

internal sealed class WrdMenu : IMenu
{
    //private (string Path, WrdData Data)? loadedData = null;
    public string HeaderText => "You can choose from the following options:";
    public int FocusedEntry { get; set; }
    
    public MenuEntry[] AvailableEntries
    {
        get
        {
            List<MenuEntry> entries = new()
            {
                // Add always-available entries
                new("Peek", "Peek at the command contents of a WRD script.", Load),
                new("Export", "Batch export an SPC archive of WRD scripts, alongside accompanying text data.", Export),
                new("Help", "View descriptions of currently-available operations.", Help),
                new("Back", "Return to the previous menu.", Program.PopMenu)
            };

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
        
        using FileStream fs = new(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        WrdSerializer.Deserialize(fs, out WrdData data);
            
        Console.WriteLine($"Loaded the WRD file successfully.");
        foreach (var command in data.Commands)
        {
            StringBuilder argText = new();
            argText.AppendJoin(' ', command.Arguments);
            Console.WriteLine($"{command.Opcode}:\t{argText}");
        }
        Console.WriteLine("Press ENTER to continue...");
        Console.ReadLine();
    }

    private void Export()
    {
        var paths = Utils.ParsePathsFromConsole("Type the SPC archive containing WRD scripts you wish to load, or drag-and-drop it onto this window: ", true, false);
        if (paths?[0] is not FileInfo fileInfo)
        {
            Console.WriteLine("Unable to find the path specified. Press ENTER to continue...");
            Console.ReadLine();
            return;
        }

        if (fileInfo.Extension.ToLowerInvariant() != ".spc")
        {
            Console.WriteLine("The specified file was not an SPC archive. It must be an SPC archive containing WRD scripts.");
            Console.WriteLine("Press ENTER to continue...");
            Console.ReadLine();
            return;
        }

        FileInfo textSpcFileInfo = GenerateTextSpcFileInfo(fileInfo);
        if (!textSpcFileInfo.Exists)
        {
            Console.WriteLine("A matching text SPC archive was not found, aborting. Press ENTER to continue...");
            Console.ReadLine();
            return;
        }
        
        // Load the WRD archive and the STX archive.
        using FileStream wrdSpcStream = new(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        using FileStream stxSpcStream = new(textSpcFileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        SpcSerializer.Deserialize(wrdSpcStream, out SpcData wrdSpc);
        SpcSerializer.Deserialize(stxSpcStream, out SpcData stxSpc);
        
        // Serialize all WRD and STX files from the two files
        Dictionary<string, WrdData> loadedWrds = new();
        Dictionary<string, StxData> loadedStxs = new();
        foreach (var file in wrdSpc.Files)
        {
            if (!file.Name.EndsWith(".wrd")) continue;
            
            byte[] data = file.IsCompressed ? SpcCompressor.Decompress(file.Data) : file.Data;
            using MemoryStream ms = new(data);
            WrdSerializer.Deserialize(ms, out WrdData wrd);
            loadedWrds.Add(file.Name.Replace(".wrd", ""), wrd);
        }
        foreach (var file in stxSpc.Files)
        {
            if (!file.Name.EndsWith(".stx")) continue;
            
            byte[] data = file.IsCompressed ? SpcCompressor.Decompress(file.Data) : file.Data;
            using MemoryStream ms = new(data);
            StxSerializer.Deserialize(ms, out StxData stx);
            loadedStxs.Add(file.Name.Replace(".stx", ""), stx);
        }
        
        // For each WRD file, generate a text output combining the opcodes in-line with the text, if applicable.
        foreach (var pair in loadedWrds)
        {
            string name = pair.Key;
            WrdData wrd = pair.Value;
            StxData? stx = loadedStxs.ContainsKey(name) ? loadedStxs[name] : null;

            string outputPath = fileInfo.DirectoryName! + Path.DirectorySeparatorChar + name + "_wrdExport.txt";
            using StreamWriter outputWriter = new(new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read));
            
            foreach (var command in wrd.Commands)
            {
                StringBuilder commandBuilder = new("<");
                List<string> commandSegments = new(command.Arguments.Count + 1);
                commandSegments.Add(command.Opcode);
                
                // If the opcode is for text, insert the true text from external or internal strings.
                if (command.Opcode == "LOC")
                {
                    var stringNum = Convert.ToInt16(command.Arguments[0]);
                    if (stx is not null)
                    {
                        StringBuilder segmentJoiner = new();
                        segmentJoiner.AppendJoin("\\n", stx.Tables[0].Strings[stringNum].Segments);
                        commandSegments.Add(segmentJoiner.ToString());
                    }
                    else if (wrd.InternalStrings is not null)
                    {
                        commandSegments.Add(wrd.InternalStrings[stringNum]);
                    }
                    else
                    {
                        commandSegments.Add(command.Arguments[0]);
                    }
                }
                else
                {
                    commandSegments.AddRange(command.Arguments);
                }
                
                commandBuilder.AppendJoin(' ', commandSegments);
                commandBuilder.Append(">");
                
                // Write the built command text to the output file.
                outputWriter.WriteLine(commandBuilder.ToString());
            }
        }
        
        return;
    }

    private FileInfo GenerateTextSpcFileInfo(FileInfo wrdSpcFileInfo)
    {
        // Start with the parent directory
        StringBuilder textSpcBuilder = new(wrdSpcFileInfo.DirectoryName!);
        textSpcBuilder.Append(Path.DirectorySeparatorChar);
        // Split at underscores to remove a trailing region code from some archives.
        List<string> underscoreSplits = new(wrdSpcFileInfo.Name.Split('_'));
        // Only remove the last segment if there is more than one.
        if (underscoreSplits.Count > 1) underscoreSplits.RemoveAt(underscoreSplits.Count - 1);
        textSpcBuilder.AppendJoin('_', underscoreSplits);
        // Add "text_REGION" segment
        textSpcBuilder.Append("_text_US");
        // Add the SPC file extension in the same case as the WRD archive, for case-sensitive OSes.
        textSpcBuilder.Append(wrdSpcFileInfo.Extension);
        // Finally build the output FileInfo based on the resultant string
        return new FileInfo(textSpcBuilder.ToString());
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}