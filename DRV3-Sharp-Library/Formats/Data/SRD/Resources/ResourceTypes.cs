using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DRV3_Sharp_Library.Formats.Data.SRD.Resources;

public sealed record TextureResource(
        Image<Rgba32> Image
    ) : ISrdResource;

public interface ISrdResource
{ }