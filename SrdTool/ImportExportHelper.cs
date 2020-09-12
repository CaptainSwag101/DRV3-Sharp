using glTFLoader.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using V3Lib.Srd;
using V3Lib.Srd.BlockTypes;
using Buffer = glTFLoader.Schema.Buffer;

namespace SrdTool
{
    static class ImportExportHelper
    {
        public static Gltf SrdModelToGltf(SrdFile srd, string srdName)
        {
            // Export the vertices and faces as a glTF
            Gltf gltf = new Gltf();
            GenerateGltfAsset(gltf, srd);
            GenerateGltfMainData(gltf, srd, srdName);
            GenerateGltfSceneTree(gltf, srd);

            return gltf;
        }

        private static void GenerateGltfAsset(Gltf gltf, SrdFile srd)
        {
            Asset asset = new Asset
            {
                Generator = "SrdTool",
                Version = "2.0",
                //MinVersion = "2.0"
            };

            gltf.Asset = asset;
        }

        private static void GenerateGltfMainData(Gltf gltf, SrdFile srd, string srdName)
        {
            // I really wish we could do this as smaller bite-sized functions but unfortunately that's not an option,
            // in order to keep track of all the cross-referenced indices we must do it all in one large monolithic function
            var accessors = new List<Accessor>();
            var meshes = new List<Mesh>();

            // Generate temporary Lists of each kind of block we need to keep track of
            var mshBlocks = srd.Blocks.Where(b => b is MshBlock).ToList();
            var vtxBlocks = srd.Blocks.Where(b => b is VtxBlock).ToList();

            if (vtxBlocks.Count != mshBlocks.Count)
            {
                Console.WriteLine("WARNING: Vertex and Mesh block counts are not equal!");
            }

            // Iterate through each VTX block simultaneously and extract the data we need
            var extractedData = new List<(List<Vector3> Vertices, List<Vector3> Normals, List<Vector2> Texcoords, List<ushort[]> Indices)>();
            foreach (VtxBlock vtx in vtxBlocks)
            {
                RsiBlock vtxResources = vtx.Children[0] as RsiBlock;

                // Extract position data
                using BinaryReader positionReader = new BinaryReader(new MemoryStream(vtxResources.ExternalData[0]));
                var curVertexList = new List<Vector3>();
                var curNormalList = new List<Vector3>();
                var curTexcoordList = new List<Vector2>();

                foreach (var subBlock in vtx.VertexSubBlockList)
                {
                    positionReader.BaseStream.Seek(subBlock.Offset, SeekOrigin.Begin);
                    for (int vNum = 0; vNum < vtx.VertexCount; ++vNum)
                    {
                        long oldPos = positionReader.BaseStream.Position;
                        switch (vtx.VertexSubBlockList.IndexOf(subBlock))
                        {
                            case 0: // Vertex/Normal data (and Texture UV for boneless models)
                                {
                                    Vector3 vertex;
                                    vertex.X = positionReader.ReadSingle() * -1.0f;         // X
                                    vertex.Y = positionReader.ReadSingle();                 // Y
                                    vertex.Z = positionReader.ReadSingle();                 // Z
                                    curVertexList.Add(vertex);

                                    Vector3 normal;
                                    normal.X = positionReader.ReadSingle() * -1.0f;         // X
                                    normal.Y = positionReader.ReadSingle();                 // Y
                                    normal.Z = positionReader.ReadSingle();                 // Z
                                    curNormalList.Add(normal);

                                    if (vtx.VertexSubBlockList.Count == 1)
                                    {
                                        Vector2 texcoord;
                                        texcoord.X = positionReader.ReadSingle();           // U
                                        texcoord.Y = positionReader.ReadSingle() * -1.0f;   // V
                                        curTexcoordList.Add(texcoord);
                                    }
                                }
                                break;

                            case 1: // Bone weights
                                {
                                    // TODO!!!
                                }
                                break;

                            case 2: // Texture UVs (only for models with bones)
                                {
                                    Vector2 texcoord;
                                    texcoord.X = positionReader.ReadSingle();               // U
                                    texcoord.Y = positionReader.ReadSingle() * -1.0f;       // V
                                    curTexcoordList.Add(texcoord);
                                }
                                break;
                        }

                        // Skip data we don't currently use, though I may add support for this data later
                        long remainingBytes = subBlock.Size - (positionReader.BaseStream.Position - oldPos);
                        positionReader.BaseStream.Seek(remainingBytes, SeekOrigin.Current);
                    }
                }

                // Extract index data
                using BinaryReader indexReader = new BinaryReader(new MemoryStream(vtxResources.ExternalData[1]));
                var curIndexList = new List<ushort[]>();
                while (indexReader.BaseStream.Position < indexReader.BaseStream.Length)
                {
                    ushort[] indices = new ushort[3];
                    for (int i = 0; i < 3; ++i)
                    {
                        ushort index = indexReader.ReadUInt16();
                        indices[i] = index;
                    }
                    curIndexList.Add(indices);
                }

                // Add the extracted data to our list
                extractedData.Add((curVertexList, curNormalList, curTexcoordList, curIndexList));
            }

            // Now that we've extracted the data, parse it and generate our accessors and meshes
            using BinaryWriter dataWriter = new BinaryWriter(new FileStream(srdName + "_data.bin", FileMode.Create, FileAccess.Write, FileShare.Read));
            for (int i = 0; i < extractedData.Count; ++i)
            {
                var (Vertices, Normals, Texcoords, Indices) = extractedData[i];

                VtxBlock vtx = vtxBlocks[i] as VtxBlock;
                RsiBlock vtxResources = vtx.Children[0] as RsiBlock;
                MshBlock msh = mshBlocks[i] as MshBlock;
                RsiBlock mshResources = msh.Children[0] as RsiBlock;

                // Process positions
                Accessor positionAccessor = new Accessor
                {
                    BufferView = 0,     // vertexView
                    ByteOffset = (int)dataWriter.BaseStream.Position,
                    Count = Vertices.Count,
                    ComponentType = Accessor.ComponentTypeEnum.FLOAT,
                    Type = Accessor.TypeEnum.VEC3,
                    Name = msh.MeshName + "_positionAccessor"
                };
                var minPositions = new float[3];
                var maxPositions = new float[3];
                foreach (Vector3 vert in Vertices)
                {
                    dataWriter.Write(vert.X);
                    dataWriter.Write(vert.Y);
                    dataWriter.Write(vert.Z);

                    // Update min/max
                    if (vert.X < minPositions[0])
                        minPositions[0] = vert.X;
                    if (vert.X > maxPositions[0])
                        maxPositions[0] = vert.X;

                    if (vert.Y < minPositions[1])
                        minPositions[1] = vert.Y;
                    if (vert.Y > maxPositions[1])
                        maxPositions[1] = vert.Y;

                    if (vert.Z < minPositions[2])
                        minPositions[2] = vert.Z;
                    if (vert.Z > maxPositions[2])
                        maxPositions[2] = vert.Z;
                }
                positionAccessor.Min = minPositions;
                positionAccessor.Max = maxPositions;
                accessors.Add(positionAccessor);

                // Process normals
                Accessor normalAccessor = new Accessor
                {
                    BufferView = 0,
                    ByteOffset = (int)dataWriter.BaseStream.Position,
                    Count = Normals.Count,
                    ComponentType = Accessor.ComponentTypeEnum.FLOAT,
                    Type = Accessor.TypeEnum.VEC3,
                    Name = msh.MeshName + "_normalAccessor"
                };
                var minNormals = new float[3];
                var maxNormals = new float[3];
                foreach (Vector3 norm in Normals)
                {
                    dataWriter.Write(norm.X);
                    dataWriter.Write(norm.Y);
                    dataWriter.Write(norm.Z);

                    // Update min/max
                    if (norm.X < minNormals[0])
                        minNormals[0] = norm.X;
                    if (norm.X > maxNormals[0])
                        maxNormals[0] = norm.X;

                    if (norm.Y < minNormals[1])
                        minNormals[1] = norm.Y;
                    if (norm.Y > maxNormals[1])
                        maxNormals[1] = norm.Y;

                    if (norm.Z < minNormals[2])
                        minNormals[2] = norm.Z;
                    if (norm.Z > maxNormals[2])
                        maxNormals[2] = norm.Z;
                }
                normalAccessor.Min = minNormals;
                normalAccessor.Max = maxNormals;
                accessors.Add(normalAccessor);

                // Process texcoords
                Accessor texcoordAccessor = new Accessor
                {
                    BufferView = 0,
                    ByteOffset = (int)dataWriter.BaseStream.Position,
                    Count = Texcoords.Count,
                    ComponentType = Accessor.ComponentTypeEnum.FLOAT,
                    Type = Accessor.TypeEnum.VEC2,
                    Name = msh.MeshName + "_texcoordAccessor"
                };
                var minTexcoords = new float[2];
                var maxTexcoords = new float[2];
                foreach (Vector2 uv in Texcoords)
                {
                    dataWriter.Write(uv.X);
                    dataWriter.Write(uv.Y);

                    // Update min/max
                    if (uv.X < minTexcoords[0])
                        minTexcoords[0] = uv.X;
                    if (uv.X > maxTexcoords[0])
                        maxTexcoords[0] = uv.X;

                    if (uv.Y < minTexcoords[1])
                        minTexcoords[1] = uv.Y;
                    if (uv.Y > maxTexcoords[1])
                        maxTexcoords[1] = uv.Y;
                }
                texcoordAccessor.Min = minTexcoords;
                texcoordAccessor.Max = maxTexcoords;
                accessors.Add(texcoordAccessor);


                // Process indices
                Accessor indexAccessor = new Accessor
                {
                    BufferView = 0,
                    ByteOffset = (int)dataWriter.BaseStream.Position,
                    Count = Indices.Count * 3,
                    ComponentType = Accessor.ComponentTypeEnum.UNSIGNED_SHORT,
                    Type = Accessor.TypeEnum.SCALAR,
                    Name = msh.MeshName + "_indexAccessor"
                };
                foreach (ushort[] triplet in Indices)
                {
                    dataWriter.Write(triplet[0]);
                    dataWriter.Write(triplet[1]);
                    dataWriter.Write(triplet[2]);
                }
                accessors.Add(indexAccessor);


                // Generate a mesh and its associated primitives
                MeshPrimitive prim = new MeshPrimitive()
                {
                    Indices = accessors.IndexOf(indexAccessor),
                    Attributes = new Dictionary<string, int>()
                    {
                        { "POSITION", accessors.IndexOf(positionAccessor) },
                        { "NORMAL", accessors.IndexOf(normalAccessor) },
                        { "TEXCOORD_0", accessors.IndexOf(texcoordAccessor) },
                    },
                    Mode = MeshPrimitive.ModeEnum.TRIANGLES,
                };
                Mesh mesh = new Mesh()
                {
                    Name = msh.MeshName,
                    Primitives = new MeshPrimitive[] { prim, },
                };
                meshes.Add(mesh);
            }

            // Generate data buffer
            string uriName = new FileInfo((dataWriter.BaseStream as FileStream).Name).Name;
            Buffer dataBuffer = new Buffer();
            dataBuffer.ByteLength = (int)dataWriter.BaseStream.Position;
            dataBuffer.Name = "dataBuffer";
            dataBuffer.Uri = uriName;

            // Generate buffer views
            BufferView vertexView = new BufferView()
            {
                Name = "vertexView",
                Buffer = 0,
                ByteLength = dataBuffer.ByteLength,
                ByteOffset = 0,
                //ByteStride = ((sizeof(float) * 8) + (sizeof(ushort) * 3)),
            };

            gltf.Buffers = new Buffer[] { dataBuffer };
            gltf.BufferViews = new BufferView[] { vertexView };
            gltf.Accessors = accessors.ToArray();
            gltf.Meshes = meshes.ToArray();
        }

        private static void GenerateGltfSceneTree(Gltf gltf, SrdFile srd)
        {
            // Parse node and scene data
            ScnBlock scn = srd.Blocks.Where(scn => scn is ScnBlock).First() as ScnBlock;
            RsiBlock scnResources = scn.Children[0] as RsiBlock;
            TreBlock tre = srd.Blocks.Where(tre => tre is TreBlock).First() as TreBlock;
            RsiBlock treResources = tre.Children[0] as RsiBlock;

            // Add nodes and anything attached to them

            // ToList() is VERY important for performance, it creates a copy of the flattened nodes
            // whereas it would otherwise need to re-flatten every time we try to enumerate it.
            var flattenedNodes = tre.RootNode.Flatten().ToList();
            gltf.Nodes = new Node[flattenedNodes.Count()];
            var usedMeshes = new List<Mesh>();
            for (int tn = 0; tn < flattenedNodes.Count(); ++tn)
            {
                Node node = new Node();
                node.Name = flattenedNodes.ElementAt(tn).StringValue;
                // Find child nodes
                var children = new List<int>();
                for (int tnc = 0; tnc < flattenedNodes.Count(); ++tnc)
                {
                    if (flattenedNodes.ElementAt(tn).Contains(flattenedNodes.ElementAt(tnc)))
                    {
                        children.Add(tnc);
                    }
                }

                if (children.Count > 0)
                    node.Children = children.ToArray();

                // See if the node has any data attached to it, such as meshes, etc.
                for (int m = 0; m < gltf.Meshes.Length; ++m)
                {
                    Mesh curMesh = gltf.Meshes[m];
                    string last = curMesh.Name.Split(':').Last();
                    if (node.Name.EndsWith(last) && !usedMeshes.Contains(curMesh))
                    {
                        node.Mesh = m;
                        usedMeshes.Add(curMesh);
                        break;
                    }
                }

                gltf.Nodes[tn] = node;
            }

            // Add scene
            var nodeArray = new int[] { 0 };
            Scene scene = new Scene()
            {
                Name = scnResources.ResourceStringList[0],
                Nodes = nodeArray,
            };
            gltf.Scenes = new Scene[] { scene };
            gltf.Scene = 0;
        }

    }
}
