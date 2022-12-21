using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Resource.SRD;

public sealed record SrdData(List<ISrdBlock> Blocks) : IDanganV3Data;