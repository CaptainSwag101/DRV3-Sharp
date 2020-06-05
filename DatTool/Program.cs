using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using V3Lib.Dat;

namespace DatTool
{
    class Program
    {
        const string UsageString = "Usage: DatTool.exe <one or more DAT or CSV files to convert>";

        static void Main(string[] args)
        {
            Console.WriteLine("DAT Tool by CaptainSwag101\n" +
                "Version 0.0.3, built on 2020-06-05\n");

            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: No targets specified.");
                Console.WriteLine(UsageString);
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

                    // Write first row (header)
                    List<string> headerEntries = new List<string>();
                    foreach (var def in dat.ColumnDefinitions)
                    {
                        headerEntries.Add($"{def.Name} ({def.Type})");
                    }
                    output.AppendJoin(',', headerEntries);
                    output.Append('\n');

                    // Write row data
                    List<string> rowData = new List<string>();
                    for (int row = 0; row < dat.Data.Count; ++row)
                    {
                        StringBuilder rowStr = new StringBuilder();

                        List<string> escapedRowStrs = dat.Data[row];
                        for (int s = 0; s < escapedRowStrs.Count; ++s)
                        {
                            escapedRowStrs[s] = escapedRowStrs[s].Insert(0, "\"").Insert(escapedRowStrs[s].Length + 1, "\"");
                            escapedRowStrs[s] = escapedRowStrs[s].Replace("\n", "\\n").Replace("\r", "\\r");
                        }

                        rowStr.AppendJoin(",", escapedRowStrs);

                        rowData.Add(rowStr.ToString());
                    }
                    output.AppendJoin('\n', rowData);

                    using StreamWriter writer = new StreamWriter(info.FullName.Substring(0, info.FullName.Length - info.Extension.Length) + ".csv", false, Encoding.Unicode);
                    writer.Write(output.ToString());
                }
                else if (info.Extension.ToLowerInvariant() == ".csv")
                {
                    // Convert CSV to DAT
                    DatFile dat = new DatFile();

                    using StreamReader reader = new StreamReader(info.FullName, Encoding.Unicode);

                    // First line is column definitions
                    string[] header = reader.ReadLine().Split(',');
                    var colDefinitions = new List<(string Name, string Type, ushort Count)>();
                    foreach (string headerPiece in header)
                    {
                        string name = headerPiece.Split('(').First();
                        string type = headerPiece.Split('(').Last().TrimEnd(')');
                        colDefinitions.Add((name, type, 0));
                    }

                    // Read row data
                    while (!reader.EndOfStream)
                    {
                        string[] rowCells = reader.ReadLine().Split(',');
                        List<string> rowStrings = new List<string>();
                        for (int col = 0; col < rowCells.Length; ++col)
                        {
                            // Update the column definitions with the proper value count
                            colDefinitions[col] = (colDefinitions[col].Name, colDefinitions[col].Type, (ushort)(rowCells[col].Count(c => c == '|') + 1));

                            if (rowCells[col].StartsWith('\"'))
                                rowCells[col] = rowCells[col].Remove(0, 1);

                            if (rowCells[col].EndsWith('\"'))
                                rowCells[col] = rowCells[col].Remove(rowCells[col].Length, 1);

                            rowStrings.Add(rowCells[col].Replace("\\n", "\n").Replace("\\r", "\r"));
                        }
                        dat.Data.Add(rowStrings);
                    }
                    dat.ColumnDefinitions = colDefinitions;

                    dat.Save(info.FullName.Substring(0, info.FullName.Length - info.Extension.Length) + ".dat");
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
