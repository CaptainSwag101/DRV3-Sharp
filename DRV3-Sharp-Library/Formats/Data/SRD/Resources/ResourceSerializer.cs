using System;
using System.Collections.Generic;
using System.IO;
using DRV3_Sharp_Library.Formats.Data.SRD.Blocks;
using Scarlet.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using ScarletImage = Scarlet.Drawing.ImageBinary;

namespace DRV3_Sharp_Library.Formats.Data.SRD.Resources;

internal static class ResourceSerializer
{
    public static TextureResource DeserializeTexture(TxrBlock txr)
    {
        // The RSI sub-block is critical because it contains the raw image data.
        if (txr.SubBlocks[0] is not RsiBlock rsi)
            throw new InvalidDataException("A TXR block within the SRD file did not have its expected RSI sub-block.");
        
        string outputName = rsi.ResourceStrings[0];
        Console.WriteLine($"Processing texture {outputName}");

        // We plan to save all images as BMP for the time being. This may change, to match the original file format.
        Configuration config = new(new BmpConfigurationModule());
        
        // Separate the palette data from the list beforehand if it exists
        byte[] paletteData = Array.Empty<byte>();
        if (txr.Palette == 1)
        {
            var paletteInfo = rsi.ExternalResources[txr.PaletteID];
            rsi.ExternalResources.Remove(paletteInfo);
            paletteData = paletteInfo.Data;
        }
        
        // Read image data/mipmaps
        List<Image<Rgba32>> outputImages = new();
        for (var m = 0; m < (false ? rsi.ExternalResources.Count : 1); ++m)
        {
            var imageResourceInfo = rsi.ExternalResources[m];
            byte[] imageRawData = imageResourceInfo.Data;

            int sourceWidth = txr.Width;
            int sourceHeight = txr.Height;
            if (rsi.Unknown02 == 8)
            {
                sourceWidth = Utils.PowerOfTwo(sourceWidth);
                sourceHeight = Utils.PowerOfTwo(sourceHeight);
            }
            
            // Calculate mipmap dimensions for this mipmap level, "m".
            // If 0, it's the full-size image, but we use the same logic anyway.
            int mipWidth = (int)Math.Max(1, sourceWidth / Math.Pow(2, m));
            int mipHeight = (int)Math.Max(1, sourceHeight / Math.Pow(2, m));
            
            // Determine the source pixel format.
            var pixelFormat = txr.Format switch
            {
                TextureFormat.ARGB8888 => PixelDataFormat.FormatArgb8888,
                TextureFormat.BGR565 => PixelDataFormat.FormatBgr565,
                TextureFormat.BGRA4444 => PixelDataFormat.FormatBgra4444,
                TextureFormat.DXT1RGB => PixelDataFormat.FormatDXT1Rgb,
                TextureFormat.DXT5 => PixelDataFormat.FormatDXT5,
                TextureFormat.BC5 =>  PixelDataFormat.FormatRGTC2,  // RGTC2 / BC5
                TextureFormat.BC4 => PixelDataFormat.FormatRGTC1,   // RGTC1 / BC4
                TextureFormat.Indexed8 => PixelDataFormat.FormatIndexed8,
                TextureFormat.BPTC => PixelDataFormat.FormatBPTC,
                _ => PixelDataFormat.Undefined
            };
            
            // Handle console-specific image data swizzling... hopefully.
            switch (txr.Swizzle)
            {
                // Swizzling?
                case 0:
                case 2:
                case 6:
                    //Console.WriteLine("WARNING: This texture is swizzled, meaning it likely came from a console version of the game. These are not supported.");

                    /*
                    if (txr.Swizzle == 0 || txr.Swizzle == 6)   // PS4
                    {
                        
                    }
                    else
                    {
                        pixelFormat |= PixelDataFormat.PixelOrderingSwizzledVita;
                    }
                    */

                    try
                    {
                        // TODO: blockSize is a placeholder and does not work with all images.
                        imageRawData = ResourceUtils.PS4UnSwizzle(imageRawData, mipWidth, mipHeight, 8);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Error de-swizzling texture, block size is probably wrong!");
                    }
                    //pixelFormat |= PixelDataFormat.PixelOrderingSwizzledVita;

                    break;
                
                // No swizzling?
                case 1: 
                    break;

                default:
                    Console.WriteLine($"WARNING: Unknown swizzle type {txr.Swizzle}.");
                    Console.WriteLine("Press ENTER to continue...");
                    Console.ReadLine();
                    break;
            }
            
            // Use Scarlet to convert the raw pixel data into something we can actually use.
            ScarletImage scarletImageBinary = new(mipWidth, mipHeight, pixelFormat, imageRawData);
            byte[] convertedPixelData = scarletImageBinary.GetOutputPixelData(0);
            Image<Rgba32> currentMipmap = new(config, mipWidth, mipHeight);
            for (var y = 0; y < mipHeight; ++y)
            {
                for (var x = 0; x < mipWidth; ++x)
                {
                    // Get the color data from either the palette or direct data.
                    Rgba32 pixelColor;
                    if (pixelFormat == PixelDataFormat.FormatIndexed8)
                    {
                        int pixelDataOffset = (y * mipWidth) + x;

                        // Apply the previously-loaded palette
                        int paletteDataOffset = convertedPixelData[pixelDataOffset];
                        pixelColor.B = paletteData[paletteDataOffset + 0];
                        pixelColor.G = paletteData[paletteDataOffset + 1];
                        pixelColor.R = paletteData[paletteDataOffset + 2];
                        pixelColor.A = paletteData[paletteDataOffset + 3];
                    }
                    else
                    {
                        int pixelDataOffset = ((y * mipWidth) + x) * 4;

                        byte[] pixelData = new byte[4];
                        Array.Copy(convertedPixelData, pixelDataOffset, pixelData, 0, 4);
                        pixelColor.B = pixelData[0];
                        pixelColor.G = pixelData[1];
                        pixelColor.R = pixelData[2];
                        pixelColor.A = pixelData[3];

                        // Perform fixups depending on the output data format
                        if (pixelFormat == PixelDataFormat.FormatRGTC2)
                        {
                            pixelColor.B = 255;
                            pixelColor.A = 255;
                        }
                        else if (pixelFormat == PixelDataFormat.FormatRGTC1)
                        {
                            pixelColor.G = pixelColor.R;
                            pixelColor.B = pixelColor.R;
                            //pixelColor.A = pixelColor.R;
                        }
                    }

                    // Assign that color to the appropriate pixel on the output image.
                    currentMipmap[x, y] = pixelColor;
                }
            }
            
            outputImages.Add(currentMipmap);
        }
        
        return new TextureResource(outputName, outputImages);
    }
}