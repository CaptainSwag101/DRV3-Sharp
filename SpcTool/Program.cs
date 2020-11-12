using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using V3Lib.Archives.SPC;
using CommandParser_Alpha;
using System.Linq;

namespace SpcTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SPC Tool by CaptainSwag101\n" +
                "Version 1.2.0, built on 2020-11-11\n");

            // Setup text encoding so we can use Shift-JIS text later on
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Ensure we actually have some arguments, if not, print usage string
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: SpcTool.exe <SPC file> (optional auto-execute commands and parameters, encapsulated in {})");
                Console.WriteLine("Example: SpcTool.exe test.spc {extract} {file1.srd *.txt} {insert} {file4.dat} {exit}");
                return;
            }

            // Parse input arguments
            // If the first argument is a valid SPC file (and if we reach this point it probably is), load it.
            string loadedSpcName = args[0];
            FileInfo loadedSpcInfo = new(loadedSpcName);
            if (!loadedSpcInfo.Exists)
            {
                Console.WriteLine($"ERROR: \"{loadedSpcName}\" does not exist.");
                return;
            }

            if (loadedSpcInfo.Extension.ToLowerInvariant() != ".spc")
            {
                Console.WriteLine("WARNING: Input file does not have the \".spc\" extension.\nIf you experience any issues, it means this file probably isn't an SPC archive.");
            }

            // Setup a filestream to read the archive
            using FileStream fs = new(loadedSpcName, FileMode.Open);
            SPCArchive loadedSpc = new(fs);

            // Combine any remaining args into a single long string to be broken down by our regex
            Queue<string>? autoExecQueue = null;
            if (args.Length > 1)
            {
                autoExecQueue = new Queue<string>();

                StringBuilder remainingArgsBuilder = new();
                remainingArgsBuilder.AppendJoin(" ", args[1..args.Length]);
                string remainingArgsCombined = remainingArgsBuilder.ToString();

                // Identify and capture any string groups, contained in curly brackets {like this}, and add them to the auto-exec queue
                Regex stringGroupRegex = new(@"(?<=\{).+?(?=\})");
                MatchCollection matches = stringGroupRegex.Matches(remainingArgsCombined);
                foreach (Match? m in matches)
                {
                    if (m == null) continue;

                    autoExecQueue.Enqueue(m.Value);
                }
            }

            // Setup command dictionary
            var commandDict = new Dictionary<string, Func<Queue<string>?, object?>>
            {
                {
                    @"extract",
                    delegate(Queue<string>? autoExecQueue)
                    {
                        // Parse target arguments
                        string targetsRaw = "";
                        if (autoExecQueue?.Count > 0)
                        {
                            targetsRaw += autoExecQueue.Dequeue();
                        }
                        else
                        {
                            Console.WriteLine("Type the files you want to extract, separated by spaces (wildcard * supported): ");
                            targetsRaw += Console.ReadLine();
                        }

                        // Implemented based on https://stackoverflow.com/a/5227134/
                        Regex regex = new("(?<match>[^\\s\"]+)|(?<match>\"[^\"]*\")", RegexOptions.None);
                        var targets = (from Match m in regex.Matches(targetsRaw)
                                        where m.Groups["match"].Success
                                        select m.Groups["match"].Value).ToList();

                        ExtractSubfiles(loadedSpc, loadedSpcInfo, targets);

                        return null;
                    }
                },
                {
                    @"insert",
                    delegate(Queue<string>? autoExecQueue)
                    {
                        // Parse target arguments
                        string target = "";
                        if (autoExecQueue?.Count > 0)
                        {
                            target += autoExecQueue.Dequeue();
                        }
                        else
                        {
                            Console.WriteLine("Type the file you want to insert (or drag and drop): ");
                            target += Console.ReadLine();
                        }

                        InsertSubfiles(loadedSpc, loadedSpcInfo, target, autoExecQueue);

                        return null;
                    }
                },
                {
                    @"list",
                    delegate(Queue<string>? autoExecQueue)
                    {
                        ListSubfiles(loadedSpc, loadedSpcInfo);

                        return null;
                    }
                },
                {
                    @"bench",
                    delegate(Queue<string>? autoExecQueue)
                    {
                        BenchmarkCompression(loadedSpc, loadedSpcInfo);

                        return null;
                    }
                },
                {
                    @"save",
                    delegate(Queue<string>? autoExecQueue)
                    {
                        using FileStream fs = new(loadedSpcName, FileMode.Create);
                        fs.Write(loadedSpc.GetBytes());

                        return null;
                    }
                }
            };

            // Show initial prompt
            StringBuilder promptBuilder = new();
            promptBuilder.Append($"Loaded SPC archive: {loadedSpcName}\n");
            promptBuilder.Append($"Valid commands to perform on this archive:\n");
            promptBuilder.AppendJoin(", ", commandDict.Keys);
            Console.WriteLine(promptBuilder.ToString());

            // Process any commands, then prompt the user
            CommandParser parser = new(commandDict);
            parser.Prompt("Please enter your command, or \"exit\" to quit: ", @"exit", autoExecQueue);
        }

        private static void ExtractSubfiles(SPCArchive loadedSpc, FileInfo loadedSpcInfo, List<string> targets)
        {
            // Setup an output directory for extracted files
            string outputDir = loadedSpcInfo.DirectoryName + Path.DirectorySeparatorChar + loadedSpcInfo.Name.Substring(0, loadedSpcInfo.Name.Length - loadedSpcInfo.Extension.Length);
            Directory.CreateDirectory(outputDir);

            // Generate list of files to be extracted that match the target regex
            List<string> filesToExtract = new();
            foreach (string target in targets)
            {
                string regexTarget = "^" + Regex.Escape(target).Replace("\\?", ".?").Replace("\\*", ".*") + "$";

                foreach (FileEntry file in loadedSpc.Entries)
                {
                    if (Regex.IsMatch(file.Name, regexTarget))
                    {
                        filesToExtract.Add(file.Name);
                    }
                }
            }

            // Extract the files in order
            foreach (string name in filesToExtract)
            {
                FileEntry file = loadedSpc.ExtractFileByName(name);
                using FileStream outFs = new(Path.Combine(outputDir, name), FileMode.Create);
                outFs.Write(file.Data);
            }
        }

        private static void InsertSubfiles(SPCArchive loadedSpc, FileInfo loadedSpcInfo, string target, Queue<string>? autoExecQueue)
        {
            // Insert the file
            Console.WriteLine($"Inserting \"{target}\"...");

            // Read in the file data to insert
            byte[] fileData = File.ReadAllBytes(target);

            // Generate a FileEntry to be inserted
            FileEntry entry = new(target, fileData, fileData.Length, FileEntry.CompressionState.Uncompressed, 4);

            // Try to insert the file. If it returns false, it means there's already
            // a file in the archive with a matching name, and we must
            // ask the user to explicitly clobber the matching file.
            if (!loadedSpc.InsertFile(entry))
            {
                while (true)
                {
                    string? yesNo;

                    if (autoExecQueue?.Count > 0)
                    {
                        yesNo = autoExecQueue.Dequeue();
                    }
                    else
                    {
                        Console.Write("A file with the name \"{fileName}\" already exists in the archive. Overwrite? (y/n) "); ;
                        yesNo = Console.ReadLine()?.ToLowerInvariant();
                    }

                    if (!string.IsNullOrEmpty(yesNo))
                    {
                        if (yesNo == "y")
                        {
                            loadedSpc.InsertFile(entry, true);
                        }
                        else if (yesNo == "n")
                        {
                            break;
                        }
                    }
                }
            }
        }

        private static void ListSubfiles(SPCArchive loadedSpc, FileInfo loadedSpcInfo)
        {
            foreach (FileEntry subfile in loadedSpc.Entries)
            {
                Console.WriteLine($"Subfile name: \"{subfile.Name}\"");
                Console.WriteLine($"\tCompression flag: {subfile.CompressionFlag}");
                Console.WriteLine($"\tUnknown flag: {subfile.UnknownFlag}");
                Console.WriteLine($"\tCurrent size: {subfile.CurrentSize:n0} bytes");
                Console.WriteLine($"\tUncompressed size: {subfile.UncompressedSize:n0} bytes");
                Console.WriteLine();
            }
        }

        private static void BenchmarkCompression(SPCArchive loadedSpc, FileInfo loadedSpcInfo)
        {
            /*
            foreach (FileEntry subfile in loadedSpc.Entries)
            {
                Console.WriteLine($"Subfile name: \"{subfile.Name}\"");
                Console.WriteLine($"\tCompression flag: {subfile.CompressionFlag}");
                Console.WriteLine($"\tUnknown flag: {subfile.UnknownFlag}");
                Console.WriteLine($"\tCurrent size: {subfile.CurrentSize:n0} bytes");
                Console.WriteLine($"\tOriginal size: {subfile.UncompressedSize:n0} bytes");

                // Benchmark decompression and compression
                Stopwatch stopwatch = new Stopwatch();

                Console.Write("Decompressing (Preliminary)...");
                stopwatch.Start();
                subfile.Decompress();
                stopwatch.Stop();
                Console.WriteLine($" Done! Took {stopwatch.Elapsed}");
                //File.WriteAllBytes("decompress1.bin", subfile.Data);

                int decompSize = subfile.Data.Length;

                Console.Write("Compressing with old method...");
                stopwatch.Restart();
                subfile.CompressOld();
                stopwatch.Stop();
                Console.WriteLine($" Done! Took {stopwatch.Elapsed}");
                //File.WriteAllBytes("compress1.bin", subfile.Data);

                int compSize = subfile.Data.Length;

                Console.WriteLine($"Compression Ratio (Old Method): {(compSize * 100.0f) / decompSize}%");

                Console.Write("Decompressing (Old Method Compressed)...");
                stopwatch.Restart();
                subfile.Decompress();
                stopwatch.Stop();
                Console.WriteLine($" Done! Took {stopwatch.Elapsed}");
                //File.WriteAllBytes("decompress2.bin", subfile.Data);

                Console.Write("Compressing with new method...");
                stopwatch.Restart();
                subfile.Compress();
                stopwatch.Stop();
                Console.WriteLine($" Done! Took {stopwatch.Elapsed}");
                //File.WriteAllBytes("compress2.bin", subfile.Data);

                compSize = subfile.Data.Length;

                Console.WriteLine($"Compression Ratio (New Method): {(compSize * 100.0f) / decompSize}%");

                Console.Write("Decompressing (New Method Compressed)...");
                stopwatch.Restart();
                subfile.Decompress();
                stopwatch.Stop();
                Console.WriteLine($" Done! Took {stopwatch.Elapsed}");
                //File.WriteAllBytes("decompress3.bin", subfile.Data);

                Console.WriteLine();
            }
            */
        }
    }
}
