using System;
using System.Collections.Generic;
using System.IO;
using V3Lib.Srd;
using V3Lib.Srd.BlockTypes;

namespace SrdTool
{
    class Program
    {
        static int tabLevel = 0;
        static void Main(string[] args)
        {
            Console.WriteLine("SRD Tool by CaptainSwag101\n" +
                "Version 0.0.2, built on 2019-08-15\n");

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

            Console.WriteLine("\"{0}\" contains the following blocks:\n", info.FullName);
            PrintBlocks(srd.Blocks);

            Console.WriteLine("Press Enter to close...");
            Console.Read();

            //srd.Save(args[0] + ".test");
        }

        static void PrintBlocks(List<Block> blockList)
        {
            foreach (UnknownBlock block in blockList)
            {
                Console.WriteLine("{0}Block Type: {1}", new string('\t', tabLevel), block.BlockType);
                Console.WriteLine("{0}Data Length: {1} bytes", new string('\t', tabLevel + 1), (block.Data != null ? block.Data.Length.ToString("n0") : "0"));

                if (block.Children.Count > 0)
                {
                    Console.WriteLine("{0}Child blocks: {1}", new string('\t', tabLevel + 1), block.Children.Count.ToString("n0"));
                    Console.WriteLine();

                    ++tabLevel;
                    PrintBlocks(block.Children);
                    --tabLevel;
                }
            }

            Console.WriteLine();
        }
    }
}
