using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp.Formats.Resource.SRD.BlockTypes
{
    record MshBlock : ISrdBlock
    {
        public uint Unknown10;
        public ushort Unknown1A;
        public ushort Unknown1C;
        public ushort StringMapDataAlmostEndOffset;
        public byte Unknown20;
        public byte Unknown21;
        public byte Unknown22;
        public byte Unknown23;
        public string MeshName;
        public string VertexBlockName;
        public string MaterialNameReference;
        public string UnknownString;
        public List<string> MappedStrings = new List<string>();

        public MshBlock(byte[] mainData, byte[] subData)
        {
            using BinaryReader reader = new(new MemoryStream(mainData));

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
            MaterialNameReference = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
            reader.BaseStream.Seek(unknownStringOffset, SeekOrigin.Begin);
            UnknownString = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);

            // Decode the RSI sub-block
            RsiBlock rsi;
            using MemoryStream subBlockStream = new(subData);
            BlockSerializer.Deserialize(subBlockStream, null, null, out ISrdBlock block);
            if (block is RsiBlock) rsi = (RsiBlock)block;
            else throw new InvalidDataException("The first sub-block was not an RSI block.");

            // Read full mesh name
            if (rsi.ResourceStrings.Count > 0)
                MeshName = rsi.ResourceStrings[0];
            else
                throw new InvalidDataException("The TXR's resource sub-block did not contain the mesh name.");
        }
    }
}
