using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V3Lib.Srd.BlockTypes
{
    public abstract class Block
    {
        public string BlockType;
        public int DataLength;
        public int SubdataLength;
        public int Unknown0C; // 1 for $CFH blocks, 0 for everything else
        public List<Block> Children = new List<Block>();

        public Block(ref BinaryReader reader)
        {
            BlockType = new ASCIIEncoding().GetString(reader.ReadBytes(4));

            // Switch from big-endian to little-endian
            DataLength = BitConverter.ToInt32(Utils.SwapEndian(reader.ReadBytes(4)));
            SubdataLength = BitConverter.ToInt32(Utils.SwapEndian(reader.ReadBytes(4)));
            Unknown0C = BitConverter.ToInt32(Utils.SwapEndian(reader.ReadBytes(4)));
        }

        public virtual void WriteData(ref BinaryWriter writer)
        {
            // Write block type string
            writer.Write(new ASCIIEncoding().GetBytes(BlockType));

            // Write data length
            writer.Write(Utils.SwapEndian(BitConverter.GetBytes(DataLength)));

            // Write subdata length
            writer.Write(Utils.SwapEndian(BitConverter.GetBytes(SubdataLength)));

            // Write unknown int
            writer.Write(Utils.SwapEndian(BitConverter.GetBytes(Unknown0C)));
        }
    }
}
