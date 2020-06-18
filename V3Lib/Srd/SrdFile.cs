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
        public List<Block> Blocks;

        public void Load(string srdPath, string srdiPath, string srdvPath)
        {
            using BinaryReader reader = new BinaryReader(new FileStream(srdPath, FileMode.Open));
            Blocks = ReadBlocks(reader, srdiPath, srdvPath);

            reader.Close();
        }

        public void Save(string srdPath, string srdiPath, string srdvPath)
        {
            // Empty out linked files if present
            if (File.Exists(srdiPath)) File.Delete(srdiPath);
            if (File.Exists(srdvPath)) File.Delete(srdvPath);

            using BinaryWriter writer = new BinaryWriter(new FileStream(srdPath, FileMode.Create));
            WriteBlocks(writer, Blocks, srdiPath, srdvPath);

            writer.Flush();
            writer.Close();
        }

        public static List<Block> ReadBlocks(BinaryReader reader, string srdiPath, string srdvPath)
        {
            List<Block> blockList = new List<Block>();

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                string blockType = Encoding.ASCII.GetString(reader.ReadBytes(4));
                Block block = blockType switch
                {
                    "$CFH" => new CfhBlock(),
                    "$RSF" => new RsfBlock(),
                    "$RSI" => new RsiBlock(),
                    "$TRE" => new TreBlock(),
                    "$TXI" => new TxiBlock(),
                    "$TXR" => new TxrBlock(),
                    "$VTX" => new VtxBlock(),
                    "$CT0" => new Ct0Block(),
                    _ => new UnknownBlock(),
                };

                block.BlockType = blockType;
                int dataLength = reader.ReadInt32BE();
                int subdataLength = reader.ReadInt32BE();
                block.Unknown0C = reader.ReadInt32BE();


                byte[] rawData = reader.ReadBytes(dataLength);
                block.DeserializeData(rawData, srdiPath, srdvPath);
                Utils.ReadPadding(reader, 16);


                byte[] rawSubdata = reader.ReadBytes(subdataLength);
                BinaryReader subdataReader = new BinaryReader(new MemoryStream(rawSubdata));
                List<Block> childBlocks = ReadBlocks(subdataReader, srdiPath, srdvPath);
                block.Children = childBlocks;
                subdataReader.Close();
                subdataReader.Dispose();
                Utils.ReadPadding(reader, 16);


                blockList.Add(block);
                Utils.ReadPadding(reader, 16);
            }

            return blockList;
        }

        public static void WriteBlocks(BinaryWriter writer, List<Block> blockList, string srdiPath, string srdvPath)
        {
            foreach (Block block in blockList)
            {
                writer.Write(Encoding.ASCII.GetBytes(block.BlockType));

                byte[] rawData = block.SerializeData(srdiPath, srdvPath);
                writer.WriteBE(rawData.Length);

                using BinaryWriter subdataWriter = new BinaryWriter(new MemoryStream());
                WriteBlocks(subdataWriter, block.Children, srdiPath, srdvPath);
                byte[] rawSubdata = ((MemoryStream)subdataWriter.BaseStream).ToArray();
                subdataWriter.Close();

                writer.WriteBE(rawSubdata.Length);

                if (block is CfhBlock)
                    writer.WriteBE((int)1);
                else
                    writer.WriteBE((int)0);

                writer.Write(rawData);
                Utils.WritePadding(writer, 16);
                writer.Write(rawSubdata);
                Utils.WritePadding(writer, 16);
            }
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
