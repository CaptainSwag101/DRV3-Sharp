using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public sealed class TxiBlock : Block
    {
        public int Unknown10;
        public int Unknown14;
        public float Unknown18;
        public byte Unknown1C;
        public byte Unknown1D;
        public byte Unknown1E;
        public byte Unknown1F;
        public int Unknown20;
        public string TextureFilename;

        public override void DeserializeData(byte[] rawData, string srdiPath, string srdvPath)
        {
            using BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            Unknown10 = reader.ReadInt32();
            Unknown14 = reader.ReadInt32();
            Unknown18 = reader.ReadSingle();
            Unknown1C = reader.ReadByte();
            Unknown1D = reader.ReadByte();
            Unknown1E = reader.ReadByte();
            Unknown1F = reader.ReadByte();
            Unknown20 = reader.ReadInt32();
            TextureFilename = Utils.ReadNullTerminatedString(reader, Encoding.GetEncoding("shift-jis"));
        }

        public override byte[] SerializeData(string srdiPath, string srdvPath)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);

            writer.Write(Unknown10);
            writer.Write(Unknown14);
            writer.Write(Unknown18);
            writer.Write(Unknown1C);
            writer.Write(Unknown1D);
            writer.Write(Unknown1E);
            writer.Write(Unknown1F);
            writer.Write(Unknown20);
            writer.Write(Encoding.GetEncoding("shift-jis").GetBytes(TextureFilename));
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
            sb.Append($"{nameof(Unknown1D)}: {Unknown1D}\n");
            sb.Append($"{nameof(Unknown1E)}: {Unknown1E}\n");
            sb.Append($"{nameof(Unknown1F)}: {Unknown1F}\n");
            sb.Append($"{nameof(Unknown20)}: {Unknown20}\n");
            sb.Append($"Texture Filename: {TextureFilename}");

            return sb.ToString();
        }
    }
}
