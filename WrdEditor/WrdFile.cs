using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WrdEditor
{
    class WrdFile
    {
        public List<(string Opcode, List<string> Arguments)> Commands = new List<(string Opcode, List<string> Arguments)>();    // Opcode, Arguments[]

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
                reader.ReadByte();
                labelNames.Add(labelName);
            }

            // Read plaintext parameters
            List<string> parameters = new List<string>();
            reader.BaseStream.Seek(parametersPtr, SeekOrigin.Begin);
            for (ushort i = 0; i < parameterCount; ++i)
            {
                string parameterName = reader.ReadString();
                reader.ReadByte();
                parameters.Add(parameterName);
            }

            // Read internal dialogue strings
            // (not really sure how to do this properly since most scripts store strings externally)
            /*
            List<string> strings = new List<string>();
            if (stringsPtr != 0)
            {
                reader.BaseStream.Seek(stringsPtr, SeekOrigin.Begin);
                for (ushort i = 0; i < stringCount; ++i)
                {
                    string str = reader.ReadString();
                    reader.ReadByte();
                    strings.Add(str);
                }
            }
            */

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

                Commands.Add((opcodeName, args));
            }
        }

        public void Save(string wrdPath)
        {
            // Compile commands to raw bytecode in a separate array,
            // then iterate through it to get the offset addresses.
            List<byte> commandData = new List<byte>();
            List<ushort> labelOffsets = new List<ushort>();
            List<(ushort ID, ushort Offset)> localBranchData = new List<(ushort ID, ushort Offset)>();
            List<string> labelNames = new List<string>();
            List<string> parameters = new List<string>();
            ushort stringCount = 0;
            
            foreach (var tuple in Commands)
            {
                // First, check the opcode to see if there's any additional processing we need to do
                switch (tuple.Opcode)
                {
                    case "LAB":
                        labelOffsets.Add((ushort)commandData.Count);
                        labelNames.Add(tuple.Arguments[0]);
                        break;

                    case "LOC":
                        ++stringCount;
                        break;

                    case "LBN":
                        // Save the branch number AND the offset
                        localBranchData.Add((ushort.Parse(tuple.Arguments[0]), (ushort)commandData.Count));
                        break;
                }

                // Next, encode the opcode into commandData
                commandData.Add(0x70);
                byte opcodeId = (byte)Array.IndexOf(WrdCommandHelper.OpcodeNames, tuple.Opcode);
                commandData.Add(opcodeId);

                // Then, iterate through each argument and process/save it according to its type
                for (int argNum = 0; argNum < tuple.Arguments.Count; ++argNum)
                {
                    switch (WrdCommandHelper.ArgTypeLists[opcodeId][argNum])
                    {
                        case 0: // Plaintext parameter
                            {
                                int found = parameters.IndexOf(tuple.Arguments[argNum]);
                                if (found == -1)
                                {
                                    found = parameters.Count;
                                    parameters.Add(tuple.Arguments[argNum]);
                                }

                                byte[] encodedArg = BitConverter.GetBytes((ushort)found);
                                Array.Reverse(encodedArg);  // Switch to big-endian
                                commandData.AddRange(encodedArg);
                                break;
                            }

                        case 1: // Raw number
                        case 2: // Dialogue string
                            {
                                byte[] encodedArg = BitConverter.GetBytes(ushort.Parse(tuple.Arguments[argNum]));
                                Array.Reverse(encodedArg);  // Switch to big-endian
                                commandData.AddRange(encodedArg);
                                break;
                            }

                        case 3: // Label
                            {
                                // Note: we can probably simplify this since we already added the label name just above
                                int found = labelNames.IndexOf(tuple.Arguments[argNum]);
                                byte[] encodedArg = BitConverter.GetBytes((ushort)found);
                                Array.Reverse(encodedArg);  // Switch to big-endian
                                commandData.AddRange(encodedArg);
                                break;
                            }
                    }
                }
            }

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
            foreach (var localBranch in localBranchData)
            {
                writer.Write(BitConverter.GetBytes(localBranch.ID));        // branch number
                writer.Write(BitConverter.GetBytes(localBranch.Offset));    // branch offset
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
                writer.Write((byte)labelName.Length);                   // string length
                writer.Write(new ASCIIEncoding().GetBytes(labelName));  // string
                writer.Write((byte)0);                                  // null terminator
            }

            // Write plaintext parameters pointer & data
            uint parametersPtr = (uint)writer.BaseStream.Position;
            writer.BaseStream.Seek(0x18, SeekOrigin.Begin);
            writer.Write(parametersPtr);
            writer.BaseStream.Seek(0, SeekOrigin.End);
            foreach (string parameter in parameters)
            {
                writer.Write((byte)parameter.Length);                   // string length
                writer.Write(new ASCIIEncoding().GetBytes(parameter));  // string
                writer.Write((byte)0);                                  // null terminator
            }

            writer.Flush(); // Just in case
            writer.Close();
        }
    }
}
