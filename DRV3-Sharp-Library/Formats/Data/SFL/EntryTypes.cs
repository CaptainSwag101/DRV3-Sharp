using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Data.SFL;

public sealed record UnknownDataEntry(uint Id, ushort EventNumber, ushort DataCount, byte[] Data);

public sealed record IntegerDataEntry(uint Id, ushort EventNumber, int[] Values);

public sealed record ShortDataEntry(uint Id, ushort EventNumber, short[] Values);

public sealed record TransformOperation(ushort Opcode, byte[] Data);
public sealed record TransformSequence(string Name, List<TransformOperation> Operations);
public sealed record TransformationEntry(uint Id, ushort EventNumber, List<TransformSequence> Sequences);

public sealed record ImagePositionSubEntry(uint ImageId, byte[] Unknown, int TopLeft, int TopRight, int BottomLeft, int BottomRight);
public sealed record ImagePositionEntry(uint Id, ushort EventNumber, List<ImagePositionSubEntry> SubEntries);