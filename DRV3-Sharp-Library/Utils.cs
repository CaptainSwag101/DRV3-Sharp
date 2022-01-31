/*
    DRV3-Sharp, a free and open-source toolkit
    for working with files and assets from Danganronpa V3.

    Copyright (C) 2020-2022  James Pelster
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
using System.IO;
using System.Text;

namespace DRV3_Sharp_Library
{
    public static class Utils
    {
        // Annoyingly, there's no easy way to read a null-terminated string in .NET
        // (or maybe I'm just a moron), so we have to do it manually.
        public static string ReadNullTerminatedString(BinaryReader reader, Encoding encoding)
        {
            using BinaryReader stringReader = new(reader.BaseStream, encoding, true);
            StringBuilder sb = new();
            while (stringReader.BaseStream.Position < stringReader.BaseStream.Length)
            {
                char c = stringReader.ReadChar();

                // Break on null terminator
                if (c == 0) break;

                sb.Append(c);
            }

            return sb.ToString();
        }

        public static void SkipToNearest(BinaryReader reader, int multipleOf)
        {
            int padLength = multipleOf - (int)(reader.BaseStream.Position % multipleOf);
            if (padLength != multipleOf)
            {
                reader.BaseStream.Seek(padLength, SeekOrigin.Current);
            }
        }

        public static void PadToNearest(BinaryWriter writer, int multipleOf, byte padValue = 0)
        {
            int padLength = multipleOf - (int)(writer.BaseStream.Position % multipleOf);
            if (padLength != multipleOf)
            {
                Span<byte> padding = new byte[padLength];
                padding.Fill(padValue);

                writer.Write(padding);
            }
        }

        public static int PowerOfTwo(int x)
        {
            if (x < 0) return 0;
            --x;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        public static int NearestMultipleOf(int x, int multipleOf)
        {
            return x + (x % multipleOf);
        }
    }
}
