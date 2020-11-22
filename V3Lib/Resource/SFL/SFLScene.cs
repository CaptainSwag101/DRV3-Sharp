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
using System.Diagnostics;
using System.IO;
using System.Text;
using V3Lib.Resource.SFL.EntryTypes;

namespace V3Lib.Resource.SFL
{
    public class SFLScene
    {
        #region Private Fields
        private const string SFL_FILE_MAGIC = "LLFS";
        private static readonly Dictionary<decimal, uint> SFL_TRANSFORM_TABLE_START_IDS = new()
        {
            { 7.03m, 3 },
            { 7.11m, 4 }
        };
        #endregion

        #region Public Properties
        public decimal Version { get; private set; }
        public List<Table> Tables { get; private set; }
        #endregion

        #region Public Methods

        #region Constructors
        public SFLScene(decimal version)
        {
            // Initialize properties to default values,
            // but we need the version to be pre-defined.
            Tables = new();
            Version = version;
        }

        public SFLScene(Stream stream)
        {
            // Initialize properties to default values
            Tables = new();

            // Set up a BinaryReader to help us deserialize the data from stream
            using BinaryReader reader = new(stream);

            // Read all stream data inside a "try" block to catch exceptions
            try
            {
                // Read magic value
                string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (magic != SFL_FILE_MAGIC)
                {
                    throw new InvalidDataException($"Invalid SFL file, expected magic value \"{SFL_FILE_MAGIC}\" but got \"{magic}\".");
                }

                // Read SFL format version (should be 7.03 for DR1/DR2, and 7.11 for DRV3)
                ushort versionPatch = reader.ReadUInt16();
                ushort versionMinor = reader.ReadUInt16();
                uint versionMajor = reader.ReadUInt32();
                Version = versionMajor + (versionMinor / 10m) + (versionPatch / 100m);

                // Read tables
                uint tableCount = reader.ReadUInt32();
                for (uint tNum = 0; tNum < tableCount; ++tNum)
                {
                    Table table = new(reader, tNum + 1, SFL_TRANSFORM_TABLE_START_IDS[Version]);
                    Tables.Add(table);
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

        #endregion
    }

    public class Table
    {
        public ushort Unknown1 { get; set; }
        public uint Unknown2 { get; set; }
        public List<Entry> Entries { get; private set; }

        public Table()
        {
            Unknown1 = ushort.MaxValue;
            Unknown2 = uint.MaxValue;
            Entries = new();
        }

        public Table(BinaryReader reader, uint expectedTableID, uint transformTableStartID)
        {
            uint tableID = reader.ReadUInt32();

            Debug.Assert(tableID == expectedTableID);

            uint tableLength = reader.ReadUInt32();
            ushort entryCount = reader.ReadUInt16();
            Unknown1 = reader.ReadUInt16();
            Unknown2 = reader.ReadUInt32();

            long endPos = reader.BaseStream.Position + tableLength;

            // Read entries
            Entries = new();
            for (uint eNum = 0; eNum < entryCount; ++eNum)
            {
                // If we've reached the end of the table before leaving this loop, something has gone wrong
                if (reader.BaseStream.Position >= endPos)
                {
                    throw new EndOfStreamException($"Read past the end of table {tableID} while parsing entry {eNum} at position {reader.BaseStream.Position}.");
                }

                Entry entry;
                if (tableID < transformTableStartID)    // First 3 or 4 tables contain single- or multi-data entries
                {
                    entry = new DataEntry(reader, Entries.Count);
                }
                else    // Final two tables contain transformation entries
                {
                    entry = new TransformationEntry(reader, Entries.Count);
                }

                // Add the entry to the table
                Entries.Add(entry);
            }
        }
    }

    public abstract class Entry
    {
        public short Unknown1 { get; set; }
    }
}
