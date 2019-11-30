using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public sealed class VtxBlock : Block
    {
        public int Unknown10;
        public int Unknown14;
        public int VertexCount;
        public int Unknown1C;
        public List<short> Unknown20;
        public int Unknown60;
        public int Unknown64;
        public int Unknown68;
        public int VertexBlockLength1;
        public int Unknown70;
        public int UVBlockOffset;
        public int Unknown78;
        public int Unknown7C;
        public List<byte> Unknown80;


        public VtxBlock(ref BinaryReader reader)
        {
            // Switch from big-endian to little-endian
            byte[] bDataLength = reader.ReadBytes(4);
            Array.Reverse(bDataLength);
            int dataLength = BitConverter.ToInt32(bDataLength);

            byte[] bSubdataLength = reader.ReadBytes(4);
            Array.Reverse(bSubdataLength);
            int subdataLength = BitConverter.ToInt32(bSubdataLength);

            byte[] bUnknown = reader.ReadBytes(4);
            Array.Reverse(bUnknown);
            Unknown0C = BitConverter.ToInt32(bUnknown);

            // Read and parse data
            if (dataLength > 0)
            {
                byte[] data = reader.ReadBytes(dataLength);
                Utils.ReadPadding(ref reader);

                Unknown10 = reader.ReadInt32();
                Unknown14 = reader.ReadInt32();
                VertexCount = reader.ReadInt32();
                Unknown1C = reader.ReadInt32();
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

        }
    }
}
