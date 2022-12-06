using System.Collections.Generic;
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
            string opName = op.ToString();  // TODO: Convert opcode name to string properly using internal names
            
            // Read arguments, two bytes at a time.
            List<string> args = new();
            while ((reader.BaseStream.Position + 1) < localBranchDataPtr)
            {
                ushort data = reader.ReadUInt16();
                // If the data contains hex 0x70 in the first byte, it is the next opcode.
                if ((data & 0x00FF) == 0x0070)
                {
                    // Backtrack two bytes and then break out so those bytes can
                    // be interpreted as an opcode.
                    reader.BaseStream.Seek(-2, SeekOrigin.Current);
                    break;
                }
                
                // TODO: Parse the argument type based on the current opcode.
                args.Add(data.ToString());
            }
            
            commands.Add(new(opName, args));
        }
        
        // Finally, construct the output data.
        outputData = new(commands, internalStrings);
    }
}