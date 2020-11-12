using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace V3Lib.Dat
{
    public class DatFile
    {
        public List<(string Name, string Type, ushort Count)> ColumnDefinitions = new List<(string Name, string Type, ushort Count)>();
        public List<List<string>> Data = new List<List<string>>();

        private static readonly Dictionary<string, Type> DataTypes = new Dictionary<string, Type>()
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

        private static readonly Dictionary<string, int> DataTypeSizes = new Dictionary<string, int>()
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

        private static readonly Dictionary<string, Func<BinaryReader, object>> ReadFunctions = new Dictionary<string, Func<BinaryReader, object>>()
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

        private static readonly Dictionary<string, Action<BinaryWriter, object>> WriteFunctions = new Dictionary<string, Action<BinaryWriter, object>>()
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

        public void Load(string datPath)
        {
            using BinaryReader reader = new BinaryReader(new FileStream(datPath, FileMode.Open, FileAccess.Read, FileShare.Read));

            // Read header
            uint rowCount = reader.ReadUInt32();
            uint bytesPerRow = reader.ReadUInt32();
            uint columnCount = reader.ReadUInt32();

            // Read column descriptors
            for (int col = 0; col < columnCount; ++col)
            {
                string colName = Utils.ReadNullTerminatedString(reader, Encoding.UTF8);
                string colType = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
                ushort colCount = reader.ReadUInt16();

                ColumnDefinitions.Add((colName, colType, colCount));
            }

            // Align to next 16-byte boundary
            Utils.ReadPadding(reader, 16);

            // Skip ahead to read the UTF-8 strings and UTF-16 strings so we can reference them while reading row data
            long rowDataPos = reader.BaseStream.Position;
            reader.BaseStream.Seek((bytesPerRow * rowCount), SeekOrigin.Current);

            List<string> utf8Strings = new List<string>();
            List<string> utf16Strings = new List<string>();
            ushort utf8Count = reader.ReadUInt16();
            ushort utf16Count = reader.ReadUInt16();

            for (int s = 0; s < utf8Count; ++s)
            {
                utf8Strings.Add(Utils.ReadNullTerminatedString(reader, Encoding.UTF8));
            }

            // Align to nearest 2-byte boundary
            Utils.ReadPadding(reader, 2);

            for (int s = 0; s < utf16Count; ++s)
            {
                utf16Strings.Add(Utils.ReadNullTerminatedString(reader, Encoding.Unicode));
            }

            reader.BaseStream.Seek(rowDataPos, SeekOrigin.Begin);

            // Read row data
            for (int row = 0; row < rowCount; ++row)
            {
                List<string> rowData = new List<string>();

                for (int col = 0; col < columnCount; ++col)
                {
                    string type = ColumnDefinitions[col].Type.ToLowerInvariant();

                    StringBuilder resultStr = new StringBuilder();

                    List<string> strList = new List<string>();
                    for (int i = 0; i < ColumnDefinitions[col].Count; ++i)
                    {
                        // This should auto-increment the BaseStream.Position value as expected, no need to do any trickery
                        object result = ReadFunctions[type].Invoke(reader);

                        if (type == "ascii" || type == "label" || type == "refer")
                        {
                            strList.Add(utf8Strings[(ushort)result]);
                        }
                        else if (type == "utf16")
                        {
                            strList.Add(utf16Strings[(ushort)result]);
                        }
                        else
                        {
                            strList.Add(result.ToString());
                        }
                    }
                    resultStr.AppendJoin("|", strList);

                    rowData.Add(resultStr.ToString());
                }

                Data.Add(rowData);
            }
        }

        public void Save(string datPath)
        {
            using BinaryWriter writer = new BinaryWriter(new FileStream(datPath, FileMode.Create, FileAccess.Write, FileShare.None));

            // Write header
            writer.Write((uint)Data.Count); // rowCount
            uint bytesPerRow = 0;
            foreach (var def in ColumnDefinitions)
            {
                bytesPerRow += (uint)DataTypeSizes[def.Type.ToLowerInvariant()] * def.Count;
            }
            writer.Write(bytesPerRow);
            writer.Write(ColumnDefinitions.Count);  // columnCount

            // Write column definitions
            foreach (var def in ColumnDefinitions)
            {
                writer.Write(Encoding.UTF8.GetBytes(def.Name.Trim()));
                writer.Write((byte)0);  // Null terminator
                writer.Write(Encoding.ASCII.GetBytes(def.Type.Trim()));
                writer.Write((byte)0);  // Null terminator
                writer.Write(def.Count);
            }

            // Write padding to next 16-byte boundary
            Utils.WritePadding(writer, 16);

            // Write row data according to its type
            List<string> utf8Strings = new List<string>();
            List<string> utf16Strings = new List<string>();
            foreach (var rowData in Data)
            {
                for (int col = 0; col < ColumnDefinitions.Count; ++col)
                {
                    // If the data type is a string, add it to its respective list and write the index
                    string type = ColumnDefinitions[col].Type.ToLowerInvariant();

                    // Split the string by the '|' character in case we have multiple values
                    string[] splitVal = rowData[col].Split('|');
                    for (int i = 0; i < splitVal.Length; ++i)
                    {
                        object value;

                        if (type == "ascii" || type == "label" || type == "refer")
                        {
                            if (!utf8Strings.Contains(splitVal[i]))
                                utf8Strings.Add(splitVal[i]);

                            value = (ushort)utf8Strings.IndexOf(splitVal[i]);
                        }
                        else if (type == "utf16")
                        {
                            if (!utf16Strings.Contains(splitVal[i]))
                                utf16Strings.Add(splitVal[i]);

                            value = (ushort)utf16Strings.IndexOf(splitVal[i]);
                        }
                        else
                        {
                            // This is only needed to convert the data to a numeric type
                            value = Convert.ChangeType(int.Parse(splitVal[i]), DataTypes[type]);
                        }

                        WriteFunctions[type].Invoke(writer, value);
                    }
                }
            }

            // Write string counts
            writer.Write((ushort)utf8Strings.Count);
            writer.Write((ushort)utf16Strings.Count);

            // Write the strings
            foreach (string utf8 in utf8Strings)
            {
                writer.Write(Encoding.UTF8.GetBytes(utf8));
                writer.Write((byte)0);  // Null terminator
            }

            // Align to next 2-byte boundary
            Utils.WritePadding(writer, 2);

            foreach (string utf16 in utf16Strings)
            {
                writer.Write(Encoding.Unicode.GetBytes(utf16));
                writer.Write((ushort)0);  // Null terminator
            }

            writer.Flush();
        }
    }
}
