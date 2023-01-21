using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace DRV3_Sharp_Library.Formats.Data.SRD.Blocks;

internal static class BlockSerializer
{
    public static byte[] GetTerminatorBlockBytes()
    {
        UnknownBlock ct0 = new("$CT0", Array.Empty<byte>(), new());
        return SerializeUnknownBlock(ct0);
    }
    public static UnknownBlock DeserializeUnknownBlock(string blockType, MemoryStream mainStream)
    {
        return new UnknownBlock(blockType, mainStream.ToArray(), new());
    }
    public static byte[] SerializeUnknownBlock(UnknownBlock block)
    {
        return block.MainData;
    }

    public static MshBlock DeserializeMshBlock(MemoryStream mainStream)
    {
        using BinaryReader reader = new(mainStream);

        uint unknown00 = reader.ReadUInt32();
        ushort vertexBlockNamePtr = reader.ReadUInt16();
        ushort materialNamePtr = reader.ReadUInt16();
        ushort specialFlagNamePtr = reader.ReadUInt16();
        ushort stringOffsetsPtr = reader.ReadUInt16();   // Still unsure about this
        ushort nodeNameOffsetsPtr = reader.ReadUInt16();   // Still unsure about this
        ushort nodeMappingDataPtr = reader.ReadUInt16();
        byte stringCount = reader.ReadByte();
        byte topLevelNodeCount = reader.ReadByte();
        byte childNodeCount = reader.ReadByte();
        byte unknown13 = reader.ReadByte();
        
        // Read strings
        List<string> strings = new();
        for (var i = 0; i < stringCount; ++i)
        {
            // Seek to the next name offset
            mainStream.Seek(stringOffsetsPtr + (sizeof(ushort) * i), SeekOrigin.Begin);
            ushort nameOffset = reader.ReadUInt16();
            
            // Seek to the name
            mainStream.Seek(nameOffset, SeekOrigin.Begin);
            string name = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
            strings.Add(name);
        }
        
        // Read top-level node names
        Dictionary<string, List<string>> mappedNodes = new();
        for (var i = 0; i < topLevelNodeCount; ++i)
        {
            // Seek to the next name offset
            mainStream.Seek(nodeNameOffsetsPtr + (sizeof(ushort) * i), SeekOrigin.Begin);
            ushort nameOffset = reader.ReadUInt16();
            
            // Seek to the name
            mainStream.Seek(nameOffset, SeekOrigin.Begin);
            string name = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
            mappedNodes[name] = new();
        }

        // Read mapped child nodes
        mainStream.Seek(nodeMappingDataPtr, SeekOrigin.Begin);
        for (var pairNum = 0; pairNum < childNodeCount; ++pairNum)
        {
            ushort valuePtr = reader.ReadUInt16();
            ushort keyPtr = reader.ReadUInt16();
            var oldPos = mainStream.Position;
            
            mainStream.Seek(valuePtr, SeekOrigin.Begin);
            string value = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
            mainStream.Seek(keyPtr, SeekOrigin.Begin);
            string key = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);

            if (mappedNodes[key] is null)
                throw new InvalidDataException("Attempted to add a child value to a node that shouldn't contain children.");
            mappedNodes[key]!.Add(value);
            
            mainStream.Seek(oldPos, SeekOrigin.Begin);
        }

        // Read other strings
        mainStream.Seek(vertexBlockNamePtr, SeekOrigin.Begin);
        string vertexBlockName = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
        mainStream.Seek(materialNamePtr, SeekOrigin.Begin);
        string materialName = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
        mainStream.Seek(specialFlagNamePtr, SeekOrigin.Begin);
        string specialFlagString = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);

        return new MshBlock(unknown00, unknown13, specialFlagString,
            vertexBlockName, materialName, strings, mappedNodes, new());
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
    
    private sealed record ExternalResourceInfo(int Pointer, int Length, int Unknown1, int Unknown2);
    private sealed record LocalResourceInfo(int NamePointer, int DataPointer, int Length, int Unknown);
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
            var location = (ResourceDataLocation)(info.Pointer & 0xF0000000);
            int address = info.Pointer & 0x0FFFFFFF;

            switch (location)
            {
                case ResourceDataLocation.Srdi when srdi == null:
                    throw new ArgumentNullException(nameof(srdi), "Tried to read resource data from the SRDI stream but it was null.");
                case ResourceDataLocation.Srdi:
                {
                    srdi.Seek(address, SeekOrigin.Begin);

                    using BinaryReader srdiReader = new(srdi, Encoding.ASCII, true);
                    externalResources.Add(new(location, srdiReader.ReadBytes(info.Length), info.Unknown1, info.Unknown2));
                    break;
                }
                case ResourceDataLocation.Srdv when srdv == null:
                    throw new ArgumentNullException(nameof(srdv), "Tried to read resource data from the SRDV stream but it was null.");
                case ResourceDataLocation.Srdv:
                {
                    srdv.Seek(address, SeekOrigin.Begin);

                    using BinaryReader srdvReader = new(srdv, Encoding.ASCII, true);
                    externalResources.Add(new(location, srdvReader.ReadBytes(info.Length), info.Unknown1, info.Unknown2));
                    break;
                }
                default:
                    throw new InvalidDataException($"There is no corresponding location for an address of {info.Pointer:X8}");
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
            reader.BaseStream.Seek(info.NamePointer, SeekOrigin.Begin);
            string name = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
            reader.BaseStream.Seek(info.DataPointer, SeekOrigin.Begin);
            localResources.Add(new(name, reader.ReadBytes(info.Length), info.Unknown));
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

        return new RsiBlock(unknown00, unknown01, unknown02, unknown06, localResources, externalResources,
            resourceStrings, unknownInts, new());
    }
    public static (byte[] main, byte[]? srdi, byte[]? srdv) SerializeRsiBlock(RsiBlock rsi)
    {
        using MemoryStream mainMem = new();
        using MemoryStream srdiMem = new();
        using MemoryStream srdvMem = new();
        using BinaryWriter mainWriter = new(mainMem, Encoding.ASCII);

        // Write external resource data and info
        List<ExternalResourceInfo> externalResourceInfo = new();
        foreach (var externalResource in rsi.ExternalResources)
        {
            int address;
            if (externalResource.Location == ResourceDataLocation.Srdi)
            {
                address = (int)srdiMem.Position | (int)externalResource.Location;
                srdiMem.Write(externalResource.Data);
            }
            else if (externalResource.Location == ResourceDataLocation.Srdv)
            {
                address = (int)srdvMem.Position | (int)externalResource.Location;
                srdvMem.Write(externalResource.Data);
            }
            else
                throw new InvalidDataException($"No defined ExternalResourceLocation for value {externalResource.Location:X8}");

            externalResourceInfo.Add(new(address, externalResource.Data.Length, externalResource.UnknownValue1, externalResource.UnknownValue2));
        }

        // Write local resource data and info.
        // We use placeholder addresses to be offset by our localResourceData's final position within the block data.
        List<LocalResourceInfo> localResourceInfo = new();
        using MemoryStream localResourceDataStream = new();
        using MemoryStream localResourceNameStream = new();
        using BinaryWriter localResourceDataWriter = new(localResourceDataStream);
        using BinaryWriter localResourceNameWriter = new(localResourceNameStream);
        int localResourceDataSize = 0;
        foreach (var localResource in rsi.LocalResources)
        {
            int nameAddressToBeOffset = (int)localResourceNameWriter.BaseStream.Position;
            int dataAddressToBeOffset = (int)localResourceDataWriter.BaseStream.Position;
            localResourceInfo.Add(new(nameAddressToBeOffset, dataAddressToBeOffset, localResource.Data.Length, -1));
            
            localResourceNameWriter.Write(Encoding.ASCII.GetBytes(localResource.Name));
            localResourceNameWriter.Write((byte)0); // Null terminator
            
            localResourceDataWriter.Write(localResource.Data);

            localResourceDataSize += localResource.Data.Length;
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

        // Finally, start writing the output, beginning with the header.
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
        foreach (var externResource in externalResourceInfo)
        {
            mainWriter.Write(externResource.Pointer);
            mainWriter.Write(externResource.Length);
            mainWriter.Write(externResource.Unknown1);
            mainWriter.Write(externResource.Unknown2);
        }
        
        // Write LocalResourceInfo
        var localResourceInfoSize = localResourceInfo.Count * 4 * sizeof(int);
        int localResourceDataOffset = (int)mainWriter.BaseStream.Position + localResourceInfoSize + (rsi.UnknownIntList.Count * sizeof(int));
        int localResourceNameOffset = localResourceDataOffset + localResourceDataSize;
        foreach (var localResource in localResourceInfo)
        {
            mainWriter.Write(localResource.NamePointer + localResourceNameOffset);
            mainWriter.Write(localResource.DataPointer + localResourceDataOffset);
            mainWriter.Write(localResource.Length);
            mainWriter.Write(localResource.Unknown);
        }
        
        // Write unknown int list
        foreach (int i in rsi.UnknownIntList)
        {
            mainWriter.Write(i);
        }
        
        // Write local resource data
        mainWriter.Write(localResourceDataStream.ToArray());
        
        // Write local resource names
        mainWriter.Write(localResourceNameStream.ToArray());
        
        // Write resource strings
        foreach (string str in rsi.ResourceStrings)
        {
            mainWriter.Write(Encoding.ASCII.GetBytes(str));
            mainWriter.Write((byte)0);  // Null terminator
        }

        // Return only the memory streams that were populated with actual data.
        return (mainMem.ToArray(), srdiMem.Length == 0 ? srdiMem.ToArray() : null, srdvMem.Length == 0 ? srdvMem.ToArray() : null);
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

        return new TxrBlock(unknown00, unknown0D, swizzle, width, height, scanline, format, palette, paletteId, new());
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

    public static VtxBlock DeserializeVtxBlock(MemoryStream mainStream)
    {
        using BinaryReader reader = new(mainStream);

        // NOTE: Quite a few of the data elements in this block are as-yet unknown and existing
        // elements may have been interpreted incorrectly or configured based on mistaken
        // assumptions. The contents and structure of this block is subject to change.
        int vectorCount = reader.ReadInt32();
        short unknown04 = reader.ReadInt16();
        short meshType = reader.ReadInt16();
        int vertexCount = reader.ReadInt32();
        short unknown0C = reader.ReadInt16();
        byte unknown0E = reader.ReadByte();
        byte vertexSectionInfoCount = reader.ReadByte();
        ushort boneRootPtr = reader.ReadUInt16();
        ushort vertexSectionInfoPtr = reader.ReadUInt16();
        ushort unknownVectorListPtr = reader.ReadUInt16();
        ushort boneListPtr = reader.ReadUInt16();
        uint unknown18 = reader.ReadUInt32();
        uint unknown1C = reader.ReadUInt32();
        
        // Read unknown short list
        List<short> unknownShorts = new();
        while ((mainStream.Position + 1) < vertexSectionInfoPtr)
        {
            unknownShorts.Add(reader.ReadInt16());
        }
        
        // Read vertex section info
        mainStream.Seek(vertexSectionInfoPtr, SeekOrigin.Begin);
        List<(uint Start, uint Size)> vertexSectionInfo = new();
        for (var s = 0; s < vertexSectionInfoCount; ++s)
        {
            vertexSectionInfo.Add((reader.ReadUInt32(), reader.ReadUInt32()));
        }
        
        // Read bone root and list
        mainStream.Seek(boneRootPtr, SeekOrigin.Begin);
        short rootBoneID = reader.ReadInt16();

        if (boneListPtr != 0) mainStream.Seek(boneListPtr, SeekOrigin.Begin);

        List<string> boneList = new();
        while (mainStream.Position < unknownVectorListPtr)
        {
            // This data structure is a list of 16-bit name offsets, followed by a list of strings.
            ushort boneNameOffset = reader.ReadUInt16();
            if (boneNameOffset == 0) break; // A zero offset indicates the end of the list.

            // Seek to and read the bone name, then return to the offset list.
            var returnPos = mainStream.Position;
            mainStream.Seek(boneNameOffset, SeekOrigin.Begin);
            boneList.Add(Utils.ReadNullTerminatedString(reader, Encoding.ASCII));
            mainStream.Seek(returnPos, SeekOrigin.Begin);
        }
        
        // Read the unknown list of floats (vector3's???)
        mainStream.Seek(unknownVectorListPtr, SeekOrigin.Begin);
        List<Vector3> unknownVectors = new();
        for (var h = 0; h < (vectorCount / 2); ++h)
        {
            Vector3 vector = new()
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle()
            };
            unknownVectors.Add(vector);
        }
        
        // Read the list of mapping strings?
        List<string> mappingStrings = new();
        while (mainStream.Position < mainStream.Length)
        {
            mappingStrings.Add(Utils.ReadNullTerminatedString(reader, Encoding.ASCII));
        }

        return new VtxBlock(unknown04, unknown0C, unknown0E, unknown18, unknown1C, meshType, vertexCount, vertexSectionInfo,
            rootBoneID, boneList, unknownShorts, unknownVectors, mappingStrings, new());
    }
    // TODO: Add serializer function when we understand this block type better
}