using System.Collections.Generic;
using DRV3_Sharp_Library.Formats.Data.SRD.Blocks;
using DRV3_Sharp_Library.Formats.Data.SRD.Resources;

namespace DRV3_Sharp_Library.Formats.Data.SRD;

public sealed record SrdData(List<ISrdResource> Resources) : IDanganV3Data;