using System.Collections.Generic;
using System.IO;
using System.Text;
using DRV3_Sharp_Library.Formats.Data.SFL.Entries;

namespace DRV3_Sharp_Library.Formats.Data.SFL;

public static class SflSerializer
{
    private const string CONST_FILE_MAGIC = "LLFS";
    
    public static void Deserialize(Stream inputStream, out SflData outputData)
    {
        using BinaryReader reader = new(inputStream);
        
        // Read magic value
        string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (magic != CONST_FILE_MAGIC)
            throw new InvalidDataException(
                $"File magic value was {magic} but expected {CONST_FILE_MAGIC}.");
        
        // Read SFL format version (should be 7.03 for DR1/DR2, and 7.11 for DRV3)
        ushort versionPatch = reader.ReadUInt16();
        ushort versionMinor = reader.ReadUInt16();
        uint versionMajor = reader.ReadUInt32();
        decimal version = versionMajor + (versionMinor / 10m) + (versionPatch / 100m);

        // Read tables
        uint tableCount = reader.ReadUInt32();
        Dictionary<uint, DataTable> dataTables = new();
        Dictionary<uint, TransformationTable> transformTables = new();
        for (uint tNum = 0; tNum < tableCount; ++tNum)
        {
            Dictionary<uint, DataEntry> dataEntries = new();
            Dictionary<uint, TransformationEntry> transformEntries = new();
            
            // Read table header
            uint tableId = reader.ReadUInt32();
            uint tableLength = reader.ReadUInt32();
            ushort tableEntryCount = reader.ReadUInt16();
            ushort tableUnknown1 = reader.ReadUInt16();
            uint tableUnknown2 = reader.ReadUInt32();

            long endPos = reader.BaseStream.Position + tableLength;

            // Read entries
            for (uint eNum = 0; eNum < tableEntryCount; ++eNum)
            {
                // If we've reached the end of the table before leaving this loop, something has gone wrong
                if (reader.BaseStream.Position >= endPos)
                    throw new InvalidDataException(
                        $"Read past the end of table {tableId} while parsing entry {eNum} at position {reader.BaseStream.Position}.");

                // Read entry data to be stored in proper type later
                uint entryId = reader.ReadUInt32();
                int entryLength = reader.ReadInt32();
                ushort entryUnknown = reader.ReadUInt16();
                ushort entrySubCount = reader.ReadUInt16();
                uint entryHasSubentries = reader.ReadUInt32();

                // First 3 or 4 tables contain single- or multi-data entries
                if (tableId < tableCount - 1)
                {
                    dataEntries.Add(entryId, new DataEntry(entryUnknown, reader.ReadBytes(entryLength)));
                }
                else // Final two tables contain transformation entries
                {
                    // Read sequences
                    List<TransformSequence> sequences = new();
                    for (uint sub = 0; sub < entrySubCount; ++sub)
                    {
                        int sequenceDataLength = reader.ReadInt32();
                        ushort sequenceHeaderLength = reader.ReadUInt16();
                        ushort sequenceOperationCount = reader.ReadUInt16();
                        string sequenceName = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
                        Utils.SkipToNearest(reader, 4);

                        // Read transformation operations
                        var operations = new List<TransformOperation>();
                        for (ushort opNum = 0; opNum < sequenceOperationCount; ++opNum)
                        {
                            ushort opcode = reader.ReadUInt16();
                            ushort commandDataLength = reader.ReadUInt16();
                            byte[] data = reader.ReadBytes((int)commandDataLength);
                            
                            operations.Add(new TransformOperation(opcode, data));
                        }
                        
                        sequences.Add(new TransformSequence(sequenceName, operations));
                    }
                    transformEntries.Add(entryId, new TransformationEntry(entryUnknown, sequences));
                }
            }

            if (tableId < tableCount - 1)
            {
                dataTables.Add(tableId, new DataTable(tableUnknown1, tableUnknown2, dataEntries));
            }
            else
            {
                transformTables.Add(tableId, new TransformationTable(tableUnknown1, tableUnknown2, transformEntries));
            }
        }

        outputData = new(version, dataTables, transformTables);
    }
}