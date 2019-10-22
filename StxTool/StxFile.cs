using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StxTool
{
    class StxFile
    {
        public List<Tuple<List<string>, uint>> StringTables = new List<Tuple<List<string>, uint>>();    // table, unknown

        public void Load(string stxPath)
        {
            using BinaryReader reader = new BinaryReader(new MemoryStream(File.ReadAllBytes(stxPath)));

            // Verify the magic value, it should be "STXT"
            string magic = new ASCIIEncoding().GetString(reader.ReadBytes(4));
            if (magic != "STXT")
            {
                Console.WriteLine($"ERROR: Invalid magic value, expected \"STXT\" but got \"{magic}\".");
                return;
            }

            // Verify the language value, it should be "JPLL"
            string lang = new ASCIIEncoding().GetString(reader.ReadBytes(4));
            if (lang != "JPLL")
            {
                Console.WriteLine($"ERROR: Invalid language string, expected \"JPLL\" but got \"{lang}\".");
                return;
            }

            int tableCount = reader.ReadInt32();

            // I have never seen an STX file with more than 1 table,
            // so I'm adding a special notice in case we encounter one so we can test it more.
            if (tableCount > 1)
            {
                Console.WriteLine("WARNING: Encountered a tableCount greater than 1! Please submit this file to the program author for further testing!\n" +
                    "Loading files with more than one table has never been tested, expect bugs and crashes.");
            }

            uint tableOffset = reader.ReadUInt32();

            var tableInfo = new List<Tuple<uint, uint>>();    // unknown, stringCount
            for (int t = 0; t < tableCount; ++t)
            {
                tableInfo.Add(new Tuple<uint, uint>(reader.ReadUInt32(), reader.ReadUInt32()));
                // Align to nearest 16-byte boundary?
                reader.BaseStream.Seek(8, SeekOrigin.Current);
            }

            reader.BaseStream.Seek(tableOffset, SeekOrigin.Begin);
            foreach (Tuple<uint, uint> table in tableInfo)
            {
                List<string> strings = new List<string>();

                for (int s = 0; s < table.Item2; ++s)
                {
                    uint stringId = reader.ReadUInt32();
                    uint stringOffset = reader.ReadUInt32();

                    long returnPos = reader.BaseStream.Position;

                    reader.BaseStream.Seek(stringOffset, SeekOrigin.Begin);

                    // C# does not include a way to read null-terminated strings, so we'll have to do it manually.
                    List<byte> charData = new List<byte>();
                    while (true)
                    {
                        byte b1 = reader.ReadByte();
                        byte b2 = reader.ReadByte();

                        if (b1 == 0 && b2 == 0)
                        {
                            break;
                        }

                        charData.Add(b1);
                        charData.Add(b2);
                    }
                    string str = new UnicodeEncoding(false, false).GetString(charData.ToArray());
                    strings.Add(str);

                    reader.BaseStream.Seek(returnPos, SeekOrigin.Begin);
                }

                StringTables.Add(new Tuple<List<string>, uint>(strings, table.Item1));
            }
        }

        public void Save(string stxPath)
        {

        }
    }
}
