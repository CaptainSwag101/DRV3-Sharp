using DRV3_Sharp_Library.Formats.Resource.SRD.Blocks;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DRV3_Sharp_Library.Formats.Resource.SRD;

public static class SrdSerializer
{
    public static void Deserialize(Stream inputSrd, Stream? inputSrdi, Stream? inputSrdv, out SrdData outputData)
    {
        // Read through the SRD stream and deserialize all blocks
        List<ISrdBlock> blocks = new();
        while (inputSrd.Position < inputSrd.Length)
        {
            DeserializeBlock(inputSrd, inputSrdi, inputSrdv, out var currentBlock);
            blocks.Add(currentBlock);
        }

        outputData = new(blocks);
    }

    public static void DeserializeBlock(Stream inputSrd, Stream? inputSrdi, Stream? inputSrdv, out ISrdBlock outputBlock)
    {
        using BinaryReader srdReader = new(inputSrd, Encoding.ASCII, true);

        // Read block header
        string blockType = Encoding.ASCII.GetString(srdReader.ReadBytes(4));
        int mainDataLength = BinaryPrimitives.ReverseEndianness(srdReader.ReadInt32());
        int subDataLength = BinaryPrimitives.ReverseEndianness(srdReader.ReadInt32());
        int unknown = BinaryPrimitives.ReverseEndianness(srdReader.ReadInt32());
        Debug.Assert(unknown is 0 or 1);

        // Read main data
        MemoryStream mainDataStream = new(srdReader.ReadBytes(mainDataLength));
        MemoryStream? subDataStream = null;
        if (subDataLength > 0)
        {
            Utils.SkipToNearest(srdReader, 16);
            subDataStream = new(srdReader.ReadBytes(subDataLength));
        }
        Utils.SkipToNearest(srdReader, 16);

        switch (blockType)
        {
            // Deserialize data based on block type
            case @"$CFH":
                outputBlock = new CfhBlock();   // We don't even need to bother serializing it
                break;
            case @"$RSF":
                RsfBlock.Deserialize(mainDataStream, out RsfBlock rsf);
                outputBlock = rsf;
                break;
            case @"$RSI":
                RsiBlock.Deserialize(mainDataStream, inputSrdi, inputSrdv, out RsiBlock rsi);
                outputBlock = rsi;
                break;
            default:
                UnknownBlock.Deserialize(blockType, mainDataStream, subDataStream, inputSrdi, inputSrdv, out UnknownBlock unk);
                outputBlock = unk;
                break;
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
            typeString = @"$CFH";
            unknownVal = 1;
        }
        else if (block is RsiBlock rsi)
        {
            RsiBlock.Serialize(rsi, mainDataStream, outputSrdi, outputSrdv);
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