/*
    DRV3-Sharp, a free and open-source toolkit
    for working with files and assets from Danganronpa V3.

    Copyright (C) 2020-2021  James Pelster
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp.Formats.Resource.SRD.BlockTypes
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

    record VtxBlock : ISrdBlock
    {
        // Header data
        public int VectorCount;   // Likely the number of half-float triplets in the "float list"
        public short Unknown04;
        public short MeshType;
        public int VertexCount;
        public short Unknown0C;
        public byte Unknown0E;
        public uint Unknown18;
        public List<short> UnknownShortList;
        public List<VertexDataSection> VertexDataSections;
        public short BindBoneRoot;
        public List<string> BindBoneList;
        public List<float> UnknownFloatList;
        public string VertexGroupName;

        // Geometry data
        public List<Vector3> Vertices = new();
        public List<Vector3> Normals = new();
        public List<Vector2> TextureCoords = new();
        public List<float> Weights = new();
        public List<(ushort, ushort, ushort)> Indices = new();

        public VtxBlock(byte[] mainData, byte[] subData, Stream? inputSrdiStream)
        {
            // Read main data
            using BinaryReader reader = new(new MemoryStream(mainData));

            VectorCount = reader.ReadInt32();
            Unknown04 = reader.ReadInt16();
            MeshType = reader.ReadInt16();
            VertexCount = reader.ReadInt32();
            Unknown0C = reader.ReadInt16();
            Unknown0E = reader.ReadByte();
            byte vertexSubBlockCount = reader.ReadByte();
            ushort bindBoneRootOffset = reader.ReadUInt16();
            ushort vertexSubBlockListOffset = reader.ReadUInt16();
            ushort unknownFloatListOffset = reader.ReadUInt16();
            ushort bindBoneListOffset = reader.ReadUInt16();
            Unknown18 = reader.ReadUInt32();
            Utils.SkipToNearest(reader, 16);

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

            // Decode the RSI sub-block
            RsiBlock rsi;
            using MemoryStream subBlockStream = new(subData);
            BlockSerializer.Deserialize(subBlockStream, null, inputSrdiStream, out ISrdBlock block);
            if (block is RsiBlock) rsi = (RsiBlock)block;
            else throw new InvalidDataException("The first sub-block was not an RSI block.");

            // Read vertex group name reference
            if (rsi.ResourceStrings.Count > 0)
                VertexGroupName = rsi.ResourceStrings[0];
            else
                throw new InvalidDataException("The VTX's resource sub-block did not contain the vertex group name.");

            // Extract geometry data
            using BinaryReader geometryReader = new(new MemoryStream(rsi.ExternalResourceData[0].Data));

            foreach (var section in VertexDataSections)
            {
                geometryReader.BaseStream.Seek(section.StartOffset, SeekOrigin.Begin);
                for (int vNum = 0; vNum < VertexCount; ++vNum)
                {
                    long oldPos = geometryReader.BaseStream.Position;
                    switch (VertexDataSections.IndexOf(section))
                    {
                        case 0: // Vertex/Normal data (and Texture UV for boneless models)
                            {
                                Vector3 vertex;
                                vertex.X = geometryReader.ReadSingle() * -1.0f;         // X
                                vertex.Y = geometryReader.ReadSingle();                 // Y
                                vertex.Z = geometryReader.ReadSingle();                 // Z
                                Vertices.Add(vertex);

                                Vector3 normal;
                                normal.X = geometryReader.ReadSingle() * -1.0f;         // X
                                normal.Y = geometryReader.ReadSingle();                 // Y
                                normal.Z = geometryReader.ReadSingle();                 // Z
                                Normals.Add(normal);

                                if (VertexDataSections.Count == 1)
                                {
                                    //Console.WriteLine($"Mesh type: {MeshType}");
                                    Vector2 texcoord;
                                    texcoord.X = float.PositiveInfinity;
                                    texcoord.X = geometryReader.ReadSingle();           // U
                                    while (float.IsNaN(texcoord.X) || !float.IsFinite(texcoord.X))
                                        texcoord.X = geometryReader.ReadSingle();
                                    texcoord.Y = geometryReader.ReadSingle();           // V, invert for non-glTF exports
                                    while (float.IsNaN(texcoord.Y) || !float.IsFinite(texcoord.Y))
                                        texcoord.Y = geometryReader.ReadSingle();

                                    if (float.IsNaN(texcoord.X) || float.IsNaN(texcoord.Y) || Math.Abs(texcoord.X) > 1 || Math.Abs(texcoord.Y) > 1)
                                    {
                                        //Console.WriteLine($"INVALID UVs DETECTED!");
                                    }
                                    TextureCoords.Add(texcoord);
                                }
                            }
                            break;

                        case 1: // Bone weights?
                            {
                                var weightsPerVert = (section.SizePerVertex / sizeof(float));   // TODO: Is this always 8?
                                for (int wNum = 0; wNum < weightsPerVert; ++wNum)
                                {
                                    Weights.Add(geometryReader.ReadSingle());
                                }
                            }
                            break;

                        case 2: // Texture UVs (only for models with bones)
                            {
                                Vector2 texcoord;
                                texcoord.X = geometryReader.ReadSingle();               // U
                                texcoord.Y = geometryReader.ReadSingle();               // V, invert for non-glTF exports
                                TextureCoords.Add(texcoord);
                            }
                            break;

                        default:
                            Console.WriteLine($"WARNING: Unknown vertex sub-block index {VertexDataSections.IndexOf(section)} is present in VTX block!");
                            break;
                    }

                    // Skip data we don't currently use, though I may add support for this data later
                    long remainingBytes = section.SizePerVertex - (geometryReader.BaseStream.Position - oldPos);
                    geometryReader.BaseStream.Seek(remainingBytes, SeekOrigin.Current);
                }
            }

            // Extract index data
            using BinaryReader indexReader = new(new MemoryStream(rsi.ExternalResourceData[1].Data));
            while (indexReader.BaseStream.Position < indexReader.BaseStream.Length)
            {
                ushort[] indices = new ushort[3];
                for (int i = 0; i < 3; ++i)
                {
                    ushort index = indexReader.ReadUInt16();
                    // We need to reverse the order of the indices to prevent the normals
                    // from becoming permanently flipped due to the clockwise/counter-clockwise
                    // order of the indices determining the face's direction
                    indices[3 - (i + 1)] = index;
                }
                Indices.Add((indices[0], indices[1], indices[2]));
            }
        }
    }
}
