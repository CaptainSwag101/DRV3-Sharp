using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DRV3_Sharp_Library.Formats.Archive.SPC;

public static class SpcCompressor
{
    // WARNING: Changing these constants will likely break the compression or reduce its effectiveness!
    private const int SPC_BLOCK_SIZE = 16;
    private const int SPC_WINDOW_MAX_SIZE = 1024;
    private const int SPC_SEQUENCE_MAX_SIZE = 65;

    public static byte[] Decompress(byte[] compressedData)
    {
        List<byte> decompressedData = new();

        int compressedSize = compressedData.Length;

        // Setup flags to track the decompression state
        int flag = 1;
        int pos = 0;

        while (pos < compressedSize)
        {
            // We use an 8-bit flag to determine whether something is raw data,
            // or if we need to pull from the buffer, going from most to least significant bit.
            // We reverse the bit order to make it easier to work with.
            if (flag == 1)
            {
                // Add an extra "1" bit so our last flag value will always cause us to read new flag data.
                flag = 0x100 | ReverseBits(compressedData[pos++]);
            }

            // Is this redundant?
            // Probably not, since just above we increment pos.
            if (pos >= compressedSize)
            {
                break;
            }

            // Check if the current flag bit is set
            if ((flag & 1) == 1)
            {
                // Raw byte
                decompressedData.Add(compressedData[pos++]);
            }
            else
            {
                // Pull from the buffer
                // xxxxxxyy yyyyyyyy
                // Count  -> x + 2 (max length of 65 bytes)
                // Offset -> y (from the beginning of a 1024-byte sliding window)
                ushort b = BitConverter.ToUInt16(new byte[] { compressedData[pos++], compressedData[pos++] });
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

    public static byte[] Compress(byte[] uncompressedData)
    {
        List<byte> compressedData = new();

        int uncompressedSize = uncompressedData.Length;

        // Make a Span<byte> of the original uncompressed data so we can slice into it for our sliding window
        ReadOnlySpan<byte> uncompressedDataSpan = uncompressedData;

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
            if (curFlagBit == 8 || pos >= uncompressedSize)
            {
                flag = ReverseBits((byte)flag);
                compressedData.Add((byte)flag);
                compressedData.AddRange(curBlock);

                // If we've reached the end, break out of the infinite compression loop
                if (pos >= uncompressedSize)
                    break;

                // Prep for the next block
                flag = 0;
                curFlagBit = 0;
                curBlock = new List<byte>(SPC_BLOCK_SIZE);
            }

            // Keep track of the current sequence of data we're trying to compress
            int seqLen = 1; // The length of the sequence that we're trying to compress
            int foundAt = -1;   // Where that sequence is found previously in the sliding window, relative to the start of the window
            int searchbackLen = Math.Min(pos, SPC_WINDOW_MAX_SIZE); // Clamp searchbackLen to prevent seeking back past the beginning

            // When we start this loop, we've just started looking for a new sequence to compress
            for (; seqLen <= SPC_SEQUENCE_MAX_SIZE; ++seqLen)
            {
                // The sliding window is a slice into the uncompressed data that we search for duplicate instances of the sequence
                // The readahead length MUST be at least one byte shorter than the current sequence length
                int readaheadLen = Math.Min(seqLen - 1, uncompressedSize - pos);
                ReadOnlySpan<byte> window = uncompressedDataSpan.Slice(pos - searchbackLen, searchbackLen + readaheadLen);

                // If we've reached the end of the file, don't try to compress any more data, just use what we've got
                // NOTE: We do NOT need to backup/restore foundAt here because it hasn't been touched yet this iteration!
                if (pos + seqLen > uncompressedSize)
                {
                    --seqLen;
                    break;
                }

                // Slice the sequence we're searching for from the uncompressed data
                ReadOnlySpan<byte> seq = uncompressedDataSpan.Slice(pos, seqLen);
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
                curBlock.Add(uncompressedDataSpan[pos]);
            }

            // Increment the current read position by the size of whatever sequence we found (even if it's non-compressable)
            pos += Math.Max(1, seqLen);
            ++curFlagBit;
        }

        return compressedData.ToArray();
    }

    public static byte[] VitaDecompressWhole(byte[] compressedData)
    {
        List<byte> decompressedData = new();

        using BinaryReader reader = new(new MemoryStream(compressedData));
        _ = reader.ReadBytes(4);    // Magic value, $CMP
        int compressedSize = BinaryPrimitives.ReverseEndianness(reader.ReadInt32());
        _ = reader.ReadBytes(8);
        int decompressedSize = BinaryPrimitives.ReverseEndianness(reader.ReadInt32());
        int compressedSize2 = BinaryPrimitives.ReverseEndianness(reader.ReadInt32());
        _ = reader.ReadBytes(4);
        int unknown = BinaryPrimitives.ReverseEndianness(reader.ReadInt32());

        while (true)
        {
            string compressionMode = Encoding.ASCII.GetString(reader.ReadBytes(4));

            if (!compressionMode.StartsWith("$CL") && compressionMode != "$CR0") break;

            int chunkDecompressedSize = BinaryPrimitives.ReverseEndianness(reader.ReadInt32());
            int chunkCompressedSize = BinaryPrimitives.ReverseEndianness(reader.ReadInt32());
            _ = reader.ReadBytes(4);

            byte[] chunkData = reader.ReadBytes(chunkCompressedSize - 0x10);

            // $CR0 means uncompressed
            if (compressionMode != "$CR0")
                chunkData = VitaDecompressChunk(chunkData, compressionMode);

            decompressedData.AddRange(chunkData);
        }

        return decompressedData.ToArray();
    }

    private static byte[] VitaDecompressChunk(byte[] chunkData, string compressionMode)
    {
        List<byte> decompressedChunk = new();

        int processedBytes = 0;

        int shift;
        if (compressionMode == "$CLN") shift = 8;
        else if (compressionMode == "$CL1") shift = 7;
        else if (compressionMode == "$CL2") shift = 6;
        else throw new InvalidDataException($"Invalid compression mode {compressionMode}.");

        byte mask = (byte)((byte)(1 << shift) - 1);

        while (processedBytes < chunkData.Length)
        {
            byte b = chunkData[processedBytes++];

            if ((b & 1) > 0)
            {
                // Read from buffer
                int count = (b & mask) >> 1;
                int offset = ((b >> shift) << 8) | chunkData[processedBytes++];

                // Duplicate the last {count} bytes starting at {offset}
                for (int i = 0; i < count; ++i)
                {
                    decompressedChunk.Add(decompressedChunk[^offset]);
                }
            }
            else
            {
                // Raw bytes
                int count = (b >> 1);
                decompressedChunk.AddRange(chunkData[processedBytes..(processedBytes + count)]);
                processedBytes += count;
            }
        }

        return decompressedChunk.ToArray();
    }

    // The 4-step method seems to be faster than the 3-step method slightly
    // taken from: https://graphics.stanford.edu/~seander/bithacks.html#ReverseByteWith64Bits
    private static byte ReverseBits(byte b)
    {
        return (byte)((((b * (ulong)0x80200802) & (ulong)0x0884422110) * (ulong)0x0101010101) >> 32);
    }
}