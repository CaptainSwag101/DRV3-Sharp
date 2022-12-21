using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DRV3_Sharp_Library.Formats.Text.STX;

public static class StxSerializer
{
    private const string CONST_FILE_MAGIC = "STXT";
    private const string CONST_LANG_MAGIC = "JPLL";

    public static void Deserialize(Stream inputStream, out StxData outputData)
    {
        using BinaryReader reader = new(inputStream, Encoding.ASCII, true);

            
        // Read header data
        string fileMagic = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (fileMagic != CONST_FILE_MAGIC) throw new InvalidDataException($"Invalid file magic, expected {CONST_FILE_MAGIC} but got {fileMagic}.");

        string langMagic = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (langMagic != CONST_LANG_MAGIC) throw new InvalidDataException($"Invalid language magic, expected {CONST_LANG_MAGIC} but got {langMagic}.");

        int tableCount = reader.ReadInt32();
        if (tableCount > 1) throw new NotImplementedException($"This STX file reports to have more than one string table, which has never been documented. PLEASE SEND THIS STX FILE TO THE DEVELOPER!");

        uint tableOffset = reader.ReadUInt32();

            
        // Read table info
        List<(int Unknown, int StringCount)> tableInfo = new();
        for (int t = 0; t < tableCount; ++t)
        {
            tableInfo.Add((reader.ReadInt32(), reader.ReadInt32()));
            // Align to nearest 16-byte boundary?
            reader.BaseStream.Seek(8, SeekOrigin.Current);
        }

            
        // For each table, read its data and split strings by newline
        // so we can export them in a more readable fashion.
        reader.BaseStream.Seek(tableOffset, SeekOrigin.Begin);
        var tables = new StringTable[tableCount];
        for (int t = 0; t < tableCount; ++t)
        {
            var (unknown, stringCount) = tableInfo[t];
            var strings = new SegmentedString[stringCount];

            for (int s = 0; s < stringCount; ++s)
            {
                uint stringId = reader.ReadUInt32();
                uint stringOffset = reader.ReadUInt32();

                long returnPos = reader.BaseStream.Position;

                reader.BaseStream.Seek(stringOffset, SeekOrigin.Begin);

                // C# does not include a way to read null-terminated strings, so we'll have to do it manually.
                // Also, some languages for this game use carriage returns, others don't. Screw consistency, am I right?
                string rawString = Utils.ReadNullTerminatedString(reader, Encoding.Unicode).Replace("\r","");
                strings[s] = new SegmentedString(rawString.Split('\n'));

                // Check if the string ID does not line up with the position it was given in the list, just in case.
                if (stringId != (strings.Length - 1)) throw new InvalidDataException($"String #{s} has a reported ID of {stringId}, this list is not sorted correctly!");

                reader.BaseStream.Seek(returnPos, SeekOrigin.Begin);
            }

            tables[t] = new StringTable(unknown, strings);
        }

            
        // Finally, construct the output record
        outputData = new(tables);
    }

    public static void Serialize(StxData inputData, Stream outputStream)
    {
        using BinaryWriter writer = new(outputStream, Encoding.ASCII, true);

            
        // Write header data
        writer.Write(Encoding.ASCII.GetBytes(CONST_FILE_MAGIC));
        writer.Write(Encoding.ASCII.GetBytes(CONST_LANG_MAGIC));

        writer.Write(inputData.Tables.Length);
        writer.Write((int)0);   // tableOffset, to be written later

        // Write table info
        foreach (var table in inputData.Tables)
        {
            writer.Write(table.UnknownData);
            writer.Write(table.Strings.Length);
            writer.Write((ulong)0); // Pad to nearest 16-byte boundary
        }

        // Write tableOffset
        long lastPos = writer.BaseStream.Position;
        writer.BaseStream.Seek(0x0C, SeekOrigin.Begin);
        writer.Write((int)lastPos);
        writer.BaseStream.Seek(lastPos, SeekOrigin.Begin);

        // Write temporary padding for string IDs/offset
        foreach (var table in inputData.Tables)
        {
            writer.Write(new byte[(8 * table.Strings.Length)]); // (4 * 2) bytes per string entry
        }

        // Write string data & corresponding ID/offset pair
        long infoPairPos = lastPos;
        foreach (var table in inputData.Tables)
        {
            uint strId = 0;
            List<(int, string)> writtenStrings = new();
            foreach (SegmentedString str in table.Strings)
            {
                // Convert segmented string to a full string
                StringBuilder sb = new();
                sb.AppendJoin('\n', str.Segments);
                string wholeString = sb.ToString();

                // De-duplicate strings by re-using offsets
                int? foundOffset = null;
                foreach (var (offset, text) in writtenStrings)
                {
                    if (text == wholeString)
                    {
                        foundOffset = offset;
                        break;
                    }
                }

                int latestPos = (int)writer.BaseStream.Position;

                int strPos;
                if (foundOffset is not null)
                {
                    strPos = (int)foundOffset;
                }
                else
                {
                    strPos = latestPos;
                    writtenStrings.Add((strPos, wholeString));  // If the string is not already present, add it to the de-duplication list
                }

                // Write ID/offset pair
                writer.BaseStream.Seek(infoPairPos, SeekOrigin.Begin);
                writer.Write(strId++);
                writer.Write(strPos);
                writer.BaseStream.Seek(latestPos, SeekOrigin.Begin);

                // Increment infoPairPos 8 bytes to next entry position
                infoPairPos += 8;

                // Write string data if there are no existing duplicates
                if (foundOffset is null)
                {
                    byte[] strData = Encoding.Unicode.GetBytes(wholeString);
                    writer.Write(strData);
                    writer.Write((ushort)0);
                }
            }
        }
    }
}