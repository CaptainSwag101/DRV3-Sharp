using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Data.SFL.Entries;

public sealed record DataEntry(ushort Unknown, byte[] Data);

public sealed record TransformOperation(ushort Opcode, byte[] Data);
public sealed record TransformSequence(string Name, List<TransformOperation> Operations);
public sealed record TransformationEntry(ushort Unknown, List<TransformSequence> Sequences);