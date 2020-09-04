using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using V3Lib;
using V3Lib.Srd;
using V3Lib.Srd.BlockTypes;
using CommandParser_Alpha;
using Scarlet.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

namespace SrdTool
{
    class Program
    {
        private static SrdFile Srd;
        private static string SrdName;
        private static string SrdiName;
        private static string SrdvName;

        static void Main(string[] args)
        {
            Console.WriteLine("SRD Tool by CaptainSwag101\n" +
                "Version 1.0.0, built on 2020-08-03\n");

            // Setup text encoding so we can use Shift-JIS text later on
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

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

            // If the file exists and is valid, load it
            Srd = new SrdFile();
            SrdName = args[0];

            // Search for linked files like SRDI and SRDV
            SrdiName = SrdName.Remove(SrdName.LastIndexOf(new FileInfo(SrdName).Extension)) + ".srdi";
            if (!File.Exists(SrdiName))
            {
                SrdiName = string.Empty;
            }

            SrdvName = SrdName.Remove(SrdName.LastIndexOf(new FileInfo(SrdName).Extension)) + ".srdv";
            if (!File.Exists(SrdvName))
            {
                SrdvName = string.Empty;
            }

            Srd.Load(SrdName, SrdiName, SrdvName);

            // Setup command dictionary
            var commandDict = new Dictionary<string, Action>
            {
                {
                    @"print_blocks",
                    delegate
                    {
                        Console.WriteLine($"\"{info.FullName}\" contains the following blocks:\n");
                        PrintBlocks(Srd.Blocks, 0);
                    }
                },
                {
                    @"extract_fonts",
                    delegate
                    {
                        ExtractFonts();
                    }
                },
                {
                    @"extract_models",
                    delegate
                    {
                        ExtractModels();
                    }
                },
                {
                    @"extract_textures",
                    delegate
                    {
                        ExtractTextures();
                    }
                },
                {
                    @"replace_textures",
                    delegate
                    {
                        ReplaceTextures();
                    }
                },
                {
                    @"save",
                    delegate
                    {
                        Srd.Save(SrdName, SrdiName, SrdvName);
                        //Console.WriteLine("Save complete.");
                    }
                }
            };

            // Process commands
            StringBuilder promptBuilder = new StringBuilder();
            promptBuilder.Append($"Loaded SRD file: {SrdName}\n");
            if (info.Extension.ToLower() != ".srd")
            {
                promptBuilder.Append("WARNING: Input file does not have the \".srd\" extension. This may be correct, however.\n");
            }
            promptBuilder.Append($"Valid commands to perform on this SRD file: ");
            promptBuilder.AppendJoin(", ", commandDict.Keys);
            Console.WriteLine(promptBuilder.ToString());

            CommandParser parser = new CommandParser(commandDict);
            parser.Prompt("Please enter your command, or \"exit\" to quit: ", @"exit");
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

        private static void ExtractFonts()
        {
            bool foundFont = false;
            foreach (Block b in Srd.Blocks)
            {
                if (b is TxrBlock txr && b.Children[0] is RsiBlock rsi)
                {
                    using BinaryReader fontReader = new BinaryReader(new MemoryStream(rsi.ResourceData));
                    string fontMagic = Encoding.ASCII.GetString(fontReader.ReadBytes(4));
                    if (fontMagic != @"SpFt")
                    {
                        continue;
                    }
                    foundFont = true;

                    uint unknown44 = fontReader.ReadUInt32();
                    uint unknown48 = fontReader.ReadUInt32();
                    uint fontNameLength = fontReader.ReadUInt32();
                    uint fontNamePtr = fontReader.ReadUInt32();
                    uint charCount = fontReader.ReadUInt32();
                    uint bbListPtr = fontReader.ReadUInt32();
                    uint unknown5C = fontReader.ReadUInt32();
                    uint unknownData2Ptr = fontReader.ReadUInt32();
                    ushort unknown64 = fontReader.ReadUInt16();
                    ushort unknown66 = fontReader.ReadUInt16();
                    uint fontNamePtrsPtr = fontReader.ReadUInt32();
                    uint unknown6C = fontReader.ReadUInt32();

                    byte[] unknownData1 = fontReader.ReadBytes((int)(unknownData2Ptr - fontReader.BaseStream.Position));
                    var unknownData2 = new List<int>();
                    //int index = 0;
                    while (fontReader.BaseStream.Position < bbListPtr)
                    {
                        unknownData2.Add(fontReader.ReadInt32());
                        //unknownData2.Add(fontReader.ReadInt32() + (index++));
                    }

                    // Count number of unique values in unknownData2
                    //var unique = new List<int>();
                    //foreach (int i in unknownData2)
                    //{
                    //    if (!unique.Contains(i))
                    //        unique.Add(i);
                    //}

                    var bbList = new List<(Rectangle Box, sbyte[] Unknown)>();
                    while (fontReader.BaseStream.Position < fontNamePtrsPtr)
                    {
                        // Each glyph's texture position is stored as two 12-bit integers
                        byte[] rawTexPos = fontReader.ReadBytes(3);
                        short xPos = (short)(((rawTexPos[1] & 0x0F) << 8) | rawTexPos[0]);
                        short yPos = (short)((rawTexPos[1] >> 4) | rawTexPos[2] << 4);

                        byte width = fontReader.ReadByte();
                        byte height = fontReader.ReadByte();

                        Rectangle bbRect = new Rectangle(xPos, yPos, width, height);

                        sbyte[] glyphKerning = new sbyte[3];
                        glyphKerning[0] = fontReader.ReadSByte();   // left
                        glyphKerning[1] = fontReader.ReadSByte();   // right
                        glyphKerning[2] = fontReader.ReadSByte();   // vertical
                        bbList.Add((bbRect, glyphKerning));
                    }

                    var fontNamePtrList = new List<uint>();
                    while (fontReader.BaseStream.Position < fontNamePtr)
                    {
                        fontNamePtrList.Add(fontReader.ReadUInt32());
                    }

                    string fontName = Utils.ReadNullTerminatedString(fontReader, Encoding.Unicode);

                    using Image img = new Image<Rgba32>(txr.DisplayWidth, txr.DisplayHeight);
                    img.Mutate(ctx => ctx
                        .Fill(Color.Black));

                    foreach (var bb in bbList)
                    {
                        PointF[] rectPoints = new PointF[]
                        {
                            new PointF(bb.Box.Left, bb.Box.Top),
                            new PointF(bb.Box.Right, bb.Box.Top),
                            new PointF(bb.Box.Right, bb.Box.Bottom),
                            new PointF(bb.Box.Left, bb.Box.Bottom)
                        };

                        img.Mutate(ctx => ctx
                            .DrawPolygon(new Pen(Color.White, 2.0f), rectPoints));
                    }
                    img.SaveAsPng(SrdName + "_font_boxes.png");
                }
            }

            if (!foundFont)
            {
                Console.WriteLine("Unable to find font data in the provided SRD file.");
            }
        }

        private static void ExtractModels()
        {
            // Export the vertices and faces as a Collada file
            XmlWriterSettings daeSettings = new XmlWriterSettings
            {
                OmitXmlDeclaration = false,
                CloseOutput = true,
                Indent = true,
                Encoding = Encoding.UTF8,
                ConformanceLevel = ConformanceLevel.Document
            };
            using XmlWriter daeWriter = XmlWriter.Create(new FileStream(SrdName + ".dae", FileMode.Create, FileAccess.Write, FileShare.Read), daeSettings);
            daeWriter.WriteStartDocument();
            daeWriter.WriteStartElement("COLLADA", "http://www.collada.org/2005/11/COLLADASchema");
            daeWriter.WriteAttributeString("version", "1.4.1");

            // Get a list of all VTX and MSH blocks
            var ScnBlockList = Srd.Blocks.Where(scn => scn is ScnBlock);
            var VtxBlockList = Srd.Blocks.Where(vtx => vtx is VtxBlock);
            var MshBlockList = Srd.Blocks.Where(msh => msh is MshBlock);
            //var MatBlockList = Srd.Blocks.Where(mat => mat is MatBlock);

            // Write asset data
            {
                daeWriter.WriteStartElement("asset");

                daeWriter.WriteStartElement("unit");
                daeWriter.WriteAttributeString("name", "meter");
                daeWriter.WriteAttributeString("meter", "1.0");
                daeWriter.WriteEndElement();    // unit

                daeWriter.WriteStartElement("up_axis");
                daeWriter.WriteString("Y_UP");
                daeWriter.WriteEndElement();    // up_axis

                daeWriter.WriteEndElement();    // asset
            }

            // Write geometry data
            {
                daeWriter.WriteStartElement("library_geometries");

                if (VtxBlockList.Count() != MshBlockList.Count())
                {
                    Console.WriteLine("ERROR: Vertex and Mesh block counts are not equal!");
                    return;
                }

                for (int b = 0; b < VtxBlockList.Count(); ++b)
                {
                    VtxBlock vtx = VtxBlockList.ElementAt(b) as VtxBlock;
                    RsiBlock vtxResources = vtx.Children[0] as RsiBlock;
                    MshBlock msh = MshBlockList.ElementAt(b) as MshBlock;
                    RsiBlock mshResources = msh.Children[0] as RsiBlock;

                    // Extract vertex data
                    using BinaryReader vertexReader = new BinaryReader(new MemoryStream(vtxResources.ExternalData[0]));
                    List<float[]> vertexList = new List<float[]>();
                    List<float[]> normalList = new List<float[]>();
                    List<float[]> texmapList = new List<float[]>();

                    int combinedSize = 0;
                    foreach (var (Offset, Size) in vtx.VertexSubBlockList)
                    {
                        combinedSize += Size;
                    }
                    if ((vtxResources.ExternalData[0].Length / combinedSize) != vtx.VertexCount)
                    {
                        Console.WriteLine("WARNING: Total vertex block length and expected vertex count are misaligned.");
                    }

                    for (int sbNum = 0; sbNum < vtx.VertexSubBlockCount; ++sbNum)
                    {
                        vertexReader.BaseStream.Seek(vtx.VertexSubBlockList[sbNum].Offset, SeekOrigin.Begin);
                        for (int vNum = 0; vNum < vtx.VertexCount; ++vNum)
                        {
                            int bytesRead = 0;
                            switch (sbNum)
                            {
                                case 0: // Vertex/Normal data (and Texture UV for boneless models)
                                    {
                                        float[] vertex = new float[3];
                                        vertex[0] = vertexReader.ReadSingle() * -1.0f;    // X
                                        vertex[1] = vertexReader.ReadSingle();            // Y
                                        vertex[2] = vertexReader.ReadSingle();            // Z
                                        vertexList.Add(vertex);

                                        float[] normal = new float[3];
                                        normal[0] = vertexReader.ReadSingle() * -1.0f;    // X
                                        normal[1] = vertexReader.ReadSingle();            // Y
                                        normal[2] = vertexReader.ReadSingle();            // Z
                                        normalList.Add(normal);

                                        if (vtx.VertexSubBlockCount == 1)
                                        {
                                            float[] texmap = new float[3];
                                            texmap[0] = vertexReader.ReadSingle();        // U
                                            texmap[1] = vertexReader.ReadSingle() * -1.0f;// V
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
                                        float[] texmap = new float[2];
                                        texmap[0] = vertexReader.ReadSingle();            // U
                                        texmap[1] = vertexReader.ReadSingle() * -1.0f;    // V
                                        texmapList.Add(texmap);
                                        bytesRead = 8;
                                        break;
                                    }
                            }

                            // Skip data we don't currently use, though I may add support for this data later
                            vertexReader.BaseStream.Seek(vtx.VertexSubBlockList[sbNum].Size - bytesRead, SeekOrigin.Current);
                        }
                    }

                    // Extract index data
                    using BinaryReader indexReader = new BinaryReader(new MemoryStream(vtxResources.ExternalData[1]));
                    List<ushort[]> indexList = new List<ushort[]>();
                    while (indexReader.BaseStream.Position < indexReader.BaseStream.Length)
                    {
                        ushort[] indices = new ushort[3];
                        for (int i = 0; i < 3; ++i)
                        {
                            ushort index = indexReader.ReadUInt16();
                            indices[i] = index;
                        }
                        indexList.Add(indices);
                    }



                    // Write data to DAE
                    string meshName = mshResources.ResourceStringList[0];

                    daeWriter.WriteStartElement("geometry");
                    daeWriter.WriteAttributeString("id", $"{meshName}");
                    daeWriter.WriteAttributeString("name", $"{meshName}");

                    daeWriter.WriteStartElement("mesh");

                    daeWriter.WriteStartElement("source");  // source Positions START
                    daeWriter.WriteAttributeString("id", $"{meshName}-positions");

                    daeWriter.WriteStartElement("float_array");
                    daeWriter.WriteAttributeString("id", $"{meshName}-positions-array");
                    daeWriter.WriteAttributeString("count", $"{vertexList.Count * 3}");
                    foreach (float[] vertexTriplet in vertexList)
                    {
                        daeWriter.WriteValue(vertexTriplet[0]);
                        daeWriter.WriteString(" ");
                        daeWriter.WriteValue(vertexTriplet[1]);
                        daeWriter.WriteString(" ");
                        daeWriter.WriteValue(vertexTriplet[2]);
                        daeWriter.WriteString(" ");
                    }
                    daeWriter.WriteEndElement();    // float_array

                    daeWriter.WriteStartElement("technique_common");

                    daeWriter.WriteStartElement("accessor");
                    daeWriter.WriteAttributeString("source", $"#{meshName}-positions-array");
                    daeWriter.WriteAttributeString("count", $"{vertexList.Count}");
                    daeWriter.WriteAttributeString("stride", $"{3}");

                    daeWriter.WriteStartElement("param");
                    daeWriter.WriteAttributeString("name", "X");
                    daeWriter.WriteAttributeString("type", "float");
                    daeWriter.WriteEndElement();    // param X
                    daeWriter.WriteStartElement("param");
                    daeWriter.WriteAttributeString("name", "Y");
                    daeWriter.WriteAttributeString("type", "float");
                    daeWriter.WriteEndElement();    // param Y
                    daeWriter.WriteStartElement("param");
                    daeWriter.WriteAttributeString("name", "Z");
                    daeWriter.WriteAttributeString("type", "float");
                    daeWriter.WriteEndElement();    // param Z

                    daeWriter.WriteEndElement();    // accessor

                    daeWriter.WriteEndElement();    // technique_common

                    daeWriter.WriteEndElement();    // source Positions END

                    daeWriter.WriteStartElement("source");  // source Normals START
                    daeWriter.WriteAttributeString("id", $"{meshName}-normals");

                    daeWriter.WriteStartElement("float_array");
                    daeWriter.WriteAttributeString("id", $"{meshName}-normals-array");
                    daeWriter.WriteAttributeString("count", $"{normalList.Count * 3}");
                    foreach (float[] normalTriplet in normalList)
                    {
                        daeWriter.WriteValue(normalTriplet[0]);
                        daeWriter.WriteString(" ");
                        daeWriter.WriteValue(normalTriplet[1]);
                        daeWriter.WriteString(" ");
                        daeWriter.WriteValue(normalTriplet[2]);
                        daeWriter.WriteString(" ");
                    }
                    daeWriter.WriteEndElement();    // float_array

                    daeWriter.WriteStartElement("technique_common");

                    daeWriter.WriteStartElement("accessor");
                    daeWriter.WriteAttributeString("source", $"#{meshName}-normals-array");
                    daeWriter.WriteAttributeString("count", $"{normalList.Count}");
                    daeWriter.WriteAttributeString("stride", $"{3}");

                    daeWriter.WriteStartElement("param");
                    daeWriter.WriteAttributeString("name", "X");
                    daeWriter.WriteAttributeString("type", "float");
                    daeWriter.WriteEndElement();    // param X
                    daeWriter.WriteStartElement("param");
                    daeWriter.WriteAttributeString("name", "Y");
                    daeWriter.WriteAttributeString("type", "float");
                    daeWriter.WriteEndElement();    // param Y
                    daeWriter.WriteStartElement("param");
                    daeWriter.WriteAttributeString("name", "Z");
                    daeWriter.WriteAttributeString("type", "float");
                    daeWriter.WriteEndElement();    // param Z

                    daeWriter.WriteEndElement();    // accessor

                    daeWriter.WriteEndElement();    // technique_common

                    daeWriter.WriteEndElement();    // source Normals END

                    daeWriter.WriteStartElement("source");  // source TexMap START
                    daeWriter.WriteAttributeString("id", $"{meshName}-map");

                    daeWriter.WriteStartElement("float_array");
                    daeWriter.WriteAttributeString("id", $"{meshName}-map-array");
                    daeWriter.WriteAttributeString("count", $"{texmapList.Count * 2}");
                    foreach (float[] texmapPair in texmapList)
                    {
                        daeWriter.WriteValue(texmapPair[0]);
                        daeWriter.WriteString(" ");
                        daeWriter.WriteValue(texmapPair[1]);
                        daeWriter.WriteString(" ");
                    }
                    daeWriter.WriteEndElement();    // float_array

                    daeWriter.WriteStartElement("technique_common");

                    daeWriter.WriteStartElement("accessor");
                    daeWriter.WriteAttributeString("source", $"#{meshName}-map-array");
                    daeWriter.WriteAttributeString("count", $"{texmapList.Count}");
                    daeWriter.WriteAttributeString("stride", $"{2}");

                    daeWriter.WriteStartElement("param");
                    daeWriter.WriteAttributeString("name", "S");
                    daeWriter.WriteAttributeString("type", "float");
                    daeWriter.WriteEndElement();    // param S
                    daeWriter.WriteStartElement("param");
                    daeWriter.WriteAttributeString("name", "T");
                    daeWriter.WriteAttributeString("type", "float");
                    daeWriter.WriteEndElement();    // param T

                    daeWriter.WriteEndElement();    // accessor

                    daeWriter.WriteEndElement();    // technique_common

                    daeWriter.WriteEndElement();    // source TexMap END

                    daeWriter.WriteStartElement("vertices");
                    daeWriter.WriteAttributeString("id", $"{meshName}-vertices");

                    daeWriter.WriteStartElement("input");
                    daeWriter.WriteAttributeString("semantic", "POSITION");
                    daeWriter.WriteAttributeString("source", $"#{meshName}-positions");
                    daeWriter.WriteEndElement();    // input

                    daeWriter.WriteEndElement();    // vertices

                    daeWriter.WriteStartElement("polylist");
                    //daeWriter.WriteAttributeString("material", $"MATERIAL_NAME_GOES_HERE");
                    daeWriter.WriteAttributeString("count", $"{indexList.Count}");

                    daeWriter.WriteStartElement("input");
                    daeWriter.WriteAttributeString("semantic", "VERTEX");
                    daeWriter.WriteAttributeString("source", $"#{meshName}-vertices");
                    daeWriter.WriteAttributeString("offset", $"{0}");
                    daeWriter.WriteEndElement();    // input VERTEX

                    daeWriter.WriteStartElement("input");
                    daeWriter.WriteAttributeString("semantic", "NORMAL");
                    daeWriter.WriteAttributeString("source", $"#{meshName}-normals");
                    daeWriter.WriteAttributeString("offset", $"{0}");
                    daeWriter.WriteEndElement();    // input NORMAL

                    daeWriter.WriteStartElement("input");
                    daeWriter.WriteAttributeString("semantic", "TEXCOORD");
                    daeWriter.WriteAttributeString("source", $"#{meshName}-map");
                    daeWriter.WriteAttributeString("offset", $"{0}");
                    daeWriter.WriteAttributeString("set", $"{0}");
                    daeWriter.WriteEndElement();    // input TEXCOORD

                    daeWriter.WriteStartElement("vcount");
                    for (int f = 0; f < indexList.Count; ++f)
                    {
                        daeWriter.WriteValue($"{3}");
                        daeWriter.WriteString(" ");
                    }
                    daeWriter.WriteEndElement();    // vcount

                    daeWriter.WriteStartElement("p");
                    foreach (ushort[] indices in indexList)
                    {
                        daeWriter.WriteValue(indices[0]);
                        daeWriter.WriteString(" ");
                        daeWriter.WriteValue(indices[1]);
                        daeWriter.WriteString(" ");
                        daeWriter.WriteValue(indices[2]);
                        daeWriter.WriteString(" ");
                    }
                    daeWriter.WriteEndElement();    // p

                    daeWriter.WriteEndElement();    // polylist

                    daeWriter.WriteEndElement();    // mesh

                    daeWriter.WriteEndElement();    // geometry
                }
                daeWriter.WriteEndElement();    // library_geometry
            }

            // Write scene data
            {
                // Get the scene block
                ScnBlock scn = ScnBlockList.First() as ScnBlock;
                RsiBlock scnResources = scn.Children[0] as RsiBlock;

                daeWriter.WriteStartElement("library_visual_scenes");
                
                daeWriter.WriteStartElement("visual_scene");
                daeWriter.WriteAttributeString("id", $"{scnResources.ResourceStringList[0]}");
                daeWriter.WriteAttributeString("name", $"{scnResources.ResourceStringList[0]}");

                for (int b = 0; b < VtxBlockList.Count(); ++b)
                {
                    VtxBlock vtx = VtxBlockList.ElementAt(b) as VtxBlock;
                    RsiBlock vtxResources = vtx.Children[0] as RsiBlock;
                    MshBlock msh = MshBlockList.ElementAt(b) as MshBlock;
                    RsiBlock mshResources = msh.Children[0] as RsiBlock;

                    daeWriter.WriteStartElement("node");
                    daeWriter.WriteAttributeString("id", $"node_{b}");
                    daeWriter.WriteAttributeString("name", $"node_{b}");
                    daeWriter.WriteAttributeString("type", "NODE");

                    daeWriter.WriteStartElement("matrix");
                    daeWriter.WriteAttributeString("sid", "transform");
                    daeWriter.WriteString("1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1");   // TODO: REPLACE THIS WITH ACTUAL TRANSFORM!!!
                    daeWriter.WriteEndElement();    // matrix

                    daeWriter.WriteStartElement("instance_geometry");
                    daeWriter.WriteAttributeString("url", $"#{mshResources.ResourceStringList[0]}");
                    daeWriter.WriteAttributeString("name", $"{mshResources.ResourceStringList[0]}");
                    daeWriter.WriteEndElement();    // instance_geometry

                    daeWriter.WriteEndElement();    // node
                }

                daeWriter.WriteEndElement();    // visual_scene

                daeWriter.WriteEndElement();    // library_visual_scenes

                daeWriter.WriteStartElement("scene");

                daeWriter.WriteStartElement("instance_visual_scene");
                daeWriter.WriteAttributeString("url", $"#{scnResources.ResourceStringList[0]}");
                daeWriter.WriteEndElement();    // instance_visual_scene

                daeWriter.WriteEndElement();    // scene
            }

            daeWriter.WriteEndElement();
            daeWriter.WriteEndDocument();
            daeWriter.Flush();
            daeWriter.Close();
        }

        private static void ExtractTextures()
        {
            /*
            SrdFile textureSrd;
            string textureSrdName;
            string textureSrdvName;

            // Check if this SRD has an accompanying SRDV file.
            // If not, search for a "_texture" SRD and SRDV file of similar name.
            if (!string.IsNullOrWhiteSpace(SrdvName))
            {
                textureSrd = Srd;
                textureSrdName = SrdName;
                textureSrdvName = SrdvName;
            }
            else
            {
                textureSrdName = SrdName.Remove(SrdName.LastIndexOf(new FileInfo(SrdName).Extension)) + "_texture.srd";
                Console.Write($"There is no accompanying SRDV file for this SRD file, searching for a dedicated \"{new FileInfo(textureSrdName).Name}\"... ");

                if (!File.Exists(textureSrdName))
                {
                    Console.WriteLine("Unable to find any accompanying texture data.");
                    return;
                }
                else
                {
                    Console.WriteLine("Found!");
                }

                textureSrd = new SrdFile();
                textureSrdvName = textureSrdName + 'v';
                textureSrd.Load(textureSrdName, SrdiName, textureSrdvName);
            }
            */

            Console.Write("Would you like to extract mipmaps (Y/N)? ");
            string answer = Console.Read().ToString().ToLowerInvariant();
            while (answer == "y" || answer == "n")
            {
                Console.Write("Would you like to extract mipmaps (Y/N)? ");
                answer = Console.Read().ToString().ToLowerInvariant();
            }
            bool extractMipmaps = (answer == "y");

            SrdFile textureSrd = Srd;
            string textureSrdName = SrdName;
            foreach (Block b in textureSrd.Blocks)
            {
                if (b is TxrBlock txr && b.Children[0] is RsiBlock rsi)
                {
                    // Separate the palette ResourceInfo from the list beforehand if it exists
                    byte[] paletteData = Array.Empty<byte>();
                    if (txr.Palette == 1)
                    {
                        ResourceInfo paletteInfo = rsi.ResourceInfoList[txr.PaletteId];
                        rsi.ResourceInfoList.RemoveAt(txr.PaletteId);
                        paletteData = rsi.ExternalData[txr.PaletteId];
                    }

                    // Read image data based on resource info
                    for (int m = 0; m < (extractMipmaps ? rsi.ResourceInfoList.Count : 1); ++m)
                    {
                        byte[] inputImageData = rsi.ExternalData[m];

                        int dispWidth = txr.DisplayWidth;
                        int dispHeight = txr.DisplayHeight;
                        if (rsi.Unknown12 == 0x08)
                        {
                            dispWidth = (ushort)Utils.PowerOfTwo(dispWidth);
                            dispHeight = (ushort)Utils.PowerOfTwo(dispHeight);
                        }

                        
                        // Determine pixel format
                        PixelDataFormat pixelFormat = PixelDataFormat.Undefined;
                        switch (txr.Format)
                        {
                            case TextureFormat.ARGB8888:
                                pixelFormat = PixelDataFormat.FormatArgb8888;
                                break;

                            case TextureFormat.BGR565:
                                pixelFormat = PixelDataFormat.FormatBgr565;
                                break;

                            case TextureFormat.BGRA4444:
                                pixelFormat = PixelDataFormat.FormatBgra4444;
                                break;

                            case TextureFormat.DXT1RGB:
                                pixelFormat = PixelDataFormat.FormatDXT1Rgb;
                                break;

                            case TextureFormat.DXT5:
                                pixelFormat = PixelDataFormat.FormatDXT5;
                                break;

                            case TextureFormat.BC5:  // RGTC2 / BC5
                                pixelFormat = PixelDataFormat.FormatRGTC2;
                                break;

                            case TextureFormat.BC4:  // RGTC1 / BC4
                                pixelFormat = PixelDataFormat.FormatRGTC1;
                                break;

                            case TextureFormat.Indexed8:
                                pixelFormat = PixelDataFormat.FormatIndexed8;
                                break;

                            case TextureFormat.BPTC:
                                pixelFormat = PixelDataFormat.FormatBPTC;
                                break;
                        }

                        bool swizzled = ((txr.Swizzle & 1) == 0);

                        // TODO: Unswizzle the image
                        if (swizzled)
                            Console.WriteLine("WARNING: This texture is swizzled, meaning it likely came from a console version of the game. These are not supported.");

                        // Calculate mipmap dimensions
                        int mipWidth = (int)Math.Max(1, dispWidth / Math.Pow(2, m));
                        int mipHeight = (int)Math.Max(1, dispHeight / Math.Pow(2, m));

                        // Convert the raw pixel data into something we can actually write to an image file
                        ImageBinary imageBinary = new ImageBinary(mipWidth, mipHeight, pixelFormat, inputImageData);
                        byte[] outputImageData = imageBinary.GetOutputPixelData(0);
                        var image = new Image<Rgba32>(mipWidth, mipHeight);
                        for (int y = 0; y < mipHeight; ++y)
                        {
                            for (int x = 0; x < mipWidth; ++x)
                            {
                                Rgba32 pixelColor;

                                if (pixelFormat == PixelDataFormat.FormatIndexed8)
                                {
                                    int pixelDataOffset = (y * mipWidth) + x;

                                    // Apply the previously-loaded palette
                                    int paletteDataOffset = outputImageData[pixelDataOffset];
                                    pixelColor.B = paletteData[paletteDataOffset + 0];
                                    pixelColor.G = paletteData[paletteDataOffset + 1];
                                    pixelColor.R = paletteData[paletteDataOffset + 2];
                                    pixelColor.A = paletteData[paletteDataOffset + 3];
                                }
                                else
                                {
                                    int pixelDataOffset = ((y * mipWidth) + x) * 4;

                                    byte[] pixelData = new byte[4];
                                    Array.Copy(outputImageData, pixelDataOffset, pixelData, 0, 4);
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

                                image[x, y] = pixelColor;
                            }
                        }
                        

                        string mipmapName = rsi.ResourceStringList.First();
                        string mipmapNameNoExtension = Path.GetFileNameWithoutExtension(mipmapName);
                        string mipmapExtension = Path.GetExtension(mipmapName);

                        // If exporting mipmaps, append the dimensions to the filename
                        if (m > 0)
                            mipmapName = mipmapName.Insert(mipmapNameNoExtension.Length, $" ({mipWidth}x{mipHeight})");

                        // Change file extension to Png if it isn't already
                        if (mipmapExtension != "png")
                            mipmapName += ".png";

                        string outputFolder = new FileInfo(textureSrdName).DirectoryName;
                        using FileStream fs = new FileStream(outputFolder + Path.DirectorySeparatorChar + mipmapName, FileMode.Create);
                        image.SaveAsPng(fs);
                        fs.Flush();
                        image.Dispose();
                    }
                }
            }
        }

        private static void ReplaceTextures()
        {
            //bool generateMipmaps = false;

            // First, load the texture the user wants to insert
            Console.WriteLine("Please type the path or drag and drop the texture file you want to insert, then press ENTER: ");
            string texturePath = Console.ReadLine().Trim('\"');

            while (string.IsNullOrWhiteSpace(texturePath) || !File.Exists(texturePath))
            {
                Console.WriteLine("ERROR: That is not a valid path, please try again.");
                texturePath = Console.ReadLine();
            }

            string textureName = new FileInfo(texturePath).Name;

            using FileStream textureStream = new FileStream(texturePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            Image<Rgba32> texture = Image.Load<Rgba32>(textureStream);
            if (texture == null)
            {
                Console.WriteLine("ERROR: Texture is null.");
                return;
            }

            List<byte> pixelData = new List<byte>();
            for (int y = 0; y < texture.Height; ++y)
            {
                for (int x = 0; x < texture.Width; ++x)
                {
                    pixelData.AddRange(BitConverter.GetBytes(texture[x, y].Rgba));
                }
            }

            ImageBinary imageBinary = new ImageBinary(texture.Width, texture.Height, PixelDataFormat.FormatAbgr8888, pixelData.ToArray());

            foreach (Block b in Srd.Blocks)
            {
                if (b is TxrBlock txr && b.Children[0] is RsiBlock rsi)
                {
                    if (rsi.ResourceStringList[0] == textureName)
                    {
                        rsi.ExternalData.Clear();
                        rsi.ExternalData.Add(imageBinary.GetOutputPixelData(0));

                        rsi.ResourceInfoList.Clear();
                        ResourceInfo info;
                        info.Values = new int[] { 0x40000000, 0, 0, 0 };    // Placeholder, will be properly populated upon saving the SRD
                        rsi.ResourceInfoList.Add(info);

                        txr.Format = TextureFormat.ARGB8888;
                        txr.DisplayWidth = (ushort)texture.Width;
                        txr.DisplayHeight = (ushort)texture.Height;
                        txr.Palette = 0;
                        txr.PaletteId = 0;
                        txr.Scanline = 0;
                        txr.Swizzle = 1;

                        Console.WriteLine($"Replaced texture in TXR block #{Srd.Blocks.IndexOf(txr)}");
                        Console.WriteLine("REMEMBER TO SAVE YOUR CHANGES!!!");
                        return;
                    }
                }
            }

            Console.WriteLine("Unable to find a texture to replace with the specified name.");
        }
    }
}
