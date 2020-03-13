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
        public int Unknown0C; // 1 for $CFH blocks, 0 for everything else
        public List<Block> Children = new List<Block>();

        public abstract void DeserializeData(byte[] rawData);

        public void DeserializeSubdata(byte[] rawSubdata)
        {
            BinaryReader subReader = new BinaryReader(new MemoryStream(rawSubdata));
            this.Children = SrdFile.ReadBlocks(ref subReader);
            subReader.Close();
            subReader.Dispose();
        }

        public abstract byte[] SerializeData();

        public byte[] SerializeSubdata()
        {
            List<byte> subdata = new List<byte>();
            foreach (Block child in Children)
            {
                byte[] childData = child.SerializeData();
                subdata.AddRange(childData);
            }

            return subdata.ToArray();
        }

        public abstract string GetInfo();
    }
}
