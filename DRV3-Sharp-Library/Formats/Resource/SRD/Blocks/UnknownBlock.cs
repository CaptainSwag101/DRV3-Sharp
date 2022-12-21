using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DRV3_Sharp_Library.Formats.Resource.SRD.Blocks;

public sealed class UnknownBlock : ISrdBlock
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