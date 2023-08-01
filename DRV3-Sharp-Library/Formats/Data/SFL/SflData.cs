using System.Collections.Generic;
using DRV3_Sharp_Library.Formats.Data.SFL.Entries;

namespace DRV3_Sharp_Library.Formats.Data.SFL;

public sealed record Table(ushort Unknown1, uint Unknown2, Dictionary<uint, ISflEntry> Entries);

public sealed record SflData(decimal Version, Dictionary<uint, Table> Tables) : IDanganV3Data;