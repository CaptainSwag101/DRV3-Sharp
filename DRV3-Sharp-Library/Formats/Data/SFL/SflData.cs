using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Data.SFL;

public sealed record SflData(
    decimal Version, 
    IntegerTable ImageIdTable,
    ShortTable ImageResolutionTable,
    PositionTable ImagePositionTable,
    Dictionary<uint, GenericTable> UnknownTables,
    Dictionary<uint, TransformationTable> TransformationTables
    );
