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

namespace DRV3_Sharp_Library.Formats.Archive.SPC
{
    public static class SpcSerializer
    {
        private const string CONST_FILE_MAGIC = "CPS.";
        private const string CONST_VITA_COMPRESSED_MAGIC = "$CMP";
        private const string CONST_TABLE_HEADER = "Root";

        public static void Deserialize(Stream inputStream, out SpcData outputData)
        {
            using BinaryReader reader = new(inputStream, Encoding.ASCII, true);

            // Read and verify SPC header magic string
            string fileMagic = Encoding.ASCII.GetString(reader.ReadBytes(4));

            // For Vita files, do Vita decompression and then recursively decompress,
            // then short-circuit and return for convenience.
            if (fileMagic == CONST_VITA_COMPRESSED_MAGIC)
            {
                Console.WriteLine("You are attempting to decompress a file from the PS Vita version of the game.");
                Console.WriteLine("This is not fully supported, and the results may be totally broken.");

                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                byte[] vitaData = reader.ReadBytes((int)reader.BaseStream.Length);
                byte[] fixedData = SpcCompressor.VitaDecompressWhole(vitaData);

                //using FileStream testVitaPartialOutput = new("vitaOutputTest.spc", FileMode.Create);
                //testVitaPartialOutput.Write(fixedData);
                //testVitaPartialOutput.Flush();
                //testVitaPartialOutput.Close();

                using MemoryStream fixedStream = new(fixedData);
                Deserialize(fixedStream, out outputData);
                return;
            }

            if (fileMagic != CONST_FILE_MAGIC) throw new InvalidDataException($"Invalid file magic, expected {CONST_FILE_MAGIC} but got {fileMagic}.");

            byte[] unknown1 = reader.ReadBytes(0x24);
            int fileCount = reader.ReadInt32();
            if (fileCount < 0) throw new InvalidDataException($"File count was negative: {fileCount}");
            int unknown2 = reader.ReadInt32();
            reader.BaseStream.Seek(0x10, SeekOrigin.Current);

            outputData = new();
            outputData.Unknown2C = unknown2;

            // Read and verify file table header
            string tableHeader = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (tableHeader != CONST_TABLE_HEADER) throw new InvalidDataException($"Invalid file table header, expected {CONST_TABLE_HEADER} but got {tableHeader}.");

            // Skip 12 bytes of padding
            reader.BaseStream.Seek(0x0C, SeekOrigin.Current);

            // Read file data
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

                outputData.Files.Add(new ArchivedFile(name, data, unknownFlag, (compressionFlag == 2), originalSize));
            }
        }

        public static void Serialize(SpcData inputData, Stream outputStream)
        {
            using BinaryWriter writer = new(outputStream, Encoding.ASCII, true);

            writer.Write(Encoding.ASCII.GetBytes(CONST_FILE_MAGIC));
            // Write unknown data 1 (24 bytes, possibly padding?)
            writer.Write((int)0);
            byte[] paddingFF = new byte[8]; // 8 bytes of 0xFF padding
            Array.Fill<byte>(paddingFF, 0xFF);
            writer.Write(paddingFF);
            writer.Write(new byte[0x18]);   // Padding
            writer.Write(inputData.FileCount);
            writer.Write(inputData.Unknown2C);
            writer.Write(new byte[0x10]);   // Padding
            writer.Write(Encoding.ASCII.GetBytes(CONST_TABLE_HEADER));
            writer.Write(new byte[0x0C]);   // Padding

            // Write file entries
            for (int i = 0; i < inputData.FileCount; ++i)
            {
                ArchivedFile entry = inputData.Files[i];

                string name = entry.Name;
                writer.Write((short)(entry.IsCompressed ? 2 : 1));
                writer.Write(entry.UnknownFlag);
                writer.Write(entry.Data.Length);
                writer.Write(entry.OriginalSize);
                writer.Write(name.Length);
                writer.Write(new byte[0x10]);   // Padding

                int namePadding = (0x10 - (name.Length + 1) % 0x10) % 0x10;
                writer.Write(Encoding.GetEncoding("shift-jis").GetBytes(name));
                writer.Write(new byte[namePadding + 1]);

                int dataPadding = (0x10 - entry.Data.Length % 0x10) % 0x10;
                writer.Write(entry.Data);
                writer.Write(new byte[dataPadding]);
            }
        }
    }
}
