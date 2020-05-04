using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    // Holds information about vertex data and index lists
    public sealed class VtxBlock : Block
    {
        public int FloatTripletCount;   // Likely the number of half-float triplets in the "float list"
        public short Unknown14;
        public short Unknown16;
        public int VertexCount;
        public short Unknown1C;
        public byte Unknown1E;
        public byte VertexSubBlockCount;
        public short BindBoneRootOffset;
        public short VertexSubBlockListOffset;
        public short UnknownFloatListOffset;
        public short BindBoneListOffset;
        public short Unknown28;
        public List<short> UnknownShortList;
        public List<(int Offset, int Size)> VertexSubBlockList;
        public short BindBoneRoot;
        public List<short> BindBoneList;
        public List<(float F1, float F2, float F3)> UnknownFloatList;
        public List<string> UnknownStringList;

        public override void DeserializeData(byte[] rawData)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            FloatTripletCount = reader.ReadInt32();
            Unknown14 = reader.ReadInt16();
            Unknown16 = reader.ReadInt16();
            VertexCount = reader.ReadInt32();
            Unknown1C = reader.ReadInt16();
            Unknown1E = reader.ReadByte();
            VertexSubBlockCount = reader.ReadByte();
            BindBoneRootOffset = reader.ReadInt16();
            VertexSubBlockListOffset = reader.ReadInt16();
            UnknownFloatListOffset = reader.ReadInt16();
            BindBoneListOffset = reader.ReadInt16();
            Unknown28 = reader.ReadInt16();
            Utils.ReadPadding(ref reader, 16);

            // Read unknown list of shorts
            UnknownShortList = new List<short>();
            while (reader.BaseStream.Position < VertexSubBlockListOffset)
            {
                UnknownShortList.Add(reader.ReadInt16());
            }

            // Read vertex sub-blocks
            reader.BaseStream.Seek(VertexSubBlockListOffset, SeekOrigin.Begin);
            VertexSubBlockList = new List<(int Offset, int Size)>();
            for (int s = 0; s < VertexSubBlockCount; ++s)
            {
                VertexSubBlockList.Add((reader.ReadInt32(), reader.ReadInt32()));
            }

            // Read bone list
            reader.BaseStream.Seek(BindBoneRootOffset, SeekOrigin.Begin);
            BindBoneRoot = reader.ReadInt16();

            if (BindBoneListOffset != 0)
                reader.BaseStream.Seek(BindBoneListOffset, SeekOrigin.Begin);

            BindBoneList = new List<short>();
            while (reader.BaseStream.Position < UnknownFloatListOffset)
            {
                BindBoneList.Add(reader.ReadInt16());
            }

            // Read unknown list of floats
            reader.BaseStream.Seek(UnknownFloatListOffset, SeekOrigin.Begin);
            UnknownFloatList = new List<(float F1, float F2, float F3)>();
            for (int h = 0; h < FloatTripletCount / 2; ++h)
            {
                var floatTriplet = (
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                    );

                UnknownFloatList.Add(floatTriplet);
            }

            // Read unknown string data
            UnknownStringList = new List<string>();
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                UnknownStringList.Add(Utils.ReadNullTerminatedString(ref reader, Encoding.ASCII));
            }

            reader.Close();
            reader.Dispose();
        }

        public override byte[] SerializeData()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(ms);

            writer.Write((int)(UnknownFloatList.Count * 2));
            writer.Write(Unknown14);
            writer.Write(Unknown16);
            writer.Write(VertexCount);
            writer.Write(Unknown1C);
            writer.Write(Unknown1E);
            writer.Write((byte)VertexSubBlockList.Count);
            writer.Write((short)0);     // Placeholder for BindBoneRootOffset
            writer.Write((short)0);     // Placeholder for VertexSubBlockListOffset
            writer.Write((short)0);     // Placeholder for FloatListOffset
            writer.Write((short)0);     // Placeholder for BindBoneListOffset
            writer.Write(Unknown28);
            Utils.WritePadding(ref writer, 16);

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
            foreach (var subBlock in VertexSubBlockList)
            {
                writer.Write(subBlock.Offset);
                writer.Write(subBlock.Size);
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
            foreach (var triplet in UnknownFloatList)
            {
                writer.Write(triplet.F1);
                writer.Write(triplet.F2);
                writer.Write(triplet.F3);
            }

            // Write unknown string data
            foreach (string str in UnknownStringList)
            {
                writer.Write(Encoding.ASCII.GetBytes(str));
                writer.Write((byte)0);  // Null terminator
            }

            byte[] result = ms.ToArray();
            writer.Close();
            writer.Dispose();
            return result;
        }

        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"{nameof(Unknown14)}: {Unknown14}\n");
            sb.Append($"{nameof(Unknown16)}: {Unknown16}\n");
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

            sb.Append($"{nameof(UnknownFloatList)}: ");
            sb.AppendJoin(", ", UnknownFloatList);
            sb.Append('\n');

            sb.Append($"{nameof(UnknownStringList)}: ");
            sb.AppendJoin(", ", UnknownStringList);

            return sb.ToString();
        }
    }
}
