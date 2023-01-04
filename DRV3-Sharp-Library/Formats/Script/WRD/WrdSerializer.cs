using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
        uint unknown = reader.ReadUInt32();
        
        // Read pointers
        uint localBranchOffsetsPtr = reader.ReadUInt32();
        uint labelOffsetsPtr = reader.ReadUInt32();
        uint labelNamesPtr = reader.ReadUInt32();
        uint parametersPtr = reader.ReadUInt32();
        uint stringsPtr = reader.ReadUInt32();
        
        // Read label names.
        inputStream.Seek(labelNamesPtr, SeekOrigin.Begin);
        List<string> labelNames = new();
        for (var i = 0; i < labelCount; ++i)
        {
            labelNames.Add(reader.ReadString());
            _ = reader.ReadByte();  // Skip the null terminator
        }
        
        // Read plaintext parameters.
        inputStream.Seek(parametersPtr, SeekOrigin.Begin);
        List<string> parameters = new();
        for (var i = 0; i < parameterCount; ++i)
        {
            parameters.Add(reader.ReadString());
            _ = reader.ReadByte();  // Skip null terminator
        }
        
        // Read local branch data
        inputStream.Seek(localBranchOffsetsPtr, SeekOrigin.Begin);
        List<(ushort Index, ushort Offset)> localBranchData = new();
        for (var i = 0; i < localBranchCount; ++i)
        {
            localBranchData.Add((reader.ReadUInt16(), reader.ReadUInt16()));
        }
        
        // Read internal dialogue strings, if any.
        List<string>? internalStrings = null;
        if (stringsPtr != 0)
        {
            inputStream.Seek(stringsPtr, SeekOrigin.Begin);
            using BinaryReader stringReader = new(inputStream, Encoding.Unicode, true);
            internalStrings = new();
            for (int i = 0; i < stringCount; ++i)
            {
                internalStrings.Add(stringReader.ReadString());
                _ = stringReader.ReadChar();    // Skip null terminator (Unicode)
            }
        }
        
        // Now that we've loaded all the plaintext, convert the opcodes
        // and arguments into their proper string representations.
        inputStream.Seek(WRD_COMMAND_PTR, SeekOrigin.Begin);
        List<WrdCommand> commands = new();
        while ((inputStream.Position + 1) < localBranchOffsetsPtr)
        {
            // Each opcode starts with 0x70, skip over it or throw if we don't see it when we should.
            byte b = reader.ReadByte();
            if (b != 0x70) throw new InvalidDataException("The provided WRD command data did not start with hex 0x70.");

            byte op = reader.ReadByte();
            var info = WrdCommandConstants.CommandInfo.Values.ToImmutableArray()[op];
            string opName = WrdCommandConstants.CommandInfo.Keys.ToImmutableArray()[op];
            
            // Read arguments, two bytes at a time.
            List<ushort> args = new();
            while ((inputStream.Position + 1) < localBranchOffsetsPtr)
            {
                ushort data = BinaryPrimitives.ReverseEndianness(reader.ReadUInt16());
                // If the data contains hex 0x70 in the most significant byte (big-endian), it is the next opcode.
                if ((data & 0xFF00) == 0x7000)
                {
                    // Backtrack two bytes and then break out so those bytes can
                    // be interpreted as an opcode.
                    inputStream.Seek(-2, SeekOrigin.Current);
                    break;
                }
                
                args.Add(data);
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
        outputData = new WrdData(commands, unknown, parameters, labelNames, internalStrings);
    }

    public static void Serialize(WrdData inputData, ushort stringCount, MemoryStream outputStream)
    {
        using BinaryWriter writer = new(outputStream, Encoding.ASCII, true);
        
        // Compute necessary counts, etc.
        ushort localBranchCount = 0;
        foreach (var command in inputData.Commands)
        {
            var info = WrdCommandConstants.CommandInfo[command.Name];
            if (info is null) throw new InvalidDataException($"The opcode {command.Name} is invalid.");

            if (command.Name == "LBN") ++localBranchCount;
        }
        
        // Write header
        writer.Write(stringCount);
        writer.Write((ushort)inputData.Labels.Count);
        writer.Write((ushort)inputData.Parameters.Count);
        writer.Write(localBranchCount);
        writer.Write(inputData.Unknown);
        
        // Remember current position so we can return and write the pointers later.
        var pointersWriteLocation = outputStream.Position;
        
        // Write placeholder null pointers.
        writer.Write((int)0);
        writer.Write((int)0);
        writer.Write((int)0);
        writer.Write((int)0);
        writer.Write((int)0);
        
        // Write label names
        var labelNamePtr = outputStream.Position;
        foreach (string lblName in inputData.Labels)
        {
            writer.Write(lblName);
            writer.Write((byte)0);  // Null terminator
        }
        
    }
}