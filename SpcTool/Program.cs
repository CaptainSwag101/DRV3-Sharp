using System;
using System.Diagnostics;
using System.IO;

namespace SpcTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SPC Tool by CaptainSwag101\n" +
                "Version 0.0.1, built on 2019-08-10\n");

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

            SpcFile spc = new SpcFile();
            spc.Load(args[0]);

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

            Console.WriteLine("Press Enter to close...");
            Console.Read();

            //spc.Save(args[0] + ".test");
        }
    }
}
