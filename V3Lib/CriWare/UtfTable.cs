using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.CriWare
{
    public class UtfTable
    {
        private const int COLUMN_MASK_STORAGE = 0xF0;
        private const int COLUMN_MASK_TYPE = 0x0F;

        private const int COLUMN_STORAGE_PERROW = 0x50;
        private const int COLUMN_STORAGE_CONSTANT = 0x30;
        private const int COLUMN_STORAGE_ZERO = 0x10;

        
        private const int COLUMN_TYPE_DATA = 0x0B;
        private const int COLUMN_TYPE_STRING = 0x0A;
        private const int COLUMN_TYPE_FLOAT = 0x08;
        private const int COLUMN_TYPE_8BYTE2 = 0x07;
        private const int COLUMN_TYPE_8BYTE = 0x06;
        private const int COLUMN_TYPE_4BYTE2 = 0x05;
        private const int COLUMN_TYPE_4BYTE = 0x04;
        private const int COLUMN_TYPE_2BYTE2 = 0x03;
        private const int COLUMN_TYPE_2BYTE = 0x02;
        private const int COLUMN_TYPE_1BYTE2 = 0x01;
        private const int COLUMN_TYPE_1BYTE = 0x00;


        //public string Name;
        public List<Dictionary<string, object>> Contents = new List<Dictionary<string, object>>();


        public void Load(byte[] rawData)
        {
            using BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (magic != "@UTF")
            {
                // Not a valid UTF table
                return;
            }

            // Read header
            uint tableSize = BitConverter.ToUInt32(Utils.SwapEndian(reader.ReadBytes(4)));
            uint schemaOffset = 0x20;
            ushort unk = BitConverter.ToUInt16(Utils.SwapEndian(reader.ReadBytes(2)));
            ushort rowsOffset = BitConverter.ToUInt16(Utils.SwapEndian(reader.ReadBytes(2)));
            uint stringTableOffset = BitConverter.ToUInt32(Utils.SwapEndian(reader.ReadBytes(4)));
            uint dataOffset = BitConverter.ToUInt32(Utils.SwapEndian(reader.ReadBytes(4)));
            uint tableNameOffset = BitConverter.ToUInt32(Utils.SwapEndian(reader.ReadBytes(4)));
            ushort columnCount = BitConverter.ToUInt16(Utils.SwapEndian(reader.ReadBytes(2)));
            ushort rowWidth = BitConverter.ToUInt16(Utils.SwapEndian(reader.ReadBytes(2)));
            uint rowCount = BitConverter.ToUInt32(Utils.SwapEndian(reader.ReadBytes(4)));

            // Read initial table schema info
            reader.BaseStream.Seek(schemaOffset, SeekOrigin.Begin);
            var schemaInfo = new List<(byte SchemaType, int ColumnNameOffset, int ConstOffset)>();
            for (uint c = 0; c < columnCount; ++c)
            {
                byte schemaType = reader.ReadByte();
                int colNameOffset = BitConverter.ToInt32(Utils.SwapEndian(reader.ReadBytes(4)));
                int constOffset = -1;

                if ((schemaType & COLUMN_MASK_STORAGE) == COLUMN_STORAGE_CONSTANT)
                {
                    constOffset = (int)reader.BaseStream.Position;

                    int dataType = (schemaType & COLUMN_MASK_TYPE);
                    switch (dataType)
                    {
                        case COLUMN_TYPE_1BYTE:
                        case COLUMN_TYPE_1BYTE2:
                            reader.BaseStream.Seek(1, SeekOrigin.Current);
                            break;

                        case COLUMN_TYPE_2BYTE:
                        case COLUMN_TYPE_2BYTE2:
                            reader.BaseStream.Seek(2, SeekOrigin.Current);
                            break;

                        case COLUMN_TYPE_4BYTE:
                        case COLUMN_TYPE_4BYTE2:
                        case COLUMN_TYPE_FLOAT:
                        case COLUMN_TYPE_STRING:
                            reader.BaseStream.Seek(4, SeekOrigin.Current);
                            break;

                        case COLUMN_TYPE_8BYTE:
                        case COLUMN_TYPE_8BYTE2:
                        case COLUMN_TYPE_DATA:
                            reader.BaseStream.Seek(8, SeekOrigin.Current);
                            break;

                        default:
                            // Unknown type
                            return;
                    }
                }

                schemaInfo.Add((schemaType, colNameOffset, constOffset));
            }

            // Read string table
            uint stringTableStart = stringTableOffset + 8;
            uint stringTableSize = dataOffset - stringTableOffset;
            uint stringTableEnd = stringTableStart + stringTableSize;

            reader.BaseStream.Seek(stringTableStart, SeekOrigin.Begin);
            byte[] stringTableData = reader.ReadBytes((int)stringTableSize);

            // Read data
            for (int r = 0; r < rowCount; ++r)
            {
                int thisRowOffset = 8 + rowsOffset + (r * rowWidth);

                var columnContents = new Dictionary<string, object>();
                for (int c = 0; c < columnCount; ++c)
                {
                    string columnName;
                    object columnValue;

                    byte schemaType = schemaInfo[c].SchemaType;
                    int columnOffset = -1;

                    BinaryReader stringReader = new BinaryReader(new MemoryStream(stringTableData));
                    stringReader.BaseStream.Seek(schemaInfo[c].ColumnNameOffset, SeekOrigin.Begin);
                    columnName = Utils.ReadNullTerminatedString(ref stringReader, Encoding.ASCII);
                    

                    if (schemaInfo[c].ConstOffset >= 0)
                    {
                        columnOffset = schemaInfo[c].ConstOffset;
                    }
                    else
                    {
                        columnOffset = thisRowOffset;
                    }

                    if ((schemaType & COLUMN_MASK_STORAGE) == COLUMN_STORAGE_ZERO)
                    { 
                        columnValue = 0; 
                    }
                    else
                    {
                        reader.BaseStream.Seek(columnOffset, SeekOrigin.Begin);

                        // TODO: The duplicate types are probably for signed/unsigned
                        int typeMask = (schemaType & COLUMN_MASK_TYPE);
                        switch (typeMask)
                        {
                            case COLUMN_TYPE_STRING:
                                int stringOffset = BitConverter.ToInt32(Utils.SwapEndian(reader.ReadBytes(4)));
                                stringReader.BaseStream.Seek(stringOffset, SeekOrigin.Begin);
                                columnValue = Utils.ReadNullTerminatedString(ref stringReader, Encoding.ASCII);
                                break;

                            case COLUMN_TYPE_DATA:
                                int varDataOffset = BitConverter.ToInt32(Utils.SwapEndian(reader.ReadBytes(4)));
                                int varDataSize = BitConverter.ToInt32(Utils.SwapEndian(reader.ReadBytes(4)));

                                if (varDataSize > 0)
                                {
                                    long tempPos = reader.BaseStream.Position;
                                    reader.BaseStream.Seek(dataOffset + varDataOffset + 8, SeekOrigin.Begin);
                                    columnValue = reader.ReadBytes(varDataSize);
                                    reader.BaseStream.Seek(tempPos, SeekOrigin.Begin);
                                }
                                else
                                {
                                    columnValue = null;
                                }
                                break;

                            case COLUMN_TYPE_FLOAT:
                                columnValue = reader.ReadSingle();
                                break;

                            case COLUMN_TYPE_8BYTE:
                            case COLUMN_TYPE_8BYTE2:
                                columnValue = BitConverter.ToInt64(Utils.SwapEndian(reader.ReadBytes(8)));
                                break;

                            case COLUMN_TYPE_4BYTE:
                            case COLUMN_TYPE_4BYTE2:
                                columnValue = BitConverter.ToInt32(Utils.SwapEndian(reader.ReadBytes(4)));
                                break;

                            case COLUMN_TYPE_2BYTE:
                            case COLUMN_TYPE_2BYTE2:
                                columnValue = BitConverter.ToInt16(Utils.SwapEndian(reader.ReadBytes(2)));
                                break;

                            case COLUMN_TYPE_1BYTE:
                            case COLUMN_TYPE_1BYTE2:
                                columnValue = reader.ReadByte();
                                break;

                            default:
                                // Unknown value type
                                return;
                        }

                        // Update the "row offset" if needed
                        if (schemaInfo[c].ConstOffset < 0)
                        {
                            thisRowOffset = (int)reader.BaseStream.Position;
                        }
                    }

                    columnContents.Add(columnName, columnValue);
                    
                    stringReader.Close();
                    stringReader.Dispose();
                }

                Contents.Add(columnContents);
            }
        }
    }
}
