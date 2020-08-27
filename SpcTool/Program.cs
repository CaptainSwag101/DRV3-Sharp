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
                "Version 1.0.0, built on 2020-08-03\n");

            // Setup text encoding so we can use Shift-JIS text later on
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Ensure we actually have some arguments, if not, print usage string
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: SpcTool.exe <SPC file>");
                return;
            }

            // If the first argument is a valid SPC file (and if we reach this point it probably is), load it.
            string loadedSpcName = args[0];

            // Parse input argument
            FileInfo loadedSpcInfo = new FileInfo(loadedSpcName);
            if (!loadedSpcInfo.Exists)
            {
                Console.WriteLine($"ERROR: \"{loadedSpcName}\" does not exist.");
                return;
            }

            if (loadedSpcInfo.Extension.ToLowerInvariant() != ".spc")
            {
                Console.WriteLine("WARNING: Input file does not have the \".spc\" extension.\nIf you experience any issues, it means this file probably isn't an SPC archive.");
            }

            SpcFile loadedSpc = new SpcFile();
            loadedSpc.Load(loadedSpcName);

            // Setup command dictionary
            var commandDict = new Dictionary<string, Action>
            {
                {
                    @"extract",
                    delegate
                    {
                        // Parse target arguments
                        Console.WriteLine("Type the files you want to extract, separated by spaces (wildcard * supported): ");
                        string targetsRaw = Console.ReadLine();
                        var targets = targetsRaw.Split();

                        ExtractSubfiles(loadedSpc, loadedSpcInfo, targets.ToList());
                    }
                },
                {
                    @"insert",
                    delegate
                    {
                        // Parse target arguments
                        Console.WriteLine("Type the files you want to insert, separated by spaces (or drag and drop): ");
                        string targetsRaw = Console.ReadLine();
                        var targets = targetsRaw.Split();

                        InsertSubfiles(loadedSpc, loadedSpcInfo, targets.ToList());
                    }
                },
                {
                    @"list",
                    delegate
                    {
                        ListSubfiles(loadedSpc, loadedSpcInfo);
                    }
                },
                {
                    @"bench",
                    delegate
                    {
                        BenchmarkCompression(loadedSpc, loadedSpcInfo);
                    }
                }
            };

            // Setup command parser
            CommandParser parser = new CommandParser(commandDict);

            // Process commands
            StringBuilder promptBuilder = new StringBuilder();
            promptBuilder.Append($"Loaded SPC archive: {loadedSpcName}\n");
            promptBuilder.Append($"Valid commands to perform on this archive:\n");
            promptBuilder.AppendJoin(", ", commandDict.Keys);
            promptBuilder.Append("\nPlease enter your command, or \"exit\" to quit: ");
            parser.Prompt(promptBuilder.ToString(), @"exit");
        }

        private static void ExtractSubfiles(SpcFile loadedSpc, FileInfo loadedSpcInfo, List<string> targets)
        {
            // Setup an output directory for extracted files
            string outputDir = loadedSpcInfo.DirectoryName + Path.DirectorySeparatorChar + loadedSpcInfo.Name.Substring(0, loadedSpcInfo.Name.Length - loadedSpcInfo.Extension.Length);
            Directory.CreateDirectory(outputDir);

            // Generate list of subfiles to be extracted that match the target regex
            List<string> subfilesToExtract = new List<string>();
            foreach (string target in targets)
            {
                string regexTarget = "^" + Regex.Escape(target).Replace("\\?", ".").Replace("\\*", ".*") + "$";

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

                insertTasks[targets.IndexOf(subfileName)] = new Task(() => loadedSpc.InsertSubfile(subfileName));
            }

            // Wait until all target subfiles have been inserted
            foreach (Task task in insertTasks)
            {
                task.Start();
                task.Wait();
            }

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
        }
    }
}
