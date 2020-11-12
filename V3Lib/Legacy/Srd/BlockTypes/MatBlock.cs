using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public sealed class MatBlock : Block
    {
        public uint Unknown10;
        public float Unknown14;
        public float Unknown18;
        public float Unknown1C;
        public ushort Unknown20;
        public ushort Unknown22;
        public Dictionary<string, string> MapTexturePairs = new Dictionary<string, string>();

        public override void DeserializeData(byte[] rawData, string srdiPath, string srdvPath)
        {
            using BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            Unknown10 = reader.ReadUInt32();
            Unknown14 = reader.ReadSingle();
            Unknown18 = reader.ReadSingle();
            Unknown1C = reader.ReadSingle();
            Unknown20 = reader.ReadUInt16();
            Unknown22 = reader.ReadUInt16();
            ushort stringMapStart = reader.ReadUInt16();
            ushort stringMapCount = reader.ReadUInt16();

            reader.BaseStream.Seek(stringMapStart, SeekOrigin.Begin);
            for (int m = 0; m < stringMapCount; ++m)
            {
                ushort textureNameOffset = reader.ReadUInt16();
                ushort mapNameOffset = reader.ReadUInt16();

                long oldPos = reader.BaseStream.Position;

                reader.BaseStream.Seek(textureNameOffset, SeekOrigin.Begin);
                string textureName = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
                reader.BaseStream.Seek(mapNameOffset, SeekOrigin.Begin);
                string mapName = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);

                MapTexturePairs.Add(mapName, textureName);

                reader.BaseStream.Seek(oldPos, SeekOrigin.Begin);
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
            sb.Append($"{nameof(Unknown18)}: {Unknown18}\n");
            sb.Append($"{nameof(Unknown1C)}: {Unknown1C}\n");
            sb.Append($"{nameof(Unknown20)}: {Unknown20}\n");
            sb.Append($"{nameof(Unknown22)}: {Unknown22}\n");

            sb.Append($"Map/Texture pairs: ");
            var infoList = new List<string>();
            foreach (var pair in MapTexturePairs)
            {
                StringBuilder sb2 = new StringBuilder();

                sb2.Append("{ ");
                sb2.Append($"{pair.Key}, {pair.Value}");
                sb2.Append(" }");

                infoList.Add(sb2.ToString());
            }
            sb.AppendJoin(", ", infoList);

            return sb.ToString();
        }
    }
}
