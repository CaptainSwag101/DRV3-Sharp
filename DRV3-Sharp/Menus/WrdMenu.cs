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
    
    public MenuEntry[] AvailableEntries => new MenuEntry[]
    {
        // Add always-available entries
        new("Peek", "Peek at the command contents of a WRD script.", Peek),
        new("Export", "Batch export an SPC archive of WRD scripts, alongside accompanying text data.", Export),
        new("Import", "Batch import a folder of exported scripts and generate archives for WRD scripts and STX text.", Import),
        new("Help", "View descriptions of currently-available operations.", Help),
        new("Back", "Return to the previous menu.", Program.PopMenu)
    };

    private void Peek()
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
            Console.WriteLine($"{command.Name}:\t{argText}");
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
            Console.WriteLine("A matching text SPC archive was not found, aborting. Press ENTER to attempt reading anyway...");
            Console.ReadLine();
        }
        
        // Load the WRD archive and the STX archive.
        using FileStream wrdSpcStream = new(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        using FileStream? stxSpcStream = textSpcFileInfo.Exists ? new(textSpcFileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
        SpcSerializer.Deserialize(wrdSpcStream, out SpcData wrdSpc);
        SpcData? stxSpc = null;
        if (stxSpcStream is not null) SpcSerializer.Deserialize(stxSpcStream, out stxSpc);
        
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
        if (stxSpc is not null)
        {
            foreach (var file in stxSpc.Files)
            {
                if (!file.Name.EndsWith(".stx")) continue;
            
                byte[] data = file.IsCompressed ? SpcCompressor.Decompress(file.Data) : file.Data;
                using MemoryStream ms = new(data);
                StxSerializer.Deserialize(ms, out StxData stx);
                loadedStxs.Add(file.Name.Replace(".stx", ""), stx);
            }
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
                commandSegments.Add(command.Name);

                for (var i = 0; i < command.Arguments.Count; ++i)
                {
                    ushort argValue = command.Arguments[i];
                    var commandInfo = WrdCommandConstants.CommandInfo[command.Name];
                    string parsedArg;
                    switch (commandInfo.ArgTypes?[i % commandInfo.ArgTypes.Length])
                    {
                        case 0:
                            parsedArg = wrd.Parameters[argValue];
                            break;
                        case 1:
                            parsedArg = argValue.ToString();
                            break;
                        case 2:
                            if (stx is not null)
                            {
                                parsedArg = stx.Tables[0].Strings[argValue];
                            }
                            else if (wrd.InternalStrings is not null)
                            {
                                parsedArg = wrd.InternalStrings[argValue];
                            }
                            else
                            {
                                parsedArg = argValue.ToString();
                            }
                            // Escape newline characters and remove carriage returns.
                            parsedArg = parsedArg.Replace("\r", "").Replace("\n", "\\n");
                            break;
                        case 3:
                            parsedArg = wrd.Labels[argValue];
                            break;
                        default:
                            parsedArg = wrd.Parameters[argValue];
                            break;
                    }
                    
                    // Now add whatever the parsed data is to the command segments
                    commandSegments.Add(parsedArg);
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
            List<string> parameters = new();
            List<string> labelNames = new();

            // Iterate through each line in the file and parse a single WRD command from it.
            foreach (var l in lines)
            {
                // Skip empty lines.
                if (string.IsNullOrWhiteSpace(l)) continue;

                // Strip leading and trailing arrow brackets.
                string line = l;
                if (line.StartsWith('<')) line = line[1..];
                if (line.EndsWith('>')) line = line[..^1];

                string[] splitLine = line.Split();
                string opName = splitLine[0];
                
                // Determine the parameter count and type based on the opcode.
                var info = WrdCommandConstants.CommandInfo[opName];

                if (info is null) throw new InvalidDataException( $"There is no valid WRD command associated with the opcode {opName}.");

                List<ushort> argIndices = new();
                for (var argNum = 0; argNum < (splitLine.Length - 1); ++argNum)
                {
                    // The first index of splitLine is the opcode name, so we skip over that.
                    string argString = splitLine[argNum + 1];
                    
                    // Append to the argIndices list first so that we don't need to subtract 1 from the respective counts each time.
                    switch (info.ArgTypes?[argNum % info.ArgTypes.Length])
                    {
                        case 0:
                            argIndices.Add((ushort)DeduplicateAndAddToList(ref parameters, argString));
                            break;
                        case 1:
                            argIndices.Add(Convert.ToUInt16(argString));
                            break;
                        case 2:
                            // Parse text lines as a whole.
                            StringBuilder sb = new();
                            sb.AppendJoin(' ', splitLine[1..]);
                            string dialogueString = sb.ToString().Replace("\\n", "\n");
                            argIndices.Add((ushort)DeduplicateAndAddToList(ref dialogue, dialogueString));
                            // Forcibly set our iterator to the end to prevent adding a dialogue string for each space in the line.
                            argNum = splitLine.Length;
                            break;
                        case 3:
                            argIndices.Add((ushort)DeduplicateAndAddToList(ref labelNames, argString));
                            break;
                        default:
                            argIndices.Add((ushort)DeduplicateAndAddToList(ref parameters, argString));
                            break;
                    }
                }
                commands.Add(new WrdCommand(opName, argIndices));
            }

            string genericName = txt.Name.Replace("_wrdExport.txt", "");
            if (useInternalStrings)
            {
                WrdData wrd = new(commands, 0, parameters, labelNames, dialogue.Count > 0 ? dialogue : null);
                parsedData.Add((genericName, wrd, null));
            }
            else
            {
                WrdData wrd = new(commands, 0, parameters, labelNames, null);
                StxData? stx = null;
                if (dialogue.Count > 0) stx = new(new StringTable[] { new(0, dialogue.ToArray()) });
                parsedData.Add((genericName, wrd, stx));
            }
        }

        // Generate the SPC file path(s).
        FileInfo wrdSpcFileInfo = new(directoryInfo.FullName + ".SPC");
        FileInfo stxSpcFileInfo = GenerateTextSpcFileInfo(wrdSpcFileInfo);
        
        // Generate the SPC data.
        List<ArchivedFile> wrdSpcContents = new();
        List<ArchivedFile> stxSpcContents = new();
        foreach ((string name, WrdData wrd, StxData? stx) in parsedData)
        {
            using MemoryStream wrdDataStream = new();
            var stringCount = useInternalStrings ? wrd.InternalStrings?.Count : stx?.Tables[0].Strings.Length;
            stringCount ??= 0;
            WrdSerializer.Serialize(wrd, (ushort)stringCount, wrdDataStream);
            // Compress the data while storing the original size.
            byte[] wrdBytes = wrdDataStream.ToArray();
            byte[] wrdBytesCompressed = SpcCompressor.Compress(wrdBytes);
            if (wrdBytesCompressed.Length < wrdBytes.Length)
            {
                wrdSpcContents.Add(new(name + ".wrd", wrdBytesCompressed, 4, true, wrdBytes.Length));
            }
            else
            {
                wrdSpcContents.Add(new(name + ".wrd", wrdBytes, 4, false, wrdBytes.Length));
            }

            if (stx is null || stringCount == 0) continue;

            using MemoryStream stxDataStream = new();
            StxSerializer.Serialize(stx, stxDataStream);
            // Compress the data while storing the original size.
            byte[] stxBytes = stxDataStream.ToArray();
            byte[] stxBytesCompressed = SpcCompressor.Compress(stxBytes);
            if (stxBytesCompressed.Length < stxBytes.Length)
            {
                stxSpcContents.Add(new(name + ".stx", stxBytesCompressed, 4, true, stxBytes.Length));
            }
            else
            {
                stxSpcContents.Add(new(name + ".stx", stxBytes, 4, false, stxBytes.Length));
            }
        }
        
        // Generate the output SPCs.
        SpcData wrdSpc = new(0, wrdSpcContents);
        SpcData? stxSpc = null;
        if (stxSpcContents.Count > 0)
        {
            stxSpc = new(0, stxSpcContents);
        }

        // Write the output SPCs.
        using MemoryStream wrdSpcStream = new();
        SpcSerializer.Serialize(wrdSpc, wrdSpcStream);
        byte[] wrdSpcBytes = wrdSpcStream.ToArray();
        File.WriteAllBytes(wrdSpcFileInfo.FullName, wrdSpcBytes);

        if (stxSpc is not null)
        {
            using MemoryStream stxSpcStream = new();
            SpcSerializer.Serialize(stxSpc, stxSpcStream);
            byte[] stxSpcBytes = stxSpcStream.ToArray();
            File.WriteAllBytes(stxSpcFileInfo.FullName, stxSpcBytes);
        }
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

    private int DeduplicateAndAddToList(ref List<string> list, string strToAdd)
    {
        // De-duplicate the list when possible
        if (list.Contains(strToAdd))
        {
            return list.IndexOf(strToAdd);
        }

        int index = list.Count;
        list.Add(strToAdd);
        return index;
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}