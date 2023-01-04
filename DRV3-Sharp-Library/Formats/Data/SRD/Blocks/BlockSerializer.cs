using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DRV3_Sharp_Library.Formats.Data.SRD.Blocks;

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
    
    private sealed record ExternalResourceInfo(int Address, int Length, int Unknown1, int Unknown2);
    private sealed record LocalResourceInfo(int Address1, int Address2, int Length, int Unknown);
    public static RsiBlock DeserializeRsiBlock(MemoryStream mainStream, Stream? srdi, Stream? srdv)
    {
        using BinaryReader reader = new(mainStream);

        // Read header data (still mostly unknown!)
        byte unknown00 = reader.ReadByte();     // 0x06 or 0x04 in some cases like $MAT blocks, this may be tied to Unknown0A?
        byte unknown01 = reader.ReadByte();     // 0x05
        sbyte unknown02 = reader.ReadSByte();   // usually 0x04 or 0xFF, but seems to be 0x30 on PS4?
        
        //if (unknown02 is not -1 or 4) throw new NotImplementedException($"Encountered an unusual value for Unknown02, expected -1 or 4 but got {outputBlock.Unknown02}.\nPlease send this file to the developer!");
        
        byte externalResourceInfoCount = reader.ReadByte();
        //Debug.Assert((unknown02 == -1 && externalResourceInfoCount == 0) || (unknown02 == 4 && externalResourceInfoCount > 0));
        short localResourceInfoCount = reader.ReadInt16();
        short unknown06 = reader.ReadInt16();
        short localResourceInfoOffset = reader.ReadInt16();
        short unknownIntListOffset = reader.ReadInt16();
        Debug.Assert((unknown00 == 6 && unknownIntListOffset == 0) || (unknown00 == 4 && unknownIntListOffset != 0));
        int resourceStringListOffset = reader.ReadInt32();

        
        // Read external resource info
        List<ExternalResourceInfo> externalResourceInfo = new();
        for (var i = 0; i < externalResourceInfoCount; ++i)
        {
            ExternalResourceInfo info = new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            externalResourceInfo.Add(info);
        }

        // Read external resource data
        List<ExternalResource> externalResources = new();
        foreach (var info in externalResourceInfo)
        {
            // Determine resource data location
            var location = (ResourceDataLocation)(info.Address & 0xF0000000);
            int address = info.Address & 0x0FFFFFFF;

            switch (location)
            {
                case ResourceDataLocation.Srdi when srdi == null:
                    throw new ArgumentNullException(nameof(srdi), "Tried to read resource data from the SRDI stream but it was null.");
                case ResourceDataLocation.Srdi:
                {
                    srdi.Seek(address, SeekOrigin.Begin);

                    using BinaryReader srdiReader = new(srdi, Encoding.ASCII, true);
                    byte[] data = srdiReader.ReadBytes(info.Length);
                    externalResources.Add(new(location, data, info.Unknown1, info.Unknown2));
                    break;
                }
                case ResourceDataLocation.Srdv when srdv == null:
                    throw new ArgumentNullException(nameof(srdv), "Tried to read resource data from the SRDV stream but it was null.");
                case ResourceDataLocation.Srdv:
                {
                    srdv.Seek(address, SeekOrigin.Begin);

                    using BinaryReader srdvReader = new(srdv, Encoding.ASCII, true);
                    byte[] data = srdvReader.ReadBytes(info.Length);
                    externalResources.Add(new(location, data, info.Unknown1, info.Unknown2));
                    break;
                }
                default:
                    throw new NotImplementedException($"There is no corresponding location for an address of {info.Address:X8}");
            }
        }

        // Read local resource info
        List<LocalResourceInfo> localResourceInfo = new();
        reader.BaseStream.Seek(localResourceInfoOffset, SeekOrigin.Begin);
        for (int i = 0; i < localResourceInfoCount; ++i)
        {
            LocalResourceInfo info = new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            localResourceInfo.Add(info);
        }

        // Read local resource data
        List<LocalResource> localResources = new();
        foreach (LocalResourceInfo info in localResourceInfo)
        {
            reader.BaseStream.Seek(info.Address1, SeekOrigin.Begin);
            string name = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
            reader.BaseStream.Seek(info.Address2, SeekOrigin.Begin);
            byte[] data = reader.ReadBytes(info.Length);
            localResources.Add(new(name, data, info.Unknown));
        }

        // Read unknown int list
        List<int> unknownInts = new();
        if (unknownIntListOffset > 0)
        {
            reader.BaseStream.Seek(unknownIntListOffset, SeekOrigin.Begin);
            while (reader.BaseStream.Position < resourceStringListOffset)   // TODO: This may not be how we should determine the ending point
            {
                unknownInts.Add(reader.ReadInt32());
            }
        }

        // Read resource string data
        reader.BaseStream.Seek(resourceStringListOffset, SeekOrigin.Begin);
        List<string> resourceStrings = new();
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            resourceStrings.Add(Utils.ReadNullTerminatedString(reader, Encoding.GetEncoding("shift-jis")));
        }

        return new RsiBlock(unknown00, unknown01, unknown02, unknown06, localResources, externalResources, unknownInts,
            resourceStrings, new());
    }
    public static (byte[] main, byte[]? srdi, byte[]? srdv) SerializeRsiBlock(RsiBlock rsi)
    {
        using MemoryStream mainMem = new();
        using MemoryStream srdiMem = new();
        using MemoryStream srdvMem = new();
        using BinaryWriter mainWriter = new(mainMem, Encoding.ASCII);

        // Write external resource data and info
        List<ExternalResourceInfo> extResourceInfo = new();
        foreach (var externalResource in rsi.ExternalResources)
        {
            int address;
            var locationEnum = (ResourceDataLocation)externalResource.Location;
            if (locationEnum == ResourceDataLocation.Srdi)
            {
                address = (int)srdiMem.Position | (int)externalResource.Location;
                srdiMem.Write(externalResource.Data);
            }
            else if (locationEnum == ResourceDataLocation.Srdv)
            {
                address = (int)srdvMem.Position | (int)externalResource.Location;
                srdvMem.Write(externalResource.Data);
            }
            else
                throw new InvalidDataException($"No defined ExternalResourceLocation for value {externalResource.Location:X8}");

            extResourceInfo.Add(new(address, externalResource.Data.Length, externalResource.UnknownValue1, externalResource.UnknownValue2));
        }

        // Write local resource data and info (using placeholder addresses)
        List<LocalResourceInfo> locResourceInfo = new();
        using BinaryWriter localResourceDataWriter = new(new MemoryStream());
        foreach (var localResource in rsi.LocalResources)
        {
            // TODO: Do stuff here
            throw new NotImplementedException();
        }

        // Write unknown int list
        using BinaryWriter unknownIntWriter = new(new MemoryStream());
        if (rsi.UnknownIntList.Count > 0)
        {
            foreach (int unk in rsi.UnknownIntList)
            {
                unknownIntWriter.Write(unk);
            }
        }

        // Finally, write everything in its final order
        if (unknownIntWriter.BaseStream.Length > 0)
        {
            mainWriter.Write((byte)4);
        }
        else
        {
            mainWriter.Write((byte)6);
        }
        mainWriter.Write(rsi.Unknown01);
        mainWriter.Write(rsi.Unknown02);
        mainWriter.Write((byte)rsi.ExternalResources.Count);
        mainWriter.Write((short)rsi.LocalResources.Count);
        mainWriter.Write(rsi.Unknown06);

        // Write ExternalResourceInfo
        using BinaryWriter externalResourceInfoWriter = new(new MemoryStream());

        throw new NotImplementedException();
    }

    public static TxrBlock DeserializeTxrBlock(MemoryStream mainStream)
    {
        using BinaryReader reader = new(mainStream);
        
        int unknown00 = reader.ReadInt32();
        ushort swizzle = reader.ReadUInt16();
        ushort width = reader.ReadUInt16();
        ushort height = reader.ReadUInt16();
        ushort scanline = reader.ReadUInt16();
        TextureFormat format = (TextureFormat)reader.ReadByte();
        byte unknown0D = reader.ReadByte();
        byte palette = reader.ReadByte();
        byte paletteId = reader.ReadByte();

        return new TxrBlock(unknown00, swizzle, width, height, scanline, format, unknown0D, palette, paletteId, new());
    }
    public static byte[] SerializeTxrBlock(TxrBlock txr)
    {
        using MemoryStream mem = new();
        using BinaryWriter writer = new(mem);

        writer.Write(txr.Unknown00);
        writer.Write(txr.Swizzle);
        writer.Write(txr.Width);
        writer.Write(txr.Height);
        writer.Write(txr.Scanline);
        writer.Write((byte)txr.Format);
        writer.Write(txr.Unknown0D);
        writer.Write(txr.Palette);
        writer.Write(txr.PaletteID);
        
        return mem.ToArray();
    }
}