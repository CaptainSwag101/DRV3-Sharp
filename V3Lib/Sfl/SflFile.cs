using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using V3Lib.Sfl.EntryTypes;

namespace V3Lib.Sfl
{
    public class SflFile
    {
        public decimal Version;
        public List<Table> Tables = new List<Table>();

        public bool Load(string sflPath, out string errorMessage)
        {
            using BinaryReader reader = new BinaryReader(new FileStream(sflPath, FileMode.Open));

            // Read magic value
            string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (magic != "LLFS")
            {
                errorMessage = $"Invalid SFL file, expected magic value \"LLFS\" but got \"{magic}\".";
                return false;
            }

            // Read SFL format version (should be 7.03 for DR1/DR2, and 7.11 for DRV3)
            ushort versionPatch = reader.ReadUInt16();
            ushort versionMinor = reader.ReadUInt16();
            uint versionMajor = reader.ReadUInt32();
            Version = versionMajor + (versionMinor / 10m) + (versionPatch / 100m);

            // Read tables
            uint tableCount = reader.ReadUInt32();
            for (uint tNum = 0; tNum < tableCount; ++tNum)
            {
                // Read table header
                Table table = new Table();
                table.Id = reader.ReadUInt32();
                uint tableLength = reader.ReadUInt32();
                ushort entryCount = reader.ReadUInt16();
                table.Unknown1 = reader.ReadUInt16();
                table.Unknown2 = reader.ReadUInt32();

                long endPos = reader.BaseStream.Position + tableLength;

                // Read entries
                for (uint eNum = 0; eNum < entryCount; ++eNum)
                {
                    // If we've reached the end of the table before leaving this loop, something has gone wrong
                    if (reader.BaseStream.Position >= endPos)
                    {
                        errorMessage = $"Read past the end of table {table.Id} while parsing entry {eNum} at position {reader.BaseStream.Position}.";
                        return false;
                    }

                    // Read entry data to be stored in proper type later
                    uint entryId = reader.ReadUInt32();
                    int entryLength = reader.ReadInt32();
                    ushort entryUnk1 = reader.ReadUInt16();
                    ushort entrySubCount = reader.ReadUInt16();
                    uint entryHasSubentries = reader.ReadUInt32();

                    // First 3 or 4 tables contain single- or multi-data entries
                    Entry entry;
                    if (table.Id < tableCount - 1)
                    {
                        entry = new DataEntry
                        {
                            Id = entryId,
                            Unknown1 = entryUnk1
                        };
                        (entry as DataEntry).Data = reader.ReadBytes(entryLength);
                    }
                    else // Final two tables contain transformation entries
                    {
                        entry = new TransformationEntry
                        {
                            Id = entryId,
                            Unknown1 = entryUnk1
                        };

                        // Read subentries
                        for (uint sub = 0; sub < entrySubCount; ++sub)
                        {
                            int subentryDataLength = reader.ReadInt32();
                            ushort subentryHeaderLength = reader.ReadUInt16();
                            ushort subentrySectionCount = reader.ReadUInt16();
                            string subentryName = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
                            Utils.ReadPadding(reader, 4);

                            // Read transformation commands
                            var commands = new List<TransformationCommand>();
                            for (ushort commandNum = 0; commandNum < subentrySectionCount; ++commandNum)
                            {
                                TransformationCommand command;
                                command.Opcode = reader.ReadUInt16();
                                ushort commandDataLength = reader.ReadUInt16();
                                command.Data = reader.ReadBytes((int)commandDataLength);
                                commands.Add(command);
                            }
                            (entry as TransformationEntry).Subentries.Add(new TransformationSubentry(subentryName, commands));
                        }
                    }
                    table.Entries.Add(entry);
                }
                Tables.Add(table);
            }

            errorMessage = "";
            return true;
        }
    }
}
