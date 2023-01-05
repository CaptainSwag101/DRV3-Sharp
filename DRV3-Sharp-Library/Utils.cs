using System;
using System.IO;
using System.Text;

namespace DRV3_Sharp_Library;

public static class Utils
{
    // Annoyingly, there's no easy way to read a null-terminated string in .NET
    // (or maybe I'm just a moron), so we have to do it manually.
    public static string ReadNullTerminatedString(BinaryReader reader, Encoding encoding)
    {
        using BinaryReader stringReader = new(reader.BaseStream, encoding, true);
        StringBuilder sb = new();
        while (stringReader.BaseStream.Position < stringReader.BaseStream.Length)
        {
            char c = stringReader.ReadChar();

            // Break on null terminator
            if (c == 0) break;

            sb.Append(c);
        }

        return sb.ToString();
    }

    public static void SkipToNearest(BinaryReader reader, int multipleOf)
    {
        int padLength = multipleOf - (int)(reader.BaseStream.Position % multipleOf);
        if (padLength != multipleOf)
        {
            reader.BaseStream.Seek(padLength, SeekOrigin.Current);
        }
    }

    public static void PadToNearest(BinaryWriter writer, int multipleOf, byte padValue = 0)
    {
        int padLength = multipleOf - (int)(writer.BaseStream.Position % multipleOf);
        if (padLength != multipleOf)
        {
            Span<byte> padding = new byte[padLength];
            padding.Fill(padValue);

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

    public static int NearestMultipleOf(int x, int multipleOf)
    {
        return x + (x % multipleOf);
    }
}