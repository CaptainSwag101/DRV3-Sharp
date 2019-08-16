using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SrdTool
{
    class Utils
    {
        // Annoyingly, there's no easy way to read a null-terminated ASCII string in .NET
        // (or maybe I'm just a moron), so we have to do it manually.
        public static string ReadNullTerminatedString(ref BinaryReader reader)
        {
            List<byte> rawString = new List<byte>();
            while (true)
            {
                byte b = reader.ReadByte();

                if (b == 0) break;

                rawString.Add(b);
            }
            string result = new ASCIIEncoding().GetString(rawString.ToArray());
            return result;
        }

        public static void ReadPadding(ref BinaryReader reader)
        {
            int paddingLength = 16 - (int)(reader.BaseStream.Position % 16);
            if (paddingLength != 16)
            {
                reader.BaseStream.Seek(paddingLength, SeekOrigin.Current);
            }
        }

        public static void WritePadding(ref BinaryWriter writer)
        {
            int paddingLength = 16 - (int)(writer.BaseStream.Position % 16);
            if (paddingLength != 16)
            {
                byte[] padding = new byte[paddingLength];
                writer.Write(padding);
            }
        }

        public static int PowerOfTwo(int x)
        {
            if (x < 0) { return 0; }
            --x;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }
    }
}
