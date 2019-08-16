using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SrdTool.BlockTypes
{
    abstract class Block
    {
        public int Unknown; // 1 for $CFH blocks, 0 for everything else
        public List<Block> Children = new List<Block>();

        public abstract void WriteData(ref BinaryWriter writer);
    }
}
