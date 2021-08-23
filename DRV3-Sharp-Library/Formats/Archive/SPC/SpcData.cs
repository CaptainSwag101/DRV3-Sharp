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
using System.Linq;

namespace DRV3_Sharp_Library.Formats.Archive.SPC
{
    public record ArchivedFile(byte[] Data, short UnknownFlag, bool IsCompressed, int OriginalSize);

    public class SpcData
    {
        public int Unknown2C;
        public int FileCount { get { return Files.Count; } }
        public IEnumerable<string> FileNames { get { return Files.Keys; } }
        private readonly Dictionary<string, ArchivedFile> Files = new();    // Name, File

        public SpcData()
        { }

        public bool AddFile(string name, byte[] data, short unknownFlag = 4, bool preCompressed = false)
        {
            // This function will abort if a file with the same name already exists.
            // If you wish to replace a file, remove the original one first.
            if (FileNames.Contains(name) == true)
                return false;

            byte[] compressedData;
            if (!preCompressed)
                compressedData = SpcCompressor.Compress(data);
            else
                compressedData = data;

            // If the compressed data is somehow larger or equal to uncompressed data (or is already compressed),
            // save the file as uncompressed. This will save space AND reduce loading times for the game.
            if (preCompressed || compressedData.Length >= data.Length)
            {
                if (preCompressed)
                    Console.WriteLine("Data is already compressed, storing as-is.");
                else
                    Console.WriteLine($"Compression resulted in a larger or equal data size! Storing as uncompressed. ({compressedData.Length} >= {data.Length})");

                Files.Add(name, new ArchivedFile(data, unknownFlag, false, data.Length));
            }
            else
            {
                float percentageSavings = (compressedData.Length - data.Length) / (data.Length * 1.0f) * -100.0f;
                Console.WriteLine($"Compression resulted in a {percentageSavings}% reduction in data size. ({data.Length} -> {compressedData.Length})");
                Files.Add(name, new ArchivedFile(compressedData, unknownFlag, true, compressedData.Length));
            }

            return true;
        }

        public bool RemoveFile(string name)
        {
            if (FileNames.Contains(name) != true)
                return false;

            Files.Remove(name);
            return true;
        }

        public byte[]? GetUncompressedFileData(string filename)
        {
            if (Files.ContainsKey(filename))
            {
                ArchivedFile f = Files[filename];
                if (f.IsCompressed)
                    return SpcCompressor.Decompress(f.Data);
                else
                    return f.Data;
            }
            else
                return null;
        }

        public (string Name, ArchivedFile File)? GetFileEntry(int index)
        {
            if (index >= 0 && index < FileCount)
                return (Files.Keys.ElementAt(index), Files.Values.ElementAt(index));
            else
                return null;
        }

        public (string Name, ArchivedFile File)? GetFileEntry(string name)
        {
            if (Files.ContainsKey(name))
                return (name, Files[name]);
            else
                return null;
        }
    }
}
