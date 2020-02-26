using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace V3Lib.Sfl.EntryTypes
{
    public struct TransformationCommand
    {
        public ushort Opcode;
        public byte[] Data;
    }

    public class TransformationSubentry
    {
        public string Name;
        public List<TransformationCommand> Commands = new List<TransformationCommand>();

        public TransformationSubentry(string name, List<TransformationCommand> commands)
        {
            Name = name;
            Commands = commands;
        }
    }

    public class TransformationEntry : Entry
    {
        public List<TransformationSubentry> Subentries = new List<TransformationSubentry>();
    }
}
