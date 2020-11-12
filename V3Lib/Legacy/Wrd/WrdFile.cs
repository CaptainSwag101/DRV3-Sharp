using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Wrd
{
    public struct WrdCommand
    {
        public string Opcode;
        public List<string> Arguments;
    }

    public class WrdFile
    {
        public List<WrdCommand> Commands = new List<WrdCommand>();
        public List<string> InternalStrings = new List<string>();
        public bool UsesExternalStrings = false;

        public void Load(string wrdPath)
        {
            using BinaryReader reader = new BinaryReader(new FileStream(wrdPath, FileMode.Open));

            // Read counts
            ushort stringCount = reader.ReadUInt16();
            ushort labelCount = reader.ReadUInt16();
            ushort parameterCount = reader.ReadUInt16();
            ushort localBranchCount = reader.ReadUInt16();

            // ???
            uint unknown1 = reader.ReadUInt32();

            // Read pointers
            uint localBranchDataPtr = reader.ReadUInt32();
            uint labelOffsetsPtr = reader.ReadUInt32();
            uint labelNamesPtr = reader.ReadUInt32();
            uint parametersPtr = reader.ReadUInt32();
            uint stringsPtr = reader.ReadUInt32();

            // Read label names
            List<string> labelNames = new List<string>();
            reader.BaseStream.Seek(labelNamesPtr, SeekOrigin.Begin);
            for (ushort i = 0; i < labelCount; ++i)
            {
                string labelName = reader.ReadString();
                reader.ReadByte();  // Null terminator
                labelNames.Add(labelName);
            }

            // Read plaintext parameters
            List<string> parameters = new List<string>();
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
                using BinaryReader stringReader = new BinaryReader(reader.BaseStream, Encoding.Unicode, true);
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
                    continue;
                }

                byte op = reader.ReadByte();
                string opcodeName = WrdCommandHelper.OpcodeNames[op];

                // We need 2 bytes per argument
                int argNumber = 0;
                List<string> args = new List<string>();
                while (reader.BaseStream.Position + 1 < localBranchDataPtr)
                {
                    byte b1 = reader.ReadByte();
                    if (b1 == 0x70)
                    {
                        reader.BaseStream.Seek(-1, SeekOrigin.Current);
                        break;
                    }

                    byte b2 = reader.ReadByte();
                    ushort arg = BitConverter.ToUInt16(new byte[] { b2, b1 });

                    byte argType = WrdCommandHelper.ArgTypeLists[op][argNumber % WrdCommandHelper.ArgTypeLists[op].Count];
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

                    ++argNumber;
                }

                Commands.Add(new WrdCommand { Opcode = opcodeName, Arguments = args });
            }
        }

        public void Save(string wrdPath)
        {
            // Compile commands to raw bytecode in a separate array,
            // then iterate through it to get the offset addresses.
            using MemoryStream commandData = new MemoryStream();
            using BinaryWriter commandWriter = new BinaryWriter(commandData);
            List<ushort> labelOffsets = new List<ushort>();
            List<(ushort ID, ushort Offset)> localBranchData = new List<(ushort ID, ushort Offset)>();
            List<string> labelNames = new List<string>();
            List<string> parameters = new List<string>();
            ushort stringCount = 0;
            
            foreach (WrdCommand command in Commands)
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
                        stringCount = Math.Max(ushort.Parse(command.Arguments[0]), stringCount);
                        break;

                    case "LBN":
                        // Save the branch number AND the offset
                        localBranchData.Add((ushort.Parse(command.Arguments[0]), (ushort)commandData.Length));
                        break;
                }

                // Next, encode the opcode into commandData
                commandWriter.Write((byte)0x70);
                byte opcodeId = (byte)Array.IndexOf(WrdCommandHelper.OpcodeNames, command.Opcode);
                commandWriter.Write(opcodeId);

                // Then, iterate through each argument and process/save it according to its type
                for (int argNum = 0; argNum < command.Arguments.Count; ++argNum)
                {
                    switch (WrdCommandHelper.ArgTypeLists[opcodeId][argNum])
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
            commandWriter.Flush();

            // Finally, save the raw data to the file
            using BinaryWriter writer = new BinaryWriter(new FileStream(wrdPath, FileMode.Create));

            // Write counts
            writer.Write(BitConverter.GetBytes(stringCount));
            writer.Write(BitConverter.GetBytes((ushort)labelNames.Count));
            writer.Write(BitConverter.GetBytes((ushort)parameters.Count));
            writer.Write(BitConverter.GetBytes((ushort)localBranchData.Count));

            // Write 4 bytes of padding?
            writer.Write(BitConverter.GetBytes((uint)0));

            // Write empty data here, to be filled with offset information later
            writer.Write(BitConverter.GetBytes((uint)0));   // local branch offsets pointer
            writer.Write(BitConverter.GetBytes((uint)0));   // label offsets pointer
            writer.Write(BitConverter.GetBytes((uint)0));   // label names pointer
            writer.Write(BitConverter.GetBytes((uint)0));   // parameters pointer
            writer.Write(BitConverter.GetBytes((uint)0));   // strings pointer

            // Write command data
            writer.Write(commandData.ToArray());

            // Write local branch offsets pointer & data
            uint localBranchOffsetsPtr = (uint)writer.BaseStream.Position;
            writer.BaseStream.Seek(0x0C, SeekOrigin.Begin);
            writer.Write(localBranchOffsetsPtr);
            writer.BaseStream.Seek(0, SeekOrigin.End);
            foreach (var (ID, Offset) in localBranchData)
            {
                writer.Write(BitConverter.GetBytes(ID));        // branch number
                writer.Write(BitConverter.GetBytes(Offset));    // branch offset
            }

            // Write label offsets pointer & data
            uint labelOffsetsPtr = (uint)writer.BaseStream.Position;
            writer.BaseStream.Seek(0x10, SeekOrigin.Begin);
            writer.Write(labelOffsetsPtr);
            writer.BaseStream.Seek(0, SeekOrigin.End);
            foreach (ushort offset in labelOffsets)
            {
                writer.Write(BitConverter.GetBytes(offset));
            }

            // Write label names pointer & data
            uint labelNamesPtr = (uint)writer.BaseStream.Position;
            writer.BaseStream.Seek(0x14, SeekOrigin.Begin);
            writer.Write(labelNamesPtr);
            writer.BaseStream.Seek(0, SeekOrigin.End);
            foreach (string labelName in labelNames)
            {
                writer.Write((byte)labelName.Length);               // string length
                writer.Write(Encoding.ASCII.GetBytes(labelName));   // string
                writer.Write((byte)0);                              // null terminator
            }

            // Write plaintext parameters pointer & data
            uint parametersPtr = (uint)writer.BaseStream.Position;
            writer.BaseStream.Seek(0x18, SeekOrigin.Begin);
            writer.Write(parametersPtr);
            writer.BaseStream.Seek(0, SeekOrigin.End);
            foreach (string parameter in parameters)
            {
                writer.Write((byte)parameter.Length);               // string length
                writer.Write(Encoding.ASCII.GetBytes(parameter));   // string
                writer.Write((byte)0);                              // null terminator
            }

            // Write internal dialogue strings, if any
            if (!UsesExternalStrings && stringCount > 0)
            {
                uint stringsPtr = (uint)writer.BaseStream.Position;
                writer.BaseStream.Seek(0x1C, SeekOrigin.Begin);
                writer.Write(stringsPtr);
                writer.BaseStream.Seek(0, SeekOrigin.End);
                using BinaryWriter stringWriter = new BinaryWriter(writer.BaseStream, Encoding.Unicode, true);
                foreach (string str in InternalStrings)
                {
                    stringWriter.Write(str);
                    stringWriter.Write((ushort)0);  // null terminator
                }
            }

            writer.Flush(); // Just in case
        }
    }
}
