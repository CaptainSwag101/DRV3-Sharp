using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using V3Lib.Spc;
using CommandParser_Alpha;
using System.Linq;

namespace SpcTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SPC Tool by CaptainSwag101\n" +
                "Version 1.1.0, built on 2020-10-15\n");

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

            SpcFile loadedSpc = new();
            loadedSpc.Load(loadedSpcName);

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
                            targetsRaw = autoExecQueue.Dequeue();
                        }
                        else
                        {
                            Console.WriteLine("Type the files you want to extract, separated by spaces (wildcard * supported): ");
                            targetsRaw = Console.ReadLine();
                        }

                        // Implemented based on https://stackoverflow.com/a/5227134/
                        Regex regex = new("(?<match>[^\\s\"]+)| (?<match>\"[^\"]*\")", RegexOptions.None);
                        var targets = (from Match m in regex.Matches(targetsRaw)
                                        where m.Groups["match"].Success
                                        select m.Groups["match"].Value).ToList();

                        ExtractSubfiles(loadedSpc, loadedSpcInfo, targets);

                        return null;
                    }
                },
                {
                    @"insert",
                    delegate(Queue<string>? autoExecStack)
                    {
                        // Parse target arguments
                        string targetsRaw = "";
                        if (autoExecStack?.Count > 0)
                        {
                            targetsRaw = autoExecStack.Dequeue();
                        }
                        else
                        {
                            Console.WriteLine("Type the files you want to insert, separated by spaces (or drag and drop): ");
                            targetsRaw = Console.ReadLine();
                        }

                        // Implemented based on https://stackoverflow.com/a/5227134
                        Regex regex = new("(?<match>[^\\s\"]+)| (?<match>\"[^\"]*\")", RegexOptions.None);
                        var targets = (from Match m in regex.Matches(targetsRaw)
                                        where m.Groups["match"].Success
                                        select m.Groups["match"].Value).ToList();

                        InsertSubfiles(loadedSpc, loadedSpcInfo, targets);

                        return null;
                    }
                },
                {
                    @"list",
                    delegate(Queue<string>? autoExecStack)
                    {
                        ListSubfiles(loadedSpc, loadedSpcInfo);

                        return null;
                    }
                },
                {
                    @"bench",
                    delegate(Queue<string>? autoExecStack)
                    {
                        BenchmarkCompression(loadedSpc, loadedSpcInfo);

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

        private static void ExtractSubfiles(SpcFile loadedSpc, FileInfo loadedSpcInfo, List<string> targets)
        {
            // Setup an output directory for extracted files
            string outputDir = loadedSpcInfo.DirectoryName + Path.DirectorySeparatorChar + loadedSpcInfo.Name.Substring(0, loadedSpcInfo.Name.Length - loadedSpcInfo.Extension.Length);
            Directory.CreateDirectory(outputDir);

            // Generate list of subfiles to be extracted that match the target regex
            List<string> subfilesToExtract = new();
            foreach (string target in targets)
            {
                string regexTarget = "^" + Regex.Escape(target).Replace("\\?", ".?").Replace("\\*", ".*") + "$";

                foreach (SpcSubfile subfile in loadedSpc.Subfiles)
                {
                    if (Regex.IsMatch(subfile.Name, regexTarget))
                    {
                        subfilesToExtract.Add(subfile.Name);
                    }
                }
            }

            // Extract the subfiles using Tasks
            Task[] extractTasks = new Task[subfilesToExtract.Count];

            // IMPORTANT: If we ever switch to a for loop instead of foreach,
            // make sure to make a local scoped copy of the subfile name in order to prevent
            // threading weirdness from passing the wrong string value and causing random issues.
            foreach (string subfileName in subfilesToExtract)
            {
                Console.WriteLine($"Extracting \"{subfileName}\"...");

                extractTasks[subfilesToExtract.IndexOf(subfileName)] = Task.Factory.StartNew(() => loadedSpc.ExtractSubfile(subfileName, outputDir));
            }

            // Wait until all target subfiles have been extracted
            Task.WaitAll(extractTasks);
        }

        private static void InsertSubfiles(SpcFile loadedSpc, FileInfo loadedSpcInfo, List<string> targets)
        {
            // Insert the subfiles using Tasks
            Task[] insertTasks = new Task[targets.Count];

            // IMPORTANT: If we ever switch to a for loop instead of foreach,
            // make sure to make a local scoped copy of the subfile name in order to prevent
            // threading weirdness from passing the wrong string value and causing random issues.
            foreach (string subfileName in targets)
            {
                Console.WriteLine($"Inserting \"{subfileName}\"...");

                // Check if the file already exists and prompt to overwrite
                if (loadedSpc.Subfiles.Where(subfile => (subfile.Name == subfileName)).Count() > 0)
                {
                    Console.WriteLine("The specified file already exists within the SPC archive. Overwrite? (y/N)");
                    string yesNo = Console.ReadLine().ToLowerInvariant();
                    if (!yesNo.StartsWith("y"))
                    {
                        continue;   // Skip this file
                    }
                }

                insertTasks[targets.IndexOf(subfileName)] = Task.Factory.StartNew(() => loadedSpc.InsertSubfile(subfileName));
            }

            // Wait until all target subfiles have been inserted
            Task.WaitAll(insertTasks);

            // Save the spc file
            loadedSpc.Save(loadedSpcInfo.FullName);
        }

        private static void ListSubfiles(SpcFile loadedSpc, FileInfo loadedSpcInfo)
        {
            foreach (SpcSubfile subfile in loadedSpc.Subfiles)
            {
                Console.WriteLine($"Subfile name: \"{subfile.Name}\"");
                Console.WriteLine($"\tCompression flag: {subfile.CompressionFlag}");
                Console.WriteLine($"\tUnknown flag: {subfile.UnknownFlag}");
                Console.WriteLine($"\tCurrent size: {subfile.CurrentSize:n0} bytes");
                Console.WriteLine($"\tOriginal size: {subfile.OriginalSize:n0} bytes");
                Console.WriteLine();
            }
        }

        private static void BenchmarkCompression(SpcFile loadedSpc, FileInfo loadedSpcInfo)
        {
            foreach (SpcSubfile subfile in loadedSpc.Subfiles)
            {
                Console.WriteLine($"Subfile name: \"{subfile.Name}\"");
                Console.WriteLine($"\tCompression flag: {subfile.CompressionFlag}");
                Console.WriteLine($"\tUnknown flag: {subfile.UnknownFlag}");
                Console.WriteLine($"\tCurrent size: {subfile.CurrentSize:n0} bytes");
                Console.WriteLine($"\tOriginal size: {subfile.OriginalSize:n0} bytes");

                // Benchmark decompression and compression
                Stopwatch stopwatch = new();

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
        }
    }
}
