using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DRV3_Sharp_Library.Formats.Data.SRD.Resources;

public sealed record TextureResource(
        string Name,
        List<Image<Rgba32>> ImageMipmaps
    ) : ISrdResource;

public interface ISrdResource
{ }