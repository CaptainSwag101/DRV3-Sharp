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
    public static class BinaryIOExtensions
    {
        public static short ReadInt16BE(this BinaryReader reader)
        {
            return BitConverter.ToInt16(Utils.SwapEndian(reader.ReadBytes(2)));
        }

        public static ushort ReadUInt16BE(this BinaryReader reader)
        {
            return BitConverter.ToUInt16(Utils.SwapEndian(reader.ReadBytes(2)));
        }

        public static int ReadInt32BE(this BinaryReader reader)
        {
            return BitConverter.ToInt32(Utils.SwapEndian(reader.ReadBytes(4)));
        }

        public static uint ReadUInt32BE(this BinaryReader reader)
        {
            return BitConverter.ToUInt32(Utils.SwapEndian(reader.ReadBytes(4)));
        }

        public static long ReadInt64BE(this BinaryReader reader)
        {
            return BitConverter.ToInt64(Utils.SwapEndian(reader.ReadBytes(8)));
        }

        public static ulong ReadUInt64BE(this BinaryReader reader)
        {
            return BitConverter.ToUInt64(Utils.SwapEndian(reader.ReadBytes(8)));
        }

        public static float ReadSingleBE(this BinaryReader reader)
        {
            return BitConverter.ToSingle(Utils.SwapEndian(reader.ReadBytes(4)));
        }

        public static double ReadDoubleBE(this BinaryReader reader)
        {
            return BitConverter.ToDouble(Utils.SwapEndian(reader.ReadBytes(8)));
        }



        public static void WriteBE(this BinaryWriter writer, short value)
        {
            writer.Write(Utils.SwapEndian(BitConverter.GetBytes(value)));
        }

        public static void WriteBE(this BinaryWriter writer, ushort value)
        {
            writer.Write(Utils.SwapEndian(BitConverter.GetBytes(value)));
        }

        public static void WriteBE(this BinaryWriter writer, int value)
        {
            writer.Write(Utils.SwapEndian(BitConverter.GetBytes(value)));
        }

        public static void WriteBE(this BinaryWriter writer, uint value)
        {
            writer.Write(Utils.SwapEndian(BitConverter.GetBytes(value)));
        }

        public static void WriteBE(this BinaryWriter writer, long value)
        {
            writer.Write(Utils.SwapEndian(BitConverter.GetBytes(value)));
        }

        public static void WriteBE(this BinaryWriter writer, ulong value)
        {
            writer.Write(Utils.SwapEndian(BitConverter.GetBytes(value)));
        }

        public static void WriteBE(this BinaryWriter writer, float value)
        {
            writer.Write(Utils.SwapEndian(BitConverter.GetBytes(value)));
        }

        public static void WriteBE(this BinaryWriter writer, double value)
        {
            writer.Write(Utils.SwapEndian(BitConverter.GetBytes(value)));
        }
    }
}
