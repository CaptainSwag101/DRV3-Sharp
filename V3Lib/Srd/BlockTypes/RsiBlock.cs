using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public struct ResourceInfo : IEquatable<ResourceInfo>
    {
        public int[] Values;

        public override bool Equals(object obj)
        {
            return obj is ResourceInfo info && Equals(info);
        }

        public bool Equals(ResourceInfo other)
        {
            if (Values.Length != other.Values.Length)
                return false;
            else
                for (int i = 0; i < Values.Length; ++i)
                    if (Values[i] != other.Values[i])
                        return false;
                return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Values);
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
        public byte FallbackResourceInfoCount;
        public short ResourceInfoCount;
        public short FallbackResourceInfoSize;
        public short ResourceInfoSize;
        public short Unknown1A;
        public List<ResourceInfo> ResourceInfoList;
        public byte[] ResourceData;
        public List<string> ResourceStringList;

        public override void DeserializeData(byte[] rawData)
        {
            using BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            // TODO: If Unknown10 == 0x04, maybe we read the remaining numbers as shorts, otherwise as ints?
            Unknown10 = reader.ReadByte();
            Unknown11 = reader.ReadByte();
            Unknown12 = reader.ReadByte();
            FallbackResourceInfoCount = reader.ReadByte();
            ResourceInfoCount = reader.ReadInt16();
            FallbackResourceInfoSize = reader.ReadInt16();
            ResourceInfoSize = reader.ReadInt16();
            Unknown1A = reader.ReadInt16();
            int resourceStringListOffset = reader.ReadInt32();

            // Read resource info
            int resourceInfoCount = (ResourceInfoCount != 0 ? ResourceInfoCount : FallbackResourceInfoCount);
            ResourceInfoList = new List<ResourceInfo>();
            for (int r = 0; r < resourceInfoCount; ++r)
            {
                int size = 4;
                if (ResourceInfoSize > 0)
                    size = ResourceInfoSize / 4;
                else if (FallbackResourceInfoSize > 0)
                    size = FallbackResourceInfoSize / 4;

                ResourceInfo info;
                info.Values = new int[size];
                for (int i = 0; i < size; ++i)
                {
                    info.Values[i] = reader.ReadInt32();
                }
                ResourceInfoList.Add(info);
            }

            // Read resource data 
            int dataLength = (resourceStringListOffset - (int)reader.BaseStream.Position);
            ResourceData = reader.ReadBytes(dataLength);

            // Read resource strings
            ResourceStringList = new List<string>();
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                ResourceStringList.Add(Utils.ReadNullTerminatedString(reader, Encoding.GetEncoding("shift-jis")));
            }
        }

        public override byte[] SerializeData()
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);

            writer.Write(Unknown10);
            writer.Write(Unknown11);
            writer.Write(Unknown12);
            writer.Write(FallbackResourceInfoCount);
            writer.Write(ResourceInfoCount);
            writer.Write(FallbackResourceInfoSize);
            writer.Write(ResourceInfoSize);
            writer.Write(Unknown1A);
            writer.Write((ResourceInfoList.Count * 0x10) + ResourceData.Length + 0x10);
            
            foreach (ResourceInfo info in ResourceInfoList)
            {
                foreach (int value in info.Values)
                {
                    writer.Write(value);
                }
            }

            writer.Write(ResourceData);

            foreach (string resourceString in ResourceStringList)
            {
                writer.Write(Encoding.GetEncoding("shift-jis").GetBytes(resourceString));
                writer.Write((byte)0);  // Null terminator
            }

            byte[] result = ms.ToArray();
            return result;
        }

        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"{nameof(Unknown10)}: {Unknown10}\n");
            sb.Append($"{nameof(Unknown11)}: {Unknown11}\n");
            sb.Append($"{nameof(Unknown12)}: {Unknown12}\n");
            sb.Append($"{nameof(FallbackResourceInfoCount)}: {FallbackResourceInfoCount}\n");
            sb.Append($"{nameof(ResourceInfoCount)}: {ResourceInfoCount}\n");
            sb.Append($"{nameof(FallbackResourceInfoSize)}: {FallbackResourceInfoSize}\n");
            sb.Append($"{nameof(ResourceInfoSize)}: {ResourceInfoSize}\n");
            sb.Append($"{nameof(Unknown1A)}: {Unknown1A}\n");

            sb.Append($"Resource Info: ");
            List<string> infoOutputList = new List<string>();
            foreach (ResourceInfo info in ResourceInfoList)
            {
                StringBuilder sb2 = new StringBuilder();
                sb2.Append("{ ");
                foreach (int val in info.Values)
                {

                }
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
