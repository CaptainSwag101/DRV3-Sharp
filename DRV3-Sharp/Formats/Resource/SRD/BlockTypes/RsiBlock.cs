/*
    DRV3-Sharp, a free and open-source toolkit
    for working with files and assets from Danganronpa V3.

    Copyright (C) 2020-2021  James Pelster
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp.Formats.Resource.SRD.BlockTypes
{
    enum ResourceDataLocation
    {
        Local = 0x00000000,
        Srdi = 0x20000000,
        Srdv = 0x40000000
    }

    record ExternalResourceInfo(int Address, int Length, int Unknown1, int Unknown2);
    record LocalResourceInfo(int Address1, int Address2, int Length, int Unknown);

    /// <summary>
    /// Resource Information Block
    /// </summary>
    record RsiBlock : ISrdBlock
    {
        public List<(string Name, byte[] Data)> LocalResourceData = new();
        public List<(byte[] Data, ResourceDataLocation Location)> ExternalResourceData = new();
        public List<string> ResourceStrings = new();

        public RsiBlock(byte[] mainData, Stream? inputSrdvStream, Stream? inputSrdiStream)
        {
            using BinaryReader reader = new(new MemoryStream(mainData));

            _ = reader.ReadByte();  // 0x06 or 0x04 in some cases like $MAT blocks
            _ = reader.ReadByte();  // 0x05
            sbyte resourceInfoEntryLen = reader.ReadSByte();    // usually 0x04 or 0xFF
            if (resourceInfoEntryLen != -1 && resourceInfoEntryLen != 4)
                throw new NotImplementedException($"Encountered an unusual value here, expected -1 or 4 but got {resourceInfoEntryLen}.");
            byte externalResourceInfoCount = reader.ReadByte();
            short localResourceInfoCount = reader.ReadInt16();
            short unknown06 = reader.ReadInt16();
            short localResourceInfoOffset = reader.ReadInt16();
            short unknown0A = reader.ReadInt16();
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
                    if (inputSrdiStream == null) throw new ArgumentNullException(nameof(inputSrdiStream), "Unable to read resource data from the SRDI stream because it is null.");

                    inputSrdiStream.Seek(address, SeekOrigin.Begin);

                    using BinaryReader srdiReader = new(inputSrdiStream, Encoding.ASCII, true);
                    byte[] data = srdiReader.ReadBytes(info.Length);
                    ExternalResourceData.Add((data, location));
                }
                else if (location == ResourceDataLocation.Srdv)
                {
                    if (inputSrdvStream == null) throw new ArgumentNullException(nameof(inputSrdvStream), "Unable to read resource data from the SRDV stream because it is null.");

                    inputSrdvStream.Seek(address, SeekOrigin.Begin);

                    using BinaryReader srdvReader = new(inputSrdvStream, Encoding.ASCII, true);
                    byte[] data = srdvReader.ReadBytes(info.Length);
                    ExternalResourceData.Add((data, location));
                }
                else
                {
                    throw new NotImplementedException();
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
                byte[] data2 = reader.ReadBytes(info.Length);
                LocalResourceData.Add((name, data2));
            }

            // Read resource string data
            reader.BaseStream.Seek(resourceStringListOffset, SeekOrigin.Begin);
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                ResourceStrings.Add(Utils.ReadNullTerminatedString(reader, Encoding.GetEncoding("shift-jis")));
            }
        }
    }
}
