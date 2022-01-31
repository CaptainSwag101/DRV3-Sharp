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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp_Library.Formats.Resource.SRD.Blocks
{
    public class UnknownBlock : ISrdBlock
    {
        #region Public Properties
        public string BlockType { get; private set; }
        public byte[] MainData { get; set; }
        public List<ISrdBlock>? SubBlocks { get; }
        #endregion

        #region Public Methods
        public UnknownBlock()
        {
            BlockType = "$XXX";
            MainData = Array.Empty<byte>();
            SubBlocks = null;
        }

        public UnknownBlock(string type, byte[] mainData, List<ISrdBlock>? subBlocks)
        {
            BlockType = type;
            MainData = mainData;
            SubBlocks = subBlocks;
        }

        public List<string> GetBlockInfo()
        {
            List<string> infoList = new();

            infoList.Add($"Block Type (Unknown): {BlockType}");
            infoList.Add($"Main Data Size: {MainData.Length}");

            if (SubBlocks is not null)
            {
                infoList.Add("Sub Blocks:");
                foreach (ISrdBlock subBlock in SubBlocks)
                {
                    List<string> subInfoList = subBlock.GetBlockInfo();
                    foreach (string line in subInfoList)
                    {
                        infoList.Add($"\t{line}");
                    }

                    if (SubBlocks.IndexOf(subBlock) != (SubBlocks.Count - 1))
                        infoList.Add("");
                }
            }

            return infoList;
        }
        #endregion

        #region Public Static Methods
        public static void Deserialize(string type, MemoryStream inputMainData, MemoryStream? inputSubData, Stream? inputSrdi, Stream? inputSrdv, out UnknownBlock outputBlock)
        {
            List<ISrdBlock>? subBlocks;
            if (inputSubData is null) subBlocks = null;
            else
            {
                subBlocks = new();
                while (inputSubData.Position < inputSubData.Length)
                {
                    SrdSerializer.DeserializeBlock(inputSubData, inputSrdi, inputSrdv, out ISrdBlock subBlock);
                    subBlocks.Add(subBlock);
                }
            }

            outputBlock = new(type, inputMainData.ToArray(), subBlocks);
        }

        public static void Serialize(UnknownBlock inputBlock, Stream outputMainData, Stream outputSubData, Stream outputSrdi, Stream outputSrdv)
        {
            using BinaryWriter mainDataWriter = new(outputMainData, Encoding.ASCII, true);

            mainDataWriter.Write(inputBlock.MainData);

            if (inputBlock.SubBlocks is not null)
            {
                using BinaryWriter subDataWriter = new(outputSubData, Encoding.ASCII, true);

                foreach (ISrdBlock subBlock in inputBlock.SubBlocks)
                {
                    SrdSerializer.SerializeBlock(subBlock, outputSubData, outputSrdi, outputSrdv);
                }
            }
        }
        #endregion
    }
}
