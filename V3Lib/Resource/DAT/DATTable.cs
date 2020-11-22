/*
    V3Lib, an open-source library for reading/writing data from Danganronpa V3
    Copyright (C) 2017-2020  James Pelster

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V3Lib.Resource.DAT
{
    public class DATTable
    {
        #region Private Fields
        private List<DataColumn> _dataColumns;
        #endregion

        #region Public Properties
        public List<string> UTF8Strings { get; private set; }
        public List<string> UTF16Strings { get; private set; }
        #endregion

        #region Public Methods

        #region Constructors
        /// <summary>
        /// Creates a new empty DAT table with no data or column definitions.
        /// </summary>
        public DATTable()
        {
            // Initialize properties to default values
            _dataColumns = new();
            UTF8Strings = new();
            UTF16Strings = new();
        }

        public DATTable(List<DataColumn> dataColumns, List<string> utf8Strings, List<string> utf16Strings)
        {
            _dataColumns = dataColumns;
            UTF8Strings = utf8Strings;
            UTF16Strings = utf16Strings;
        }

        /// <summary>
        /// Reads a DAT table from a data stream.
        /// </summary>
        /// <param name="stream">The stream to read data from.</param>
        /// <exception cref="EndOfStreamException">Occurs when the end of the data stream is reached before the data has been fully read.</exception>
        /// <exception cref="InvalidDataException">Occurs when the file you're trying to read does not conform to the DAT specification, and is likely invalid.</exception>
        public DATTable(Stream stream)
        {
            // Initialize properties to default values
            _dataColumns = new();
            UTF8Strings = new();
            UTF16Strings = new();

            // Set up a BinaryReader to help us deserialize the data from stream
            using BinaryReader reader = new(stream);

            // Read all stream data inside a "try" block to catch exceptions
            try
            {
                // Read header
                int rowCount = reader.ReadInt32();
                int bytesPerRow = reader.ReadInt32();
                int columnCount = reader.ReadInt32();

                // Read column definitions and store them temporarily until we read the actual data
                List<(string Name, string Type, ushort Count)> columnDefs = new();
                for (int col = 0; col < columnCount; ++col)
                {
                    string colName = Utils.ReadNullTerminatedString(reader, Encoding.UTF8);
                    string colType = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
                    ushort colCount = reader.ReadUInt16();

                    columnDefs.Add((colName, colType, colCount));
                }

                // Align to next 16-byte boundary
                Utils.ReadPadding(reader, 16);

                // Skip ahead to read the UTF-8 strings and UTF-16 strings so we can reference them while reading row data
                long rawDataPtr = reader.BaseStream.Position;
                reader.BaseStream.Seek((bytesPerRow * rowCount), SeekOrigin.Current);

                ushort utf8Count = reader.ReadUInt16();
                ushort utf16Count = reader.ReadUInt16();

                // Read UTF-8 strings
                for (int s = 0; s < utf8Count; ++s)
                {
                    UTF8Strings.Add(Utils.ReadNullTerminatedString(reader, Encoding.UTF8));
                }

                // Align to nearest 2-byte boundary
                Utils.ReadPadding(reader, 2);

                // Read UTF-16 strings
                for (int s = 0; s < utf16Count; ++s)
                {
                    UTF16Strings.Add(Utils.ReadNullTerminatedString(reader, Encoding.Unicode));
                }

                // Seek to the data start pointer in preparation for reading
                reader.BaseStream.Seek(rawDataPtr, SeekOrigin.Begin);

                // Read the data column-by-column using *MATH* and construct DataColumns accordingly
                int columnOffset = 0;
                for (int c = 0; c < columnCount; ++c)
                {
                    // Initialize a new DataColumn
                    DataColumn column = new(columnDefs[c].Name, columnDefs[c].Type);

                    // Store the size of a single value in this column for convenience
                    int bytesPerValue = DATHelper.DataTypeSizes[columnDefs[c].Type.ToLowerInvariant()];

                    for (int r = 0; r < rowCount; ++r)
                    {
                        // Absolute seek here to always ensure we start in the right place
                        reader.BaseStream.Seek(rawDataPtr + columnOffset + (bytesPerRow * r), SeekOrigin.Begin);

                        // Read row data
                        List<byte[]> rowData = new();
                        for (int i = 0; i < columnDefs[c].Count; ++i)
                        {
                            byte[] valueData = reader.ReadBytes(bytesPerValue);
                            rowData.Add(valueData);
                        }

                        // Add the row data to the DataColumn
                        column.Add(rowData);
                    }

                    // Add the new DataColumn to our list
                    _dataColumns.Add(column);

                    // Increment the column data offset by the size of the column we just finished reading
                    columnOffset += bytesPerValue * columnDefs[c].Count;
                }
            }
            catch (EndOfStreamException)
            {
                throw;
            }
            catch (InvalidDataException)
            {
                throw;
            }
        }
        #endregion

        public List<(string Name, string Type, int Count)> GetColumnDefinitions()
        {
            List<(string Name, string Type, int Count)> definitions = new();
            foreach (DataColumn column in _dataColumns)
            {
                definitions.Add((column.Name, column.Type, column.ValuesPerRow));
            }
            return definitions;
        }

        public DataColumn GetColumn(int column)
        {
            return _dataColumns[column];
        }

        public List<object> GetCell(int column, int row)
        {
            return _dataColumns[column].GetRowData(row);
        }

        public byte[] GetBytes()
        {
            using MemoryStream datData = new();
            using BinaryWriter datWriter = new(datData);

            throw new NotImplementedException();

            return datData.ToArray();
        }

        #endregion
    }

    public class DataColumn
    {
        #region Private Fields
        private readonly List<List<byte[]>> _data;
        #endregion

        #region Public Properties
        public string Name { get; set; }
        public string Type { get; private set; }
        public int RowCount { get { return _data.Count; } }
        public int ValuesPerRow { get { return _data.First().Count; } }
        #endregion

        #region Public Methods

        #region Constructors
        public DataColumn(string name, string type)
        {
            // Initialize properties and fields
            _data = new();
            Type = type;
            Name = name;
        }
        #endregion

        public void Add(List<byte[]> rowData)
        {
            // Ensure the row we're adding has the same number of values per row as the rest,
            // but only if there are already existing rows in the column.
            if (_data.Count > 0 && rowData.Count != ValuesPerRow)
            {
                throw new ArgumentException("The number of values per row in the new data is not equal to the existing rows.");
            }

            _data.Add(rowData);
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index > RowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "The specified row index to remove is outside the bounds of the column.");
            }

            _data.RemoveAt(index);
        }

        public void SetValuesPerRow(int count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Cannot have less than one value per row.");
            }

            // Add or remove values from each row as needed
            for (int row = 0; row < RowCount; ++row)
            {
                // We could compute this value outside of the for loop,
                // but doing it inside the loop lets us correct any
                // discrepancies in individual row sizes if they arise.
                int sizeDifference = count - _data[row].Count;

                if (sizeDifference > 0) // Add values per row
                {
                    int arrayLen = _data[row].First().Length;
                    for (int i = 0; i < sizeDifference; ++i)
                    {
                        _data[row].Add(new byte[arrayLen]);
                    }
                }
                else if (sizeDifference < 0)    // Remove values per row
                {
                    _data[row].RemoveRange(count, Math.Abs(sizeDifference));
                }
            }
        }

        public List<object> GetRowData(int row)
        {
            List<object> result = new();

            for (int i = 0; i < ValuesPerRow; ++i)
            {
                var value = DATHelper.BytesToTypeFunctions[Type.ToLowerInvariant()].Invoke(_data[row][i]);
                result.Add(value);
            }

            return result;
        }

        #endregion
    }
}
