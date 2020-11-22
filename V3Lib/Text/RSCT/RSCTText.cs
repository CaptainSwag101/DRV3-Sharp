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

namespace V3Lib.Text.RSCT
{
    /// <summary>
    /// The text container format used by Danganronpa V3,
    /// for the Talent Development Plan minigame.
    /// </summary>
    public class RSCTText
    {
        #region Private Fields
        private const string RSCT_FILE_MAGIC = "RSCT";
        #endregion

        #region Public Properties
        public List<(string String, int Unknown)> Strings { get; private set; }
        #endregion

        #region Public Methods

        #region Constructors
        /// <summary>
        /// Creates a new empty RSCT text container with no strings.
        /// </summary>
        public RSCTText()
        {
            // Initialize properties to default values
            Strings = new();
        }

        /// <summary>
        /// Reads an RSCT text container from a data stream.
        /// </summary>
        /// <param name="stream">The stream to read data from.</param>
        /// <exception cref="EndOfStreamException">Occurs when the end of the data stream is reached before the text has been fully read.</exception>
        /// <exception cref="InvalidDataException">Occurs when the file you're trying to read does not conform to the RSCT specification, and is likely invalid.</exception>
        public RSCTText(Stream stream)
        {
            // Initialize properties to default values
            Strings = new();

            // Set up a BinaryReader to help us deserialize the data from stream
            using BinaryReader reader = new(stream);

            // Read all stream data inside a "try" block to catch exceptions
            try
            {
                // Verify the magic value, it should be "RSCT"
                string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (magic != RSCT_FILE_MAGIC)
                {
                    throw new InvalidDataException($"ERROR: Invalid magic value, expected \"{RSCT_FILE_MAGIC}\" but got \"{magic}\".");
                }

                // Skip 4 bytes of padding
                _ = reader.ReadInt32();

                int stringCount = reader.ReadInt32();
                int unknown0C = reader.ReadInt32(); // always 0x00000014?
                int stringDataStart = reader.ReadInt32();

                // Read string data
                for (int i = 0; i < stringCount; ++i)
                {
                    int unk = reader.ReadInt32();
                    int stringOffset = reader.ReadInt32();

                    long lastPos = reader.BaseStream.Position;
                    reader.BaseStream.Seek(stringOffset, SeekOrigin.Begin);

                    int strLen = reader.ReadInt32();
                    string str = Utils.ReadNullTerminatedString(reader, Encoding.Unicode);

                    Strings.Add((str, unk));

                    reader.BaseStream.Seek(lastPos, SeekOrigin.Begin);
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
        }
        #endregion

        /// <summary>
        /// Serializes the whole RSCT text container to a byte array, ready to be written to a file or stream.
        /// </summary>
        /// <returns>A byte array containing the full text container.</returns>
        public byte[] GetBytes()
        {
            using MemoryStream rsctData = new();
            using BinaryWriter rsctWriter = new(rsctData);

            rsctWriter.Write(Encoding.ASCII.GetBytes(RSCT_FILE_MAGIC));
            rsctWriter.Write((int)0);   // 4 bytes of padding

            rsctWriter.Write(Strings.Count);
            rsctWriter.Write((int)14);  // unknown0C

            using MemoryStream strInfoStream = new();
            using BinaryWriter strInfoWriter = new(strInfoStream);
            using MemoryStream stringStream = new();
            using BinaryWriter stringWriter = new(stringStream, Encoding.Unicode);
            foreach (var strTuple in Strings)
            {
                strInfoWriter.Write(strTuple.Unknown);
                strInfoWriter.Write((uint)(rsctWriter.BaseStream.Position + (8 * Strings.Count) + stringStream.Position));

                stringWriter.Write(Encoding.Unicode.GetByteCount(strTuple.String));
                stringWriter.Write(strTuple.String);
                stringWriter.Write((ushort)0);
            }

            rsctWriter.Write((uint)(rsctWriter.BaseStream.Position + 4 + strInfoStream.Length));    // stringDataStart
            rsctWriter.Write(strInfoStream.ToArray());
            rsctWriter.Write(stringStream.ToArray());

            return rsctData.ToArray();
        }

        #endregion

    }
}
