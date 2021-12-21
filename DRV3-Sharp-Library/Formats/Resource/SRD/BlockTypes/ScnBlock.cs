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
    public class ScnBlock : ISrdBlock
    {
        public uint Unknown00;
        public List<string> SceneRootNodes = new();
        public List<string> UnknownStrings = new();
        public string SceneName;
        public int OpMdlVersion;
        public int Op2MdlType;

        public ScnBlock(byte[] mainData, byte[] subData)
        {
            // Read main data
            using BinaryReader reader = new(new MemoryStream(mainData));

            Unknown00 = reader.ReadUInt32();
            ushort sceneRootNodeIndexOffset = reader.ReadUInt16();
            ushort sceneRootNodeIndexCount = reader.ReadUInt16();
            ushort unknownStringIndexOffset = reader.ReadUInt16();
            ushort unknownStringIndexCount = reader.ReadUInt16();

            // Read scene root node names
            reader.BaseStream.Seek(sceneRootNodeIndexOffset, SeekOrigin.Begin);
            for (int i = 0; i < sceneRootNodeIndexCount; ++i)
            {
                ushort stringOffset = reader.ReadUInt16();
                long oldPos = reader.BaseStream.Position;
                reader.BaseStream.Seek(stringOffset, SeekOrigin.Begin);
                SceneRootNodes.Add(Utils.ReadNullTerminatedString(reader, Encoding.ASCII));
                reader.BaseStream.Seek(oldPos, SeekOrigin.Begin);
            }

            // Read unknown strings
            reader.BaseStream.Seek(unknownStringIndexOffset, SeekOrigin.Begin);
            for (int i = 0; i < unknownStringIndexCount; ++i)
            {
                ushort stringOffset = reader.ReadUInt16();
                long oldPos = reader.BaseStream.Position;
                reader.BaseStream.Seek(stringOffset, SeekOrigin.Begin);
                UnknownStrings.Add(Utils.ReadNullTerminatedString(reader, Encoding.ASCII));
                reader.BaseStream.Seek(oldPos, SeekOrigin.Begin);
            }

            // Decode the RSI sub-block
            RsiBlock rsi;
            using MemoryStream subBlockStream = new(subData);
            BlockSerializer.Deserialize(subBlockStream, null, null, out ISrdBlock block);
            if (block is RsiBlock) rsi = (RsiBlock)block;
            else throw new InvalidDataException("The first sub-block was not an RSI block.");

            // Read texture filename
            if (rsi.ResourceStrings.Count > 0)
                SceneName = rsi.ResourceStrings[0];
            else
                throw new InvalidDataException("The SCN's resource sub-block did not contain the scene name.");

            // Read texture/mipmap data
            if (rsi.LocalResourceData.Count != 2) throw new InvalidDataException("The SCN's resource sub-block did not contain the expected local resource count.");
            OpMdlVersion = BitConverter.ToInt32(rsi.LocalResourceData[0].Data);
            Op2MdlType = BitConverter.ToInt32(rsi.LocalResourceData[1].Data);
        }
    }
}
