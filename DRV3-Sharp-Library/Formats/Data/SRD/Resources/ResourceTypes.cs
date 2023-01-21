using System;
using System.Collections.Generic;
using System.Numerics;
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

public sealed record VertexResource(
        string Name,
        List<Vector3> Vertices,
        List<Vector3> Normals,
        List<Vector2> TextureCoords,
        List<Tuple<ushort, ushort, ushort>> Indices,
        List<string> Bones,
        List<float> Weights)
    : ISrdResource;

public interface ISrdResource
{ }