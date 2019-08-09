using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpcTool
{
    class SpcSubfile
    {
        public string Name;
        public byte[] Data;
        public short CompressionFlag;
        public short UnknownFlag;
        public int CurrentSize;
        public int OriginalSize;

        public void Compress()
        {
            // Don't decompress the data if it is already compressed
            if (CompressionFlag != 1)
            {
                return;
            }

            List<byte> compressedData = new List<byte>(OriginalSize);
            // Make a copy of the original Data as a List<byte> so we can grab ranges via shallow copy
            List<byte> originalData = new List<byte>(Data);

            // First, read from the readahead area into the sequence one byte at a time.
            // Then, see if the sequence already exists in the previous 1023 bytes.
            // If it does, note its position. Once we encounter a sequence that
            // is not duplicated, take the last found duplicate and compress it.
            // If we haven't found any duplicate sequences, add the first byte as raw data.
            // If we did find a duplicate sequence, and it is adjacent to the readahead area,
            // see how many bytes of that sequence can be repeated until we encounter
            // a non-duplicate byte or reach the end of the readahead area.
            List<byte> block = new List<byte>(16);
            int pos = 0;
            int flag = 0;
            byte currentFlagBit = 0;

            // This repeats until we've stored the final compressed block,
            // after we reach the end of the uncompressed data.
            while (true)
            {
                // At the end of each 8-byte block (or the end of the uncompressed data),
                // append the flag and compressed block to the compressed data.
                if (currentFlagBit == 8 || pos >= OriginalSize)
                {
                    flag = reverseBits((byte)flag);
                    compressedData.Add((byte)flag);
                    compressedData.AddRange(block);

                    block = new List<byte>(16);

                    flag = 0;
                    currentFlagBit = 0;
                }

                if (pos >= OriginalSize)
                {
                    break;
                }


                int lookaheadLength = Math.Min(OriginalSize - pos, 65);
                List<byte> lookahead = originalData.GetRange(pos, lookaheadLength);
                int searchbackLength = Math.Min(pos, 1024);
                List<byte> window = originalData.GetRange(pos - searchbackLength, searchbackLength + (lookaheadLength - 1));

                // Find the largest matching sequence in the window
                int foundIndex = -1;
                int foundLength = 1;
                List<byte> seq = new List<byte>(65);
                seq.Add(lookahead[0]);

                for (; foundLength <= lookaheadLength; ++foundLength)
                {
                    int lastFoundIndex = foundIndex;
                    if (searchbackLength < 1)
                        break;

                    foundIndex = window.LastIndexOf(seq, searchbackLength - 1);
                    if (foundIndex != -1)
                    {
                        Console.Write("");
                    }

                    if (foundIndex == -1)
                    {
                        if (foundLength > 1)
                        {
                            --foundLength;
                            seq.RemoveAt(seq.Count - 1);
                        }
                        foundIndex = lastFoundIndex;
                        break;
                    }

                    // Break if the sequence is max length (65 bytes)
                    if (foundLength == lookaheadLength)
                        break;

                    seq.Add(lookahead[foundLength]);
                }

                if (foundLength >= 2 && foundIndex != -1)
                {
                    // We found a duplicate sequence
                    ushort repeat_data = 0;
                    repeat_data |= (ushort)(1024 - searchbackLength + foundIndex);
                    repeat_data |= (ushort)((foundLength - 2) << 10);
                    block.AddRange(BitConverter.GetBytes(repeat_data));
                }
                else
                {
                    // We found a new raw byte
                    flag |= (1 << currentFlagBit);
                    block.AddRange(seq);
                }


                ++currentFlagBit;
                // Seek forward to the end of the duplicated sequence,
                // in case it continued into the lookahead buffer.
                pos += foundLength;
            }

            Data = compressedData.ToArray();
            CurrentSize = Data.Length;
            CompressionFlag = 2;
        }

        public void Decompress()
        {
            // Don't decompress the data if it isn't already compressed
            if (CompressionFlag != 2)
            {
                return;
            }

            List<byte> decompressedData = new List<byte>(OriginalSize);

            int flag = 1;
            int pos = 0;

            while (pos < CurrentSize)
            {
                // We use an 8-bit flag to determine whether something is raw data,
                // or if we need to pull from the buffer, going from most to least significant bit.
                // We reverse the bit order to make it easier to work with.

                if (flag == 1)
                {
                    // Add an extra "1" bit so our last flag value will always cause us to read new flag data.
                    flag = 0x100 | reverseBits(Data[pos++]);
                }

                // Is this redundant?
                if (pos >= CurrentSize)
                {
                    break;
                }

                if ((flag & 1) == 1)
                {
                    // Raw byte
                    decompressedData.Add(Data[pos++]);
                }
                else
                {
                    // Pull from the buffer
                    // xxxxxxyy yyyyyyyy
                    // Count  -> x + 2 (max length of 65 bytes)
                    // Offset -> y (from the beginning of a 1023-byte sliding window)
                    ushort b = BitConverter.ToUInt16(new byte[] { Data[pos++], Data[pos++] });
                    byte count = (byte)((b >> 10) + 2);
                    short offset = (short)(b & 1023);

                    for (int i = 0; i < count; ++i)
                    {
                        int reverseIndex = decompressedData.Count - 1024 + offset;
                        decompressedData.Add(decompressedData[reverseIndex]);
                    }
                }

                flag >>= 1;
            }

            Data = decompressedData.ToArray();
            CurrentSize = Data.Length;
            CompressionFlag = 1;
        }

        private byte reverseBits(byte b)
        {
            return (byte)((b * 0x0202020202 & 0x010884422010) % 1023);
        }
    }

    public static class ListExtensions
    {
        public static int LastIndexOf(this List<byte> list, List<byte> seq, int index, byte compressionLevel = 1)
        {
            if (list.Count - index < seq.Count)
            {
                return -1;
            }

            // Start at the end of the index and work backwards
            if (compressionLevel == 0)  // Max compression, very slow
            {
                for (int i = index; i >= 0; --i)
                {
                    bool found = true;
                    for (int j = 0; j < seq.Count; ++j)
                    {
                        if (list[i + j] != seq[j])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        return i;
                    }
                }
            }
            else if (compressionLevel == 1) // Less compression, way faster
            {
                int foundIndex = list.LastIndexOf(seq[0], index);
                if (foundIndex == -1)
                    return -1;

                bool found = true;
                for (int j = 0; j < seq.Count; ++j)
                {
                    if (list[foundIndex + j] != seq[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    return foundIndex;
                }
            }

            return -1;
        }
    }
}
