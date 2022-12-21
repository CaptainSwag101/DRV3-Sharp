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