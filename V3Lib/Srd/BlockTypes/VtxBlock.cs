using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    // Holds information about vertex data and index lists
    public sealed class VtxBlock : Block
    {
        public int VectorCount;   // Likely the number of half-float triplets in the "float list"
        public short Unknown14;
        public short MeshType;
        public int VertexCount;
        public short Unknown1C;
        public byte Unknown1E;
        public short Unknown28;
        public List<short> UnknownShortList;
        public List<(int Offset, int Size)> VertexSubBlockList;
        public short BindBoneRoot;
        public List<short> BindBoneList;
        public List<Vector3> UnknownVectorList;
        public List<string> UnknownStringList;

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
            short bindBoneRootOffset = reader.ReadInt16();
            short vertexSubBlockListOffset = reader.ReadInt16();
            short unknownFloatListOffset = reader.ReadInt16();
            short bindBoneListOffset = reader.ReadInt16();
            Unknown28 = reader.ReadInt16();
            Utils.ReadPadding(reader, 16);

            // Read unknown list of shorts
            UnknownShortList = new List<short>();
            while (reader.BaseStream.Position < vertexSubBlockListOffset)
            {
                UnknownShortList.Add(reader.ReadInt16());
            }

            // Read vertex sub-blocks
            reader.BaseStream.Seek(vertexSubBlockListOffset, SeekOrigin.Begin);
            VertexSubBlockList = new List<(int Offset, int Size)>();
            for (int s = 0; s < vertexSubBlockCount; ++s)
            {
                VertexSubBlockList.Add((reader.ReadInt32(), reader.ReadInt32()));
            }

            // Read bone list
            reader.BaseStream.Seek(bindBoneRootOffset, SeekOrigin.Begin);
            BindBoneRoot = reader.ReadInt16();

            if (bindBoneListOffset != 0)
                reader.BaseStream.Seek(bindBoneListOffset, SeekOrigin.Begin);

            BindBoneList = new List<short>();
            while (reader.BaseStream.Position < unknownFloatListOffset)
            {
                BindBoneList.Add(reader.ReadInt16());
            }

            // Read unknown list of floats
            reader.BaseStream.Seek(unknownFloatListOffset, SeekOrigin.Begin);
            UnknownVectorList = new List<Vector3>();
            for (int h = 0; h < VectorCount / 2; ++h)
            {
                Vector3 vec;
                vec.X = reader.ReadSingle();
                vec.Y = reader.ReadSingle();
                vec.Z = reader.ReadSingle();

                UnknownVectorList.Add(vec);
            }

            // Read unknown string data
            UnknownStringList = new List<string>();
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                UnknownStringList.Add(Utils.ReadNullTerminatedString(reader, Encoding.ASCII));
            }
        }

        public override byte[] SerializeData(string srdiPath, string srdvPath)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);

            writer.Write((int)(UnknownVectorList.Count * 2));
            writer.Write(Unknown14);
            writer.Write(MeshType);
            writer.Write(VertexCount);
            writer.Write(Unknown1C);
            writer.Write(Unknown1E);
            writer.Write((byte)VertexSubBlockList.Count);
            writer.Write((short)0);     // Placeholder for BindBoneRootOffset
            writer.Write((short)0);     // Placeholder for VertexSubBlockListOffset
            writer.Write((short)0);     // Placeholder for FloatListOffset
            writer.Write((short)0);     // Placeholder for BindBoneListOffset
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
            writer.Write((short)lastPos);   // VertexSubBlockListOffset
            writer.BaseStream.Seek(lastPos, SeekOrigin.Begin);
            foreach (var (Offset, Size) in VertexSubBlockList)
            {
                writer.Write(Offset);
                writer.Write(Size);
            }

            // Write bone list
            lastPos = writer.BaseStream.Position;
            writer.BaseStream.Seek(0x10, SeekOrigin.Begin);
            writer.Write((short)lastPos);   // BindBoneRootOffset
            writer.BaseStream.Seek(lastPos, SeekOrigin.Begin);
            writer.Write(BindBoneRoot);

            if (BindBoneList.Count > 0)
            {
                if (BindBoneList.Count > 1 || (BindBoneList.Count == 1 && BindBoneList[0] != 0))
                {
                    lastPos = writer.BaseStream.Position;
                    writer.BaseStream.Seek(0x16, SeekOrigin.Begin);
                    writer.Write((short)lastPos);   // BindBoneListOffset
                    writer.BaseStream.Seek(lastPos, SeekOrigin.Begin);
                }

                foreach (short bone in BindBoneList)
                {
                    writer.Write(bone);
                }
            }

            // Write unknown list of floats
            lastPos = writer.BaseStream.Position;
            writer.BaseStream.Seek(0x14, SeekOrigin.Begin);
            writer.Write((short)lastPos);   // UnknownFloatListOffset
            writer.BaseStream.Seek(lastPos, SeekOrigin.Begin);
            foreach (Vector3 vec in UnknownVectorList)
            {
                writer.Write(vec.X);
                writer.Write(vec.Y);
                writer.Write(vec.Z);
            }

            // Write unknown string data
            foreach (string str in UnknownStringList)
            {
                writer.Write(Encoding.ASCII.GetBytes(str));
                writer.Write((byte)0);  // Null terminator
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

            sb.Append($"{nameof(VertexSubBlockList)}: ");
            sb.AppendJoin(", ", VertexSubBlockList);
            sb.Append('\n');
            
            sb.Append($"{nameof(BindBoneRoot)}: {BindBoneRoot}\n");

            sb.Append($"{nameof(BindBoneList)}: ");
            sb.AppendJoin(", ", BindBoneList);
            sb.Append('\n');

            sb.Append($"{nameof(UnknownVectorList)}: ");
            sb.AppendJoin(", ", UnknownVectorList);
            sb.Append('\n');

            sb.Append($"{nameof(UnknownStringList)}: ");
            sb.AppendJoin(", ", UnknownStringList);

            return sb.ToString();
        }
    }
}
