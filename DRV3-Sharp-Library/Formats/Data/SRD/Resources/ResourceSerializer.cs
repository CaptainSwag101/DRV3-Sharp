using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DRV3_Sharp_Library.Formats.Data.SRD.Blocks;
using Scarlet.Drawing;
using Scarlet.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using ScarletImage = Scarlet.Drawing.ImageBinary;

namespace DRV3_Sharp_Library.Formats.Data.SRD.Resources;

internal static class ResourceSerializer
{
    public static ISrdResource DeserializeUnknown(ISrdBlock block)
    {
        return new UnknownResource(block);
    }
    public static ISrdBlock SerializeUnknown(UnknownResource unknown)
    {
        return unknown.UnderlyingBlock;
    }
    
    public static ISrdResource DeserializeTexture(TxrBlock txr)
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
        bool processSmallerMipmaps = false;
        for (var m = 0; m < (processSmallerMipmaps ? rsi.ExternalResources.Count : 1); ++m) // Ignore mipmaps for now
        {
            var imageResourceInfo = rsi.ExternalResources[m];
            var imageRawData = imageResourceInfo.Data;

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
            
            // Handle console-specific image data swizzling... later, when we understand it better.
            /*
            switch (txr.Swizzle)
            {
                // Swizzling?
                case 0:
                case 2:
                case 6:
                    //Console.WriteLine("WARNING: This texture is swizzled, meaning it likely came from a console version of the game. These are not supported.");
                    try
                    {
                        // TODO: blockSize is a placeholder and does not work with all images.
                        // It seems to be based on the input texture type:
                        // https://gbatemp.net/threads/is-there-any-tool-to-unswizzle-textures-from-switch-games.609228/post-9774349
                        imageRawData = ResourceUtils.PS4UnSwizzle(imageRawData, mipWidth, mipHeight, 8).ToArray();
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Error de-swizzling texture, block size is probably wrong!");
                    }

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
            */
            
            // Use Scarlet to convert the raw pixel data into something we can actually use.
            ScarletImage scarletImageBinary = new(mipWidth, mipHeight, pixelFormat, imageRawData);
            var convertedPixelData = new ReadOnlySpan<byte>(scarletImageBinary.GetOutputPixelData(0));
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

                        var pixelBytes = convertedPixelData[pixelDataOffset..(pixelDataOffset + 4)];
                        pixelColor.B = pixelBytes[0];
                        pixelColor.G = pixelBytes[1];
                        pixelColor.R = pixelBytes[2];
                        pixelColor.A = pixelBytes[3];

                        // Perform fixups depending on the output data format.
                        // BC5 and BC4 are two-channel and one-channel formats respectively,
                        // so we need to imply the remaining color and alpha channels' data.
                        if (pixelFormat == PixelDataFormat.FormatRGTC2)         // BC5
                        {
                            // Set missing channels to maximum value since this
                            // format is only used for normal map textures.
                            pixelColor.B = 255;
                            pixelColor.A = 255;
                        }
                        else if (pixelFormat == PixelDataFormat.FormatRGTC1)    // BC4
                        {
                            // Export as grayscale instead of just R, G, or B.
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
    public static TxrBlock SerializeTexture(TextureResource texture)
    {
        // First, convert the texture(s) from ImageSharp format to Scarlet
        // so we can compress it to a Direct3D compatible byte format.
        List<byte[]> convertedPixelBytes = new();
        foreach (var mipmap in texture.ImageMipmaps)
        {
            // ImageSharp pixel data is stored as ARGB little-endian (alpha being MSB, blue being LSB).
            var numBytes = 4 * mipmap.Height * mipmap.Width;
            var pixelData = new byte[numBytes];

            for (var y = 0; y < mipmap.Height; ++y)
            {
                for (var x = 0; x < mipmap.Width; ++x)
                {
                    int byteOffset = 4 * ((y * mipmap.Width) + x);

                    pixelData[byteOffset + 0] = mipmap.Frames[0][x, y].B;
                    pixelData[byteOffset + 1] = mipmap.Frames[0][x, y].G;
                    pixelData[byteOffset + 2] = mipmap.Frames[0][x, y].R;
                    pixelData[byteOffset + 3] = mipmap.Frames[0][x, y].A;
                }
            }
            
            // Now, convert the pixel data to a ScarletImage and get the output bytes.
            ScarletImage scarletImageBinary = new(mipmap.Width, mipmap.Height, PixelDataFormat.FormatArgb8888,
                Endian.LittleEndian, PixelDataFormat.FormatBPTC, Endian.LittleEndian, pixelData);

            var convertedData = scarletImageBinary.GetOutputPixelData(0);
            convertedPixelBytes.Add(convertedData);
        }
        
        // Now, generate the TXR and RSI block data based on this info.
        var mipmapResources = convertedPixelBytes.Select(mipmapData => new ExternalResource(ResourceDataLocation.Srdv, mipmapData, 0, -1)).ToList();

        RsiBlock rsi = new(6, 5, 4, -1, new(), mipmapResources,
            new() { texture.Name },new(), new());
        TxrBlock txr = new(8, 28, 1, (ushort)texture.ImageMipmaps[0].Width, (ushort)texture.ImageMipmaps[0].Height,
            1024, TextureFormat.BPTC, 0, 0, new() { rsi });

        return txr;
    }
}