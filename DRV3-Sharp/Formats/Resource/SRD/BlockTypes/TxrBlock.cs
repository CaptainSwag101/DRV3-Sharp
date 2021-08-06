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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp.Formats.Resource.SRD.BlockTypes
{
    public enum TextureFormat
    {
        Unknown = 0x00,
        ARGB8888 = 0x01,
        BGR565 = 0x02,
        BGRA4444 = 0x05,
        DXT1RGB = 0x0F,
        DXT5 = 0x11,
        BC5 = 0x14,
        BC4 = 0x16,
        Indexed8 = 0x1A,
        BPTC = 0x1C
    }

    /// <summary>
    /// Texture Resource Block
    /// </summary>
    record TxrBlock : ISrdBlock
    {
        public int Unknown10;
        public ushort Swizzle;
        public ushort DisplayWidth;
        public ushort DisplayHeight;
        public ushort Scanline;
        public TextureFormat Format;
        public byte Unknown1D;
        public byte Palette;
        public byte PaletteId;
        public string TextureFilename;
        public List<byte[]> TextureData = new();

        public TxrBlock(byte[] mainData, byte[] subData, Stream? inputSrdvStream)
        {
            // Read main data
            using BinaryReader reader = new(new MemoryStream(mainData));

            Unknown10 = reader.ReadInt32();
            Swizzle = reader.ReadUInt16();
            DisplayWidth = reader.ReadUInt16();
            DisplayHeight = reader.ReadUInt16();
            Scanline = reader.ReadUInt16();
            Format = (TextureFormat)reader.ReadByte();
            Unknown1D = reader.ReadByte();
            Palette = reader.ReadByte();
            PaletteId = reader.ReadByte();

            // Decode the RSI sub-block
            RsiBlock rsi;
            using MemoryStream subBlockStream = new(subData);
            BlockSerializer.Deserialize(subBlockStream, inputSrdvStream, null, out ISrdBlock block);
            if (block is RsiBlock) rsi = (RsiBlock)block;
            else throw new InvalidDataException("The first sub-block was not an RSI block.");

            // Read texture filename
            if (rsi.ResourceStrings.Count > 0)
                TextureFilename = rsi.ResourceStrings[0];
            else
                throw new InvalidDataException("The TXR's resource sub-block did not contain the texture filename.");

            // Read texture/mipmap data
            foreach (var externalData in rsi.ExternalResourceData)
            {
                if (externalData.Location == ResourceDataLocation.Srdv)
                    TextureData.Add(externalData.Data);
            }
        }
    }
}
