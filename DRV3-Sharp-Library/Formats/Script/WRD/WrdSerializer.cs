using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DRV3_Sharp_Library.Formats.Script.WRD;

public static class WrdSerializer
{
    private const uint WRD_COMMAND_PTR = 0x20;
    
    public static void Deserialize(Stream inputStream, out WrdData outputData)
    {
        using BinaryReader reader = new(inputStream, Encoding.ASCII, true);
        
        // Read header.
        ushort stringCount = reader.ReadUInt16();
        ushort labelCount = reader.ReadUInt16();
        ushort parameterCount = reader.ReadUInt16();
        ushort localBranchCount = reader.ReadUInt16();
        
        // Unknown what this is.
        uint unknown1 = reader.ReadUInt32();
        
        // Read pointers
        uint localBranchDataPtr = reader.ReadUInt32();
        uint labelOffsetsPtr = reader.ReadUInt32();
        uint labelNamesPtr = reader.ReadUInt32();
        uint parametersPtr = reader.ReadUInt32();
        uint stringsPtr = reader.ReadUInt32();
        
        // Read label names.
        reader.BaseStream.Seek(labelNamesPtr, SeekOrigin.Begin);
        List<string> labelNames = new();
        for (int i = 0; i < labelCount; ++i)
        {
            labelNames.Add(reader.ReadString());
            _ = reader.ReadByte();  // Skip the null terminator
        }
        
        // Read plaintext parameters.
        reader.BaseStream.Seek(parametersPtr, SeekOrigin.Begin);
        List<string> parameters = new();
        for (int i = 0; i < parameterCount; ++i)
        {
            parameters.Add(reader.ReadString());
            _ = reader.ReadByte();  // Skip null terminator
        }
        
        // Read internal dialogue strings, if any.
        List<string>? internalStrings = null;
        if (stringsPtr != 0)
        {
            reader.BaseStream.Seek(stringsPtr, SeekOrigin.Begin);
            using BinaryReader stringReader = new(reader.BaseStream, Encoding.Unicode, true);
            internalStrings = new();
            for (int i = 0; i < stringCount; ++i)
            {
                internalStrings.Add(stringReader.ReadString());
                _ = stringReader.ReadChar();    // Skip null terminator (Unicode)
            }
        }
        
        // Now that we've loaded all the plaintext, convert the opcodes
        // and arguments into their proper string representations.
        reader.BaseStream.Seek(WRD_COMMAND_PTR, SeekOrigin.Begin);
        List<WrdCommand> commands = new();
        while ((reader.BaseStream.Position + 1) < localBranchDataPtr)
        {
            // Each opcode starts with 0x70, skip over it or throw if we don't see it when we should.
            byte b = reader.ReadByte();
            if (b != 0x70) throw new InvalidDataException("The provided WRD command data did not start with hex 0x70.");

            byte op = reader.ReadByte();
            var info = WrdCommandConstants.CommandInfo[op];
            string opName = info.Name;  // Convert opcode name to string properly using internal names
            
            // Read arguments, two bytes at a time.
            List<string> args = new();
            var argNum = 0;
            while ((reader.BaseStream.Position + 1) < localBranchDataPtr)
            {
                ushort data = BinaryPrimitives.ReverseEndianness(reader.ReadUInt16());
                // If the data contains hex 0x70 in the most significant byte (big-endian), it is the next opcode.
                if ((data & 0xFF00) == 0x7000)
                {
                    // Backtrack two bytes and then break out so those bytes can
                    // be interpreted as an opcode.
                    reader.BaseStream.Seek(-2, SeekOrigin.Current);
                    break;
                }
                
                // Parse the argument type based on the current opcode.
                if (info.ArgTypes is null)
                {
                    // Forcibly interpret the argument as a plaintext parameter, to help determine its purpose/validity.
                    args.Add(parameters[data]);
                    continue;
                }
                
                string parsedArg = info.ArgTypes[argNum % info.ArgTypes.Length] switch
                {
                    0 => parameters[data],  // Plaintext parameter
                    1 => data.ToString(),   // Raw number
                    2 => data.ToString(),   // Dialogue string
                    3 => labelNames[data],  // Label name
                    _ => parameters[data]
                };
                args.Add(parsedArg);

                ++argNum;
            }

            // If we're trying to parse args for an opcode that shouldn't have any, alert the user.
            if (info.ArgTypes is null)
            {
                if (args.Count > 0)
                {
                    Console.WriteLine($"Found arguments for opcode {opName} which should not have any.\nThis may indicate a bug in the software, or in the script file.");
                    Console.WriteLine("Press ENTER to continue...");
                    Console.ReadLine();
                }
            }
            // If we parsed more args than expected, and the opcode doesn't support
            // variable argument counts, alert the user.
            // Also alert in the event that we read fewer than the minimum expected arg count,
            // regardless of whether the opcode supports variable arg counts.
            else if ((args.Count > info.ArgTypes.Length && !info.VariableArgCount)
                || args.Count < info.ArgTypes.Length)
            {
                Console.WriteLine($"Parsed {args.Count} for opcode {opName} but expected {info.ArgTypes.Length}.");
                Console.WriteLine("Press ENTER to continue...");
                Console.ReadLine();
            }
            
            commands.Add(new(opName, args));
        }
        
        // Finally, construct the output data.
        outputData = new(commands, internalStrings);
    }
}