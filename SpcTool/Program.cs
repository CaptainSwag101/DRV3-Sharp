using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SpcTool
{
    class Program
    {
        private static bool pauseAfterComplete = false;
        private static string operation = "";
        private static string input = "";
        private static List<string> targets = new List<string>();
        private static string output = "";

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

            if (info.Extension.ToLower() != ".spc")
            {
                Console.WriteLine("ERROR: Input file does not have the \".spc\" extension.");
                return;
            }

            // If the first argument is a valid SPC file, it is the input
            input = args[0];

            // Parse operation argument
            // If command starts with "--", it is our operation to perform
            if (args.Length > 1 && args[1].StartsWith("--"))
            {
                switch (args[1].ToLower().TrimStart('-'))
                {
                    case "extract":
                    case "inject":
                        operation = args[1].ToLower().TrimStart('-');
                        break;

                    default:
                        Console.WriteLine("ERROR: Invalid operation specified.");
                        break;
                }
            }

            // Parse target arguments
            for (int i = 2; i < args.Length; ++i)
            {
                string regexModifiedString = "^" + Regex.Escape(args[i]).Replace("\\?", ".").Replace("\\*", ".*") + "$";
                targets.Add(regexModifiedString);
            }

            // Load the input file
            SpcFile spc = new SpcFile();
            spc.Load(input);

            // Execute operation
            if (operation != "" && targets.Count > 0)
            {
                switch (operation)
                {
                    case "extract":
                        // Setup an output directory for extracted files
                        string outDir = info.DirectoryName + '\\' + info.Name.Substring(0, info.Name.Length - info.Extension.Length);
                        Directory.CreateDirectory(outDir);

                        // Generate list of subfiles to be extracted
                        List<string> subfilesToExtract = new List<string>();
                        for (int t = 0; t < targets.Count; ++t)
                        {
                            foreach (SpcSubfile subfile in spc.Subfiles)
                            {
                                if (Regex.IsMatch(subfile.Name, targets[t]))
                                {
                                    Console.WriteLine($"Extracting \"{subfile.Name}\"...");
                                    subfilesToExtract.Add(subfile.Name);
                                }
                            }
                        }

                        // Extract the subfiles using Tasks
                        TaskFactory taskFactory = new TaskFactory();
                        Task[] extractTasks = new Task[subfilesToExtract.Count];

                        // IMPORTANT: Do testing to see if .NET Core 3.0 broke for loops,
                        // it seems like we're looping one value farther than should be possible, but only in this particular for loop.
                        // We're getting an off-by-one error despite all my logic saying we shouldn't be.
                        // Perhaps this is a bug in .NET Core 3.0?

                        //for (int s = 0; s < subfilesToExtract.Count; ++s) // for loop
                        foreach (string subfileName in subfilesToExtract) // foreach loop
                        {
                            //extractTasks[s] = taskFactory.StartNew(() => spc.ExtractSubfile(subfilesToExtract[s], outDir)); // for loop
                            extractTasks[subfilesToExtract.IndexOf(subfileName)] = taskFactory.StartNew(() => spc.ExtractSubfile(subfileName, outDir)); // foreach loop
                        }

                        // Wait until all target subfiles have been extracted
                        Task.WaitAll(extractTasks);

                        break;

                    case "inject":

                        break;

                }
                
                
            }
            else
            {
                Console.WriteLine($"\"{info.Name}\" contains the following subfiles:\n");
                foreach (SpcSubfile subfile in spc.Subfiles)
                {
                    Console.WriteLine($"File name: {subfile.Name}");
                    Console.WriteLine($"\tCompression flag: {subfile.CompressionFlag}");
                    Console.WriteLine($"\tUnknown flag: {subfile.UnknownFlag}");
                    Console.WriteLine($"\tCurrent size: {subfile.CurrentSize.ToString("n0")} bytes");
                    Console.WriteLine($"\tOriginal size: {subfile.OriginalSize.ToString("n0")} bytes");

                    /*
                    // This is just some test code for verifying and benchmarking SPC compression. It will be replaced later.
                    Stopwatch stopwatch = new Stopwatch();

                    Console.Write("Decompressing...");
                    stopwatch.Start();
                    subfile.Decompress();
                    stopwatch.Stop();
                    Console.WriteLine(" Done! Took {0}", stopwatch.Elapsed.ToString());

                    Console.Write("Compressing...");
                    stopwatch.Start();
                    subfile.Compress();
                    stopwatch.Stop();
                    Console.WriteLine(" Done! Took {0}", stopwatch.Elapsed.ToString());
                    */


                    Console.WriteLine();
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
