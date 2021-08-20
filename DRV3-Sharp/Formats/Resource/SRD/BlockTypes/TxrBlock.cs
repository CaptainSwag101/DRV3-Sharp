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
    
    public struct FontGlyphData
    {

    }

    /// <summary>
    /// Texture Resource Block
    /// </summary>
    record TxrBlock : ISrdBlock
    {
        public int Unknown00;
        public ushort Swizzle;
        public ushort DisplayWidth;
        public ushort DisplayHeight;
        public ushort Scanline;
        public TextureFormat Format;
        public byte Unknown0D;
        public byte Palette;
        public byte PaletteId;
        public string TextureFilename;

        public List<byte[]> TextureData = new();
        public List<FontGlyphData>? FontGlyphs = null;

        public TxrBlock(byte[] mainData, byte[] subData, Stream? inputSrdvStream)
        {
            // Read main data
            using BinaryReader reader = new(new MemoryStream(mainData));

            Unknown00 = reader.ReadInt32();
            Swizzle = reader.ReadUInt16();
            DisplayWidth = reader.ReadUInt16();
            DisplayHeight = reader.ReadUInt16();
            Scanline = reader.ReadUInt16();
            Format = (TextureFormat)reader.ReadByte();
            Unknown0D = reader.ReadByte();
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
            foreach (var (data, location) in rsi.ExternalResourceData)
            {
                if (location == ResourceDataLocation.Srdv) TextureData.Add(data);
            }

            // Read font table, if present
            foreach (var (name, data) in rsi.LocalResourceData)
            {
                if (name == "font_table")
                {
                    FontGlyphs = new();

                    // Decode font table
                    using BinaryReader fontReader = new(new MemoryStream(data));
                    //fontReader.BaseStream.Seek(0, SeekOrigin.Begin);

                    string fontMagic = Encoding.ASCII.GetString(fontReader.ReadBytes(4));
                    Debug.Assert(fontMagic == "SpFt");
                    int unknown04 = fontReader.ReadInt32();
                    int potentialGlyphCount = fontReader.ReadInt32();
                    int unknown0C = fontReader.ReadInt32(); // Starting glyph ID?
                    int fontNameAddress = fontReader.ReadInt32();
                    int unknown14 = fontReader.ReadInt32();
                    int boundingBoxListAddress = fontReader.ReadInt32();
                    int unknown1C = fontReader.ReadInt32();
                    int glyphOffsetListAddress = fontReader.ReadInt32();
                    short unknown24 = fontReader.ReadInt16();
                    short unknown26 = fontReader.ReadInt16();
                    int unknownAddressListAddress = fontReader.ReadInt32();
                    int unknown2C = fontReader.ReadInt32(); // Padding?

                    // Read enabled/disabled glyph list
                    // These are single-bit flags, 1 or 0 to indicate that
                    // any given glyph is present or not present in the font, respectively.
                    List<bool> glyphFlags = new();

                    // Always round up to the nearest full byte
                    int glyphFlagByteCount = (int)Math.Ceiling(potentialGlyphCount / 8m);
                    for (int gNum = 0; gNum < glyphFlagByteCount; ++gNum)
                    {
                        byte b = fontReader.ReadByte();
                        for (int i = 0; i < 8; ++i)
                        {
                            if (glyphFlags.Count >= potentialGlyphCount) break;

                            glyphFlags.Add(((b >> i) & 1) > 0);
                        }
                    }

                    Debugger.Break();
                }
            }
        }
    }
}
