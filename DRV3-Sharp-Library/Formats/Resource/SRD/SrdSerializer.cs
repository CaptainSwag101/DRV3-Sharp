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

using DRV3_Sharp_Library.Formats.Resource.SRD.Blocks;
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
        public static void Deserialize(Stream inputSrd, Stream? inputSrdi, Stream? inputSrdv, out SrdData outputData)
        {
            outputData = new();

            // Read through the SRD stream and deserialize all blocks
            while (inputSrd.Position < inputSrd.Length)
            {
                DeserializeBlock(inputSrd, inputSrdi, inputSrdv, out var currentBlock);
                outputData.Blocks.Add(currentBlock);
            }
        }

        public static void DeserializeBlock(Stream inputSrd, Stream? inputSrdi, Stream? inputSrdv, out ISrdBlock outputBlock)
        {
            using BinaryReader srdReader = new(inputSrd, Encoding.ASCII, true);

            // Read block header
            string blockType = Encoding.ASCII.GetString(srdReader.ReadBytes(4));
            int mainDataLength = BinaryPrimitives.ReverseEndianness(srdReader.ReadInt32());
            int subDataLength = BinaryPrimitives.ReverseEndianness(srdReader.ReadInt32());
            int unknown = BinaryPrimitives.ReverseEndianness(srdReader.ReadInt32());
            Debug.Assert(unknown == 0 || unknown == 1);

            MemoryStream mainDataStream = new(srdReader.ReadBytes(mainDataLength));
            MemoryStream? subDataStream = null;
            if (subDataLength > 0)
            {
                Utils.SkipToNearest(srdReader, 16);
                subDataStream = new(srdReader.ReadBytes(subDataLength));
            }
            Utils.SkipToNearest(srdReader, 16);

            // Deserialize data based on block type
            if (blockType == "$CFH")
            {
                outputBlock = new CfhBlock();   // We don't even need to bother serializing it
            }
            else
            {
                UnknownBlock.Deserialize(blockType, mainDataStream, subDataStream, inputSrdi, inputSrdv, out UnknownBlock unk);
                outputBlock = unk;
            }
            subDataStream?.Dispose();
            mainDataStream.Dispose();
        }

        public static void Serialize(SrdData inputData, Stream outputSrd, Stream outputSrdi, Stream outputSrdv)
        {
            // Remember to check if outputSrdi and outputSrdv exist for each block,
            // and create it if it is needed and doesn't already exist.
            foreach (var block in inputData.Blocks)
            {
                SerializeBlock(block, outputSrd, outputSrdi, outputSrdv);
            }
        }

        public static void SerializeBlock(ISrdBlock block, Stream outputSrd, Stream outputSrdi, Stream outputSrdv)
        {
            using BinaryWriter srdWriter = new(outputSrd, Encoding.ASCII, true);

            // Setup memory streams for serialized main and sub-block data
            MemoryStream mainDataStream = new();
            MemoryStream subDataStream = new();
            string typeString = "";
            int unknownVal = 0;

            // Serialize data based on block type
            if (block is UnknownBlock unk)
            {
                UnknownBlock.Serialize(unk, mainDataStream, subDataStream, outputSrdi, outputSrdv);
                typeString = unk.BlockType;
            }
            else if (block is CfhBlock cfh)
            {
                CfhBlock.Serialize(cfh, mainDataStream, subDataStream, outputSrdi, outputSrdv);
                typeString = @"$CFH";
                unknownVal = 1;
            }

            // Sanity checks
            Debug.Assert(typeString.Length == 4);   // The block type magic string must be exactly 4 characters

            // Write block header
            srdWriter.Write(Encoding.ASCII.GetBytes(typeString));
            srdWriter.Write(BinaryPrimitives.ReverseEndianness((int)mainDataStream.Length));
            srdWriter.Write(BinaryPrimitives.ReverseEndianness((int)subDataStream.Length));
            srdWriter.Write(BinaryPrimitives.ReverseEndianness(unknownVal));

            // Write main block data
            srdWriter.Write(mainDataStream.ToArray());
            Utils.PadToNearest(srdWriter, 16);

            // Write sub block data, if it exists
            if (subDataStream.Length > 0)
            {
                srdWriter.Write(subDataStream.ToArray());
                Utils.PadToNearest(srdWriter, 16);
            }

            // Dispose of our memory streams
            subDataStream?.Dispose();
            mainDataStream.Dispose();
        }
    }
}
