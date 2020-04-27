using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V3Lib
{
    public class Utils
    {
        // Annoyingly, there's no easy way to read a null-terminated ASCII string in .NET
        // (or maybe I'm just a moron), so we have to do it manually.
        public static string ReadNullTerminatedString(ref BinaryReader reader, Encoding encoding)
        {
            int bytesPerChar = encoding.GetByteCount("\0");

            List<byte> charData = new List<byte>();
            while (true)
            {
                List<byte> charBytes = new List<byte>();
                charBytes.AddRange(reader.ReadBytes(bytesPerChar));

                // If all bytes are zero, it's a null terminator
                if (charBytes.All((byte b) => b == 0))
                {
                    break;
                }

                charData.AddRange(charBytes);
            }
            string result = encoding.GetString(charData.ToArray());
            return result;
        }

        public static void ReadPadding(ref BinaryReader reader, int padTo)
        {
            int paddingLength = padTo - (int)(reader.BaseStream.Position % padTo);
            if (paddingLength != padTo)
            {
                reader.BaseStream.Seek(paddingLength, SeekOrigin.Current);
            }
        }

        public static void WritePadding(ref BinaryWriter writer, int padTo, byte padValue = 0)
        {
            int paddingLength = padTo - (int)(writer.BaseStream.Position % padTo);
            if (paddingLength != padTo)
            {
                byte[] padding = new byte[paddingLength];
                Array.Fill(padding, padValue);
                writer.Write(padding);
            }
        }

        public static int PowerOfTwo(int x)
        {
            if (x < 0) return 0;
            --x;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        public static byte[] SwapEndian(byte[] bytes)
        {
            Array.Reverse(bytes);
            return bytes;
        }
    }
}
