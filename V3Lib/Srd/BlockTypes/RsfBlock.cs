using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public sealed class RsfBlock : Block
    {
        public int Unknown10;
        public int Unknown14;
        public int Unknown18;
        public int Unknown1C;
        public string FolderName;

        public override void DeserializeData(byte[] rawData, string srdiPath, string srdvPath)
        {
            using BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            Unknown10 = reader.ReadInt32();
            Unknown14 = reader.ReadInt32();
            Unknown18 = reader.ReadInt32();
            Unknown1C = reader.ReadInt32();
            FolderName = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
        }

        public override byte[] SerializeData(string srdiPath, string srdvPath)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);

            writer.Write(Unknown10);
            writer.Write(Unknown14);
            writer.Write(Unknown18);
            writer.Write(Unknown1C);
            writer.Write(Encoding.ASCII.GetBytes(FolderName));
            writer.Write((byte)0);  // Null terminator

            byte[] result = ms.ToArray();
            return result;
        }
        
        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"{nameof(Unknown10)}: {Unknown10}\n");
            sb.Append($"{nameof(Unknown14)}: {Unknown14}\n");
            sb.Append($"{nameof(Unknown18)}: {Unknown18}\n");
            sb.Append($"{nameof(Unknown1C)}: {Unknown1C}\n");
            sb.Append($"Folder Name: {FolderName}");

            return sb.ToString();
        }
    }
}
