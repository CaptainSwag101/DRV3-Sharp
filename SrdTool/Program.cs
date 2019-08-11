using System;
using System.IO;

namespace SrdTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SRD Tool by CaptainSwag101\n" +
                "Version 0.0.1, built on 2019-08-10\n");

            FileInfo info = new FileInfo(args[0]);
            if (!info.Exists)
            {
                Console.WriteLine("ERROR: \"{0}\" does not exist.", args[0]);
                return;
            }

            if (info.Extension.ToLower() != ".srd")
            {
                Console.WriteLine("ERROR: Input file does not have the \".srd\" extension.");
                return;
            }

            SrdFile srd = new SrdFile();
            srd.Load(args[0]);

            Console.WriteLine("\"{0}\" contains the following blocks:\n", info.Name);
            foreach (var block in srd.Blocks)
            {
                Console.WriteLine("Block Type: {0}", block.Type);
                Console.WriteLine("\tData Length: {0} bytes", block.Data.Length.ToString("n0"));
                Console.WriteLine("\tSubdata Length: {0} bytes", block.Subdata.Length.ToString("n0"));

                Console.WriteLine();
            }

            Console.WriteLine("Press Enter to close...");
            Console.Read();

            //srd.Save(args[0] + ".test");
        }
    }
}
