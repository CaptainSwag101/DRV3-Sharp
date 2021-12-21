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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp_Library.Formats.Resource.SRD.BlockTypes
{
    /// <summary>
    /// Texture Instance Block
    /// </summary>
    public class TxiBlock : ISrdBlock
    {
        public int Unknown00;   // Always 1, like the value in $TXR blocks?
        public int Unknown04;   // Always 16?
        public int Unknown08;   // Always 0?
        public byte Unknown0C;  // Always 1?
        public byte Unknown0D;  // Always 1?
        public byte Unknown0E;  // Always 1? (Nope, can sometimes be 2, too)
        public byte Unknown0F;  // Always 5? (Nope, can sometimes be 1 and 3 and 4 and 6, too)
        public int Unknown10;   // Always 20, is this a pointer to the immediately following data?
        public string TextureFilenameReference;
        public string MaterialNameReference;

        public TxiBlock(byte[] mainData, byte[] subData)
        {
            // Read main data
            using BinaryReader reader = new(new MemoryStream(mainData));

            Unknown00 = reader.ReadInt32();
            Debug.Assert(Unknown00 == 1);
            Unknown04 = reader.ReadInt32();
            Debug.Assert(Unknown04 == 16);
            Unknown08 = reader.ReadInt32();
            Debug.Assert(Unknown08 == 0);
            Unknown0C = reader.ReadByte();
            Debug.Assert(Unknown0C == 1);
            Unknown0D = reader.ReadByte();
            Debug.Assert(Unknown0D == 1);
            Unknown0E = reader.ReadByte();
            Debug.Assert(Unknown0E == 1 || Unknown0E == 2);
            Unknown0F = reader.ReadByte();
            Debug.Assert(Unknown0F == 1 || Unknown0F == 3 || Unknown0F == 4 || Unknown0F == 5 || Unknown0F == 6);
            Unknown10 = reader.ReadInt32();
            Debug.Assert(Unknown10 == 20);
            TextureFilenameReference = Utils.ReadNullTerminatedString(reader, Encoding.GetEncoding("shift-jis"));

            // Is there more data after that texture filename reference?
            // If so, it would lend credence to the idea that each $TXI could contain a LIST
            // of texture instances rather than just one per block.
            Debug.Assert(reader.BaseStream.Position == reader.BaseStream.Length);

            // Decode the RSI sub-block for later use
            RsiBlock rsi;
            using MemoryStream subBlockStream = new(subData);
            BlockSerializer.Deserialize(subBlockStream, null, null, out ISrdBlock block);
            if (block is RsiBlock) rsi = (RsiBlock)block;
            else throw new InvalidDataException("The first sub-block was not an RSI block.");

            // Read material name reference
            if (rsi.ResourceStrings.Count > 0)
                MaterialNameReference = rsi.ResourceStrings[0];
            else
                throw new InvalidDataException("The TXI's resource sub-block did not contain the material name reference.");
        }
    }
}
