using System;
using System.Diagnostics;

namespace SpcTool
{
    class Program
    {
        static void Main(string[] args)
        {
            SpcFile spc = new SpcFile();
            spc.Load(args[0]);

            Console.WriteLine("SPC file contains the following subfiles:");
            foreach (SpcSubfile subfile in spc.Subfiles)
            {
                Console.WriteLine(subfile.Name);

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

                Console.WriteLine();
            }

            Console.WriteLine("Press Enter to write a test copy of the current SPC...");
            Console.Read();

            spc.Save(args[0] + ".test");
        }
    }
}
