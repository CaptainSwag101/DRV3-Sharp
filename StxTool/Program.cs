using System;
using System.Collections.Generic;
using System.IO;

namespace StxTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("STX Tool by CaptainSwag101\n" +
                "Version 0.0.1, built on 2019-10-14\n");

            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: No targets specified.");
                return;
            }

            foreach (string arg in args)
            {
                FileInfo info = new FileInfo(arg);
                if (!info.Exists)
                {
                    Console.WriteLine($"ERROR: File \"{arg}\" does not exist, skipping.");
                    continue;
                }

                if (info.Extension.ToLowerInvariant() == ".stx")
                {
                    // Convert STX to TXT
                    StxFile stx = new StxFile();
                    stx.Load(info.FullName);

                    using StreamWriter writer = new StreamWriter(info.FullName.Replace(info.Extension, "") + ".txt", false);
                    foreach (var tuple in stx.StringTables)
                    {
                        writer.WriteLine("{");

                        foreach (string str in tuple.Item1)
                        {
                            writer.WriteLine(str.Replace("\r", @"\r").Replace("\n", @"\n"));
                        }

                        writer.WriteLine("}");
                    }
                }
                else if (info.Extension.ToLowerInvariant() == ".txt")
                {
                    // Convert TXT to STX
                    StxFile stx = new StxFile();

                    using StreamReader reader = new StreamReader(info.FullName);
                    while (!reader.EndOfStream)
                    {
                        if (reader.ReadLine().StartsWith('{'))
                        {
                            List<string> table = new List<string>();

                            while (true)
                            {
                                string line = reader.ReadLine();
                                if (string.IsNullOrEmpty(line))
                                {
                                    continue;
                                }

                                if (line.StartsWith('}'))
                                {
                                    break;
                                }

                                table.Add(line.Replace(@"\n", "\n").Replace(@"\r", "\r"));
                            }

                            stx.StringTables.Add((table, 8));
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
