using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public sealed class RsiBlock : Block
    {
        public byte Unknown10;
        public byte Unknown11;
        public byte Unknown12;
        public byte Unknown13;
        public int Unknown14;
        public int Unknown18;
        public byte[] ResourceData;
        public List<string> ResourceStrings;

        public override void DeserializeData(byte[] rawData)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            Unknown10 = reader.ReadByte();
            Unknown11 = reader.ReadByte();
            Unknown12 = reader.ReadByte();
            Unknown13 = reader.ReadByte();
            Unknown14 = reader.ReadInt32();
            Unknown18 = reader.ReadInt32();
            int resourceDataLength = reader.ReadInt32();
            ResourceData = reader.ReadBytes(resourceDataLength - 0x10);

            // Read resource strings
            ResourceStrings = new List<string>();
            do
            {
                ResourceStrings.Add(Utils.ReadNullTerminatedString(ref reader, new ASCIIEncoding()));
            }
            while (reader.BaseStream.Position < reader.BaseStream.Length);

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
            writer.Write(Unknown18);
            writer.Write(ResourceData.Length + 0x10);
            writer.Write(ResourceData);

            foreach (string resourceString in ResourceStrings)
            {
                writer.Write(new ASCIIEncoding().GetBytes(resourceString));
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
            sb.Append($"{nameof(Unknown18)}: {Unknown18}\n");
            sb.Append($"Resource Data Length: {ResourceData.Length:n0} bytes\n");
            sb.Append($"Resource Strings: ");
            sb.AppendJoin(", ", ResourceStrings);

            return sb.ToString();
        }
    }
}
