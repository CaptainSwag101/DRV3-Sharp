using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DRV3_Sharp_Library.Formats.Resource.SRD.Blocks;

namespace DRV3_Sharp_Library.Formats.Resource.SRD.Blocks;

internal static class BlockSerializer
{
    public static UnknownBlock DeserializeUnknownBlock(string blockType, MemoryStream mainStream)
    {
        return new UnknownBlock(blockType, mainStream.ToArray(), new());
    }
    public static byte[] SerializeUnknownBlock(UnknownBlock block)
    {
        return block.MainData;
    }

    public static RsfBlock DeserializeRsfBlock(MemoryStream mainStream)
    {
        using BinaryReader reader = new(mainStream);

        int unknown00 = reader.ReadInt32();
        int unknown04 = reader.ReadInt32();
        int unknown08 = reader.ReadInt32();
        int unknown0C = reader.ReadInt32();
        string folderName = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);

        return new RsfBlock(unknown00, unknown04, unknown08, unknown0C, folderName, new());
    }
    public static byte[] SerializeRsfBlock(RsfBlock block)
    {
        using MemoryStream mem = new();
        using BinaryWriter writer = new(mem);
        
        writer.Write(block.Unknown00);
        writer.Write(block.Unknown04);
        writer.Write(block.Unknown08);
        writer.Write(block.Unknown0C);
        writer.Write(Encoding.ASCII.GetBytes(block.FolderName));

        return mem.ToArray();
    }
}