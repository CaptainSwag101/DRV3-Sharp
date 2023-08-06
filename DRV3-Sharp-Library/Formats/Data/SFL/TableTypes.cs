using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Data.SFL;

public sealed record GenericTable(Dictionary<uint, UnknownDataEntry> Entries);

public sealed record IntegerTable(Dictionary<uint, IntegerDataEntry> Entries);
public sealed record ShortTable(Dictionary<uint, ShortDataEntry> Entries);

public sealed record TransformationTable(Dictionary<uint, TransformationEntry> Entries);

public sealed record PositionTable(Dictionary<uint, ImagePositionEntry> Entries);
