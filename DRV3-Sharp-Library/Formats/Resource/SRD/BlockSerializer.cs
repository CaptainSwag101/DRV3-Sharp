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
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp_Library.Formats.Resource.SRD
{
    static class BlockSerializer
    {
        public static void Deserialize(Stream inputSrdStream, Stream? inputSrdvStream, Stream? inputSrdiStream, out ISrdBlock outputBlock)
        {
            using BinaryReader srdReader = new(inputSrdStream, Encoding.ASCII, true);

            // Read block header
            string blockType = Encoding.ASCII.GetString(srdReader.ReadBytes(4));
            int mainDataSize = BinaryPrimitives.ReverseEndianness(srdReader.ReadInt32());
            int subDataSize = BinaryPrimitives.ReverseEndianness(srdReader.ReadInt32());
            _ = BinaryPrimitives.ReverseEndianness(srdReader.ReadInt32()); // Unknown, should be 1 for $CFH blocks and 0 for everything else

            // Read block and sub-block data
            byte[] mainData = srdReader.ReadBytes(mainDataSize);
            Utils.SkipToNearest(srdReader, 16);
            byte[] subData = srdReader.ReadBytes(subDataSize);
            Utils.SkipToNearest(srdReader, 16);

            // Each block decodes sub-block data in its own way, since different blocks use their sub-data differently
            outputBlock = blockType switch
            {
                "$CFH" => new CfhBlock(),
                "$RSF" => new RsfBlock(mainData),
                "$RSI" => new RsiBlock(mainData, inputSrdvStream, inputSrdiStream),
                "$TXR" => new TxrBlock(mainData, subData, inputSrdvStream),
                "$TXI" => new TxiBlock(mainData, subData),
                //"$VTX" => new VtxBlock(mainData, subData, inputSrdiStream),
                //"$MSH" => new MshBlock(mainData, subData),
                //"$MAT" => new MatBlock(mainData, subData),
                //"$SCN" => new ScnBlock(mainData, subData),
                //"$TRE" => new TreBlock(),
                //"$SKL" => new SklBlock(),
                "$CT0" => new Ct0Block(),
                _ => new UnknownBlock(blockType, mainData, subData, inputSrdvStream, inputSrdiStream),
            };
        }

        public static void Serialize(ISrdBlock inputBlock, Stream outputSrdStream, Stream outputSrdvStream, Stream outputSrdiStream)
        {
            throw new NotImplementedException();
        }
    }
}
