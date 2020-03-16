using System;
using System.Collections.Generic;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public sealed class Ct0Block : Block
    {
        public override void DeserializeData(byte[] rawData)
        {
            return;
        }

        public override byte[] SerializeData()
        {
            return Array.Empty<byte>();
        }
        
        public override string GetInfo()
        {
            return "";
        }
    }
}
