using System;
using System.Collections.Generic;
using System.Text;

namespace SrdTool.Blocks
{
    class Block
    {
        public string Type;
        public int Unknown; // Always 1 for $CFH blocks, 0 for everything else
        public byte[] Data;
        public byte[] Subdata;
    }
}
