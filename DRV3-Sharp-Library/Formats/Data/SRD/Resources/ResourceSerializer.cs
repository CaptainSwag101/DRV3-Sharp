using System;
using System.IO;
using DRV3_Sharp_Library.Formats.Data.SRD.Blocks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace DRV3_Sharp_Library.Formats.Data.SRD.Resources;

internal sealed class ResourceSerializer
{
    public static TextureResource DeserializeTexture(TxrBlock txr)
    {
        // The RSI sub-block is critical because it contains the raw image data.
        if (txr.SubBlocks[0] is not RsiBlock rsi)
            throw new InvalidDataException("A TXR block within the SRD file did not have its expected RSI sub-block.");

        // We plan to save all images as PNG for the time being. This may change, to match the original file format.
        Configuration config = new(new PngConfigurationModule());
        Image<Rgba32> outputImage = new(config, txr.Width, txr.Height);

        return new TextureResource(outputImage);
    }
}