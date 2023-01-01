using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                new("Import", "Batch import a folder of exported scripts and generate archives for WRD scripts and STX text.", Import),
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
        string outputFolderName = fileInfo.FullName.Replace(fileInfo.Extension, "");
        Directory.CreateDirectory(outputFolderName);
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

            string outputPath = outputFolderName + Path.DirectorySeparatorChar + name + "_wrdExport.txt";
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
                        commandSegments.Add(stx.Tables[0].Strings[stringNum]);
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
                commandBuilder.Append('>');
                
                // Write the built command text to the output file.
                outputWriter.WriteLine(commandBuilder.ToString());
            }
        }
    }

    private void Import()
    {
        // Get the directory of exported WRDs from the user.
        var paths = Utils.ParsePathsFromConsole("Type the SPC archive containing WRD scripts you wish to load, or drag-and-drop it onto this window: ", true, true);
        if (paths?[0] is not DirectoryInfo directoryInfo)
        {
            Console.WriteLine("Unable to find the path specified. Please ensure you provided a valid folder to import. Press ENTER to continue...");
            Console.ReadLine();
            return;
        }

        // Determine if/which files are actually exported WRDs.
        FileInfo[] contents = directoryInfo.GetFiles();
        List<FileInfo> txtFiles = (from file in contents where file.Extension == ".txt" select file).ToList();
        if (txtFiles.Count == 0)
        {
            Console.WriteLine("The specified folder does not contain any exported WRD TXT files. It must be a directory containing exported WRD scripts in TXT form.");
            Console.WriteLine("Press ENTER to continue...");
            Console.ReadLine();
            return;
        }

        // Prompt the user whether they want to use internal strings or not.
        bool useInternalStrings = false;
        Console.Write("Would you like to put dialogue strings directly inside the WRD file? Choose 'N' unless you are sure (y/N): ");
        var response = Console.ReadLine()?.ToLowerInvariant();
        if (response is not null && response == "y") useInternalStrings = true;
        
        // Load and parse the files we determined in the last step.
        List<(string Name, WrdData Wrd, StxData? Stx)> parsedData = new();
        foreach (var txt in txtFiles)
        {
            string[] lines = File.ReadAllLines(txt.FullName);

            List<WrdCommand> commands = new();
            List<string> dialogue = new();

            // Strip leading and trailing arrow brackets.
            for (var lineNum = 0; lineNum < lines.Length; ++lineNum)
            {
                lines[lineNum] = lines[lineNum].Trim('<').Trim('>');

                string[] splitLine = lines[lineNum].Split();

                if (splitLine.Length == 0) continue;

                string opName = splitLine[0];
                
                // Determine the parameter count and type based on the opcode.
                WrdCommand command;
                if (opName == "LOC")
                {
                    // Parse text lines specially.
                    StringBuilder sb = new();
                    sb.AppendJoin(' ', splitLine[1..splitLine.Length]);
                    command = new(opName, new(){ dialogue.Count.ToString() });
                    dialogue.Add(sb.ToString());
                }
                else
                {
                    command = new(opName, splitLine[1..splitLine.Length].ToList());
                }
                commands.Add(command);
            }

            string genericName = txt.Name.Replace("_wrdExport.txt", "");
            if (useInternalStrings)
            {
                WrdData wrd = new(commands, dialogue);
                parsedData.Add((genericName, wrd, null));
            }
            else
            {
                WrdData wrd = new(commands, null);
                StxData stx = new(new StringTable[] { new(0, dialogue.ToArray()) });
                parsedData.Add((genericName, wrd, stx));
            }
        }

        // Generate the SPC file(s).
        FileInfo wrdSpcFileInfo = new(directoryInfo.FullName + ".SPC");
        FileInfo? textSpcFileInfo = useInternalStrings ? GenerateTextSpcFileInfo(wrdSpcFileInfo) : null;
        
        // TODO
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