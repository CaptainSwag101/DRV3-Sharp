using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Resource.SRD.Blocks;

public sealed record CfhBlock(
        List<ISrdBlock> SubBlocks)
    : ISrdBlock;

public sealed record RsfBlock(
        int Unknown00, int Unknown04, int Unknown08, int Unknown0C,
        string FolderName,
        List<ISrdBlock> SubBlocks)
    : ISrdBlock;

public enum ResourceDataLocation
{
    Srdi = 0x20000000,
    Srdv = 0x40000000
}
public sealed record LocalResource(string Name, byte[] Data, int UnknownValue);
public sealed record ExternalResource(ResourceDataLocation Location, byte[] Data, int UnknownValue1, int UnknownValue2);
public sealed record RsiBlock(
        byte Unknown00, byte Unknown01, sbyte Unknown02, short Unknown06,
        List<LocalResource> LocalResources,
        List<ExternalResource> ExternalResources,
        List<int> UnknownIntList,
        List<string> ResourceStrings,
        List<ISrdBlock> SubBlocks)
    : ISrdBlock;

public enum TextureFormat
{
    Unknown     = 0x00,
    ARGB8888    = 0x01,
    BGR565      = 0x02,
    BGRA4444    = 0x05,
    DXT1RGB     = 0x0F,
    DXT5        = 0x11,
    BC5         = 0x14,
    BC4         = 0x16,
    Indexed8    = 0x1A,
    BPTC        = 0x1C
}
public sealed record TxrBlock(
        int Unknown00,
        ushort Swizzle, ushort Width, ushort Height, ushort Scanline,
        TextureFormat Format,
        byte Unknown0D,
        byte Palette, byte PaletteID,
        List<ISrdBlock> SubBlocks)
    : ISrdBlock;

public sealed record UnknownBlock(
        string BlockType,
        byte[] MainData,
        List<ISrdBlock> SubBlocks)
    : ISrdBlock;

public interface ISrdBlock
{
    public List<ISrdBlock> SubBlocks { get; }
}