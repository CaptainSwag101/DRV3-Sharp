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
using System.IO;
using System.Text;

namespace DRV3_Sharp.Formats.Archive.SPC
{
    static class SpcSerializer
    {
        private const string CONST_FILE_MAGIC = "CPS.";
        private const string CONST_VITA_COMPRESSED_MAGIC = "$CMP";
        private const string CONST_TABLE_HEADER = "Root";

        public static void Deserialize(Stream inputStream, out SpcData outputData)
        {
            using BinaryReader reader = new(inputStream, Encoding.ASCII, true);

            string fileMagic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (fileMagic == CONST_VITA_COMPRESSED_MAGIC) throw new NotImplementedException("Files from the PS Vita version are not currently supported.");
            if (fileMagic != CONST_FILE_MAGIC) throw new InvalidDataException($"Invalid file magic, expected {CONST_FILE_MAGIC} but got {fileMagic}.");

            byte[] unknown1 = reader.ReadBytes(0x24);
            int fileCount = reader.ReadInt32();
            if (fileCount < 0) throw new InvalidDataException($"File count was negative: {fileCount}");
            int unknown2 = reader.ReadInt32();
            reader.BaseStream.Seek(0x10, SeekOrigin.Current);

            outputData = new(unknown1, unknown2);

            string tableHeader = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (tableHeader != CONST_TABLE_HEADER) throw new InvalidDataException($"Invalid file table header, expected {CONST_TABLE_HEADER} but got {tableHeader}.");

            reader.BaseStream.Seek(0x0C, SeekOrigin.Current);

            for (int i = 0; i < fileCount; ++i)
            {
                short compressionFlag = reader.ReadInt16();
                short unknownFlag = reader.ReadInt16();
                int currentSize = reader.ReadInt32();
                int originalSize = reader.ReadInt32();

                int nameLength = reader.ReadInt32();
                reader.BaseStream.Seek(0x10, SeekOrigin.Current);
                int namePadding = (0x10 - (nameLength + 1) % 0x10) % 0x10;
                string name = Encoding.GetEncoding("shift-jis").GetString(reader.ReadBytes(nameLength));
                reader.BaseStream.Seek(namePadding + 1, SeekOrigin.Current);    // Discard the null terminator

                int dataPadding = (0x10 - currentSize % 0x10) % 0x10;
                byte[] data = reader.ReadBytes(currentSize);
                reader.BaseStream.Seek(dataPadding, SeekOrigin.Current);

                outputData.AddFile(name, data, unknownFlag, (compressionFlag == 2));
            }
        }

        public static void Serialize(SpcData inputData, Stream outputStream)
        {
            using BinaryWriter writer = new(outputStream, Encoding.ASCII, true);

            writer.Write(Encoding.ASCII.GetBytes(CONST_FILE_MAGIC));
            writer.Write(inputData.UnknownData1);
            writer.Write(inputData.FileCount);
            writer.Write(inputData.UnknownData2);
            writer.Write(new byte[0x10]);
            writer.Write(Encoding.ASCII.GetBytes(CONST_TABLE_HEADER));
            writer.Write(new byte[0x0C]);

            for (int i = 0; i < inputData.FileCount; ++i)
            {
                var entry = inputData.GetFileEntry(i);
                // We can use null forgiveness because we know we're iterating in-bounds.
                string name = entry?.Name!;
                ArchivedFile subfile = entry?.File!;

                writer.Write(subfile.IsCompressed ? 2 : 1);
                writer.Write(subfile.UnknownFlag);
                writer.Write(subfile.Data.Length);
                writer.Write(subfile.OriginalSize);
                writer.Write(name.Length);
                writer.Write(new byte[0x10]);

                int namePadding = (0x10 - (name.Length + 1) % 0x10) % 0x10;
                writer.Write(Encoding.GetEncoding("shift-jis").GetBytes(name));
                writer.Write(new byte[namePadding + 1]);

                int dataPadding = (0x10 - subfile.Data.Length % 0x10) % 0x10;
                writer.Write(subfile.Data);
                writer.Write(new byte[dataPadding]);
            }
        }
    }
}
