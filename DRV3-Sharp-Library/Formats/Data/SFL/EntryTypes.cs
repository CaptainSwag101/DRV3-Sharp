using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Data.SFL;

public sealed record UnknownDataEntry(ushort EventNumber, ushort DataCount, byte[] Data);

public sealed record IntegerDataEntry(ushort EventNumber, int[] Values);

public sealed record ShortDataEntry(ushort EventNumber, short[] Values);

public sealed record TransformOperation(ushort Opcode, byte[] Data);
public sealed record TransformSequence(string Name, List<TransformOperation> Operations);
public sealed record TransformationEntry(ushort EventNumber, List<TransformSequence> Sequences);

public sealed record ImagePositionSubEntry(uint ImageId, byte[] Unknown, int TopLeft, int TopRight, int BottomLeft, int BottomRight);
public sealed record ImagePositionEntry(ushort EventNumber, List<ImagePositionSubEntry> SubEntries);