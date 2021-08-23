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

namespace DRV3_Sharp_Library.Formats.Resource.SRD.BlockTypes
{
    record MatBlock : ISrdBlock
    {
        public uint Unknown00;
        public float Unknown04;
        public float Unknown08;
        public float Unknown0C;
        public ushort Unknown10;
        public ushort Unknown12;
        public List<(string, string)> MapTexturePairs = new();
        public List<(string Name, byte[] Data)> ExtraMaterialInfo = new();
        public string MaterialName;
        public string[] MaterialShaderReferences;

        public MatBlock(byte[] mainData, byte[] subData)
        {
            // Read main data
            using BinaryReader reader = new(new MemoryStream(mainData));

            Unknown00 = reader.ReadUInt32();
            Unknown04 = reader.ReadSingle();
            Unknown08 = reader.ReadSingle();
            Unknown0C = reader.ReadSingle();
            Unknown10 = reader.ReadUInt16();
            Unknown12 = reader.ReadUInt16();
            ushort stringMapStart = reader.ReadUInt16();
            ushort stringMapCount = reader.ReadUInt16();

            reader.BaseStream.Seek(stringMapStart, SeekOrigin.Begin);
            for (int m = 0; m < stringMapCount; ++m)
            {
                ushort textureNameOffset = reader.ReadUInt16();
                ushort mapNameOffset = reader.ReadUInt16();

                long oldPos = reader.BaseStream.Position;

                reader.BaseStream.Seek(textureNameOffset, SeekOrigin.Begin);
                string textureName = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
                reader.BaseStream.Seek(mapNameOffset, SeekOrigin.Begin);
                string mapName = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);

                MapTexturePairs.Add((mapName, textureName));

                reader.BaseStream.Seek(oldPos, SeekOrigin.Begin);
            }

            // Decode the RSI sub-block
            RsiBlock rsi;
            using MemoryStream subBlockStream = new(subData);
            BlockSerializer.Deserialize(subBlockStream, null, null, out ISrdBlock block);
            if (block is RsiBlock) rsi = (RsiBlock)block;
            else throw new InvalidDataException("The first sub-block was not an RSI block.");

            // Copy over extra shader/material info from the RSI data
            ExtraMaterialInfo = rsi.LocalResourceData;

            // Material name is the first RSI resource string
            if (rsi.ResourceStrings.Count > 0)
                MaterialName = rsi.ResourceStrings[0];
            else
                throw new InvalidDataException("The MAT's resource sub-block did not contain the material name.");

            MaterialShaderReferences = new string[rsi.ResourceStrings.Count - 1];
            if (rsi.ResourceStrings.Count > 1)
            {
                rsi.ResourceStrings.CopyTo(1, MaterialShaderReferences, 0, rsi.ResourceStrings.Count - 1);
            }
        }
    }
}
