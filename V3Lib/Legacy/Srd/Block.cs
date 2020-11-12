using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V3Lib.Srd
{
    public abstract class Block
    {
        public string BlockType;
        public int Unknown0C;   // 1 for $CFH blocks, 0 for everything else
        public List<Block> Children = new List<Block>();

        public abstract void DeserializeData(byte[] rawData, string srdiPath, string srdvPath);
        public abstract byte[] SerializeData(string srdiPath, string srdvPath);
        public abstract string GetInfo();
    }
}
