using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public struct VertexDataSection
    {
        public uint StartOffset;
        public uint SizePerVertex;

        public VertexDataSection(uint start, uint size)
        {
            StartOffset = start;
            SizePerVertex = size;
        }
    }

    // Holds information about vertex data and index lists
    public sealed class VtxBlock : Block
    {
        public int VectorCount;   // Likely the number of half-float triplets in the "float list"
        public short Unknown14;
        public short MeshType;
        public int VertexCount;
        public short Unknown1C;
        public byte Unknown1E;
        public uint Unknown28;
        public List<short> UnknownShortList;
        public List<VertexDataSection> VertexDataSections;
        public short BindBoneRoot;
        public List<string> BindBoneList;
        public List<float> UnknownFloatList;

        public override void DeserializeData(byte[] rawData, string srdiPath, string srdvPath)
        {
            using BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            VectorCount = reader.ReadInt32();
            Unknown14 = reader.ReadInt16();
            MeshType = reader.ReadInt16();
            VertexCount = reader.ReadInt32();
            Unknown1C = reader.ReadInt16();
            Unknown1E = reader.ReadByte();
            byte vertexSubBlockCount = reader.ReadByte();
            ushort bindBoneRootOffset = reader.ReadUInt16();
            ushort vertexSubBlockListOffset = reader.ReadUInt16();
            ushort unknownFloatListOffset = reader.ReadUInt16();
            ushort bindBoneListOffset = reader.ReadUInt16();
            Unknown28 = reader.ReadUInt32();
            Utils.ReadPadding(reader, 16);

            // Read unknown list of shorts
            UnknownShortList = new List<short>();
            while (reader.BaseStream.Position < vertexSubBlockListOffset)
            {
                UnknownShortList.Add(reader.ReadInt16());
            }

            // Read vertex sub-blocks
            reader.BaseStream.Seek(vertexSubBlockListOffset, SeekOrigin.Begin);
            VertexDataSections = new List<VertexDataSection>();
            for (int s = 0; s < vertexSubBlockCount; ++s)
            {
                VertexDataSections.Add(new VertexDataSection(reader.ReadUInt32(), reader.ReadUInt32()));
            }

            // Read bone list
            reader.BaseStream.Seek(bindBoneRootOffset, SeekOrigin.Begin);
            BindBoneRoot = reader.ReadInt16();

            if (bindBoneListOffset != 0)
                reader.BaseStream.Seek(bindBoneListOffset, SeekOrigin.Begin);

            BindBoneList = new List<string>();
            while (reader.BaseStream.Position < unknownFloatListOffset)
            {
                ushort boneNameOffset = reader.ReadUInt16();

                if (boneNameOffset == 0)
                    break;

                long oldPos = reader.BaseStream.Position;
                reader.BaseStream.Seek(boneNameOffset, SeekOrigin.Begin);
                BindBoneList.Add(Utils.ReadNullTerminatedString(reader, Encoding.ASCII));
                reader.BaseStream.Seek(oldPos, SeekOrigin.Begin);
            }

            // Read unknown list of floats
            reader.BaseStream.Seek(unknownFloatListOffset, SeekOrigin.Begin);
            UnknownFloatList = new List<float>();
            for (int h = 0; h < VectorCount / 2; ++h)
            {
                UnknownFloatList.Add(reader.ReadSingle());
                UnknownFloatList.Add(reader.ReadSingle());
                UnknownFloatList.Add(reader.ReadSingle());
            }
        }

        public override byte[] SerializeData(string srdiPath, string srdvPath)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);

            writer.Write((int)((UnknownFloatList.Count / 3.0f) * 2.0f));
            writer.Write(Unknown14);
            writer.Write(MeshType);
            writer.Write(VertexCount);
            writer.Write(Unknown1C);
            writer.Write(Unknown1E);
            writer.Write((byte)VertexDataSections.Count);
            writer.Write((ushort)0);     // Placeholder for BindBoneRootOffset
            writer.Write((ushort)0);     // Placeholder for VertexSubBlockListOffset
            writer.Write((ushort)0);     // Placeholder for FloatListOffset
            writer.Write((ushort)0);     // Placeholder for BindBoneListOffset
            writer.Write(Unknown28);
            Utils.WritePadding(writer, 16);

            // Write unknown list of shorts
            foreach (short s in UnknownShortList)
            {
                writer.Write(s);
            }

            // Write vertex sub-blocks
            long lastPos = writer.BaseStream.Position;
            writer.BaseStream.Seek(0x12, SeekOrigin.Begin);
            writer.Write((ushort)lastPos);   // VertexSubBlockListOffset
            writer.BaseStream.Seek(lastPos, SeekOrigin.Begin);
            foreach (var section in VertexDataSections)
            {
                writer.Write(section.StartOffset);
                writer.Write(section.SizePerVertex);
            }

            // Write bone list
            lastPos = writer.BaseStream.Position;
            writer.BaseStream.Seek(0x10, SeekOrigin.Begin);
            writer.Write((ushort)lastPos);   // BindBoneRootOffset
            writer.BaseStream.Seek(lastPos, SeekOrigin.Begin);
            writer.Write(BindBoneRoot);

            // Write placeholder(s) for BindBoneList
            long bindBoneListOffset = writer.BaseStream.Position;
            writer.BaseStream.Seek(0x26, SeekOrigin.Begin);
            writer.Write((ushort)bindBoneListOffset);
            writer.BaseStream.Seek(bindBoneListOffset, SeekOrigin.Begin);
            foreach (string str in BindBoneList)
            {
                writer.Write((ushort)0);
            }
            Utils.WritePadding(writer, 4);

            // Write unknown list of floats
            lastPos = writer.BaseStream.Position;
            writer.BaseStream.Seek(0x14, SeekOrigin.Begin);
            writer.Write((ushort)lastPos);   // UnknownFloatListOffset
            writer.BaseStream.Seek(lastPos, SeekOrigin.Begin);
            foreach (float f in UnknownFloatList)
            {
                writer.Write(f);
            }

            // Write bone names and store their location
            List<ushort> boneNameOffsets = new List<ushort>();
            foreach (string str in BindBoneList)
            {
                boneNameOffsets.Add((ushort)writer.BaseStream.Position);

                writer.Write(Encoding.ASCII.GetBytes(str));
                writer.Write((byte)0);  // Null terminator
            }

            // Go back and write final values for BindBoneList
            writer.BaseStream.Seek(bindBoneListOffset, SeekOrigin.Begin);
            foreach (ushort offset in boneNameOffsets)
            {
                writer.Write(offset);
            }

            byte[] result = ms.ToArray();
            return result;
        }

        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"{nameof(Unknown14)}: {Unknown14}\n");
            sb.Append($"{nameof(MeshType)}: {MeshType}\n");
            sb.Append($"{nameof(VertexCount)}: {VertexCount}\n");
            sb.Append($"{nameof(Unknown1C)}: {Unknown1C}\n");
            sb.Append($"{nameof(Unknown1E)}: {Unknown1E}\n");
            sb.Append($"{nameof(Unknown28)}: {Unknown28}\n");

            sb.Append($"{nameof(UnknownShortList)}: ");
            sb.AppendJoin(", ", UnknownShortList);
            sb.Append('\n');

            sb.Append($"{nameof(VertexDataSections)}: ");
            sb.AppendJoin(", ", VertexDataSections);
            sb.Append('\n');

            sb.Append($"{nameof(BindBoneRoot)}: {BindBoneRoot}\n");

            sb.Append($"{nameof(BindBoneList)}: ");
            sb.AppendJoin(", ", BindBoneList);
            sb.Append('\n');

            sb.Append($"{nameof(UnknownFloatList)}: ");
            sb.AppendJoin(", ", UnknownFloatList);

            return sb.ToString();
        }
    }
}
