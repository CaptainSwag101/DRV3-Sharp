using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public sealed class MshBlock : Block
    {
        public uint Unknown10;
        public ushort Unknown14;
        public ushort Unknown16;
        public ushort Unknown18;
        public ushort Unknown1A;
        public ushort Unknown1C;
        public ushort Unknown1E;
        public byte Unknown20;
        public byte Unknown21;
        public byte Unknown22;
        public byte Unknown23;
        public List<ushort> UnknownShortList = new List<ushort>();
        public List<string> UnknownStringList = new List<string>();

        public override void DeserializeData(byte[] rawData, string srdiPath, string srdvPath)
        {
            using BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            Unknown10 = reader.ReadUInt32();
            Unknown14 = reader.ReadUInt16();
            Unknown16 = reader.ReadUInt16();
            Unknown18 = reader.ReadUInt16();
            Unknown1A = reader.ReadUInt16();
            Unknown1C = reader.ReadUInt16();
            Unknown1E = reader.ReadUInt16();
            Unknown20 = reader.ReadByte();
            Unknown21 = reader.ReadByte();
            Unknown22 = reader.ReadByte();
            Unknown23 = reader.ReadByte();

            // Read unknown shorts
            while (true)
            {
                // If the first byte of a supposed ushort is zero, abort; we've reached string data
                if (reader.PeekChar() == 0)
                    break;

                UnknownShortList.Add(reader.ReadUInt16());
            }

            // Skip the one empty byte separating short data from string data
            _ = reader.ReadByte();

            // Read unknown string data
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
            sb.Append($"{nameof(Unknown18)}: {Unknown18}\n");
            sb.Append($"{nameof(Unknown1A)}: {Unknown1A}\n");
            sb.Append($"{nameof(Unknown1C)}: {Unknown1C}\n");
            sb.Append($"{nameof(Unknown1E)}: {Unknown1E}\n");
            sb.Append($"{nameof(Unknown20)}: {Unknown20}\n");
            sb.Append($"{nameof(Unknown21)}: {Unknown21}\n");
            sb.Append($"{nameof(Unknown22)}: {Unknown22}\n");
            sb.Append($"{nameof(Unknown23)}: {Unknown23}\n");

            sb.Append($"{nameof(UnknownShortList)}: ");
            sb.AppendJoin(", ", UnknownShortList);
            sb.Append('\n');

            sb.Append($"{nameof(UnknownStringList)}: ");
            sb.AppendJoin(", ", UnknownStringList);

            return sb.ToString();
        }
    }
}
