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
    /// <summary>
    /// Texture Instance Block
    /// </summary>
    record TxiBlock : ISrdBlock
    {
        public int Unknown00;
        public int Unknown04;
        public int Unknown08;
        public byte[] Unknown0C;
        public int Unknown10;
        public string TextureFilenameReference;
        public string MaterialNameReference;

        public TxiBlock(byte[] mainData, byte[] subData, Stream? inputSrdvStream, Stream? inputSrdiStream)
        {
            using BinaryReader reader = new(new MemoryStream(mainData));

            Unknown00 = reader.ReadInt32();
            Unknown04 = reader.ReadInt32();
            Unknown08 = reader.ReadInt32();
            Unknown0C = reader.ReadBytes(4);
            Unknown10 = reader.ReadInt32();
            TextureFilenameReference = Utils.ReadNullTerminatedString(reader, Encoding.GetEncoding("shift-jis"));

            // Decode the RSI sub-block for later use
            RsiBlock rsi;
            using MemoryStream subBlockStream = new(subData);
            BlockSerializer.Deserialize(subBlockStream, inputSrdvStream, inputSrdiStream, out ISrdBlock block);
            if (block is RsiBlock) rsi = (RsiBlock)block;
            else throw new InvalidDataException("The first sub-block was not an RSI block.");

            if (rsi.ResourceStrings.Count > 0)
                MaterialNameReference = rsi.ResourceStrings[0];
            else
                throw new InvalidDataException("The TXI's resource sub-block did not contain a material name reference.");

            return;
        }
    }
}
