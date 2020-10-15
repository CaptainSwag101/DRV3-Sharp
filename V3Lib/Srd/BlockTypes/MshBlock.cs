using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public sealed class MshBlock : Block
    {
        public uint Unknown10;
        public ushort Unknown1A;
        public ushort Unknown1C;
        public ushort StringMapDataAlmostEndOffset;
        public byte Unknown20;
        public byte Unknown21;
        public byte Unknown22;
        public byte Unknown23;
        public string VertexBlockName;
        public string MaterialName;
        public string UnknownString;
        public List<string> MappedStrings = new List<string>();

        public override void DeserializeData(byte[] rawData, string srdiPath, string srdvPath)
        {
            using BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            Unknown10 = reader.ReadUInt32();
            ushort vertexBlockNameOffset = reader.ReadUInt16();
            ushort materialNameOffset = reader.ReadUInt16();
            ushort unknownStringOffset = reader.ReadUInt16();
            Unknown1A = reader.ReadUInt16();
            Unknown1C = reader.ReadUInt16();
            StringMapDataAlmostEndOffset = reader.ReadUInt16();
            Unknown20 = reader.ReadByte();
            Unknown21 = reader.ReadByte();
            Unknown22 = reader.ReadByte();
            Unknown23 = reader.ReadByte();

            // Read string mapping offsets
            while (reader.BaseStream.Position < (StringMapDataAlmostEndOffset + 4))
            {
                ushort strOff = reader.ReadUInt16();
                long oldPos = reader.BaseStream.Position;
                reader.BaseStream.Seek(strOff, SeekOrigin.Begin);
                MappedStrings.Add(Utils.ReadNullTerminatedString(reader, Encoding.ASCII));
                reader.BaseStream.Seek(oldPos, SeekOrigin.Begin);
            }

            // Read other strings
            reader.BaseStream.Seek(vertexBlockNameOffset, SeekOrigin.Begin);
            VertexBlockName = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
            reader.BaseStream.Seek(materialNameOffset, SeekOrigin.Begin);
            MaterialName = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
            reader.BaseStream.Seek(unknownStringOffset, SeekOrigin.Begin);
            UnknownString = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
        }

        public override byte[] SerializeData(string srdiPath, string srdvPath)
        {
            throw new NotImplementedException();
        }

        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"{nameof(Unknown10)}: {Unknown10}\n");
            sb.Append($"{nameof(VertexBlockName)}: {VertexBlockName}\n");
            sb.Append($"{nameof(MaterialName)}: {MaterialName}\n");
            sb.Append($"{nameof(UnknownString)}: {UnknownString}\n");
            sb.Append($"{nameof(Unknown1A)}: {Unknown1A}\n");
            sb.Append($"{nameof(Unknown1C)}: {Unknown1C}\n");
            sb.Append($"{nameof(StringMapDataAlmostEndOffset)}: {StringMapDataAlmostEndOffset}\n");
            sb.Append($"{nameof(Unknown20)}: {Unknown20}\n");
            sb.Append($"{nameof(Unknown21)}: {Unknown21}\n");
            sb.Append($"{nameof(Unknown22)}: {Unknown22}\n");
            sb.Append($"{nameof(Unknown23)}: {Unknown23}\n");

            sb.Append($"{nameof(MappedStrings)}: ");
            sb.AppendJoin(", ", MappedStrings);

            return sb.ToString();
        }
    }
}
