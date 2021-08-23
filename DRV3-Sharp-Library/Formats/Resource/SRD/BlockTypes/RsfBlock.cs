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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp_Library.Formats.Resource.SRD.BlockTypes
{
    record RsfBlock : ISrdBlock
    {
        public string FolderName;

        public RsfBlock(byte[] mainData)
        {
            using BinaryReader reader = new(new MemoryStream(mainData));

            int unknown00 = reader.ReadInt32();
            Debug.Assert(unknown00 == 16);
            int unknown04 = reader.ReadInt32();
            Debug.Assert(unknown04 == 20110331);
            int unknown08 = reader.ReadInt32();
            Debug.Assert(unknown08 == 20110401);
            int unknown0C = reader.ReadInt32();
            Debug.Assert(unknown0C == 0);
            FolderName = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
        }
    }
}
