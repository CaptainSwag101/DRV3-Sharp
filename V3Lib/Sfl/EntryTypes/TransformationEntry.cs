using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace V3Lib.Sfl.EntryTypes
{
    public class TransformationEntry : Entry
    {
        public List<(string Name, List<(ushort Opcode, byte[] Data)> Commands)> Subentries { get; set; }

        public TransformationEntry()
        {
            Subentries = new List<(string Name, List<(ushort Opcode, byte[] Data)> Commands)>();
        }
    }
}
