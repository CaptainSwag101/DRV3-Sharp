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
using System.Text;

namespace DRV3_Sharp_Library.Formats.Text.STX
{
    public static class StxSerializer
    {
        private const string CONST_FILE_MAGIC = "STXT";
        private const string CONST_LANG_MAGIC = "JPLL";

        public static void Deserialize(Stream inputStream, out StxData outputData)
        {
            using BinaryReader reader = new(inputStream, Encoding.ASCII, true);

            string fileMagic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (fileMagic != CONST_FILE_MAGIC) throw new InvalidDataException($"Invalid file magic, expected {CONST_FILE_MAGIC} but got {fileMagic}.");

            string langMagic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (langMagic != CONST_LANG_MAGIC) throw new InvalidDataException($"Invalid language magic, expected {CONST_LANG_MAGIC} but got {langMagic}.");

            int tableCount = reader.ReadInt32();
            if (tableCount > 1) throw new NotImplementedException($"This STX file reports to have more than one string table, which has never been documented. PLEASE SEND THIS STX FILE TO THE DEVELOPER!");

            uint tableOffset = reader.ReadUInt32();

            List<(int Unknown, int StringCount)> tableInfo = new();
            for (int t = 0; t < tableCount; ++t)
            {
                tableInfo.Add((reader.ReadInt32(), reader.ReadInt32()));
                // Align to nearest 16-byte boundary?
                reader.BaseStream.Seek(8, SeekOrigin.Current);
            }

            outputData = new();

            reader.BaseStream.Seek(tableOffset, SeekOrigin.Begin);
            foreach (var (unknown, stringCount) in tableInfo)
            {
                List<string> strings = new();

                for (int s = 0; s < stringCount; ++s)
                {
                    uint stringId = reader.ReadUInt32();
                    uint stringOffset = reader.ReadUInt32();

                    long returnPos = reader.BaseStream.Position;

                    reader.BaseStream.Seek(stringOffset, SeekOrigin.Begin);

                    // C# does not include a way to read null-terminated strings, so we'll have to do it manually.
                    strings.Add(Utils.ReadNullTerminatedString(reader, Encoding.Unicode));

                    // Check if the string ID does not line up with the position it was given in the list, just in case.
                    if (stringId != (strings.Count - 1)) throw new InvalidDataException($"String #{s} has a reported ID of {stringId}, this list is not sorted correctly!");

                    reader.BaseStream.Seek(returnPos, SeekOrigin.Begin);
                }

                outputData.Tables.Add(new StringTable(unknown, strings));
            }
        }

        public static void Serialize(StxData inputData, Stream outputStream)
        {
            using BinaryWriter writer = new(outputStream, Encoding.ASCII, true);

            writer.Write(Encoding.ASCII.GetBytes(CONST_FILE_MAGIC));
            writer.Write(Encoding.ASCII.GetBytes(CONST_LANG_MAGIC));

            writer.Write(inputData.Tables.Count);
            writer.Write((int)0);   // tableOffset, to be written later

            // Write table info
            foreach (var table in inputData.Tables)
            {
                writer.Write(table.UnknownData);
                writer.Write(table.Strings.Count);
                writer.Write((ulong)0); // Pad to nearest 16-byte boundary
            }

            // Write tableOffset
            long lastPos = writer.BaseStream.Position;
            writer.BaseStream.Seek(0x0C, SeekOrigin.Begin);
            writer.Write((int)lastPos);
            writer.BaseStream.Seek(lastPos, SeekOrigin.Begin);

            // Write temporary padding for string IDs/offset
            foreach (var table in inputData.Tables)
            {
                writer.Write(new byte[(8 * table.Strings.Count)]);  // (4 * 2) bytes per string entry
            }

            // Write string data & corresponding ID/offset pair
            long infoPairPos = lastPos;
            foreach (var table in inputData.Tables)
            {
                uint strId = 0;
                List<(int, string)> writtenStrings = new();
                foreach (string str in table.Strings)
                {
                    // De-duplicate strings by re-using offsets
                    int? foundOffset = null;
                    foreach (var (offset, text) in writtenStrings)
                    {
                        if (text == str)
                        {
                            foundOffset = offset;
                            break;
                        }
                    }

                    int latestPos = (int)writer.BaseStream.Position;

                    int strPos;
                    if (foundOffset is not null)
                        strPos = (int)foundOffset;
                    else
                        strPos = latestPos;

                    // Write ID/offset pair
                    writer.BaseStream.Seek(infoPairPos, SeekOrigin.Begin);
                    writer.Write(strId++);
                    writer.Write(strPos);
                    writer.BaseStream.Seek(latestPos, SeekOrigin.Begin);

                    // Increment infoPairPos 8 bytes to next entry position
                    infoPairPos += 8;

                    // Write string data if there are no existing duplicates
                    if (foundOffset is null)
                    {
                        byte[] strData = Encoding.Unicode.GetBytes(str);
                        writer.Write(strData);
                        writer.Write((ushort)0);
                    }
                }
            }
        }
    }
}
