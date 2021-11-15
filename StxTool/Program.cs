using System;
using System.Collections.Generic;
using System.IO;
using V3Lib.Stx;

namespace StxTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("STX Tool by CaptainSwag101\n" +
                "Version 1.0.1, built on 2020-10-15\n");

            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: No targets specified.");
                return;
            }

            foreach (string arg in args)
            {
                FileInfo info = new(arg);
                if (!info.Exists)
                {
                    Console.WriteLine($"ERROR: File \"{arg}\" does not exist, skipping.");
                    continue;
                }

                if (info.Extension.ToLowerInvariant() == ".stx")
                {
                    // Convert STX to TXT
                    StxFile stx = new();
                    stx.Load(info.FullName);

                    using StreamWriter writer = new(info.FullName.Replace(info.Extension, "") + ".txt", false);
                    foreach (var table in stx.StringTables)
                    {
                        writer.WriteLine("{");

                        foreach (string str in table.Strings)
                        {
                            writer.WriteLine(str.Replace("\r", @"\r").Replace("\n", @"\n"));
                        }

                        writer.WriteLine("}");
                    }
                }
                else if (info.Extension.ToLowerInvariant() == ".txt")
                {
                    // Convert TXT to STX
                    StxFile stx = new();

                    using StreamReader reader = new(info.FullName);
                    while (reader != null && !reader.EndOfStream)
                    {
                        if (reader.ReadLine().StartsWith('{'))
                        {
                            List<string> table = new();

                            while (true)
                            {
                                string line = reader.ReadLine();

                                if (line.StartsWith('}'))
                                {
                                    break;
                                }

                                table.Add(line.Replace(@"\n", "\n").Replace(@"\r", "\r"));
                            }

                            stx.StringTables.Add(new StringTable(table, 8));
                        }
                    }

                    stx.Save(info.FullName.Replace(info.Extension, "") + ".stx");
                }
                else
                {
                    Console.WriteLine($"ERROR: Invalid file extension \"{info.Extension}\".");
                    continue;
                }
            }
        }
    }
}
