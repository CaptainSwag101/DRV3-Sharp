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
                "Version 0.0.2, built on 2020-03-12\n");

            FileInfo info = new FileInfo(args[0]);
            if (!info.Exists)
            {
                Console.WriteLine($"ERROR: \"{args[0]}\" does not exist.");
                return;
            }

            if (info.Extension.ToLower() != ".srd")
            {
                Console.WriteLine("ERROR: Input file does not have the \".srd\" extension.");
                return;
            }

            SrdFile srd = new SrdFile();
            srd.Load(args[0]);

            Console.WriteLine($"\"{info.FullName}\" contains the following blocks:\n");
            PrintBlocks(srd.Blocks);

            Console.WriteLine("Press Enter to close...");
            Console.Read();

            //srd.Save(args[0] + ".test");
        }

        static void PrintBlocks(List<Block> blockList)
        {
            foreach (UnknownBlock block in blockList)
            {
                Console.Write(new string('\t', tabLevel));
                Console.WriteLine($"Block Type: {block.BlockType}");

                Console.Write(new string('\t', tabLevel + 1));
                Console.WriteLine($"Data Length: {(block.Data != null ? block.Data.Length.ToString("n0") : "0")} bytes");

                if (block.Children.Count > 0)
                {
                    Console.Write(new string('\t', tabLevel + 1));
                    Console.WriteLine($"Child blocks: {block.Children.Count.ToString("n0")}");
                    Console.WriteLine();

                    ++tabLevel;
                    PrintBlocks(block.Children);
                    --tabLevel;
                }

                Console.WriteLine();
            }
        }
    }
}
