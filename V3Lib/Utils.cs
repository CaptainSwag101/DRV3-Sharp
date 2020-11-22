/*
    V3Lib, an open-source library for reading/writing data from Danganronpa V3
    Copyright (C) 2017-2020  James Pelster

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
using System.Text;

namespace V3Lib
{
    public static class Utils
    {
        // Annoyingly, there's no easy way to read a null-terminated ASCII string in .NET
        // (or maybe I'm just a moron), so we have to do it manually.
        public static string ReadNullTerminatedString(BinaryReader reader, Encoding encoding)
        {
            using BinaryReader stringReader = new BinaryReader(reader.BaseStream, encoding, true);
            StringBuilder sb = new StringBuilder();
            while (stringReader.BaseStream.Position < stringReader.BaseStream.Length)
            {
                char c = stringReader.ReadChar();

                if (c == 0)
                    break;

                sb.Append(c);
            }

            return sb.ToString();
        }

        public static void ReadPadding(BinaryReader reader, int padTo)
        {
            int padLength = padTo - (int)(reader.BaseStream.Position % padTo);
            if (padLength != padTo)
            {
                reader.BaseStream.Seek(padLength, SeekOrigin.Current);
            }
        }

        public static void WritePadding(BinaryWriter writer, int padTo, byte padValue = 0)
        {
            int padLength = padTo - (int)(writer.BaseStream.Position % padTo);
            if (padLength != padTo)
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

        public static byte[] SwapEndian(byte[] bytes)
        {
            Array.Reverse(bytes);
            return bytes;
        }

        // The 4-step method seems to be faster than the 3-step method slightly
        // taken from: https://graphics.stanford.edu/~seander/bithacks.html#ReverseByteWith64Bits
        public static byte ReverseBits(byte b)
        {
            return (byte)((((b * (ulong)0x80200802) & (ulong)0x0884422110) * (ulong)0x0101010101) >> 32);
        }
    }
}
