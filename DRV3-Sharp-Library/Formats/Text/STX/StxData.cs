/*
    DRV3-Sharp, a free and open-source toolkit
    for working with files and assets from Danganronpa V3.

    Copyright (C) 2020-2022  James Pelster
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp_Library.Formats.Text.STX
{
    public sealed class SegmentedString
    {
        public List<string> Segments { get; }

        public SegmentedString()
        {
            Segments = new();
        }

        public SegmentedString(IEnumerable<string> initSegments)
        {
            Segments = new();
            Segments.AddRange(initSegments);
        }
    }
    public sealed class StringTable
    {
        public int UnknownData { get; set; }
        public List<SegmentedString> Strings { get; set; }

        public StringTable()
        {
            UnknownData = 0;
            Strings = new();
        }

        public StringTable(int unknown)
        {
            UnknownData = unknown;
            Strings = new();
        }

        public StringTable(int unknown, List<SegmentedString> initStrings)
        {
            UnknownData = unknown;
            Strings = initStrings;
        }
    }

    public sealed class StxData : IDanganV3Data
    {
        public List<StringTable> Tables { get; set; }

        public StxData()
        {
            Tables = new();
        }

        public StxData(List<StringTable> initTables)
        {
            Tables = initTables;
        }
    }
}
