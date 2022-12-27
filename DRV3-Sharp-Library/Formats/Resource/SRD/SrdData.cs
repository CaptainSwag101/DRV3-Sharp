using System.Collections.Generic;
using DRV3_Sharp_Library.Formats.Resource.SRD.Blocks;

namespace DRV3_Sharp_Library.Formats.Resource.SRD;

public sealed record SrdData(List<ISrdBlock> Blocks) : IDanganV3Data;