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

using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Resource.SRD.Blocks;

public sealed class CfhBlock : ISrdBlock
{
    #region Public Properties
    private static string BlockType => @"$CFH";
    #endregion

    #region Public Methods
    public CfhBlock()
    { }

    public List<string> GetBlockInfo()
    {
        List<string> infoList = new() { $"Block Type: {BlockType}" };

        return infoList;
    }
    #endregion

    #region Public Static Methods
    public static void Deserialize(out CfhBlock outputBlock)
    {
        outputBlock = new();

        // No data to deserialize
    }

    public static void Serialize()
    {
        // No data to serialize
    }
    #endregion
}