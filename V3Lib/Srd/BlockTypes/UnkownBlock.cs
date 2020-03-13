using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V3Lib.Srd.BlockTypes
{
    /// <summary>
    /// Catch-all class for any block type that we don't have a dedicated class for
    /// </summary>
    public sealed class UnknownBlock : Block
    {
        public byte[] Data;

        public UnknownBlock(ref BinaryReader reader) : base(ref reader)
        {
            if (DataLength > 0)
            {
                Data = reader.ReadBytes(DataLength);
                Utils.ReadPadding(ref reader);
            }

            if (SubdataLength > 0)
            {
                byte[] subdata = reader.ReadBytes(SubdataLength);
                Utils.ReadPadding(ref reader);

                BinaryReader subReader = new BinaryReader(new MemoryStream(subdata));
                Children = SrdFile.ReadBlocks(ref subReader);
                subReader.Close();
                subReader.Dispose();
            }
        }

        public override void WriteData(ref BinaryWriter writer)
        {
            DataLength = (Data != null) ? Data.Length : 0;

            // In order to calculate the subdata size, we write our child block data
            // into a temporary byte array and use its length as our subdata size.
            MemoryStream subdataStream = new MemoryStream();
            BinaryWriter subdataWriter = new BinaryWriter(subdataStream);
            foreach (Block child in Children)
            {
                child.WriteData(ref subdataWriter);
            }
            SubdataLength = (int)subdataStream.Length;

            // Call the base WriteData function to write the main block header
            base.WriteData(ref writer);

            // Write data
            if (Data != null)
            {
                writer.Write(Data);
                Utils.WritePadding(ref writer);
            }

            // Write subdata
            if (subdataStream.Length > 0)
            {
                writer.Write(subdataStream.ToArray());
            }

            // Close and dispose temporary streams
            subdataWriter.Close();
            subdataWriter.Dispose();
        }
    }
}
