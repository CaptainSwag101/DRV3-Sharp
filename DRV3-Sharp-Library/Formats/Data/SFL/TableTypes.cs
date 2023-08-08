using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Data.SFL;

public sealed record GenericTable(uint Id, List<UnknownDataEntry> Entries);

public sealed record IntegerTable(uint Id, List<IntegerDataEntry> Entries);

public sealed record ShortTable(uint Id, List<ShortDataEntry> Entries);

public sealed record TransformationTable(uint Id, List<TransformationEntry> Entries);

public sealed record PositionTable(uint Id, List<ImagePositionEntry> Entries);
