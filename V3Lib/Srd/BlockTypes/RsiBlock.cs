using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public struct ResourceInfo : IEquatable<ResourceInfo>
    {
        public int Offset;
        public int Length;
        public int Unknown08;
        public int Unknown0C;

        public override bool Equals(object obj)
        {
            return obj is ResourceInfo info && Equals(info);
        }

        public bool Equals(ResourceInfo other)
        {
            return Offset == other.Offset &&
                   Length == other.Length &&
                   Unknown08 == other.Unknown08 &&
                   Unknown0C == other.Unknown0C;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Offset, Length, Unknown08, Unknown0C);
        }

        public static bool operator ==(ResourceInfo left, ResourceInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ResourceInfo left, ResourceInfo right)
        {
            return !(left == right);
        }
    }

    public sealed class RsiBlock : Block
    {
        public byte Unknown10;
        public byte Unknown11;
        public byte Unknown12;
        public byte Unknown13;
        public short Unknown14;
        public short Unknown16;
        public short Unknown18;
        public short Unknown1A;
        public List<ResourceInfo> ResourceInfoList;
        public byte[] ResourceData;
        public List<string> ResourceStringList;

        public override void DeserializeData(byte[] rawData)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            Unknown10 = reader.ReadByte();
            Unknown11 = reader.ReadByte();
            Unknown12 = reader.ReadByte();
            Unknown13 = reader.ReadByte();
            Unknown14 = reader.ReadInt16();
            Unknown16 = reader.ReadInt16();
            Unknown18 = reader.ReadInt16();
            Unknown1A = reader.ReadInt16();
            int resourceStringListOffset = reader.ReadInt32();

            // Read resource info
            int resourceInfoCount = (Unknown12 == 0xFF ? Unknown14 : Unknown13);
            ResourceInfoList = new List<ResourceInfo>();
            for (int r = 0; r < resourceInfoCount; ++r)
            {
                ResourceInfo info;
                info.Offset = reader.ReadInt32();
                info.Length = reader.ReadInt32();
                info.Unknown08 = reader.ReadInt32();
                info.Unknown0C = reader.ReadInt32();
                ResourceInfoList.Add(info);
            }

            // Read resource data 
            int dataLength = (resourceStringListOffset - (int)reader.BaseStream.Position);
            ResourceData = reader.ReadBytes(dataLength);

            // Read resource strings
            ResourceStringList = new List<string>();
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                ResourceStringList.Add(Utils.ReadNullTerminatedString(ref reader, Encoding.GetEncoding("shift-jis")));
            }

            reader.Close();
            reader.Dispose();
        }

        public override byte[] SerializeData()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(ms);

            writer.Write(Unknown10);
            writer.Write(Unknown11);
            writer.Write(Unknown12);
            writer.Write(Unknown13);
            writer.Write(Unknown14);
            writer.Write(Unknown16);
            writer.Write(Unknown18);
            writer.Write(Unknown1A);
            writer.Write((ResourceInfoList.Count * 0x10) + ResourceData.Length + 0x10);
            
            foreach (ResourceInfo info in ResourceInfoList)
            {
                writer.Write(info.Offset);
                writer.Write(info.Length);
                writer.Write(info.Unknown08);
                writer.Write(info.Unknown0C);
            }

            writer.Write(ResourceData);

            foreach (string resourceString in ResourceStringList)
            {
                writer.Write(Encoding.GetEncoding("shift-jis").GetBytes(resourceString));
            }

            byte[] result = ms.ToArray();
            writer.Close();
            writer.Dispose();
            return result;
        }

        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"{nameof(Unknown10)}: {Unknown10}\n");
            sb.Append($"{nameof(Unknown11)}: {Unknown11}\n");
            sb.Append($"{nameof(Unknown12)}: {Unknown12}\n");
            sb.Append($"{nameof(Unknown13)}: {Unknown13}\n");
            sb.Append($"{nameof(Unknown14)}: {Unknown14}\n");
            sb.Append($"{nameof(Unknown16)}: {Unknown16}\n");
            sb.Append($"{nameof(Unknown18)}: {Unknown18}\n");
            sb.Append($"{nameof(Unknown1A)}: {Unknown1A}\n");

            sb.Append($"Resource Info: ");
            List<string> infoOutputList = new List<string>();
            foreach (ResourceInfo info in ResourceInfoList)
            {
                StringBuilder sb2 = new StringBuilder();
                sb2.Append("{ ");

                sb2.Append($"{nameof(info.Offset)}: {info.Offset & 0x1FFFFFFF}");
                if ((info.Offset & 0xE0000000) == 0x20000000)
                    sb2.Append(" (located in SRDI)");
                else if ((info.Offset & 0xE0000000) == 0x40000000)
                    sb2.Append(" (located in SRDV)");
                else
                    sb2.Append(" (unknown location)");
                sb2.Append(", ");

                sb2.Append($"{nameof(info.Length)}: {info.Length}, ");
                sb2.Append($"{nameof(info.Unknown08)}: {info.Unknown08}, ");
                sb2.Append($"{nameof(info.Unknown0C)}: {info.Unknown0C}");

                sb2.Append(" }");
                infoOutputList.Add(sb2.ToString());
            }
            sb.AppendJoin(", ", infoOutputList);
            sb.Append('\n');

            sb.Append($"Resource Data length: {ResourceData.Length:n0} bytes\n");

            sb.Append($"Resource Strings: ");
            sb.AppendJoin(", ", ResourceStringList);

            return sb.ToString();
        }
    }
}
