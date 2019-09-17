using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace SpcTool
{
    class Program
    {
        private static bool pauseAfterComplete = false;
        private static string operation = "";
        private static string input = "";
        private static List<string> subfiles = new List<string>();
        private static string output = "";

        static void Main(string[] args)
        {
            Console.WriteLine("SPC Tool by CaptainSwag101\n" +
                "Version 0.0.2, built on 2019-09-17\n");

            // Parse input argument
            if (args.Length == 0)
                return;

            FileInfo info = new FileInfo(args[0]);
            if (!info.Exists)
            {
                Console.WriteLine("ERROR: \"{0}\" does not exist.", args[0]);
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

            // Parse subfile arguments
            for (int i = 2; i < args.Length; ++i)
            {
                // If you want to implement both "*" and "?"
                string regexModifiedString = "^" + Regex.Escape(args[i]).Replace("\\?", ".").Replace("\\*", ".*") + "$";
                subfiles.Add(regexModifiedString);
            }

            // Load the input file
            SpcFile spc = new SpcFile();
            spc.Load(input);

            // Execute operation
            if (operation == "" || subfiles.Count == 0)
            {
                Console.WriteLine("\"{0}\" contains the following subfiles:\n", info.Name);
                foreach (SpcSubfile subfile in spc.Subfiles)
                {
                    Console.WriteLine("File name: {0}", subfile.Name);
                    Console.WriteLine("\tCompression flag: {0}", subfile.CompressionFlag);
                    Console.WriteLine("\tUnknown flag: {0}", subfile.UnknownFlag);
                    Console.WriteLine("\tCurrent size: {0} bytes", subfile.CurrentSize.ToString("n0"));
                    Console.WriteLine("\tOriginal size: {0} bytes", subfile.OriginalSize.ToString("n0"));

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
            else
            {
                switch (operation)
                {
                    case "extract":
                        // Setup an output directory for extracted files
                        string outDir = info.DirectoryName + '\\' + info.Name.Substring(0, info.Name.Length - info.Extension.Length);
                        Directory.CreateDirectory(outDir);
                        foreach (string name in subfiles)
                        {
                            foreach (SpcSubfile subfile in spc.Subfiles)
                            {
                                if (Regex.IsMatch(subfile.Name, name))
                                {
                                    Console.Write("Extracting \"{0}\"... ", subfile.Name);
                                    spc.ExtractSubfile(subfile.Name, outDir);
                                    Console.WriteLine("Done!");
                                }
                            }
                        }
                        break;

                    case "inject":

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
