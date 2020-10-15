using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public sealed class SklBlock : Block
    {
        public uint Unknown10;
        public ushort Unknown14;
        public ushort BoneCount;
        public ushort Unknown18;
        public ushort BoneNameListOffset;
        public uint NullBoneNameOffset;
        public List<(ushort ParentBoneID, ushort Unknown, float[,] Matrix1, float[,] Matrix2, float[,] Matrix3, string BoneName)> BoneInfoList;

        public override void DeserializeData(byte[] rawData, string srdiPath, string srdvPath)
        {
            using BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            Unknown10 = reader.ReadUInt32();
            Unknown14 = reader.ReadUInt16();
            BoneCount = reader.ReadUInt16();
            Unknown18 = reader.ReadUInt16();
            BoneNameListOffset = reader.ReadUInt16();
            NullBoneNameOffset = reader.ReadUInt32();

            BoneInfoList = new List<(ushort ParentBoneID, ushort Unknown, float[,] Matrix1, float[,] Matrix2, float[,] Matrix3, string BoneName)>();
            for (int b = 0; b < BoneCount; ++b)
            {
                ushort id = reader.ReadUInt16BE();
                ushort unk = reader.ReadUInt16();

                float[,] mat1 = new float[3,4];
                for (int x = 0; x < 3; ++x)
                {
                    for (int y = 0; y < 4; ++y)
                    {
                        mat1[x, y] = reader.ReadSingle();
                    }
                }

                float[,] mat2 = new float[3, 4];
                for (int x = 0; x < 3; ++x)
                {
                    for (int y = 0; y < 4; ++y)
                    {
                        mat2[x, y] = reader.ReadSingle();
                    }
                }

                float[,] mat3 = new float[2, 3];
                for (int x = 0; x < 2; ++x)
                {
                    for (int y = 0; y < 3; ++y)
                    {
                        mat1[x, y] = reader.ReadSingle();
                    }
                }

                uint nameOffset = reader.ReadUInt32();
                long oldPos = reader.BaseStream.Position;
                reader.BaseStream.Seek(nameOffset, SeekOrigin.Begin);
                string name = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
                reader.BaseStream.Seek(oldPos, SeekOrigin.Begin);

                BoneInfoList.Add((id, unk, mat1, mat2, mat3, name));
            }

            return;
        }

        public override byte[] SerializeData(string srdiPath, string srdvPath)
        {
            throw new NotImplementedException();
        }

        public override string GetInfo()
        {
            StringBuilder sb1 = new StringBuilder();
            List<string> tmp = new List<string>();
            foreach (var (ParentBoneID, Unknown, Matrix1, Matrix2, Matrix3, BoneName) in BoneInfoList)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"Parent Bone ID: {ParentBoneID}, Unknown: {Unknown}, Name: {BoneName}\n");

                sb.Append("{matrix1\n");
                for (int y = 0; y < 4; ++y)
                {
                    for (int x = 0; x < 3; ++x)
                    {
                        sb.Append(Matrix1[x, y]);
                        if (x < 2) sb.Append(' ');
                    }
                    if (y < 3) sb.Append('\n');
                }
                sb.Append("\n}");

                sb.Append("\n{matrix2\n");
                for (int y = 0; y < 4; ++y)
                {
                    for (int x = 0; x < 3; ++x)
                    {
                        sb.Append(Matrix2[x, y]);
                        if (x < 2) sb.Append(' ');
                    }
                    if (y < 3) sb.Append('\n');
                }
                sb.Append("\n}");

                sb.Append("\n{matrix3\n");
                for (int y = 0; y < 3; ++y)
                {
                    for (int x = 0; x < 2; ++x)
                    {
                        sb.Append(Matrix3[x, y]);
                        if (x < 1) sb.Append(' ');
                    }
                    if (y < 2) sb.Append('\n');
                }
                sb.Append("\n}");

                tmp.Add(sb.ToString());
            }
            sb1.AppendJoin("\n", tmp);

            return sb1.ToString();
        }
    }
}
