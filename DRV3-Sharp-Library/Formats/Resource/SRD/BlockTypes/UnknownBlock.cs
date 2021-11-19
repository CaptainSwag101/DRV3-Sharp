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
    public record UnknownBlock : ISrdBlock
    {
        public readonly string Type;
        public readonly byte[] MainData;
        public List<ISrdBlock> SubBlocks;

        public UnknownBlock(string type, byte[] mainData, byte[] subData, Stream? inputSrdvStream, Stream? inputSrdiStream)
        {
            Type = type;
            MainData = mainData;

            // Decode sub-block data into discrete sub-blocks via recursion (should be +1 layer deep AT MOST in a non-malformed SRD)
            SubBlocks = new();
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
