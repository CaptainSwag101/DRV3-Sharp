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
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp.Formats.Resource.SRD.BlockTypes
{
    record MshBlock : ISrdBlock
    {
        public uint Unknown00;
        public ushort Unknown0A;
        public ushort Unknown0C;
        public ushort StringMapDataAlmostEndOffset;
        public byte Unknown10;
        public byte Unknown11;
        public byte Unknown12;
        public byte Unknown13;
        public string MeshName;
        public string VertexBlockName;
        public string MaterialNameReference;
        public string UnknownString;
        public List<string> MappedStrings = new List<string>();

        public MshBlock(byte[] mainData, byte[] subData)
        {
            using BinaryReader reader = new(new MemoryStream(mainData));

            Unknown00 = reader.ReadUInt32();
            ushort vertexBlockNameOffset = reader.ReadUInt16();
            ushort materialNameOffset = reader.ReadUInt16();
            ushort unknownStringOffset = reader.ReadUInt16();
            Unknown0A = reader.ReadUInt16();
            Unknown0C = reader.ReadUInt16();
            StringMapDataAlmostEndOffset = reader.ReadUInt16();
            Unknown10 = reader.ReadByte();
            Unknown11 = reader.ReadByte();
            Unknown12 = reader.ReadByte();
            Unknown13 = reader.ReadByte();

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
                throw new InvalidDataException("The MSH's resource sub-block did not contain the mesh name.");
        }
    }
}
