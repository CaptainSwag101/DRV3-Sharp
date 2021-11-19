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

using DRV3_Sharp_Library.Formats.Resource.SRD.BlockTypes;
using DRV3_Sharp_Library.Formats.Resource.SRD.ResourceTypes;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp_Library.Formats.Resource.SRD
{
    public static class SrdSerializer
    {
        public static void Deserialize(Stream inputSrdStream, Stream? inputSrdvStream, Stream? inputSrdiStream, out SrdData outputData)
        {
            outputData = new();

            // Step 1: Read in all blocks
            while (inputSrdStream.Position < inputSrdStream.Length)
            {
                BlockSerializer.Deserialize(inputSrdStream, inputSrdvStream, inputSrdiStream, out ISrdBlock block);
                outputData.Blocks.Add(block);
            }

            // Step 2: Abstract blocks into their respective resource types
            //outputData = new();

            //List<ISrdBlock> cfhBlocks = outputData.Blocks.Where(block => block is CfhBlock).ToList();
            //Debug.Assert(cfhBlocks.Count == 1);
            //List<ISrdBlock> rsfBlocks = outputData.Blocks.Where(block => block is RsfBlock).ToList();
            //Debug.Assert(rsfBlocks.Count <= 1);
            //List<ISrdBlock> txrBlocks = outputData.Blocks.Where(block => block is TxrBlock).ToList();
            //List<ISrdBlock> txiBlocks = outputData.Blocks.Where(block => block is TxiBlock).ToList();
            //List<ISrdBlock> vtxBlocks = outputData.Blocks.Where(block => block is VtxBlock).ToList();
            //List<ISrdBlock> mshBlocks = outputData.Blocks.Where(block => block is MshBlock).ToList();
            //List<ISrdBlock> matBlocks = outputData.Blocks.Where(block => block is MatBlock).ToList();
            //List<ISrdBlock> scnBlocks = outputData.Blocks.Where(block => block is ScnBlock).ToList();
            //List<ISrdBlock> treBlocks = outputData.Blocks.Where(block => block is TreBlock).ToList();
            //List<ISrdBlock> sklBlocks = outputData.Blocks.Where(block => block is SklBlock).ToList();
            //List<ISrdBlock> anmBlocks = outputData.Blocks.Where(block => block is AnmBlock).ToList();
        }

        public static void Serialize(SrdData inputData, Stream outputSrdStream, Stream outputSrdvStream, Stream outputSrdiStream)
        {
            using BinaryWriter srdWriter = new(outputSrdStream, Encoding.ASCII, true);
            using BinaryWriter srdvWriter = new(outputSrdvStream, Encoding.ASCII, true);
            using BinaryWriter srdiWriter = new(outputSrdiStream, Encoding.ASCII, true);

        }
    }
}
