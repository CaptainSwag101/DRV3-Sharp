using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Archive.SPC;

public sealed record ArchivedFile(string Name, byte[] Data, short UnknownFlag, bool IsCompressed, int OriginalSize);

public sealed record SpcData(int Unknown2C, List<ArchivedFile> Files) : IDanganV3Data;