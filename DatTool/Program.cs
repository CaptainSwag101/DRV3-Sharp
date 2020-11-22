using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using V3Lib.Resource.DAT;

namespace DatTool
{
    class Program
    {
        const string UsageString = "Usage: DatTool.exe <one or more DAT or CSV files to convert>";

        static void Main(string[] args)
        {
            Console.WriteLine("DAT Tool by CaptainSwag101\n" +
                "Version 1.1.0, built on 2020-11-16\n");

            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: No targets specified.");
                Console.WriteLine(UsageString);
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

                if (info.Extension.ToLowerInvariant() == ".dat")
                {
                    // Convert DAT to CSV
                    DATTable dat = new(new FileStream(info.FullName, FileMode.Open));

                    // Use a StringBuilder to build the output file text
                    StringBuilder output = new();

                    // Write first row (header)
                    List<string> headerEntries = new();
                    var columnDefinitions = dat.GetColumnDefinitions();
                    foreach (var def in columnDefinitions)
                    {
                        headerEntries.Add($"{def.Name} ({def.Type})");
                    }
                    output.AppendJoin(',', headerEntries);
                    output.Append('\n');

                    // Write row data
                    List<string> rowData = new();
                    for (int row = 0; row < dat.GetColumn(0).RowCount; ++row)
                    {
                        StringBuilder rowStr = new();

                        // Parse each value in the cell
                        List<string> escapedCellStrings = new();
                        for (int col = 0; col < columnDefinitions.Count; ++col)
                        {

                            var cellValues = dat.GetCell(col, row);

                            for (int i = 0; i < cellValues.Count; ++i)
                            {
                                // Exception for string types: get actual string data
                                string colType = columnDefinitions[col].Type.ToLowerInvariant();
                                if (colType == "ascii" || colType == "label" || colType == "refer")
                                {
                                    ushort stringIndex = (ushort)cellValues[i];
                                    escapedCellStrings.Add(dat.UTF8Strings[stringIndex]);
                                }
                                else if (colType == "utf16")
                                {
                                    ushort stringIndex = (ushort)cellValues[i];
                                    escapedCellStrings.Add(dat.UTF16Strings[stringIndex]);
                                }
                                else
                                {
                                    string? strVal = cellValues[i].ToString();
                                    if (strVal == null)
                                    {
                                        throw new InvalidDataException("Unable to convert the underlying value to a string. This is a serious problem.");
                                    }
                                    escapedCellStrings.Add(strVal);
                                }
                            }
                        }

                        // Escape the cell strings
                        for (int s = 0; s < escapedCellStrings.Count; ++s)
                        {
                            escapedCellStrings[s] = escapedCellStrings[s].Insert(0, "\"").Insert(escapedCellStrings[s].Length + 1, "\"");
                            escapedCellStrings[s] = escapedCellStrings[s].Replace("\n", "\\n").Replace("\r", "\\r");
                        }

                        rowStr.AppendJoin(",", escapedCellStrings);
                        rowData.Add(rowStr.ToString());
                    }
                    output.AppendJoin('\n', rowData);

                    using StreamWriter writer = new(info.FullName.Substring(0, info.FullName.Length - info.Extension.Length) + ".csv", false, Encoding.Unicode);
                    writer.Write(output.ToString());
                }
                else if (info.Extension.ToLowerInvariant() == ".csv")
                {
                    // Convert CSV to DAT
                    using StreamReader reader = new StreamReader(info.FullName, Encoding.Unicode);

                    // First line is column definitions
                    string? headerLine = reader.ReadLine();
                    if (headerLine == null)
                    {
                        throw new EndOfStreamException("Unexpectedly reached the end of the file before completing DAT building.");
                    }
                    string[] header = headerLine.Split(',');

                    // Generate column definitions
                    List<DataColumn> dataColumns = new();
                    foreach (string headerPiece in header)
                    {
                        string name = headerPiece.Split('(').First();
                        string type = headerPiece.Split('(').Last().TrimEnd(')');

                        DataColumn column = new(name, type);
                        dataColumns.Add(column);
                    }

                    // Read rows
                    List<string> utf8Strings = new();
                    List<string> utf16Strings = new();
                    while (!reader.EndOfStream)
                    {
                        string? rowLine = reader.ReadLine();
                        if (rowLine == null)
                        {
                            throw new EndOfStreamException("Unexpectedly reached the end of the file before completing DAT building.");
                        }

                        // Split the line at every comma
                        string[] rowStrings = rowLine.Split(',');

                        List<string> reformattedRowStrings = new();
                        for (int col = 0; col < rowStrings.Length; ++col)
                        {
                            // Remove leading and trailing quotes originally needed
                            // to make the CSV parse correctly in spreadsheet programs.
                            if (rowStrings[col].StartsWith('\"'))
                                rowStrings[col] = rowStrings[col].Remove(0, 1);
                            if (rowStrings[col].EndsWith('\"'))
                                rowStrings[col] = rowStrings[col].Remove(rowStrings[col].Length, 1);

                            // Add the un-escaped final string to the list
                            reformattedRowStrings.Add(rowStrings[col].Replace("\\n", "\n").Replace("\\r", "\r"));
                        }

                        // reformattedRowStrings now contains each column's data in its own index.
                        // Iterate through each string and convert then add the data within to its respective DataColumn.
                        for (int col = 0; col < dataColumns.Count; ++col)
                        {
                            try
                            {
                                // Split if there's multiple values in the cell
                                string[] splitValues = reformattedRowStrings[col].Split('|');

                                List<byte[]> cellByteList = new();
                                for (int v = 0; v < splitValues.Length; ++v)
                                {
                                    // Cast the string to its appropriate type
                                    string colType = dataColumns[col].Type.ToLowerInvariant();
                                    object parsedValue = DATHelper.StringToTypeFunctions[colType]
                                        .Invoke(reformattedRowStrings[col]);

                                    // Convert the newly-typed value to a byte array and add it to the list of values for this cell
                                    byte[] bytes = DATHelper.TypeToBytesFunctions[colType].Invoke(parsedValue);
                                    cellByteList.Add(bytes);
                                }

                                // Add all the values for the row to its DataColumn
                                dataColumns[col].Add(cellByteList);
                            }
                            catch (InvalidCastException invalidCastEx)
                            {
                                throw new InvalidCastException("Failed to cast a value from string to type.", invalidCastEx);
                            }
                        }
                    }

                    // Add the DataColumns to the DAT table
                    DATTable dat = new(dataColumns, utf8Strings, utf16Strings);

                    using FileStream fs = new(info.FullName.Substring(0, info.FullName.Length - info.Extension.Length) + ".dat", FileMode.Create);
                    fs.Write(dat.GetBytes());
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
