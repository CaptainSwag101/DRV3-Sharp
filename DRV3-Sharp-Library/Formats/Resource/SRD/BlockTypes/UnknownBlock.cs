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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp_Library.Formats.Resource.SRD.BlockTypes
{
    public class UnknownBlock : ISrdBlock
    {
        public readonly string TypeCode;
        public readonly byte[] MainData;
        public readonly List<ISrdBlock> SubBlocks = new();

        public UnknownBlock(string type, byte[] mainData, byte[] subData, Stream? inputSrdvStream, Stream? inputSrdiStream)
        {
            TypeCode = type;
            MainData = mainData;

            // Decode sub-block data (if present) into discrete sub-blocks via recursion.
            // Recursion should be +1 layer deep AT MOST in a non-malformed SRD.
            if (subData.Length > 0)
            {
                using MemoryStream subStream = new(subData);
                while (subStream.Position < subStream.Length)
                {
                    BlockSerializer.Deserialize(subStream, inputSrdvStream, inputSrdiStream, out ISrdBlock subBlock);
                    SubBlocks.Add(subBlock);
                }
            }
        }
    }
}
