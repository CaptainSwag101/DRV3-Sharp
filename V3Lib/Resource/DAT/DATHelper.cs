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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V3Lib.Resource.DAT
{
    public static class DATHelper
    {
        public static readonly Dictionary<string, Type> DataTypes = new()
        {
            { "u8", typeof(byte) },
            { "u16", typeof(ushort) },
            { "u32", typeof(uint) },
            { "u64", typeof(ulong) },
            { "s8", typeof(sbyte) },
            { "s16", typeof(short) },
            { "s32", typeof(int) },
            { "s64", typeof(long) },
            { "f32", typeof(float) },
            { "f64", typeof(double) },
            { "ascii", typeof(ushort) },
            { "label", typeof(ushort) },
            { "refer", typeof(ushort) },
            { "utf16", typeof(ushort) }
        };

        public static readonly Dictionary<string, int> DataTypeSizes = new()
        {
            { "u8", sizeof(byte) },
            { "u16", sizeof(ushort) },
            { "u32", sizeof(uint) },
            { "u64", sizeof(ulong) },
            { "s8", sizeof(sbyte) },
            { "s16", sizeof(short) },
            { "s32", sizeof(int) },
            { "s64", sizeof(long) },
            { "f32", sizeof(float) },
            { "f64", sizeof(double) },
            { "ascii", sizeof(ushort) },
            { "label", sizeof(ushort) },
            { "refer", sizeof(ushort) },
            { "utf16", sizeof(ushort) }
        };

        public static readonly Dictionary<string, Func<BinaryReader, object>> ReadFunctions = new()
        {
            { "u8", reader => reader.ReadByte() },
            { "u16", reader => reader.ReadUInt16() },
            { "u32", reader => reader.ReadUInt32() },
            { "u64", reader => reader.ReadUInt64() },
            { "s8", reader => reader.ReadSByte() },
            { "s16", reader => reader.ReadInt16() },
            { "s32", reader => reader.ReadInt32() },
            { "s64", reader => reader.ReadInt64() },
            { "f32", reader => reader.ReadSingle() },
            { "f64", reader => reader.ReadDouble() },
            { "ascii", reader => reader.ReadUInt16() },
            { "label", reader => reader.ReadUInt16() },
            { "refer", reader => reader.ReadUInt16() },
            { "utf16", reader => reader.ReadUInt16() }
        };

        public static readonly Dictionary<string, Action<BinaryWriter, object>> WriteFunctions = new()
        {
            { "u8", (writer, val) => writer.Write((byte)val) },
            { "u16", (writer, val) => writer.Write((ushort)val) },
            { "u32", (writer, val) => writer.Write((uint)val) },
            { "u64", (writer, val) => writer.Write((ulong)val) },
            { "s8", (writer, val) => writer.Write((sbyte)val) },
            { "s16", (writer, val) => writer.Write((short)val) },
            { "s32", (writer, val) => writer.Write((int)val) },
            { "s64", (writer, val) => writer.Write((long)val) },
            { "f32", (writer, val) => writer.Write((float)val) },
            { "f64", (writer, val) => writer.Write((double)val) },
            { "ascii", (writer, val) => writer.Write((ushort)val) },
            { "label", (writer, val) => writer.Write((ushort)val) },
            { "refer", (writer, val) => writer.Write((ushort)val) },
            { "utf16", (writer, val) => writer.Write((ushort)val) }
        };

        public static readonly Dictionary<string, Func<string, object>> StringToTypeFunctions = new()
        {
            { "u8", str => byte.Parse(str) },
            { "u16", str => ushort.Parse(str) },
            { "u32", str => uint.Parse(str) },
            { "u64", str => ulong.Parse(str) },
            { "s8", str => sbyte.Parse(str) },
            { "s16", str => short.Parse(str) },
            { "s32", str => int.Parse(str) },
            { "s64", str => long.Parse(str) },
            { "f32", str => float.Parse(str) },
            { "f64", str => double.Parse(str) },
            { "ascii", str => str },
            { "label", str => str },
            { "refer", str => str },
            { "utf16", str => str }
        };

        public static readonly Dictionary<string, Func<byte[], object>> BytesToTypeFunctions = new()
        {
            { "u8", array => array[0] },
            { "u16", array => BitConverter.ToUInt16(array) },
            { "u32", array => BitConverter.ToUInt32(array) },
            { "u64", array => BitConverter.ToUInt64(array) },
            { "s8", array => (sbyte)array[0] },
            { "s16", array => BitConverter.ToInt16(array) },
            { "s32", array => BitConverter.ToInt32(array) },
            { "s64", array => BitConverter.ToInt64(array) },
            { "f32", array => BitConverter.ToSingle(array) },
            { "f64", array => BitConverter.ToDouble(array) },
            { "ascii", array => BitConverter.ToUInt16(array) },
            { "label", array => BitConverter.ToUInt16(array) },
            { "refer", array => BitConverter.ToUInt16(array) },
            { "utf16", array => BitConverter.ToUInt16(array) }
        };

        public static readonly Dictionary<string, Func<object, byte[]>> TypeToBytesFunctions = new()
        {
            { "u8", value => new byte[] { (byte)value } },
            { "u16", value => BitConverter.GetBytes((ushort)value) },
            { "u32", value => BitConverter.GetBytes((uint)value) },
            { "u64", value => BitConverter.GetBytes((ulong)value) },
            { "s8", value => new byte[] { (byte)value } },
            { "s16", value => BitConverter.GetBytes((short)value) },
            { "s32", value => BitConverter.GetBytes((int)value) },
            { "s64", value => BitConverter.GetBytes((long)value) },
            { "f32", value => BitConverter.GetBytes((float)value) },
            { "f64", value => BitConverter.GetBytes((double)value) },
            { "ascii", value => BitConverter.GetBytes((ushort)value) },
            { "label", value => BitConverter.GetBytes((ushort)value) },
            { "refer", value => BitConverter.GetBytes((ushort)value) },
            { "utf16", value => BitConverter.GetBytes((ushort)value) }
        };
    }
}
