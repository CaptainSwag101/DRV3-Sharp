using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public sealed class ScnBlock : Block
    {
        public uint Unknown10;
        public List<string> SceneRootNodes = new List<string>();
        public List<string> UnknownStrings = new List<string>();

        public override void DeserializeData(byte[] rawData, string srdiPath, string srdvPath)
        {
            using BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            Unknown10 = reader.ReadUInt32();
            ushort sceneRootNodeIndexOffset = reader.ReadUInt16();
            ushort sceneRootNodeIndexCount = reader.ReadUInt16();
            ushort unknownStringIndexOffset = reader.ReadUInt16();
            ushort unknownStringIndexCount = reader.ReadUInt16();

            // Read scene root node names
            reader.BaseStream.Seek(sceneRootNodeIndexOffset, SeekOrigin.Begin);
            for (int i = 0; i < sceneRootNodeIndexCount; ++i)
            {
                ushort stringOffset = reader.ReadUInt16();
                long oldPos = reader.BaseStream.Position;
                reader.BaseStream.Seek(stringOffset, SeekOrigin.Begin);
                SceneRootNodes.Add(Utils.ReadNullTerminatedString(reader, Encoding.ASCII));
                reader.BaseStream.Seek(oldPos, SeekOrigin.Begin);
            }

            // Read unknown strings
            reader.BaseStream.Seek(unknownStringIndexOffset, SeekOrigin.Begin);
            for (int i = 0; i < unknownStringIndexCount; ++i)
            {
                ushort stringOffset = reader.ReadUInt16();
                long oldPos = reader.BaseStream.Position;
                reader.BaseStream.Seek(stringOffset, SeekOrigin.Begin);
                UnknownStrings.Add(Utils.ReadNullTerminatedString(reader, Encoding.ASCII));
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

            sb.Append($"{nameof(SceneRootNodes)}: ");
            sb.AppendJoin(", ", SceneRootNodes);
            sb.Append('\n');

            sb.Append($"{nameof(UnknownStrings)}: ");
            sb.AppendJoin(", ", UnknownStrings);

            return sb.ToString();
        }
    }
}
