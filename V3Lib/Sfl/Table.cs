using System;
using System.Collections.Generic;
using System.Text;

namespace V3Lib.Sfl
{
    public class Table
    {
        public uint Id { get; set; }
        public ushort Unknown1 { get; set; }
        public uint Unknown2 { get; set; }
        public List<Entry> Entries { get; set; }

        public Table()
        {
            Entries = new List<Entry>();
        }
    }
}
