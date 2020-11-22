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
using System.Diagnostics;
using System.IO;
using System.Text;

namespace V3Lib.Resource.SFL.EntryTypes
{
    public struct TransformationCommand
    {
        public short Opcode;
        public byte[] Data;
    }

    public class TransformationSubentry
    {
        public string Name { get; set; }
        public List<TransformationCommand> Commands { get; private set; }

        public TransformationSubentry()
        {
            Name = string.Empty;
            Commands = new();
        }

        public TransformationSubentry(string name, List<TransformationCommand> commands)
        {
            Name = name;
            Commands = commands;
        }

        public TransformationSubentry(BinaryReader reader)
        {
            int subentryDataLength = reader.ReadInt32();
            short subentryHeaderLength = reader.ReadInt16();
            short subentrySectionCount = reader.ReadInt16();
            Name = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
            Utils.ReadPadding(reader, 4);

            // Read transformation commands
            Commands = new();
            for (ushort commandNum = 0; commandNum < subentrySectionCount; ++commandNum)
            {
                TransformationCommand command;
                command.Opcode = reader.ReadInt16();
                short commandDataLength = reader.ReadInt16();
                command.Data = reader.ReadBytes(commandDataLength);

                Commands.Add(command);
            }
        }
    }

    public class TransformationEntry : Entry
    {
        public List<TransformationSubentry> Subentries { get; private set; }

        public TransformationEntry()
        {
            Subentries = new();
        }

        public TransformationEntry(BinaryReader reader, int expectedEntryID)
        {
            int entryID = reader.ReadInt32();
            Debug.Assert(entryID == expectedEntryID);

            int entryLength = reader.ReadInt32();
            Unknown1 = reader.ReadInt16();

            short subentryCount = reader.ReadInt16();
            int hasSubentries = reader.ReadInt32();

            // Read subentries
            Subentries = new();
            for (int sub = 0; sub < subentryCount; ++sub)
            {
                TransformationSubentry subentry = new(reader);
                Subentries.Add(subentry);
            }
        }
    }
}
