using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    // Holds information about vertex data and index lists
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


        public override void DeserializeData(byte[] rawData)
        {
            throw new NotImplementedException();
        }

        public override byte[] SerializeData()
        {
            byte[] temp = new byte[1];
            return temp;
        }

        public override string GetInfo()
        {
            throw new NotImplementedException();
        }
    }
}
