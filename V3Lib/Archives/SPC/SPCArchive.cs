/*
    V3Lib, an open-source library for reading/writing data from Danganronpa V3
    Copyright (C) 2017-2020  James Pelster

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

namespace V3Lib.Archives.SPC
{
    /// <summary>
    /// The primary archive format used in Danganronpa V3.
    /// Can contain an arbitrary number of individually compressed/uncompressed files.
    /// </summary>
    public class SPCArchive
    {
        #region Private Fields
        private const string SPC_FILE_MAGIC = "CPS.";
        private const string SPC_TABLE_MAGIC = "Root";
        private const string SPC_UNKNOWN_DATA_1 = "AAAAAP//////////AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        private const int SPC_UNKNOWN_DATA_2 = 4;

        private const int SPC_BLOCK_SIZE = 16;
        private const int SPC_WINDOW_MAX_SIZE = 1024;
        private const int SPC_SEQUENCE_MAX_SIZE = 65;
        #endregion

        #region Public Properties
        public List<FileEntry> Entries { get; private set; }
        #endregion

        #region Public Methods

        #region Constructors
        /// <summary>
        /// Creates a new empty SPC archive, with no files.
        /// </summary>
        public SPCArchive()
        {
            // Initialize the entry list
            Entries = new();
        }

        /// <summary>
        /// Reads an SPC archive from an arbitrary data stream.
        /// </summary>
        /// <param name="stream">The stream to read data from.</param>
        /// <exception cref="EndOfStreamException">Occurs when the end of the data stream is reached before the archive has been fully read.</exception>
        /// <exception cref="InvalidDataException">Occurs when the file you're trying to read does not conform to the SPC specification, and is likely invalid.</exception>
        /// <exception cref="NotImplementedException">Occurs when encountering Vita-compressed file data or an "external" data compression flag, as these are not supported yet.</exception>
        public SPCArchive(Stream stream)
        {
            // Initialize the entry list
            Entries = new();

            // Set up a BinaryReader to help us deserialize the data from stream
            using BinaryReader reader = new(stream);

            // Read all stream data inside a "try" block to catch exceptions
            try
            {
                // Read file magic
                string fileMagic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (fileMagic == "$CMP")
                {
                    // Decompress using SRD method first, then resume (not implemented)
                    throw new NotImplementedException("Reading of Vita-compressed file data is not currently supported.");
                }
                if (fileMagic != SPC_FILE_MAGIC)
                {
                    throw new InvalidDataException($"Invalid file magic, expected \"{SPC_FILE_MAGIC}\" but got \"{fileMagic}\".");
                }

                // Read unknown data 1
                string unkData1 = Convert.ToBase64String(reader.ReadBytes(0x24));
                if (unkData1 != SPC_UNKNOWN_DATA_1)
                {
                    throw new InvalidDataException($"Unknown Data 1 does not match the expected value, expected base64 of \"{SPC_UNKNOWN_DATA_1}\" but got base64 of \"{unkData1}\".");
                }

                // Read file count
                int fileCount = reader.ReadInt32();
                if (fileCount < 0)
                {
                    throw new InvalidDataException($"This archive appears to have {fileCount} files, which is invalid.");
                }

                // Read unknown data 2
                int unkData2 = reader.ReadInt32();
                if (unkData2 != SPC_UNKNOWN_DATA_2)
                {
                    throw new InvalidDataException($"Unknown Data 2 does not match the expected value, expected {SPC_UNKNOWN_DATA_2} but got {unkData2}.");
                }

                // Skip padding data
                reader.BaseStream.Seek(0x10, SeekOrigin.Current);

                // Verify file table header
                string tableMagic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (tableMagic != SPC_TABLE_MAGIC)
                {
                    throw new InvalidDataException($"Invalid table magic, expected \"{SPC_TABLE_MAGIC}\" but got \"{tableMagic}\".");
                }

                // Skip padding data
                reader.BaseStream.Seek(0x0C, SeekOrigin.Current);

                // Read file data
                for (int f = 0; f < fileCount; ++f)
                {
                    FileEntry.CompressionState compFlag = (FileEntry.CompressionState)reader.ReadInt16();
                    short unkFlag = reader.ReadInt16();
                    int curSize = reader.ReadInt32();
                    int uncompSize = reader.ReadInt32();

                    int nameLength = reader.ReadInt32();
                    reader.BaseStream.Seek(0x10, SeekOrigin.Current);
                    int namePadding = (0x10 - (nameLength + 1) % 0x10) % 0x10;
                    string name = Encoding.GetEncoding("shift-jis").GetString(reader.ReadBytes(nameLength));
                    reader.BaseStream.Seek(namePadding + 1, SeekOrigin.Current);    // Discard the null terminator

                    int dataPadding = (0x10 - curSize % 0x10) % 0x10;
                    byte[] data = reader.ReadBytes(curSize);
                    reader.BaseStream.Seek(dataPadding, SeekOrigin.Current);

                    FileEntry entry = new(name, data, uncompSize, compFlag, unkFlag);
                    Entries.Add(entry);
                }
            }
            catch (EndOfStreamException)
            {
                throw;
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (NotImplementedException)
            {
                throw;
            }
        }
        #endregion

        /// <summary>
        /// Searches for a file in the archive with the specified name.
        /// </summary>
        /// <param name="name">The file name to search for.</param>
        /// <returns>The file entry with a matching name.</returns>
        /// <exception cref="FileNotFoundException">Occurs when no file with the specified name exists within the archive.</exception>
        public FileEntry ExtractFileByName(string name)
        {
            // First, find the matching entry's index
            int foundIndex = -1;
            for (int i = 0; i < Entries.Count; ++i)
            {
                if (name == Entries[i].Name)
                {
                    foundIndex = i;
                    break;
                }
            }

            // Throw an error if there is no file with the specified name
            if (foundIndex == -1)
            {
                throw new FileNotFoundException($"No file with the name \"{name}\" exists within this archive.");
            }

            return ExtractFileByIndex(foundIndex);
        }

        /// <summary>
        /// Returns the file entry at the specified index.
        /// </summary>
        /// <param name="index">The index of the file entry to retrieve.</param>
        /// <returns>The file entry at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">Occurs when the specified file entry index is out of range.</exception>
        public FileEntry ExtractFileByIndex(int index)
        {
            // Ensure the requested index is within bounds
            if (index < 0 || index >= Entries.Count)
            {
                throw new IndexOutOfRangeException($"The index {index} is not within the range of file entries.");
            }

            FileEntry foundEntry = Entries[index];

            // If the file data is compressed, decompress it
            foundEntry = DecompressFile(foundEntry);

            return foundEntry;
        }

        /// <summary>
        /// Inserts a file into the archive, potentially replacing one that has the same filename.
        /// </summary>
        /// <param name="entry">The file entry to insert.</param>
        /// <param name="clobber">If true, existing files with matching names will be replaced.</param>
        /// <returns>True if the insertion succeeded, false if clobbering is disalbed and a file with the same name already exists.</returns>
        public bool InsertFile(FileEntry entry, bool clobber = false)
        {
            // First, check if there's an entry with the same filename already,
            // as we will need to overwrite it if clobbering is enabled.
            int foundIndex = -1;
            for (int i = 0; i < Entries.Count; ++i)
            {
                if (entry.Name == Entries[i].Name)
                {
                    // Abort if we would need to clobber and it's not allowed
                    if (!clobber)
                        return false;

                    foundIndex = i;
                    break;
                }
            }

            // If entry is not compressed, compress it and use it if it's smaller
            FileEntry compressedEntry = CompressFile(entry);
            if (compressedEntry.CurrentSize < entry.CurrentSize)
                entry = compressedEntry;

            // If clobbering is allowed and we've found a matching file, replace it
            if (foundIndex != -1)
            {
                Entries[foundIndex] = entry;
            }
            else
            {
                Entries.Add(entry);
            }

            return true;
        }

        /// <summary>
        /// Compresses the data in a <see cref="FileEntry"/> if the compressed size would be smaller.
        /// </summary>
        /// <param name="input">The <see cref="FileEntry"/> to try to compress.</param>
        /// <returns>Either a compressed or uncompressed <see cref="FileEntry"/>, whichever is smaller.</returns>
        private static FileEntry CompressFile(FileEntry input)
        {
            // If the file is already compressed, return immediately
            if (input.CompressionFlag == FileEntry.CompressionState.Compressed)
                return input;

            byte[] compData = CompressData(input.Data);

            FileEntry compressedEntry = new(input.Name, compData, input.UncompressedSize, FileEntry.CompressionState.Compressed, input.UnknownFlag);
            return compressedEntry;
        }

        // First, read from the readahead area into the sequence one byte at a time.
        // Then, see if the sequence already exists in the previous 1024 bytes.
        // If it does, note its position. Once we encounter a sequence that
        // is not duplicated, take the last found duplicate and compress it.
        // If we haven't found any duplicate sequences, add the first byte as raw data.
        // If we did find a duplicate sequence, and it is adjacent to the readahead area,
        // see how many bytes of that sequence can be repeated until we encounter
        // a non-duplicate byte or reach the end of the readahead area.
        private static byte[] CompressData(byte[] input)
        {
            // Create our result array where we store compressed data as we generate it
            List<byte> compressedData = new(input.Length);

            // Make a Span<byte> of the original uncompressed data so we can slice into it for our sliding window
            ReadOnlySpan<byte> originalData = input;

            // Keep track of the current compression state
            int pos = 0;    // Current reading position in uncompressed data
            int flag = 0;   // 8-bit (technically 9-bit) flag describing which parts of the current block are compressed/uncompressed
            byte curFlagBit = 0;    // Which bit of the flag we're currently operating on ('8' triggers a block write and resets)
            List<byte> curBlock = new(SPC_BLOCK_SIZE);  // The current block of data that's finished being compressed

            // This repeats until we've stored the final compressed block,
            // after we reach the end of the uncompressed data.
            while (true)
            {
                // At the end of each compressed block (or the end of the uncompressed data),
                // append the flag and block to the compressed data.
                if (curFlagBit == 8 || pos >= input.Length)
                {
                    flag = Utils.ReverseBits((byte)flag);
                    compressedData.Add((byte)flag);
                    compressedData.AddRange(curBlock);

                    // If we've reached the end, break out of the infinite compression loop
                    if (pos >= input.Length)
                        break;

                    // Prep for the next block
                    flag = 0;
                    curFlagBit = 0;
                    curBlock = new List<byte>(SPC_BLOCK_SIZE);
                }

                // Keep track of the current sequence of data we're trying to compress
                int seqLen = 1; // The length of the sequence that we're trying to compress
                int foundAt = -1;   // Where that sequence is found previously in the sliding window, relative to the start of the window
                int searchbackLen = Math.Min(pos, SPC_WINDOW_MAX_SIZE);

                // When we start this loop, we've just started looking for a new sequence to compress
                for (; seqLen <= SPC_SEQUENCE_MAX_SIZE; ++seqLen)
                {
                    // The sliding window is a slice into the uncompressed data that we search for duplicate instances of the sequence
                    // The readahead length MUST be at least one byte shorter than the current sequence length
                    int readaheadLen = Math.Min(seqLen - 1, input.Length - pos);
                    ReadOnlySpan<byte> window = originalData.Slice(pos - searchbackLen, searchbackLen + readaheadLen);

                    // If we've reached the end of the file, don't try to compress any more data, just use what we've got
                    // NOTE: We do NOT need to backup/restore foundAt here because it hasn't been touched yet this iteration!
                    if (pos + seqLen > input.Length)
                    {
                        --seqLen;
                        break;
                    }

                    // Slice the sequence we're searching for from the uncompressed data
                    ReadOnlySpan<byte> seq = originalData.Slice(pos, seqLen);
                    int lastFoundAt = foundAt;  // Back up last found value
                    foundAt = window.LastIndexOf(seq);

                    // If we fail to find a match, then try and restore the last match we found,
                    // which must be one size smaller than the current.
                    if (foundAt == -1)
                    {
                        foundAt = lastFoundAt;
                        --seqLen;
                        break;
                    }
                }

                // If we exit the above loop due to seqLen exceeding SPC_SEQUENCE_MAX_SIZE then we must decrement seqLen
                if (seqLen > SPC_SEQUENCE_MAX_SIZE)
                {
                    seqLen = SPC_SEQUENCE_MAX_SIZE;
                }

                if (seqLen >= 2 && foundAt != -1)
                {
                    // We found a duplicate sequence
                    ushort repeat_data = 0;
                    repeat_data |= (ushort)(SPC_WINDOW_MAX_SIZE - searchbackLen + foundAt);
                    repeat_data |= (ushort)((seqLen - 2) << 10);
                    curBlock.AddRange(BitConverter.GetBytes(repeat_data));
                }
                else
                {
                    // We found a new raw byte
                    flag |= (1 << curFlagBit);
                    curBlock.Add(originalData[pos]);
                }

                // Increment the current read position by the size of whatever sequence we found (even if it's non-compressable)
                pos += Math.Max(1, seqLen);
                ++curFlagBit;
            }

            // Return the compressed data
            return compressedData.ToArray();
        }

        /// <summary>
        /// Decompresses the data of a <see cref="FileEntry"/>, if it isn't already.
        /// </summary>
        /// <param name="input">The <see cref="FileEntry"/> to decompress.</param>
        /// <returns></returns>
        private static FileEntry DecompressFile(FileEntry input)
        {
            // If the file is already uncompressed, return immediately
            if (input.CompressionFlag == FileEntry.CompressionState.Uncompressed)
                return input;

            byte[] decompData = DecompressData(input.Data);

            FileEntry decompressedEntry = new(input.Name, decompData, decompData.Length, FileEntry.CompressionState.Uncompressed, input.UnknownFlag);
            return decompressedEntry;
        }

        private static byte[] DecompressData(byte[] input)
        {
            List<byte> decompressedData = new(input.Length);

            int flag = 1;
            int pos = 0;

            while (pos < input.Length)
            {
                // We use an 8-bit flag to determine whether something is raw data,
                // or if we need to pull from the buffer, going from most to least significant bit.
                // We reverse the bit order to make it easier to work with.

                if (flag == 1)
                {
                    // Add an extra "1" bit so our last flag value will always cause us to read new flag data.
                    flag = 0x100 | Utils.ReverseBits(input[pos++]);
                }

                // Is this redundant?
                // Probably not, since just above we increment pos.
                if (pos >= input.Length)
                {
                    break;
                }

                if ((flag & 1) == 1)
                {
                    // Raw byte
                    decompressedData.Add(input[pos++]);
                }
                else
                {
                    // Pull from the buffer
                    // xxxxxxyy yyyyyyyy
                    // Count  -> x + 2 (max length of 65 bytes)
                    // Offset -> y (from the beginning of a 1024-byte sliding window)
                    ushort b = BitConverter.ToUInt16(new byte[] { input[pos++], input[pos++] });
                    byte count = (byte)((b >> 10) + 2);
                    short offset = (short)(b & (SPC_WINDOW_MAX_SIZE - 1));

                    for (int i = 0; i < count; ++i)
                    {
                        int reverseIndex = decompressedData.Count - SPC_WINDOW_MAX_SIZE + offset;
                        decompressedData.Add(decompressedData[reverseIndex]);
                    }
                }

                flag >>= 1;
            }

            return decompressedData.ToArray();
        }

        /// <summary>
        /// Serializes the whole SRD archive to a byte array, ready to be written to a file or stream.
        /// </summary>
        /// <returns>A byte array containing the full archive file.</returns>
        public byte[] GetBytes()
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms);

            // Write header and unknown data 1
            writer.Write(SPC_FILE_MAGIC);
            writer.Write(SPC_UNKNOWN_DATA_1);

            // Write file count
            writer.Write((int)Entries.Count);

            // Write unknown data 2
            writer.Write(SPC_UNKNOWN_DATA_2);

            // Write padding
            writer.Write(new byte[0x10]);

            // Write file table magic
            writer.Write(SPC_TABLE_MAGIC);

            // Write padding
            writer.Write(new byte[0x0C]);

            // Write files
            foreach (FileEntry file in Entries)
            {
                writer.Write((short)file.CompressionFlag);
                writer.Write(file.UnknownFlag);
                writer.Write(file.CurrentSize);
                writer.Write(file.UncompressedSize);
                writer.Write(file.Name.Length);
                writer.Write(new byte[0x10]);

                int namePadding = (0x10 - (file.Name.Length + 1) % 0x10) % 0x10;
                writer.Write(Encoding.GetEncoding("shift-jis").GetBytes(file.Name));
                writer.Write(new byte[namePadding + 1]);

                int dataPadding = (0x10 - file.CurrentSize % 0x10) % 0x10;
                writer.Write(file.Data);
                writer.Write(new byte[dataPadding]);
            }

            return ms.ToArray();
        }
        #endregion
    }

    /// <summary>
    /// Associated data for any files stored in an SPC archive.
    /// </summary>
    public class FileEntry
    {
        public enum CompressionState : short
        {
            Undefined = 0,
            Uncompressed = 1,
            Compressed = 2,
            External = 3,
        }

        public string Name { get; private set; }
        public byte[] Data { get; private set; }
        public int UncompressedSize { get; private set; }
        public int CurrentSize { get { return Data.Length; } }
        public CompressionState CompressionFlag { get; private set; }
        public short UnknownFlag { get; private set; }

        public FileEntry(string name, byte[] data, int uncompSize, CompressionState compFlag, short unkFlag)
        {
            Name = name;
            Data = data;
            UncompressedSize = uncompSize;
            CompressionFlag = compFlag;
            UnknownFlag = unkFlag;
        }
    }
}
