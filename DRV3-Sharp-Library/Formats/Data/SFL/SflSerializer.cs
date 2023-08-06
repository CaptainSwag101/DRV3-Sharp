using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

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

        // Read tables as containing generic binary data entries, so we can decode
        // and deserialize their data in a second pass.
        uint tableCount = reader.ReadUInt32();
        Dictionary<uint, GenericTable> unprocessedTables = new();
        for (uint tNum = 0; tNum < tableCount; ++tNum)
        {
            // Read table header
            uint tableId = reader.ReadUInt32();
            uint tableLength = reader.ReadUInt32();
            ushort tableEntryCount = reader.ReadUInt16();
            ushort tableUnknown1 = reader.ReadUInt16();
            uint tableUnknown2 = reader.ReadUInt32();

            long endPos = reader.BaseStream.Position + tableLength;

            // Read entries
            Dictionary<uint, UnknownDataEntry> entries = new();
            for (var eNum = 0; eNum < tableEntryCount; ++eNum)
            {
                // If we've reached the end of the table before leaving this loop, something has gone wrong
                if (reader.BaseStream.Position >= endPos)
                    throw new InvalidDataException(
                        $"Read past the end of table {tableId} while parsing entry {eNum} at position {reader.BaseStream.Position}.");
                
                // Read entry data to be stored in proper type later
                uint entryId = reader.ReadUInt32();
                int entryLength = reader.ReadInt32();
                ushort entryUnknown = reader.ReadUInt16();
                ushort entrySequenceCount = reader.ReadUInt16();
                uint entryUsesSequences = reader.ReadUInt32();

                entries.Add(entryId, new(entryUnknown, entrySequenceCount, reader.ReadBytes(entryLength)));
            }
            
            unprocessedTables.Add(tableId, new(tableUnknown1, tableUnknown2, entries));
        }
        
        // Now, parse the tables and their entries into their proper types
        IntegerTable? imageIdTable = null;
        ShortTable? imageResolutionTable = null;
        Dictionary<uint, GenericTable> unknownTables = new();
        Dictionary<uint, TransformationTable> transformationTables = new();
        foreach ((var tableId, var rawTable) in unprocessedTables)
        {
            // Determine the table type based on total table count and absolute ID
            if (tableId == 1)
            {
                Dictionary<uint, IntegerDataEntry> intEntries = new();
                foreach ((var entryId, var rawEntry) in rawTable.Entries)
                {
                    // Data length must be evenly divisible by 4 (length of 32-bit int)
                    Debug.Assert(rawEntry.Data.Length % 4 == 0);

                    var values = new int[rawEntry.Data.Length / 4];
                    ReadOnlySpan<byte> dataSpan = rawEntry.Data;
                    for (var i = 0; i < values.Length; ++i)
                    {
                        values[i] = BitConverter.ToInt16(dataSpan[(i * 4)..((i + 1) * 4)]);
                    }
                    
                    intEntries.Add(entryId, new(rawEntry.EventNumber, values));
                }
                
                imageIdTable = new(rawTable.Unknown1, rawTable.Unknown2, intEntries);
            }
            else if (tableId == 2)
            {
                Dictionary<uint, ShortDataEntry> shortEntries = new();
                foreach ((var entryId, var rawEntry) in rawTable.Entries)
                {
                    // Data length must be evenly divisible by 2 (length of 16-bit int)
                    Debug.Assert(rawEntry.Data.Length % 2 == 0);

                    var values = new short[rawEntry.Data.Length / 2];
                    ReadOnlySpan<byte> dataSpan = rawEntry.Data;
                    for (var i = 0; i < values.Length; ++i)
                    {
                        values[i] = BitConverter.ToInt16(dataSpan[(i * 2)..((i + 1) * 2)]);
                    }
                    
                    shortEntries.Add(entryId, new(rawEntry.EventNumber, values));
                }
                
                imageResolutionTable = new(rawTable.Unknown1, rawTable.Unknown2, shortEntries);
            }
            else if (tableId < unprocessedTables.Count - 1)
            {
                // We don't understand these tables yet,
                // add them without further processing
                unknownTables.Add(tableId, rawTable);
            }
            else
            {
                Dictionary<uint, TransformationEntry> transformEntries = new();
                foreach ((var entryId, var rawEntry) in rawTable.Entries)
                {
                    // Read sequences
                    BinaryReader entryReader = new(new MemoryStream(rawEntry.Data));
                    List<TransformSequence> sequences = new();
                    for (uint sub = 0; sub < rawEntry.SequenceCount; ++sub)
                    {
                        int sequenceDataLength = entryReader.ReadInt32();
                        ushort sequenceHeaderLength = entryReader.ReadUInt16();
                        ushort sequenceOperationCount = entryReader.ReadUInt16();
                        string sequenceName = Utils.ReadNullTerminatedString(entryReader, Encoding.ASCII);
                        Utils.SkipToNearest(entryReader, 4);

                        // Read transformation operations
                        var operations = new List<TransformOperation>();
                        for (ushort opNum = 0; opNum < sequenceOperationCount; ++opNum)
                        {
                            ushort opcode = entryReader.ReadUInt16();
                            ushort commandDataLength = entryReader.ReadUInt16();
                            byte[] data = entryReader.ReadBytes((int)commandDataLength);
                            
                            operations.Add(new TransformOperation(opcode, data));
                        }
                        
                        sequences.Add(new TransformSequence(sequenceName, operations));
                    }
                    
                    transformEntries.Add(entryId, new TransformationEntry(rawEntry.EventNumber, sequences));
                }
                
                transformationTables.Add(tableId, new(rawTable.Unknown1, rawTable.Unknown2, transformEntries));
            }
        }

        outputData = new(version, imageIdTable!, imageResolutionTable!, unknownTables, transformationTables);
    }
}