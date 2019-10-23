using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DatTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("DAT Tool by CaptainSwag101\n" +
                "Version 0.0.1, built on 2019-10-22\n");

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

                if (info.Extension.ToLowerInvariant() == ".dat")
                {
                    // Convert DAT to CSV
                    DatFile dat = new DatFile();
                    dat.Load(info.FullName);

                    StringBuilder output = new StringBuilder();

                    // Write header
                    List<string> headerEntries = new List<string>();
                    foreach (var value in dat.ValueInfo)
                    {
                        headerEntries.Add($"{value.Name} ({value.Type})");
                    }
                    output.AppendJoin(',', headerEntries);
                    output.Append('\n');

                    // Write struct entries
                    List<string> structEntries = new List<string>();
                    foreach (var entry in dat.StructEntries)
                    {
                        StringBuilder combinedEntry = new StringBuilder();
                        combinedEntry.AppendJoin(',', entry);
                        structEntries.Add(combinedEntry.ToString());
                    }
                    output.AppendJoin('\n', structEntries);

                    using StreamWriter writer = new StreamWriter(info.FullName.TrimEnd(info.Extension.ToCharArray()) + ".csv", false);
                    writer.Write(output.ToString());
                }
                else if (info.Extension.ToLowerInvariant() == ".csv")
                {
                    // Convert CSV to DAT
                    DatFile dat = new DatFile();

                    using StreamReader reader = new StreamReader(info.FullName);

                    // First line is header
                    string[] header = reader.ReadLine().Split(',');
                    List<(string Name, string Type)> valInfo = new List<(string Name, string Type)>();
                    foreach (string headerPiece in header)
                    {
                        string name = headerPiece.Split('(').First();
                        string type = headerPiece.Split('(').Last().TrimEnd(')');
                        valInfo.Add((name, type));
                    }
                    dat.ValueInfo = valInfo;

                    // Read struct entries
                    List<List<string>> entries = new List<List<string>>();
                    while (!reader.EndOfStream)
                    {
                        List<string> entry = new List<string>();
                        entry.AddRange(reader.ReadLine().Split(','));
                        entries.Add(entry);
                    }
                    dat.StructEntries = entries;

                    dat.Save(info.FullName.TrimEnd(info.Extension.ToCharArray()) + ".dat");
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
