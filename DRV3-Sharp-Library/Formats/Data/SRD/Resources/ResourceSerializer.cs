using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using DRV3_Sharp_Library.Formats.Data.SRD.Blocks;
using Scarlet.Drawing;
using Scarlet.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.PixelFormats;
using ScarletImage = Scarlet.Drawing.ImageBinary;

namespace DRV3_Sharp_Library.Formats.Data.SRD.Resources;

internal static class ResourceSerializer
{
    public static UnknownResource DeserializeUnknown(ISrdBlock block)
    {
        return new UnknownResource(block);
    }
    public static ISrdBlock SerializeUnknown(UnknownResource unknown)
    {
        return unknown.UnderlyingBlock;
    }

    public static MaterialResource DeserializeMaterial(MatBlock mat)
    {
        // The RSI sub-block is critical because it contains the material name and property data.
        if (mat.SubBlocks[0] is not RsiBlock rsi)
            throw new InvalidDataException("A MAT block within the SRD file did not have its expected RSI sub-block.");

        string name = rsi.ResourceStrings[0];
        var materialProperties = rsi.LocalResources;
        var shaderReferences = rsi.ResourceStrings.GetRange(1, rsi.ResourceStrings.Count - 1);

        return new MaterialResource(name, shaderReferences, mat.MapTexturePairs, materialProperties);
    }

    public static MeshResource DeserializeMesh(MshBlock msh)
    {
        // The RSI sub-block is critical because it contains the mesh name and other info.
        if (msh.SubBlocks[0] is not RsiBlock rsi)
            throw new InvalidDataException("A MSH block within the SRD file did not have its expected RSI sub-block.");

        string meshName = rsi.ResourceStrings[0];
        return new MeshResource(meshName, msh.LinkedVertexName, msh.LinkedMaterialName, msh.Strings, msh.MappedNodes);
    }

    public static SceneResource DeserializeScene(ScnBlock scn)
    {
        // The RSI sub-block is critical because it contains the scene name and other data.
        if (scn.SubBlocks[0] is not RsiBlock rsi)
            throw new InvalidDataException("A SCN block within the SRD file did not have its expected RSI sub-block.");

        string name = rsi.ResourceStrings[0];

        return new SceneResource(name, scn.LinkedTreeNames, scn.UnknownStrings);
    }

    public static TreeResource DeserializeTree(TreBlock tre)
    {
        // The RSI sub-block is critical because it contains the tree name and other strings.
        if (tre.SubBlocks[0] is not RsiBlock rsi)
            throw new InvalidDataException("A TRE block within the SRD file did not have its expected RSI sub-block.");

        string name = rsi.ResourceStrings[0];
        // TODO: Copy over the LocalResources in the RSI block, it contains info
        // about the tree and its associated geometry/materials.

        return new TreeResource(name, tre.RootNode, tre.UnknownMatrix, rsi.LocalResources);
    }

    public static TextureInstanceResource DeserializeTextureInstance(TxiBlock txi)
    {
        // The RSI sub-block is critical because it contains the material name.
        if (txi.SubBlocks[0] is not RsiBlock rsi)
            throw new InvalidDataException("A TXI block within the SRD file did not have its expected RSI sub-block.");

        string linkedMaterialName = rsi.ResourceStrings[0];

        return new TextureInstanceResource(txi.LinkedTextureName, linkedMaterialName);
    }
    
    public static TextureResource DeserializeTexture(TxrBlock txr)
    {
        // The RSI sub-block is critical because it contains the raw image data.
        if (txr.SubBlocks[0] is not RsiBlock rsi)
            throw new InvalidDataException("A TXR block within the SRD file did not have its expected RSI sub-block.");
        
        string outputName = rsi.ResourceStrings[0];
        Console.WriteLine($"Found texture resource {outputName}");

        string textureExtension = outputName.Split('.').Last().ToLowerInvariant();
        Configuration config = textureExtension switch
        {
            "bmp" => new(new BmpConfigurationModule()),
            "tga" => new(new TgaConfigurationModule()),
            "png" => new(new PngConfigurationModule()),
            _ => new(new BmpConfigurationModule())
        };
        
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

    public static VertexResource DeserializeVertex(VtxBlock vtx)
    {
        // The RSI sub-block is critical because it contains the geometry and index data.
        if (vtx.SubBlocks[0] is not RsiBlock rsi)
            throw new InvalidDataException("A VTX block within the SRD file did not have its expected RSI sub-block.");

        string name = rsi.ResourceStrings[0];
        Console.WriteLine($"Found vertex resource {name}");
        
        using MemoryStream geometryStream = new(rsi.ExternalResources[0].Data);
        using BinaryReader geometryReader = new(geometryStream);

        // Read data from each geometry section
        List<Vector3> vertices = new();
        List<Vector3> normals = new();
        List<Vector2> textureCoords = new();
        List<float> weights = new();
        for (var sectionNum = 0; sectionNum < vtx.VertexSectionInfo.Count; ++sectionNum)
        {
            var sectionInfo = vtx.VertexSectionInfo[sectionNum];
            geometryStream.Seek(sectionInfo.Start, SeekOrigin.Begin);

            for (var vertexNum = 0; vertexNum < vtx.VertexCount; ++vertexNum)
            {
                var thisVertexDataStart = geometryStream.Position;
                switch (sectionNum)
                {
                    // Vertex and Normal data (and Texture coordinates for boneless models?)
                    case 0:
                    {
                        Vector3 vertex = new()
                        {
                            X = geometryReader.ReadSingle() * -1.0f,
                            Y = geometryReader.ReadSingle(),
                            Z = geometryReader.ReadSingle()
                        };
                        vertices.Add(vertex);

                        Vector3 normal = new()
                        {
                            X = geometryReader.ReadSingle() * -1.0f,
                            Y = geometryReader.ReadSingle(),
                            Z = geometryReader.ReadSingle()
                        };
                        normals.Add(normal);

                        if (vtx.VertexSectionInfo.Count == 1)
                        {
                            Vector2 textureCoord = new()
                            {
                                X = geometryReader.ReadSingle(),
                                Y = geometryReader.ReadSingle()
                            };
                            textureCoords.Add(textureCoord);
                        }

                        break;
                    }
                    // Bone weights?
                    case 1:
                    {
                        var weightsPerVertex = (sectionInfo.DataSizePerVertex / sizeof(float));
                        for (var weightNum = 0; weightNum < weightsPerVertex; ++weightNum)
                        {
                            weights.Add(geometryReader.ReadSingle());
                        }

                        break;
                    }
                    // Texture coordinates (only for models with bones?)
                    case 2:
                    {
                        Vector2 textureCoord = new()
                        {
                            X = geometryReader.ReadSingle(),
                            Y = geometryReader.ReadSingle()
                        };
                        textureCoords.Add(textureCoord);
                        break;
                    }
                    default:
                        throw new InvalidDataException($"Unknown geometry data section index {sectionNum}.");
                }
            
                // Skip data we don't currently use, though I may add support for this data later
                var remainingBytes = sectionInfo.DataSizePerVertex - (geometryStream.Position - thisVertexDataStart);
                //Console.WriteLine($"Skipping {remainingBytes} unknown bytes for this vertex.");
                geometryStream.Seek(remainingBytes, SeekOrigin.Current);
            }
        }
        
        // Read index data
        using MemoryStream indexStream = new(rsi.ExternalResources[1].Data);
        using BinaryReader indexReader = new(indexStream);
        List<Tuple<ushort, ushort, ushort>> indices = new();
        while (indexStream.Position < indexStream.Length)
        {
            // We need to reverse the order of the indices to prevent the normals
            // from becoming permanently flipped due to the clockwise/counter-clockwise
            // order of the indices determining the face's direction.
            ushort index3 = indexReader.ReadUInt16();
            ushort index2 = indexReader.ReadUInt16();
            ushort index1 = indexReader.ReadUInt16();
            
            Tuple<ushort, ushort, ushort> indexTriplet = new(index1, index2, index3);
            indices.Add(indexTriplet);
        }
        

        return new VertexResource(name, vertices, normals, textureCoords, indices, vtx.BoneList, weights);
    }
}