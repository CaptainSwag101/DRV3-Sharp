using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Data.SFL;

public sealed record GenericTable(ushort Unknown1, uint Unknown2, Dictionary<uint, UnknownDataEntry> Entries);

public sealed record IntegerTable(ushort Unknown1, uint Unknown2, Dictionary<uint, IntegerDataEntry> Entries);
public sealed record ShortTable(ushort Unknown1, uint Unknown2, Dictionary<uint, ShortDataEntry> Entries);

public sealed record TransformationTable(ushort Unknown1, uint Unknown2, Dictionary<uint, TransformationEntry> Entries);

public sealed record ImagePositionTable(ushort Unknown1, uint Unknown2, Dictionary<uint, ImagePositionEntry> Entries);
