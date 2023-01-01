namespace DRV3_Sharp_Library.Formats.Text.STX;

public sealed record StxData(StringTable[] Tables) : IDanganV3Data;

public sealed record StringTable(int UnknownData, string[] Strings);