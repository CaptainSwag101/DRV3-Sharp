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

namespace V3Lib.Resource.SFL.EntryTypes
{
    public class DataEntry : Entry
    {
        public byte[] Data { get; set; }

        public DataEntry()
        {
            Data = Array.Empty<byte>();
        }

        public DataEntry(BinaryReader reader, int expectedEntryID)
        {
            int entryID = reader.ReadInt32();
            Debug.Assert(entryID == expectedEntryID);

            int entryLength = reader.ReadInt32();
            Unknown1 = reader.ReadInt16();

            // These are ignored for data entries
            short subentryCount = reader.ReadInt16();
            int hasSubentries = reader.ReadInt32();

            // Read binary data
            Data = reader.ReadBytes(entryLength);
        }
    }
}
