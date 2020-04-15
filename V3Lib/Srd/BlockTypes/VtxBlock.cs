using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    // Holds information about vertex data and index lists
    public sealed class VtxBlock : Block
    {
        public int HalfFloatTripletCount;   // Likely the number of half-float triplets in the "float list"
        public short Unknown14;
        public short Unknown16;
        public int VertexCount;
        public short Unknown1C;
        public byte Unknown1E;
        public byte VertexSubBlockCount;
        public short BindBoneListOffset;
        public short VertexSubBlockListOffset;
        public short HalfFloatListOffset;
        public List<short> UnknownShortList;
        public List<(int Offset, int Size)> VertexSubBlockList;
        public short BindBoneRoot;
        public List<short> BindBoneList;
        public List<float> UnknownHalfFloatList;
        public List<string> UnknownStringList;

        public override void DeserializeData(byte[] rawData)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            HalfFloatTripletCount = reader.ReadInt32();
            Unknown14 = reader.ReadInt16();
            Unknown16 = reader.ReadInt16();
            VertexCount = reader.ReadInt32();
            Unknown1C = reader.ReadInt16();
            Unknown1E = reader.ReadByte();
            VertexSubBlockCount = reader.ReadByte();
            BindBoneListOffset = reader.ReadInt16();
            VertexSubBlockListOffset = reader.ReadInt16();
            HalfFloatListOffset = reader.ReadInt16();

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
            reader.BaseStream.Seek(BindBoneListOffset, SeekOrigin.Begin);
            BindBoneList = new List<short>();
            BindBoneRoot = reader.ReadInt16();
            while (reader.BaseStream.Position < HalfFloatListOffset)
            {
                BindBoneList.Add(reader.ReadInt16());
            }

            // Read unknown list of floats
            reader.BaseStream.Seek(HalfFloatListOffset, SeekOrigin.Begin);
            UnknownHalfFloatList = new List<float>();
            for (int h = 0; h < HalfFloatTripletCount; ++h)
            {
                // TODO: THIS IS WRONG, READ AS HALF FLOATS AND NOT INT16
                UnknownHalfFloatList.Add((float)reader.ReadInt16());
                UnknownHalfFloatList.Add((float)reader.ReadInt16());
                UnknownHalfFloatList.Add((float)reader.ReadInt16());
            }

            // Read unknown string data
            UnknownStringList = new List<string>();
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                UnknownStringList.Add(Utils.ReadNullTerminatedString(ref reader, new ASCIIEncoding()));
            }

            reader.Close();
            reader.Dispose();
        }

        public override byte[] SerializeData()
        {
            throw new NotImplementedException();
        }

        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"{nameof(HalfFloatTripletCount)}: {HalfFloatTripletCount}\n");
            sb.Append($"{nameof(Unknown14)}: {Unknown14}\n");
            sb.Append($"{nameof(Unknown16)}: {Unknown16}\n");
            sb.Append($"{nameof(VertexCount)}: {VertexCount}\n");
            sb.Append($"{nameof(Unknown1C)}: {Unknown1C}\n");
            sb.Append($"{nameof(Unknown1E)}: {Unknown1E}\n");
            sb.Append($"{nameof(BindBoneListOffset)}: {BindBoneListOffset}\n");
            sb.Append($"{nameof(VertexSubBlockListOffset)}: {VertexSubBlockListOffset}\n");
            sb.Append($"{nameof(HalfFloatListOffset)}: {HalfFloatListOffset}\n");

            sb.Append($"{nameof(UnknownShortList)}: ");
            sb.AppendJoin(", ", UnknownShortList);
            sb.Append('\n');

            //sb.Append($"{nameof(UnknownDataSize)}: {UnknownDataSize}\n");
            //sb.Append($"{nameof(VertexDataSize)}: {VertexDataSize}\n");
            sb.Append($"{nameof(BindBoneRoot)}: {BindBoneRoot}\n");
            sb.Append($"{nameof(BindBoneListOffset)}: {BindBoneListOffset}\n");

            sb.Append($"{nameof(UnknownHalfFloatList)}: ");
            sb.AppendJoin(", ", UnknownHalfFloatList);
            sb.Append('\n');

            sb.Append($"{nameof(UnknownStringList)}: ");
            sb.AppendJoin(", ", UnknownStringList);

            return sb.ToString();
        }
    }
}
