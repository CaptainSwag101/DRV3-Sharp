using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SrdTool.Blocks;

namespace SrdTool
{
    class SrdFile
    {
        public List<Block> Blocks = new List<Block>();

        public void Load(string filepath)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(File.ReadAllBytes(filepath)));

            // Read blocks
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                Block block = new Block();

                block.Type = new ASCIIEncoding().GetString(reader.ReadBytes(4));

                // Read raw data, then swap endianness
                byte[] b1 = reader.ReadBytes(4);
                Array.Reverse(b1);
                int dataLength = BitConverter.ToInt32(b1, 0);

                byte[] b2 = reader.ReadBytes(4);
                Array.Reverse(b2);
                int subdataLength = BitConverter.ToInt32(b2, 0);

                byte[] b3 = reader.ReadBytes(4);
                Array.Reverse(b3);
                block.Unknown = BitConverter.ToInt32(b3, 0);

                block.Data = reader.ReadBytes(dataLength);
                Utils.ReadPadding(ref reader);

                block.Subdata = reader.ReadBytes(subdataLength);
                Utils.ReadPadding(ref reader);

                Blocks.Add(block);
            }
        }

        public void Save(string filepath)
        {

        }
    }
}
