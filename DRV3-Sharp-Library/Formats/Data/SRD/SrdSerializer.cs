using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DRV3_Sharp_Library.Formats.Data.SRD.Blocks;
using DRV3_Sharp_Library.Formats.Data.SRD.Resources;

namespace DRV3_Sharp_Library.Formats.Data.SRD;

public static class SrdSerializer
{
    public static void Deserialize(Stream inputSrd, Stream? inputSrdi, Stream? inputSrdv, out SrdData outputData)
    {
        // Pass 1: Read through the SRD stream and deserialize all blocks.
        List<ISrdBlock> blocks = new();
        while (inputSrd.Position < inputSrd.Length)
        {
            blocks.Add(DeserializeBlock(inputSrd, inputSrdi, inputSrdv));
        }
        
        // Pass 2: Read through the block list and deserialize resources from their contents.
        var resources = DeserializeResources(blocks);
        
        // (To be added later) Pass 3: Build higher-level data structures from resources,
        // such as 3D models, etc.

        outputData = new(resources.Result);
    }

    private static ISrdBlock DeserializeBlock(Stream inputSrd, Stream? inputSrdi, Stream? inputSrdv)
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
        Utils.SkipToNearest(srdReader, 16);
        
        // Read sub-block data
        MemoryStream? subDataStream = null;
        if (subDataLength > 0)
        {
            subDataStream = new(srdReader.ReadBytes(subDataLength));
            Utils.SkipToNearest(srdReader, 16);
        }

        // Read and parse main block data
        ISrdBlock outputBlock = blockType switch
        {
            "$CFH" => new CfhBlock(new()),
            "$RSF" => BlockSerializer.DeserializeRsfBlock(mainDataStream),
            "$RSI" => BlockSerializer.DeserializeRsiBlock(mainDataStream, inputSrdi, inputSrdv),
            "$TXR" => BlockSerializer.DeserializeTxrBlock(mainDataStream),
            "$VTX" => BlockSerializer.DeserializeVtxBlock(mainDataStream),
            _ => BlockSerializer.DeserializeUnknownBlock(blockType, mainDataStream),
        };
        
        // If there is any sub-block data, parse it too.
        while (subDataStream is not null && subDataStream.Position < subDataLength)
        {
            outputBlock.SubBlocks.Add(DeserializeBlock(subDataStream, inputSrdi, inputSrdv));
        }
        
        // Clean up
        subDataStream?.Dispose();
        mainDataStream.Dispose();

        return outputBlock;
    }

    private static async Task<List<ISrdResource>> DeserializeResources(List<ISrdBlock> inputBlocks)
    {
        List<Task<ISrdResource>> resourceTasks = new();
        foreach (var block in inputBlocks)
        {
            if (block is TxrBlock txr)
            {
                resourceTasks.Add(Task.Run(() => ResourceSerializer.DeserializeTexture(txr)));
            }
        }
        var outputResources = await Task.WhenAll(resourceTasks);

        return outputResources.ToList();
    }

    public static void Serialize(SrdData inputData, Stream outputSrd, Stream outputSrdi, Stream outputSrdv)
    {
        // Pass 1: Serialize resources into blocks
        List<ISrdBlock> blocks = SerializeResources(inputData);
        
        // Pass 2: Serialize blocks into data and raw binary chunks.
        // Remember to check if outputSrdi and outputSrdv exist for each block,
        // and create it if it is needed and doesn't already exist.
        foreach (var block in blocks)
        {
            SerializeBlock(block, outputSrd, outputSrdi, outputSrdv);
        }
    }

    private static void SerializeBlock(ISrdBlock block, Stream outputSrd, Stream outputSrdi, Stream outputSrdv)
    {
        // Setup memory stream for serialized main data
        MemoryStream mainDataStream = new();
        string typeString = "";
        int unknownVal = 0;

        switch (block)
        {
            // Serialize data based on block type
            case UnknownBlock unk:
                typeString = unk.BlockType;
                mainDataStream.Write(BlockSerializer.SerializeUnknownBlock(unk));
                break;
            case CfhBlock:
                typeString = "$CFH";
                unknownVal = 1;
                break;
            case RsfBlock rsf:
                typeString = "$RSF";
                mainDataStream.Write(BlockSerializer.SerializeRsfBlock(rsf));
                break;
            case RsiBlock rsi:
                typeString = "$RSI";
                var results = BlockSerializer.SerializeRsiBlock(rsi);
                mainDataStream.Write(results.main);
                outputSrdi.Write(results.srdi);
                outputSrdv.Write(results.srdv);
                break;
            case TxrBlock txr:
                typeString = "$TXR";
                mainDataStream.Write(BlockSerializer.SerializeTxrBlock(txr));
                break;
        }
        
        
        // Serialize sub-blocks
        MemoryStream subDataStream = new();
        foreach (var subBlock in block.SubBlocks)
        {
            SerializeBlock(subBlock, subDataStream, outputSrdi, outputSrdv);
        }
        

        // Sanity checks
        Debug.Assert(typeString.Length == 4);   // The block type magic string must be exactly 4 characters

        // Write actual data to the SRD file, or the subdata stream if this is a nested call.
        // Write block header
        using BinaryWriter srdWriter = new(outputSrd, Encoding.ASCII, true);
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
        subDataStream.Dispose();
        mainDataStream.Dispose();
    }

    private static List<ISrdBlock> SerializeResources(SrdData inputData)
    {
        List<ISrdBlock> outputBlocks = new();

        return outputBlocks;
    }
}