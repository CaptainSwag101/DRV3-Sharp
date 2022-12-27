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

public sealed record RsiBlock(
    byte Unknown00, byte Unknown01, sbyte Unknown02, short Unknown06,
    List<(string Name, byte[] Data, int UnknownValue)> LocalResourceData,
    List<(ResourceDataLocation Location, byte[] Data, int UnknownValue1, int UnknownValue2)> ExternalResourceData,
    List<int> UnknownIntList,
    List<string> ResourceStrings,
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