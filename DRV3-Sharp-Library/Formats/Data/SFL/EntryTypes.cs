using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Data.SFL;

public sealed record UnknownDataEntry(ushort Unknown, ushort SequenceCount, byte[] Data);

public sealed record IntegerDataEntry(ushort Unknown, int[] Values);

public sealed record ShortDataEntry(ushort Unknown, short[] Values);

public sealed record TransformOperation(ushort Opcode, byte[] Data);
public sealed record TransformSequence(string Name, List<TransformOperation> Operations);
public sealed record TransformationEntry(ushort Unknown, List<TransformSequence> Sequences);