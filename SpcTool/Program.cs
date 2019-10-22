using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SpcTool
{
    class Program
    {
        private static bool pauseAfterComplete = false;
        private static string operation = null;
        private static string input = null;
        private static List<string> targets = new List<string>();
        private static string output = null;

        private static readonly Dictionary<string, int> validOperations = new Dictionary<string, int> {
            { "list", 0 }, { "bench", 0 }, { "extract", -1 }, { "insert", -1 } };

        static void Main(string[] args)
        {
            Console.WriteLine("SPC Tool by CaptainSwag101\n" +
                "Version 0.0.2, built on 2019-09-25\n");

            // Parse input argument
            if (args.Length == 0)
                return;

            FileInfo info = new FileInfo(args[0]);
            if (!info.Exists)
            {
                Console.WriteLine($"ERROR: \"{args[0]}\" does not exist.");
                return;
            }

            if (info.Extension.ToLowerInvariant() != ".spc")
            {
                Console.WriteLine("ERROR: Input file does not have the \".spc\" extension.");
                return;
            }

            // If the first argument is a valid SPC file (and if we reach this point it probably is), it is the input.
            input = args[0];

            // Parse operation argument
            // If command starts with "--", it is our operation to perform
            if (args.Length > 1 && args[1].StartsWith("--"))
            {
                string op = args[1].TrimStart('-').ToLowerInvariant();
                if (validOperations.Keys.Any(op.Contains))
                {
                    operation = op;
                }
                else
                {
                    Console.WriteLine("ERROR: Invalid operation specified.");
                    return;
                }
            }

            // Parse target arguments
            for (int i = 2; i < args.Length; ++i)
            {
                targets.Add(args[i]);
            }

            // Load the input file
            SpcFile spc = new SpcFile();
            spc.Load(input);

            // Execute operation
            if (operation == null)
            {
                Console.WriteLine("ERROR: No operation specified.");
            }
            else if (validOperations[operation] != -1 && targets.Count != validOperations[operation])
            {
                Console.WriteLine($"ERROR: Invalid number of target(s) specified, expected {validOperations[operation]}.");
            }
            else
            {
                switch (operation)
                {
                    case "list":
                    case "bench":
                        Console.WriteLine($"\"{info.Name}\" contains the following subfiles:\n");
                        foreach (SpcSubfile subfile in spc.Subfiles)
                        {
                            Console.WriteLine($"Subfile name: \"{subfile.Name}\"");
                            Console.WriteLine($"\tCompression flag: {subfile.CompressionFlag}");
                            Console.WriteLine($"\tUnknown flag: {subfile.UnknownFlag}");
                            Console.WriteLine($"\tCurrent size: {subfile.CurrentSize.ToString("n0")} bytes");
                            Console.WriteLine($"\tOriginal size: {subfile.OriginalSize.ToString("n0")} bytes");

                            // Benchmark decompression and compression
                            if (operation == "bench")
                            {
                                Stopwatch stopwatch = new Stopwatch();

                                Console.Write("Decompressing...");
                                stopwatch.Start();
                                subfile.Decompress();
                                stopwatch.Stop();
                                Console.WriteLine($" Done! Took {stopwatch.Elapsed.ToString()}");

                                Console.Write("Compressing...");
                                stopwatch.Restart();
                                subfile.Compress();
                                stopwatch.Stop();
                                Console.WriteLine($" Done! Took {stopwatch.Elapsed.ToString()}");
                            }

                            Console.WriteLine();
                        }
                        break;

                    case "extract":
                        // Setup an output directory for extracted files
                        output ??= info.DirectoryName + Path.DirectorySeparatorChar + info.Name.Substring(0, info.Name.Length - info.Extension.Length);
                        Directory.CreateDirectory(output);

                        // Generate list of subfiles to be extracted that match the target regex
                        List<string> subfilesToExtract = new List<string>();
                        foreach (string target in targets)
                        {
                            string regexTarget = "^" + Regex.Escape(target).Replace("\\?", ".").Replace("\\*", ".*") + "$";

                            foreach (SpcSubfile subfile in spc.Subfiles)
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

                            extractTasks[subfilesToExtract.IndexOf(subfileName)] = Task.Factory.StartNew(() => spc.ExtractSubfile(subfileName, output));
                        }

                        // Wait until all target subfiles have been extracted
                        Task.WaitAll(extractTasks);
                        break;

                    case "insert":
                        // Insert the subfiles using Tasks
                        Task[] insertTasks = new Task[targets.Count];

                        // IMPORTANT: If we ever switch to a for loop instead of foreach,
                        // make sure to make a local scoped copy of the subfile name in order to prevent
                        // threading weirdness from passing the wrong string value and causing random issues.
                        foreach (string subfileName in targets)
                        {
                            Console.WriteLine($"Inserting \"{subfileName}\"...");

                            insertTasks[targets.IndexOf(subfileName)] = new Task(() => spc.InsertSubfile(subfileName));
                        }

                        // Wait until all target subfiles have been inserted
                        foreach (Task task in insertTasks)
                        {
                            task.Start();
                            task.Wait();
                        }

                        // Save the spc file
                        spc.Save(input);
                        break;

                }
            }

            if (pauseAfterComplete)
            {
                Console.WriteLine("Press Enter to close...");
                Console.Read();
            }
            
        }
    }
}
