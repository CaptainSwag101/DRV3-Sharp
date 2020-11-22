using System;
using System.Collections.Generic;
using System.Text;

namespace V3Lib.Legacy.Sfl
{
    public class Table
    {
        public uint Id;
        public ushort Unknown1;
        public uint Unknown2;
        public List<Entry> Entries = new List<Entry>();
    }
}
