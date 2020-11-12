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

namespace V3Lib.Script.WRD
{
    /// <summary>
    /// The proprietary scripting format for Danganronpa V3.
    /// Controls a significant amount of the game engine via various
    /// commands and their parameters. Can also contain dialogue strings,
    /// or reference an external file to contain them.
    /// </summary>
    public class WRDScript
    {
        #region Public Properties
        public List<ScriptCommand> Commands { get; private set; }
        public List<string> InternalStrings { get; private set; }
        public bool UsesExternalStrings { get; set; }
        #endregion

        #region Public Methods

        #region Constructors
        /// <summary>
        /// Creates a new empty WRD script with no commands or strings.
        /// </summary>
        public WRDScript()
        {
            // Initialize properties to default
            Commands = new();
            InternalStrings = new();
            UsesExternalStrings = true;
        }

        /// <summary>
        /// Reads a WRD script from a data stream.
        /// </summary>
        /// <param name="stream">The stream to read data from.</param>
        /// <exception cref="EndOfStreamException">Occurs when the end of the data stream is reached before the script has been fully read.</exception>
        /// <exception cref="InvalidDataException">Occurs when the file you're trying to read does not conform to the WRD specification, and is likely invalid.</exception>
        public WRDScript(Stream stream)
        {
            // Initialize properties to default
            Commands = new();
            InternalStrings = new();
            UsesExternalStrings = true;

            // Set up a BinaryReader to help us deserialize the data from stream
            using BinaryReader reader = new(stream);

            // Read all stream data inside a "try" block to catch exceptions
            try
            {
                // Read counts
                short stringCount = reader.ReadInt16();
                if (stringCount < 0)
                {
                    throw new InvalidDataException($"The script appears to have {stringCount} strings, which is invalid.");
                }
                short labelCount = reader.ReadInt16();
                if (labelCount < 0)
                {
                    throw new InvalidDataException($"The script appears to have {labelCount} labels, which is invalid.");
                }
                short parameterCount = reader.ReadInt16();
                if (parameterCount < 0)
                {
                    throw new InvalidDataException($"The script appears to have {parameterCount} parameters, which is invalid.");
                }
                short localBranchCount = reader.ReadInt16();
                if (localBranchCount < 0)
                {
                    throw new InvalidDataException($"The script appears to have {localBranchCount} local branches, which is invalid.");
                }

                // Read unknown value (padding?)
                int unknown1 = reader.ReadInt32();

                // Read pointers
                int localBranchDataPtr = reader.ReadInt32();
                int labelOffsetsPtr = reader.ReadInt32();
                int labelNamesPtr = reader.ReadInt32();
                int parametersPtr = reader.ReadInt32();
                int stringsPtr = reader.ReadInt32();

                // Read label names
                List<string> labelNames = new();
                reader.BaseStream.Seek(labelNamesPtr, SeekOrigin.Begin);
                for (ushort i = 0; i < labelCount; ++i)
                {
                    string labelName = reader.ReadString();
                    reader.ReadByte();  // Null terminator
                    labelNames.Add(labelName);
                }

                // Read plaintext parameters
                List<string> parameters = new();
                reader.BaseStream.Seek(parametersPtr, SeekOrigin.Begin);
                for (ushort i = 0; i < parameterCount; ++i)
                {
                    string parameterName = reader.ReadString();
                    reader.ReadByte();  // Null terminator
                    parameters.Add(parameterName);
                }

                // Read internal dialogue strings, if any
                if (stringsPtr != 0)
                {
                    using BinaryReader stringReader = new(reader.BaseStream, Encoding.Unicode, true);
                    stringReader.BaseStream.Seek(stringsPtr, SeekOrigin.Begin);
                    for (ushort i = 0; i < stringCount; ++i)
                    {
                        string str = stringReader.ReadString();
                        stringReader.ReadBytes(2);  // Null terminator
                        InternalStrings.Add(str);
                    }
                }

                // If no strings were loaded but the WRD indicates there should be some, it means they're external
                if (InternalStrings.Count == 0 && stringCount > 0)
                {
                    UsesExternalStrings = true;
                }

                // Now that we've loaded all the plaintext,
                // convert the opcodes and arguments into their proper string representations
                reader.BaseStream.Seek(0x20, SeekOrigin.Begin);
                while (reader.BaseStream.Position + 1 < localBranchDataPtr)
                {
                    byte b = reader.ReadByte();
                    if (b != 0x70)
                    {
                        throw new InvalidDataException($"Encountered a non-opcode byte at {reader.BaseStream.Position} outside of argument data!");
                    }

                    byte op = reader.ReadByte();
                    string opcodeName = ScriptCommandHelper.OpcodeNames[op];

                    // We need 2 bytes per argument
                    List<string> args = new();
                    while (reader.BaseStream.Position + 1 < localBranchDataPtr)
                    {
                        // If the first byte starts with 0x70, it's a new opcode, and we need to break out of this loop.
                        if ((byte)reader.PeekChar() == 0x70)
                        {
                            break;
                        }

                        // If it's not a new opcode, it's an argument.
                        ushort arg = reader.ReadUInt16();

                        // Modulus the argument number by the argType count, to handle arbitrary argument counts supported by some opcodes
                        byte argType = ScriptCommandHelper.ArgTypeLists[op][args.Count % ScriptCommandHelper.ArgTypeLists[op].Count];
                        switch (argType)
                        {
                            case 0: // Plaintext parameter
                                args.Add(parameters[arg]);
                                break;

                            case 1: // Raw number
                            case 2: // Dialogue string
                                args.Add(arg.ToString());
                                break;

                            case 3: // Label name
                                args.Add(labelNames[arg]);
                                break;
                        }
                    }

                    Commands.Add(new ScriptCommand { Opcode = opcodeName, Arguments = args });
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

        /// <summary>
        /// Serializes the whole WRD script to a byte array, ready to be written to a file or stream.
        /// </summary>
        /// <returns>A byte array containing the full script file.</returns>
        public byte[] GetBytes()
        {
            // Compile commands to raw bytecode in a separate array,
            // then iterate through it to get the offset addresses.
            using MemoryStream commandData = new();
            using BinaryWriter commandWriter = new(commandData);

            List<ushort> labelOffsets = new();
            List<(ushort ID, ushort Offset)> localBranchData = new();
            List<string> labelNames = new();
            List<string> parameters = new();
            ushort stringCount = 0;

            foreach (ScriptCommand command in Commands)
            {
                // First, check the opcode to see if there's any additional processing we need to do
                switch (command.Opcode)
                {
                    case "LAB":
                        labelOffsets.Add((ushort)commandData.Length);
                        labelNames.Add(command.Arguments[0]);
                        break;

                    case "LOC":
                        // The highest-numbered string index the script references must be
                        // the total number of strings the script contains.
                        // Therefore, take the larger of the current count or current string reference,
                        // and set it to be the new count.
                        stringCount = Math.Max(ushort.Parse(command.Arguments[0]), stringCount);
                        break;

                    case "LBN":
                        // Save the branch number AND the offset
                        localBranchData.Add((ushort.Parse(command.Arguments[0]), (ushort)commandData.Length));
                        break;
                }

                // Next, encode the opcode into commandData
                commandWriter.Write((byte)0x70);
                byte opcodeId = (byte)Array.IndexOf(ScriptCommandHelper.OpcodeNames, command.Opcode);
                commandWriter.Write(opcodeId);

                // Then, iterate through each argument and process/save it according to its type
                for (int argNum = 0; argNum < command.Arguments.Count; ++argNum)
                {
                    switch (ScriptCommandHelper.ArgTypeLists[opcodeId][argNum])
                    {
                        case 0: // Plaintext parameter
                            {
                                int found = parameters.IndexOf(command.Arguments[argNum]);
                                if (found == -1)
                                {
                                    found = parameters.Count;
                                    parameters.Add(command.Arguments[argNum]);
                                }

                                commandWriter.WriteBE((ushort)found);
                                break;
                            }

                        case 1: // Raw number
                        case 2: // Dialogue string
                            {
                                commandWriter.WriteBE(ushort.Parse(command.Arguments[argNum]));
                                break;
                            }

                        case 3: // Label
                            {
                                // Note: we can probably simplify this since we already added the label name just above
                                int found = labelNames.IndexOf(command.Arguments[argNum]);
                                commandWriter.WriteBE((ushort)found);
                                break;
                            }
                    }
                }
            }
            commandWriter.Flush();  // Just in case

            // Finally, save the raw data to the file
            using MemoryStream wrdData = new();
            using BinaryWriter wrdWriter = new(wrdData);

            // Write counts
            wrdWriter.Write(BitConverter.GetBytes(stringCount));
            wrdWriter.Write(BitConverter.GetBytes((ushort)labelNames.Count));
            wrdWriter.Write(BitConverter.GetBytes((ushort)parameters.Count));
            wrdWriter.Write(BitConverter.GetBytes((ushort)localBranchData.Count));

            // Write 4 bytes of padding?
            wrdWriter.Write(BitConverter.GetBytes((uint)0));

            // Write empty data here, to be filled with offset information later
            wrdWriter.Write(BitConverter.GetBytes((uint)0));   // local branch offsets pointer
            wrdWriter.Write(BitConverter.GetBytes((uint)0));   // label offsets pointer
            wrdWriter.Write(BitConverter.GetBytes((uint)0));   // label names pointer
            wrdWriter.Write(BitConverter.GetBytes((uint)0));   // parameters pointer
            wrdWriter.Write(BitConverter.GetBytes((uint)0));   // strings pointer

            // Write command data
            wrdWriter.Write(commandData.ToArray());

            // Write local branch offsets pointer & data
            uint localBranchOffsetsPtr = (uint)wrdWriter.BaseStream.Position;
            wrdWriter.BaseStream.Seek(0x0C, SeekOrigin.Begin);
            wrdWriter.Write(localBranchOffsetsPtr);
            wrdWriter.BaseStream.Seek(0, SeekOrigin.End);
            foreach (var (ID, Offset) in localBranchData)
            {
                wrdWriter.Write(BitConverter.GetBytes(ID));        // branch number
                wrdWriter.Write(BitConverter.GetBytes(Offset));    // branch offset
            }

            // Write label offsets pointer & data
            uint labelOffsetsPtr = (uint)wrdWriter.BaseStream.Position;
            wrdWriter.BaseStream.Seek(0x10, SeekOrigin.Begin);
            wrdWriter.Write(labelOffsetsPtr);
            wrdWriter.BaseStream.Seek(0, SeekOrigin.End);
            foreach (ushort offset in labelOffsets)
            {
                wrdWriter.Write(BitConverter.GetBytes(offset));
            }

            // Write label names pointer & data
            uint labelNamesPtr = (uint)wrdWriter.BaseStream.Position;
            wrdWriter.BaseStream.Seek(0x14, SeekOrigin.Begin);
            wrdWriter.Write(labelNamesPtr);
            wrdWriter.BaseStream.Seek(0, SeekOrigin.End);
            foreach (string labelName in labelNames)
            {
                wrdWriter.Write((byte)labelName.Length);               // string length
                wrdWriter.Write(Encoding.ASCII.GetBytes(labelName));   // string
                wrdWriter.Write((byte)0);                              // null terminator
            }

            // Write plaintext parameters pointer & data
            uint parametersPtr = (uint)wrdWriter.BaseStream.Position;
            wrdWriter.BaseStream.Seek(0x18, SeekOrigin.Begin);
            wrdWriter.Write(parametersPtr);
            wrdWriter.BaseStream.Seek(0, SeekOrigin.End);
            foreach (string parameter in parameters)
            {
                wrdWriter.Write((byte)parameter.Length);               // string length
                wrdWriter.Write(Encoding.ASCII.GetBytes(parameter));   // string
                wrdWriter.Write((byte)0);                              // null terminator
            }

            // Write internal dialogue strings, if any
            if (!UsesExternalStrings && stringCount > 0)
            {
                uint stringsPtr = (uint)wrdWriter.BaseStream.Position;
                wrdWriter.BaseStream.Seek(0x1C, SeekOrigin.Begin);
                wrdWriter.Write(stringsPtr);
                wrdWriter.BaseStream.Seek(0, SeekOrigin.End);
                using BinaryWriter stringWriter = new(wrdWriter.BaseStream, Encoding.Unicode, true);
                foreach (string str in InternalStrings)
                {
                    stringWriter.Write(str);
                    stringWriter.Write((ushort)0);  // null terminator
                }
            }
            wrdWriter.Flush(); // Just in case

            return wrdData.ToArray();
        }

        #endregion
    }

    public struct ScriptCommand
    {
        public string Opcode { get; set; }
        public List<string> Arguments { get; set; }
    }
}
