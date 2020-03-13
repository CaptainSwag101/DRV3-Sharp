using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
//using Scarlet.Drawing;
using V3Lib.Srd.BlockTypes;

namespace V3Lib.Srd
{
    public class SrdFile
    {
        public string Filepath;
        public List<Block> Blocks;

        public void Load(string srdPath)
        {
            FileInfo info = new FileInfo(srdPath);
            if (!info.Exists)
            {
                Console.WriteLine($"ERROR: {info.FullName} does not exist.");
                return;
            }

            if (info.Extension.ToLower() != ".srd")
            {
                Console.WriteLine("ERROR: Input file does not have the \".srd\" extension.");
                return;
            }

            Filepath = srdPath;

            BinaryReader reader = new BinaryReader(new FileStream(srdPath, FileMode.Open));
            Blocks = ReadBlocks(ref reader);

            reader.Close();
        }

        public static List<Block> ReadBlocks(ref BinaryReader reader)
        {
            List<Block> blockList = new List<Block>();

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                Block block;

                string blockType = new ASCIIEncoding().GetString(reader.ReadBytes(4));
                reader.BaseStream.Seek(-4, SeekOrigin.Current);
                switch (blockType)
                {
                    default:
                        block = new UnknownBlock(ref reader);
                        break;
                }
                blockList.Add(block);

                Utils.ReadPadding(ref reader);
            }

            return blockList;
        }

        /*
        public void ExtractImages(bool extractMipmaps = false)
        {
            Console.WriteLine("Searching for texture data in {0}:", Filepath);

            string srdvPath = Filepath + 'v';
            if (!File.Exists(srdvPath))
            {
                Console.WriteLine("ERROR: No SRDV file found.");
                return;
            }

            // Create the folder given by the RSF block and extract images
            // into it instead of into the program's root directory
            Directory.CreateDirectory(ResourceFolder.Name);

            // Iterate through blocks and extract image data
            int txrIndex = 0;
            foreach (Block block in Blocks)
            {
                if (block is TxrBlock)
                {
                    Console.WriteLine(string.Format("Extracting texture index {0}: {1}", txrIndex++, ((TxrBlock)block).ResourceBlock.StringData));
                    ((TxrBlock)block).ExtractImages(srdvPath, ResourceFolder.Name, extractMipmaps);
                }
            }
        }

        public void ReplaceImages(string replacementImagePath, int indexToReplace, bool generateMipmaps)
        {
            Console.WriteLine("Searching for texture data in {0}:", Filepath);

            string srdvPath = Filepath + 'v';
            if (!File.Exists(srdvPath))
            {
                Console.WriteLine("ERROR: No SRDV file found.");
                return;
            }

            // Iterate through the TXR blocks and replace the requested block
            int txrIndex = 0;
            foreach (Block block in Blocks)
            {
                if (block is TxrBlock)
                {
                    if (txrIndex == indexToReplace)
                    {
                        Console.WriteLine(string.Format("Replacing texture index {0}: {1}", txrIndex, ((TxrBlock)block).ResourceBlock.StringData));
                        ((TxrBlock)block).ReplaceImages(srdvPath, replacementImagePath, generateMipmaps);
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Skipping texture index {0}: {1}", txrIndex, ((TxrBlock)block).ResourceBlock.StringData));
                    }
                    txrIndex++;
                }
            }

            // Save the SDR file
            File.Delete(Filepath);
            BinaryWriter srdWriter = new BinaryWriter(File.OpenWrite(Filepath));
            foreach (Block block in Blocks)
            {
                block.WriteData(ref srdWriter);
                Utils.WritePadding(ref srdWriter);
            }
            srdWriter.Close();
        }
        */
    }
}
