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

namespace V3Lib.Text.STX
{
    /// <summary>
    /// The primary text format used by Danganronpa V3. Contains tables of strings,
    /// which allows for easy sorting and de-duplication.
    /// </summary>
    public class STXText
    {
        #region Private Fields
        private const string STX_FILE_MAGIC = "STXT";
        private const string STX_LANG_MAGIC = "JPLL";
        #endregion

        #region Public Properties
        public List<StringTable> StringTables { get; private set; }
        #endregion

        #region Public Methods

        #region Constructors
        /// <summary>
        /// Creates a new empty STX text container with no strings.
        /// </summary>
        public STXText()
        {
            // Initialize properties to default values
            StringTables = new();
        }

        /// <summary>
        /// Reads a STX text container from a data stream.
        /// </summary>
        /// <param name="stream">The stream to read data from.</param>
        /// <exception cref="EndOfStreamException">Occurs when the end of the data stream is reached before the text has been fully read.</exception>
        /// <exception cref="InvalidDataException">Occurs when the file you're trying to read does not conform to the STX specification, and is likely invalid.</exception>
        /// <exception cref="NotImplementedException"></exception>
        public STXText(Stream stream)
        {
            // Initialize properties to default values
            StringTables = new();

            // Set up a BinaryReader to help us deserialize the data from stream
            using BinaryReader reader = new(stream);

            // Read all stream data inside a "try" block to catch exceptions
            try
            {
                // Verify the magic value, it should be "STXT"
                string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (magic != STX_FILE_MAGIC)
                {
                    Console.WriteLine($"ERROR: Invalid magic value, expected \"{STX_FILE_MAGIC}\" but got \"{magic}\".");
                    return;
                }

                // Verify the language value, it should be "JPLL"
                string lang = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (lang != STX_LANG_MAGIC)
                {
                    Console.WriteLine($"ERROR: Invalid language string, expected \"{STX_LANG_MAGIC}\" but got \"{lang}\".");
                    return;
                }

                // Read table count (at least, that's what I think it is)
                int tableCount = reader.ReadInt32();

                // I have never seen an STX file with more than 1 table, so I'm adding
                // a special notice in case someone encounters one, so I can test it more.
                if (tableCount > 1)
                {
                    throw new NotImplementedException("WARNING: Encountered a tableCount greater than 1! Please submit this STX file to the program author for further testing!");
                }

                // Read the offset to the first string table
                int tableOffset = reader.ReadInt32();

                // Read the info for all string tables
                var tableInfo = new List<(int Unknown, int StringCount)>();    // unknown, stringCount
                for (int t = 0; t < tableCount; ++t)
                {
                    tableInfo.Add((reader.ReadInt32(), reader.ReadInt32()));
                    // Align to nearest 16-byte boundary?
                    reader.BaseStream.Seek(8, SeekOrigin.Current);
                }

                // Read the data for each string table
                reader.BaseStream.Seek(tableOffset, SeekOrigin.Begin);
                foreach (var (Unknown, StringCount) in tableInfo)
                {
                    List<string> strings = new();

                    for (int s = 0; s < StringCount; ++s)
                    {
                        int stringId = reader.ReadInt32();
                        int stringOffset = reader.ReadInt32();

                        long returnPos = reader.BaseStream.Position;
                        reader.BaseStream.Seek(stringOffset, SeekOrigin.Begin);

                        // C# does not include a way to read null-terminated strings, so we'll have to do it manually.
                        strings.Add(Utils.ReadNullTerminatedString(reader, Encoding.Unicode));

                        reader.BaseStream.Seek(returnPos, SeekOrigin.Begin);
                    }

                    StringTables.Add(new StringTable(strings, Unknown));
                }
            }
            catch (EndOfStreamException)
            {
                throw;
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (NotImplementedException)
            {
                throw;
            }
        }
        #endregion

        /// <summary>
        /// Serializes the whole STX text container to a byte array, ready to be written to a file or stream.
        /// </summary>
        /// <returns>A byte array containing the full text container.</returns>
        public byte[] GetBytes()
        {
            using MemoryStream stxData = new();
            using BinaryWriter stxWriter = new(stxData);

            stxWriter.Write(Encoding.ASCII.GetBytes(STX_FILE_MAGIC));
            stxWriter.Write(Encoding.ASCII.GetBytes(STX_LANG_MAGIC));

            stxWriter.Write((int)StringTables.Count);
            stxWriter.Write((int)0);    // tableOffset, to be written later

            // Write table info
            foreach (var table in StringTables)
            {
                stxWriter.Write(table.Unknown);
                stxWriter.Write(table.Strings.Count);
                stxWriter.Write((ulong)0);  // Pad to nearest 16-byte boundary
            }

            // Write tableOffset
            long lastPos = stxWriter.BaseStream.Position;
            stxWriter.BaseStream.Seek(0x0C, SeekOrigin.Begin);
            stxWriter.Write((uint)lastPos);
            stxWriter.BaseStream.Seek(lastPos, SeekOrigin.Begin);

            // Write temporary padding for string IDs/offset
            foreach (var table in StringTables)
            {
                stxWriter.Write(new byte[(8 * table.Strings.Count)]);
            }

            // Write string data & corresponding ID/offset pair
            long infoPairPos = lastPos;
            foreach (var table in StringTables)
            {
                uint strId = 0;
                foreach (string str in table.Strings)
                {
                    // Write ID/offset pair
                    long strPos = stxWriter.BaseStream.Position;
                    stxWriter.BaseStream.Seek(infoPairPos, SeekOrigin.Begin);
                    stxWriter.Write(strId++);
                    stxWriter.Write((uint)strPos);
                    stxWriter.BaseStream.Seek(strPos, SeekOrigin.Begin);

                    // Increment infoPairPos 8 bytes to next entry position
                    infoPairPos += 8;

                    // Write string data
                    byte[] strData = Encoding.Unicode.GetBytes(str);
                    stxWriter.Write(strData);
                    stxWriter.Write((ushort)0);
                }
            }

            return stxData.ToArray();
        }

        #endregion
    }

    public class StringTable
    {
        public List<string> Strings { get; private set; }
        public int Unknown { get; private set; }

        public StringTable(List<string> strings, int unknown)
        {
            Strings = strings;
            Unknown = unknown;
        }
    }
}
