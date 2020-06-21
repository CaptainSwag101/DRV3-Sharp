using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public sealed class ScnBlock : Block
    {
        public uint Unknown10;
        public ushort Unknown14;
        public ushort Unknown16;
        public List<ushort> UnknownShortList = new List<ushort>();
        public ushort UnknownShortBetween;
        public List<string> UnknownStringList = new List<string>();

        public override void DeserializeData(byte[] rawData, string srdiPath, string srdvPath)
        {
            using BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            Unknown10 = reader.ReadUInt32();
            Unknown14 = reader.ReadUInt16();
            Unknown16 = reader.ReadUInt16();

            // Read unknown short pairs
            for (int i = 0; i < Unknown16; ++i)
            {
                UnknownShortList.Add(reader.ReadUInt16());
                UnknownShortList.Add(reader.ReadUInt16());
            }

            // Read unknown short in between the short list and string data
            UnknownShortBetween = reader.ReadUInt16();

            // Read unknown strings
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                UnknownStringList.Add(Utils.ReadNullTerminatedString(reader, Encoding.ASCII));
            }
        }

        public override byte[] SerializeData(string srdiPath, string srdvPath)
        {
            throw new NotImplementedException();
        }

        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"{nameof(Unknown10)}: {Unknown10}\n");
            sb.Append($"{nameof(Unknown14)}: {Unknown14}\n");
            sb.Append($"{nameof(Unknown16)}: {Unknown16}\n");

            sb.Append($"{nameof(UnknownShortList)}: ");
            sb.AppendJoin(", ", UnknownShortList);
            sb.Append('\n');

            sb.Append($"{nameof(UnknownShortBetween)}: {UnknownShortBetween}\n");

            sb.Append($"{nameof(UnknownStringList)}: ");
            sb.AppendJoin(", ", UnknownStringList);

            return sb.ToString();
        }
    }
}
