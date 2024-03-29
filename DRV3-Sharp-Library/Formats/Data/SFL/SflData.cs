﻿using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Data.SFL;

public sealed record SflData(
    decimal Version, 
    SortedDictionary<uint, List<Entry>> Entries
    );
