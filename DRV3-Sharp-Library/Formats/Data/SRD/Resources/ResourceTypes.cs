using System.Collections.Generic;
using DRV3_Sharp_Library.Formats.Data.SRD.Blocks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DRV3_Sharp_Library.Formats.Data.SRD.Resources;

public sealed record UnknownResource(
        ISrdBlock UnderlyingBlock)
    : ISrdResource;

public sealed record TextureResource(
        string Name,
        List<Image<Rgba32>> ImageMipmaps)
    : ISrdResource;

public interface ISrdResource
{ }