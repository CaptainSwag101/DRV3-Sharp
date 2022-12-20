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
using System.Text;

namespace DRV3_Sharp_Library.Formats.Resource.SRD.Blocks;

public enum ResourceDataLocation
{
    Srdi = 0x20000000,
    Srdv = 0x40000000
}

public sealed record ExternalResourceInfo(int Address, int Length, int Unknown1, int Unknown2);
public sealed record LocalResourceInfo(int Address1, int Address2, int Length, int Unknown);

public sealed class RsiBlock : ISrdBlock
{
    #region Public Properties
    public string BlockType { get { return @"$RSI"; } }
    public byte Unknown00 { get; private set; }
    public byte Unknown01 { get; private set; }
    public sbyte Unknown02 { get; private set; }
    public short Unknown06 { get; private set; }
    public List<(string Name, byte[] Data, int UnknownValue)> LocalResourceData { get; }
    public List<(ResourceDataLocation Location, byte[] Data, int UnknownValue1, int UnknownValue2)> ExternalResourceData { get; }
    public List<int> UnknownIntList { get; }
    public List<string> ResourceStrings { get; }
    #endregion

    #region Public Methods
    public RsiBlock()
    {
        LocalResourceData = new();
        ExternalResourceData = new();
        UnknownIntList = new();
        ResourceStrings = new();
    }

    public List<string> GetBlockInfo()
    {
        List<string> infoList = new();

        infoList.Add($"Block Type: {BlockType}");
        infoList.Add($"{nameof(Unknown00)}: {Unknown00}");
        infoList.Add($"{nameof(Unknown01)}: {Unknown01}");
        infoList.Add($"{nameof(Unknown02)}: {Unknown02}");
        infoList.Add($"{nameof(Unknown06)}: {Unknown06}");

        if (LocalResourceData.Count > 0)
        {
            infoList.Add("Local Resource Data:");

            foreach ((string Name, byte[] Data, int UnknownValue) in LocalResourceData)
            {
                infoList.Add($"\tName: {Name}, Data Size: {Data.Length}, Unknown: {UnknownValue}");
            }
        }

        if (ExternalResourceData.Count > 0)
        {
            infoList.Add("External Resource Data:");

            foreach ((ResourceDataLocation Location, byte[] Data, int UnknownValue1, int UnknownValue2) in ExternalResourceData)
            {
                infoList.Add($"\tResource Location: {Location}, Data Size: {Data.Length}, Unknown 1: {UnknownValue1}, Unknown 2: {UnknownValue2}");
            }
        }

        if (ResourceStrings.Count > 0)
        {
            infoList.Add("Resource Strings:");

            foreach (string str in ResourceStrings)
            {
                infoList.Add($"\t{str}");
            }
        }

        return infoList;
    }
    #endregion

    #region Public Static Methods
    public static void Deserialize(MemoryStream inputMainData, Stream? inputSrdi, Stream? inputSrdv, out RsiBlock outputBlock)
    {
        outputBlock = new();

        using BinaryReader reader = new(inputMainData);

        outputBlock.Unknown00 = reader.ReadByte();  // 0x06 or 0x04 in some cases like $MAT blocks, this may be tied to Unknown0A?
        outputBlock.Unknown01 = reader.ReadByte();  // 0x05
        outputBlock.Unknown02 = reader.ReadSByte(); // usually 0x04 or 0xFF, but seems to be 0x30 on PS4?
        //if (outputBlock.Unknown02 != -1 && outputBlock.Unknown02 != 4)
        //    throw new NotImplementedException($"Encountered an unusual value for Unknown02, expected -1 or 4 but got {outputBlock.Unknown02}.\nPlease send this file to the developer!");
        byte externalResourceInfoCount = reader.ReadByte();
        //Debug.Assert((outputBlock.Unknown02 == -1 && externalResourceInfoCount == 0) || (outputBlock.Unknown02 == 4 && externalResourceInfoCount > 0));
        short localResourceInfoCount = reader.ReadInt16();
        outputBlock.Unknown06 = reader.ReadInt16();
        short localResourceInfoOffset = reader.ReadInt16();
        short unknownIntListOffset = reader.ReadInt16();
        Debug.Assert((outputBlock.Unknown00 == 6 && unknownIntListOffset == 0) || (outputBlock.Unknown00 == 4 && unknownIntListOffset != 0));
        int resourceStringListOffset = reader.ReadInt32();

        // Read external resource info
        List<ExternalResourceInfo> externalResourceInfo = new();
        for (int i = 0; i < externalResourceInfoCount; ++i)
        {
            ExternalResourceInfo info = new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            externalResourceInfo.Add(info);
        }

        // Read external resource data
        foreach (ExternalResourceInfo info in externalResourceInfo)
        {
            // Determine resource data location
            ResourceDataLocation location = (ResourceDataLocation)(info.Address & 0xF0000000);
            int address = info.Address & 0x0FFFFFFF;

            if (location == ResourceDataLocation.Srdi)
            {
                if (inputSrdi == null) throw new ArgumentNullException(nameof(inputSrdi), "Tried to read resource data from the SRDI stream but it was null.");

                inputSrdi.Seek(address, SeekOrigin.Begin);

                using BinaryReader srdiReader = new(inputSrdi, Encoding.ASCII, true);
                byte[] data = srdiReader.ReadBytes(info.Length);
                outputBlock.ExternalResourceData.Add((location, data, info.Unknown1, info.Unknown2));
            }
            else if (location == ResourceDataLocation.Srdv)
            {
                if (inputSrdv == null) throw new ArgumentNullException(nameof(inputSrdv), "Tried to read resource data from the SRDV stream but it was null.");

                inputSrdv.Seek(address, SeekOrigin.Begin);

                using BinaryReader srdvReader = new(inputSrdv, Encoding.ASCII, true);
                byte[] data = srdvReader.ReadBytes(info.Length);
                outputBlock.ExternalResourceData.Add((location, data, info.Unknown1, info.Unknown2));
            }
            else
            {
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
        foreach (LocalResourceInfo info in localResourceInfo)
        {
            reader.BaseStream.Seek(info.Address1, SeekOrigin.Begin);
            string name = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
            reader.BaseStream.Seek(info.Address2, SeekOrigin.Begin);
            byte[] data = reader.ReadBytes(info.Length);
            outputBlock.LocalResourceData.Add((name, data, info.Unknown));
        }

        // Read unknown int list
        if (unknownIntListOffset > 0)
        {
            reader.BaseStream.Seek(unknownIntListOffset, SeekOrigin.Begin);
            while (reader.BaseStream.Position < resourceStringListOffset)   // TODO: This may not be how we should determine the ending point
            {
                outputBlock.UnknownIntList.Add(reader.ReadInt32());
            }
        }

        // Read resource string data
        reader.BaseStream.Seek(resourceStringListOffset, SeekOrigin.Begin);
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            outputBlock.ResourceStrings.Add(Utils.ReadNullTerminatedString(reader, Encoding.GetEncoding("shift-jis")));
        }
    }

    public static void Serialize(RsiBlock inputBlock, Stream outputMainData, Stream outputSrdi, Stream outputSrdv)
    {
        using BinaryWriter mainDataWriter = new(outputMainData, Encoding.ASCII, true);

        // Write external resource data and info
        List<ExternalResourceInfo> extResourceInfo = new();
        foreach (var externalResource in inputBlock.ExternalResourceData)
        {
            int address;
            if (externalResource.Location == ResourceDataLocation.Srdi)
            {
                address = (int)outputSrdi.Position | (int)externalResource.Location;
                outputSrdi.Write(externalResource.Data);
            }
            else if (externalResource.Location == ResourceDataLocation.Srdv)
            {
                address = (int)outputSrdv.Position | (int)externalResource.Location;
                outputSrdv.Write(externalResource.Data);
            }
            else
                throw new InvalidDataException($"No defined ExternalResourceLocation for value {externalResource.Location:X8}");

            extResourceInfo.Add(new(address, externalResource.Data.Length, externalResource.UnknownValue1, externalResource.UnknownValue2));
        }

        // Write local resource data and info (using placeholder addresses)
        List<LocalResourceInfo> locResourceInfo = new();
        using BinaryWriter localResourceDataWriter = new(new MemoryStream());
        foreach (var localResource in inputBlock.LocalResourceData)
        {
                
        }

        // Write unknown int list
        using BinaryWriter unknownIntWriter = new(new MemoryStream());
        if (inputBlock.UnknownIntList.Count > 0)
        {
            foreach (int unk in inputBlock.UnknownIntList)
            {
                unknownIntWriter.Write(unk);
            }
        }

        // Finally, write everything in its final order
        if (unknownIntWriter.BaseStream.Length > 0)
        {
            mainDataWriter.Write((byte)4);
        }
        else
        {
            mainDataWriter.Write((byte)6);
        }
        mainDataWriter.Write(inputBlock.Unknown01);
        mainDataWriter.Write(inputBlock.Unknown02);
        mainDataWriter.Write((byte)inputBlock.ExternalResourceData.Count);
        mainDataWriter.Write((short)inputBlock.LocalResourceData.Count);
        mainDataWriter.Write(inputBlock.Unknown06);

        // Write ExternalResourceInfo
        using BinaryWriter externalResourceInfoWriter = new(new MemoryStream());

        throw new NotImplementedException();
    }
    #endregion
}