using System.Collections.Generic;
using DRV3_Sharp_Library.Formats.Data.SFL.Entries;

namespace DRV3_Sharp_Library.Formats.Data.SFL;

public sealed record DataTable(ushort Unknown1, uint Unknown2, Dictionary<uint, DataEntry> Entries);
public sealed record TransformationTable(ushort Unknown1, uint Unknown2, Dictionary<uint, TransformationEntry> Entries);

public sealed record SflData(decimal Version, Dictionary<uint, DataTable> DataTables, Dictionary<uint, TransformationTable> TransformationTables) : IDanganV3Data;