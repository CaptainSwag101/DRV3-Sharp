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
