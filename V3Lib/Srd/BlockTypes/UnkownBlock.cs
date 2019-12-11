using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V3Lib.Srd.BlockTypes
{
    public sealed class UnknownBlock : Block
    {
        public string BlockType;
        public byte[] Data;

        public UnknownBlock(ref BinaryReader reader, string type)
        {
            BlockType = type;

            // Switch from big-endian to little-endian
            int dataLength = BitConverter.ToInt32(Utils.SwapEndian(reader.ReadBytes(4)));
            int subdataLength = BitConverter.ToInt32(Utils.SwapEndian(reader.ReadBytes(4)));
            Unknown0C = BitConverter.ToInt32(Utils.SwapEndian(reader.ReadBytes(4)));

            if (dataLength > 0)
            {
                Data = reader.ReadBytes(dataLength);
                Utils.ReadPadding(ref reader);
            }

            if (subdataLength > 0)
            {
                byte[] subdata = reader.ReadBytes(subdataLength);
                Utils.ReadPadding(ref reader);

                BinaryReader subReader = new BinaryReader(new MemoryStream(subdata));
                Children = SrdFile.ReadBlocks(ref subReader);
                subReader.Close();
            }
        }

        public override void WriteData(ref BinaryWriter writer)
        {
            // Write block type string
            writer.Write(new ASCIIEncoding().GetBytes(BlockType));

            // Write data size
            if (Data == null)
            {
                writer.Write((int)0);
            }
            else
            {
                writer.Write(Utils.SwapEndian(BitConverter.GetBytes(Data.Length)));
            }

            // Write dummy subdata size to be replaced later
            writer.Write((int)0);

            // Write unknown
            writer.Write(Utils.SwapEndian(BitConverter.GetBytes(Unknown0C)));

            // Write main block data
            if (Data != null)
            {
                writer.Write(Data);
                Utils.WritePadding(ref writer);
            }

            // Mark the current position so we can calculate the subdata size for the block header
            long lastPos = writer.BaseStream.Position;
            // Write child block data
            if (Children.Count > 0)
            {
                foreach (Block child in Children)
                {
                    child.WriteData(ref writer);
                }
            }
            long subdataLength = writer.BaseStream.Position - lastPos;

            // Seek backwards to the dummy subdata length we wrote earlier
            writer.BaseStream.Seek(-(subdataLength + Data.Length + 4), SeekOrigin.Current);

            // Write the true subdata size
            writer.Write(Utils.SwapEndian(BitConverter.GetBytes(subdataLength)));

            // Seek forwards to the end of subdata
            writer.BaseStream.Seek(Data.Length + subdataLength, SeekOrigin.Current);
        }
    }
}
