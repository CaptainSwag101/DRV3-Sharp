using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using V3Lib;
using V3Lib.Srd;
using V3Lib.Srd.BlockTypes;

namespace SrdTool
{
    class Program
    {
        private static SrdFile Srd;
        private static string SrdName;
        private static byte[] Srdi;
        private static string SrdiName;
        private static byte[] Srdv;
        private static string SrdvName;

        static void Main(string[] args)
        {
            Console.WriteLine("SRD Tool by CaptainSwag101\n" +
                "Version 0.0.4, built on 2020-04-26\n");

            if (args.Length != 1)
            {
                Console.WriteLine("Usage: SrdTool.exe <SRD file to analyze>");
                return;
            }

            FileInfo info = new FileInfo(args[0]);
            if (!info.Exists)
            {
                Console.WriteLine($"ERROR: \"{args[0]}\" does not exist.");
                return;
            }

            if (info.Extension.ToLower() != ".srd")
            {
                Console.WriteLine("WARNING: Input file does not have the \".srd\" extension. This may be correct, however.");
            }

            // If the file exists and is valid, load it
            Srd = new SrdFile();
            SrdName = args[0];
            Srd.Load(SrdName);

            // Search for linked files like SRDI and SRDV and load them
            Srdi = Array.Empty<byte>();
            if (File.Exists(SrdName + 'i'))
            {
                Srdi = File.ReadAllBytes(SrdName + 'i');
                SrdiName = SrdName + 'i';
            }

            Srdv = Array.Empty<byte>();
            if (File.Exists(SrdName + 'v'))
            { 
                Srdv = File.ReadAllBytes(args[0] + 'v');
                SrdvName = SrdName + 'v';
            }

            // Process commands
            while (true)
            {
                Console.Write("Type a command to perform on this SRD (print_blocks, extract_models, extract_textures, exit): ");
                string command = Console.ReadLine().ToLowerInvariant();

                switch (command)
                {
                    case "print_blocks":
                        Console.WriteLine($"\"{info.FullName}\" contains the following blocks:\n");
                        PrintBlocks(Srd.Blocks, 0);
                        break;

                    case "extract_models":
                        ExtractModels();
                        break;

                    case "extract_textures":
                        ExtractTextures();
                        break;

                    case "exit":
                        return;

                    default:
                        Console.WriteLine("Invalid command.");
                        break;
                }
            }
        }

        private static void PrintBlocks(List<Block> blockList, int tabLevel)
        {
            foreach (Block block in blockList)
            {
                Console.Write(new string('\t', tabLevel));
                // Print an extra message for debugging if the block type is considered "unknown"
                Console.WriteLine($"Block Type: {block.BlockType}" + ((block is UnknownBlock) ? " (unknown block type)" : ""));

                // Print block-specific info
                string[] blockInfoLines = block.GetInfo().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in blockInfoLines)
                {
                    Console.Write(new string('\t', tabLevel + 1));
                    Console.WriteLine(line);
                }
                
                // Print child block info
                if (block.Children.Count > 0)
                {
                    Console.Write(new string('\t', tabLevel + 1));
                    Console.WriteLine($"Child Blocks: {block.Children.Count:n0}");
                    Console.WriteLine();

                    PrintBlocks(block.Children, tabLevel + 1);
                }

                Console.WriteLine();
            }
        }

        private static void ExtractModels()
        {
            // Export the vertices and faces as an ASCII OBJ
            StringBuilder sb = new StringBuilder();
            int totalVerticesProcessed = 0;
            foreach (Block b in Srd.Blocks)
            {
                if ((b is VtxBlock) && (b.Children[0] is RsiBlock))
                {
                    VtxBlock vtx = (VtxBlock)b;
                    RsiBlock rsi = (RsiBlock)b.Children[0];

                    BinaryReader srdiReader = new BinaryReader(new MemoryStream(Srdi));

                    // Extract vertex data
                    List<float[]> vertexList = new List<float[]>();
                    List<float[]> normalList = new List<float[]>();
                    List<float[]> texmapList = new List<float[]>();
                    int vertexBlockOffset = rsi.ResourceInfoList[0].Offset & 0x1FFFFFFF;    // NOTE: This might need to be 0x00FFFFFF
                    int vertexBlockLength = rsi.ResourceInfoList[0].Length;

                    int combinedSize = 0;
                    foreach (var subBlock in vtx.VertexSubBlockList)
                    {
                        combinedSize += subBlock.Size;
                    }
                    if ((vertexBlockLength / combinedSize) != vtx.VertexCount)
                    {
                        Console.WriteLine("WARNING: Total vertex block length and expected vertex count are misaligned.");
                    }

                    for (int sbNum = 0; sbNum < vtx.VertexSubBlockCount; ++sbNum)
                    {
                        srdiReader.BaseStream.Seek(vertexBlockOffset + vtx.VertexSubBlockList[sbNum].Offset, SeekOrigin.Begin);
                        for (int vNum = 0; vNum < vtx.VertexCount; ++vNum)
                        {
                            int bytesRead = 0;
                            switch (sbNum)
                            {
                                case 0: // Vertex/Normal data (and Texture UV for boneless models)
                                    {
                                        float[] vertex = new float[3];
                                        vertex[0] = srdiReader.ReadSingle() * -1.0f;    // X
                                        vertex[1] = srdiReader.ReadSingle();            // Y
                                        vertex[2] = srdiReader.ReadSingle();            // Z
                                        vertexList.Add(vertex);

                                        float[] normal = new float[3];
                                        normal[0] = srdiReader.ReadSingle() * -1.0f;    // X
                                        normal[1] = srdiReader.ReadSingle();            // Y
                                        normal[2] = srdiReader.ReadSingle();            // Z
                                        normalList.Add(normal);

                                        if (vtx.VertexSubBlockCount == 1)
                                        {
                                            float[] texmap = new float[3];
                                            texmap[0] = srdiReader.ReadSingle();        // U
                                            texmap[1] = srdiReader.ReadSingle() * -1.0f;// V
                                            texmapList.Add(texmap);
                                            bytesRead = 32;
                                        }
                                        else
                                        {
                                            bytesRead = 24;
                                        }

                                        break;
                                    }

                                case 1: // Bone weights
                                    {

                                    }
                                    break;

                                case 2: // Texture UVs (only for models with bones)
                                    {
                                        float[] texmap = new float[3];
                                        texmap[0] = srdiReader.ReadSingle();            // U
                                        texmap[1] = srdiReader.ReadSingle() * -1.0f;    // V
                                        texmapList.Add(texmap);
                                        bytesRead = 8;
                                        break;
                                    }
                            }

                            // Skip data we don't currently use, though I may add support for this data later
                            srdiReader.BaseStream.Seek(vtx.VertexSubBlockList[sbNum].Size - bytesRead, SeekOrigin.Current);
                        }
                    }

                    // Extract face data
                    List<ushort[]> faceList = new List<ushort[]>();
                    int faceBlockOffset = rsi.ResourceInfoList[1].Offset & 0x1FFFFFFF;  // NOTE: This might need to be 0x00FFFFFF
                    int faceBlockLength = rsi.ResourceInfoList[1].Length;

                    srdiReader.BaseStream.Seek(faceBlockOffset, SeekOrigin.Begin);
                    while (srdiReader.BaseStream.Position < (faceBlockOffset + faceBlockLength))
                    {
                        ushort[] faceIndices = new ushort[3];
                        for (int i = 0; i < 3; ++i)
                        {
                            ushort index = srdiReader.ReadUInt16();
                            faceIndices[i] = index;
                        }
                        faceList.Add(faceIndices);
                    }


                    // Close and Dispose our reader instances, we don't need them anymore
                    srdiReader.Close();
                    srdiReader.Dispose();


                    // Write mesh object data
                    sb.Append($"o {rsi.ResourceStringList[0]}\n");

                    int verticesProcessed = 0;
                    foreach (float[] v in vertexList)
                    {
                        sb.Append($"v {v[0]} {v[1]} {v[2]}\n");
                        ++verticesProcessed;
                    }
                    sb.Append("\n");

                    foreach (float[] vn in normalList)
                    {
                        sb.Append($"vn {vn[0]} {vn[1]} {vn[2]}\n");
                    }
                    sb.Append("\n");


                    foreach (float[] vt in texmapList)
                    {
                        sb.Append($"vt {vt[0]} {vt[1]}\n");
                    }
                    sb.Append("\n");

                    foreach (ushort[] f in faceList)
                    {
                        int faceIndex1 = f[0] + totalVerticesProcessed + 1;
                        int faceIndex2 = f[1] + totalVerticesProcessed + 1;
                        int faceIndex3 = f[2] + totalVerticesProcessed + 1;
                        sb.Append($"f {faceIndex1}/{faceIndex1}/{faceIndex1} {faceIndex2}/{faceIndex2}/{faceIndex2} {faceIndex3}/{faceIndex3}/{faceIndex3}\n");
                    }

                    totalVerticesProcessed += verticesProcessed;
                    sb.Append('\n');
                }
            }

            string objString = sb.ToString();
            File.WriteAllText(SrdName + ".obj", objString);
        }

        private static void ExtractTextures()
        {
            SrdFile textureSrd;
            string textureSrdName;
            byte[] textureSrdv;
            string textureSrdvName;

            // Check if this SRD has an accompanying SRDV file.
            // If not, search for a "_texture" SRD and SRDV file of similar name.
            if (!string.IsNullOrWhiteSpace(SrdvName) && Srdv.Length > 0)
            {
                textureSrd = Srd;
                textureSrdName = SrdName;
                textureSrdv = Srdv;
                textureSrdvName = SrdvName;
            }
            else
            {
                textureSrdName = SrdName.Remove(SrdName.LastIndexOf(".srd")) + "_texture.srd";
                Console.WriteLine($"There is no accompanying SRDV file for this SRD file, searching for a dedicated \"{new FileInfo(textureSrdName).Name}\"...");

                if (!File.Exists(textureSrdName))
                {
                    Console.WriteLine("Unable to find any accompanying texture data.");
                    return;
                }

                textureSrd = new SrdFile();
                textureSrd.Load(textureSrdName);
                textureSrdvName = textureSrdName + 'v';
                textureSrdv = File.ReadAllBytes(textureSrdvName);
            }

            // Now that we've found texture info, we can start extracting it
            BinaryReader textureReader = new BinaryReader(new MemoryStream(textureSrdv));

            foreach (Block b in textureSrd.Blocks)
            {
                if (b is TxrBlock && b.Children[0] is RsiBlock)
                {
                    TxrBlock txr = (TxrBlock)b;
                    RsiBlock rsi = (RsiBlock)b.Children[0];

                    // Read image data based on resource info
                    bool extractMipmaps = false;    // FIXME: TEMPORARY!!!
                    for (int m = 0; m < (extractMipmaps ? rsi.ResourceInfoList.Count : 1); m++)
                    {
                        byte[] imageData;

                        using (BinaryReader srdvReader = new BinaryReader(new FileStream(textureSrdvName, FileMode.Open)))
                        {
                            srdvReader.BaseStream.Seek(rsi.ResourceInfoList[m].Offset, SeekOrigin.Begin);
                            imageData = srdvReader.ReadBytes(rsi.ResourceInfoList[m].Length);

                            // TODO: Read palette data
                        }


                        int dispWidth = txr.DisplayWidth;
                        int dispHeight = txr.DisplayHeight;
                        if (rsi.Unknown12 == 0x08)
                        {
                            dispWidth = (ushort)Utils.PowerOfTwo(dispWidth);
                            dispHeight = (ushort)Utils.PowerOfTwo(dispHeight);
                        }

                        /*
                        // Determine pixel format
                        PixelDataFormat pixelFormat = PixelDataFormat.Undefined;
                        switch (Format)
                        {
                            case 0x01:
                                pixelFormat = PixelDataFormat.FormatArgb8888;
                                break;

                            case 0x02:
                                pixelFormat = PixelDataFormat.FormatBgr565;
                                break;

                            case 0x05:
                                pixelFormat = PixelDataFormat.FormatBgra4444;
                                break;

                            case 0x0F:
                                pixelFormat = PixelDataFormat.FormatDXT1Rgb;
                                break;

                            case 0x11:
                                pixelFormat = PixelDataFormat.FormatDXT5;
                                break;

                            case 0x14:  // RGTC2 / BC5
                                pixelFormat = PixelDataFormat.FormatRGTC2;
                                break;

                            case 0x16:  // RCTC1 / BC4
                                pixelFormat = PixelDataFormat.FormatRGTC1;
                                break;

                            case 0x1C:
                                pixelFormat = PixelDataFormat.FormatBPTC;
                                break;
                        }

                        bool swizzled = ((txr.Swizzle & 1) == 0);

                        // Calculate mipmap dimensions
                        int mipWidth = (int)Math.Max(1, dispWidth / Math.Pow(2, m));
                        int mipHeight = (int)Math.Max(1, dispHeight / Math.Pow(2, m));

                        ImageBinary imageBinary = new ImageBinary(mipWidth, mipHeight, pixelFormat, imageData);
                        Bitmap tex = imageBinary.GetBitmap();


                        string mipmapName = rsi.ResourceStringList.First();
                        int extensionLength = mipmapName.Split('.').Last().Length + 1;

                        if (m > 0)
                            mipmapName = mipmapName.Insert(mipmapName.Length - extensionLength, $" ({mipWidth}x{mipHeight})");


                        ImageFormat imageFormat;
                        switch (mipmapName.Split('.').Last().ToLower())
                        {
                            case "bmp":
                                imageFormat = ImageFormat.Bmp;
                                break;

                            case "png":
                                imageFormat = ImageFormat.Png;
                                break;

                            default:
                                imageFormat = ImageFormat.Png;
                                mipmapName += ".png";
                                break;
                        }

                        FileStream imageOut = File.Create(outputFolder + '/' + mipmapName);
                        tex.Save(imageOut, imageFormat);
                        imageOut.Close();
                        */
                    }
                }
            }

            textureReader.Close();
            textureReader.Dispose();
        }
    }
}
