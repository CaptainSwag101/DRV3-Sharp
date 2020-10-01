using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using V3Lib.Srd;
using V3Lib.Srd.BlockTypes;
using Assimp;

namespace SrdTool
{
    static class ImportExportHelper
    {
        private struct MeshData
        {
            public readonly List<Vector3> Vertices;
            public readonly List<Vector3> Normals;
            public readonly List<Vector2> Texcoords;
            public readonly List<ushort[]> Indices;
            public readonly List<string> Bones;
            public readonly List<float> Weights;

            public MeshData(List<Vector3> vertices,
                List<Vector3> normals,
                List<Vector2> texcoords,
                List<ushort[]> indices,
                List<string> bones,
                List<float> weights)
            {
                Vertices = vertices;
                Normals = normals;
                Texcoords = texcoords;
                Indices = indices;
                Bones = bones;
                Weights = weights;
            }
        }

        public static void ExportModel(SrdFile srd, string exportName)
        {
            // Setup the scene for the root of our model
            Scene scene = new Scene();

            // Get all our various data
            scene.Materials.AddRange(GetMaterials(srd));
            scene.Meshes.AddRange(GetMeshes(srd, scene.Materials));
            scene.RootNode = GetNodeTree(srd, scene.Materials, scene.Meshes);

            AssimpContext exportContext = new AssimpContext();
            var exportFormats = exportContext.GetSupportedExportFormats();
            foreach (var format in exportFormats)
            {
                if (format.FileExtension == "gltf")
                {
                    exportContext.ExportFile(scene, exportName + '.' + format.FileExtension, format.FormatId);
                    break;
                }
            }
        }

        private static Node GetNodeTree(SrdFile srd, List<Material> materials, List<Mesh> meshes)
        {
            var scn = srd.Blocks.Where(b => b is ScnBlock).First() as ScnBlock;
            var scnResources = scn.Children[0] as RsiBlock;
            var treBlocks = srd.Blocks.Where(b => b is TreBlock).ToList();

            // Create a manual root node in case the scene has multiple roots
            Node RootNode = new Node("RootNode");

            foreach (TreBlock tre in treBlocks)
            {
                var treResources = tre.Children[0] as RsiBlock;

                var flattenedTreeNodes = tre.RootNode.Flatten().ToList();

                // This is done in two passes, one to generate the list of nodes, second to assign children to parents
                var flattenedNodes = new List<Node>();

                // Generate a list of real nodes in the scene
                foreach (TreeNode treeNode in flattenedTreeNodes)
                {
                    Node n = new Node(treeNode.StringValue);

                    foreach (Mesh mesh in meshes)
                    {
                        if (mesh.Name == n.Name)
                            n.MeshIndices.Add(meshes.IndexOf(mesh));
                    }

                    flattenedNodes.Add(n);
                    //nodeNameList.Add(n.Name);
                }
                foreach (TreeNode treeNode in flattenedTreeNodes)
                {
                    int currentNodeIndex = flattenedTreeNodes.IndexOf(treeNode);

                    foreach (TreeNode childTreeNode in treeNode)
                    {
                        int childNodeIndex = flattenedTreeNodes.IndexOf(childTreeNode);
                        flattenedNodes[currentNodeIndex].Children.Add(flattenedNodes[childNodeIndex]);
                    }
                }

                // Add any root nodes to the manual root node
                foreach (string rootNodeName in scn.SceneRootNodes)
                {
                    foreach (Node matchingNode in flattenedNodes.Where(n => n.Name == rootNodeName))
                    {
                        RootNode.Children.Add(matchingNode);
                    }
                }
            }

            return RootNode;
        }

        private static List<Mesh> GetMeshes(SrdFile srd, List<Material> materials)
        {
            var meshList = new List<Mesh>();

            //var treBlocks = srd.Blocks.Where(b => b is TreBlock).ToList();
            var sklBlocks = srd.Blocks.Where(b => b is SklBlock).ToList();
            var mshBlocks = srd.Blocks.Where(b => b is MshBlock).ToList();
            var vtxBlocks = srd.Blocks.Where(b => b is VtxBlock).ToList();

            // For debugging
            //var vtxNameList = new List<string>();
            //var mshNameList = new List<string>();
            //foreach (VtxBlock vtx in vtxBlocks)
            //{
            //    vtxNameList.Add((vtx.Children[0] as RsiBlock).ResourceStringList[0]);
            //}
            //foreach (MshBlock msh in mshBlocks)
            //{
            //    mshNameList.Add((msh.Children[0] as RsiBlock).ResourceStringList[0]);
            //}

            // Iterate through each VTX block simultaneously and extract the data we need
            var extractedData = new List<MeshData>();
            foreach (MshBlock msh in mshBlocks)
            {
                var vtx = vtxBlocks.Where(b => (b.Children[0] as RsiBlock).ResourceStringList[0] == msh.VertexBlockName).First() as VtxBlock;
                var vtxResources = vtx.Children[0] as RsiBlock;

                // Extract position data
                using BinaryReader positionReader = new BinaryReader(new MemoryStream(vtxResources.ExternalData[0]));
                var curVertexList = new List<Vector3>();
                var curNormalList = new List<Vector3>();
                var curTexcoordList = new List<Vector2>();
                var curWeightList = new List<float>();

                foreach (var section in vtx.VertexDataSections)
                {
                    positionReader.BaseStream.Seek(section.StartOffset, SeekOrigin.Begin);
                    for (int vNum = 0; vNum < vtx.VertexCount; ++vNum)
                    {
                        long oldPos = positionReader.BaseStream.Position;
                        switch (vtx.VertexDataSections.IndexOf(section))
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

                                    if (vtx.VertexDataSections.Count == 1)
                                    {
                                        Console.WriteLine($"Mesh type: {vtx.MeshType}");
                                        Vector2 texcoord;
                                        texcoord.X = float.PositiveInfinity;
                                        texcoord.X = positionReader.ReadSingle();           // U
                                        while (float.IsNaN(texcoord.X) || !float.IsFinite(texcoord.X))
                                            texcoord.X = positionReader.ReadSingle();
                                        texcoord.Y = positionReader.ReadSingle();           // V, invert for non-glTF exports
                                        while (float.IsNaN(texcoord.Y) || !float.IsFinite(texcoord.Y))
                                            texcoord.Y = positionReader.ReadSingle();

                                        if (float.IsNaN(texcoord.X) || float.IsNaN(texcoord.Y) || Math.Abs(texcoord.X) > 1 || Math.Abs(texcoord.Y) > 1)
                                        {
                                            Console.WriteLine($"INVALID UVs DETECTED!");
                                        }
                                        curTexcoordList.Add(texcoord);
                                    }
                                }
                                break;

                            case 1: // Bone weights?
                                {
                                    var weightsPerVert = (section.SizePerVertex / sizeof(float));   // TODO: Is this always 8?
                                    for (int wNum = 0; wNum < weightsPerVert; ++wNum)
                                    {
                                        curWeightList.Add(positionReader.ReadSingle());
                                    }
                                }
                                break;

                            case 2: // Texture UVs (only for models with bones)
                                {
                                    Vector2 texcoord;
                                    texcoord.X = positionReader.ReadSingle();               // U
                                    texcoord.Y = positionReader.ReadSingle();               // V, invert for non-glTF exports
                                    curTexcoordList.Add(texcoord);
                                }
                                break;

                            default:
                                Console.WriteLine($"WARNING: Unknown vertex sub-block index {vtx.VertexDataSections.IndexOf(section)} is present in VTX block {vtxBlocks.IndexOf(vtx)}!");
                                break;
                        }

                        // Skip data we don't currently use, though I may add support for this data later
                        long remainingBytes = section.SizePerVertex - (positionReader.BaseStream.Position - oldPos);
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
                        // We need to reverse the order of the indices to prevent the normals
                        // from becoming permanently flipped due to the clockwise/counter-clockwise
                        // order of the indices determining the face's direction
                        indices[3 - (i + 1)] = index;
                    }
                    curIndexList.Add(indices);
                }

                // Add the extracted data to our list
                extractedData.Add(new MeshData(curVertexList, curNormalList, curTexcoordList, curIndexList, vtx.BindBoneList, curWeightList));
            }

            // Now that we've extracted the data we need, convert it to Assimp equivalents
            for (int d = 0; d < extractedData.Count; ++d)
            {
                MeshData meshData = extractedData[d];
                var msh = mshBlocks[d] as MshBlock;
                var mshResources = msh.Children[0] as RsiBlock;

                Mesh mesh = new Mesh()
                {
                    Name = mshResources.ResourceStringList[0],
                    PrimitiveType = PrimitiveType.Triangle,
                    MaterialIndex = materials.IndexOf(materials.Where(m => m.Name == msh.MaterialName).First()),
                };

                // Add vertices
                foreach (var vertex in meshData.Vertices)
                {
                    Vector3D vec3D = new Vector3D(vertex.X, vertex.Y, vertex.Z);
                    mesh.Vertices.Add(vec3D);
                }

                // Add normals
                foreach (var normal in meshData.Normals)
                {
                    Vector3D vec3D = new Vector3D(normal.X, normal.Y, normal.Z);
                    mesh.Normals.Add(vec3D);
                }

                // Add UVs
                mesh.UVComponentCount[0] = 2;
                mesh.TextureCoordinateChannels[0] = new List<Vector3D>();
                foreach (var uv in meshData.Texcoords)
                {
                    Vector3D vec3D = new Vector3D(uv.X, uv.Y, 0.0f);
                    mesh.TextureCoordinateChannels[0].Add(vec3D);
                }

                // Add faces
                foreach (var indexArray in meshData.Indices)
                {
                    Face face = new Face();

                    foreach (ushort index in indexArray)
                    {
                        face.Indices.Add(index);
                    }

                    mesh.Faces.Add(face);
                }

                // Add bones
                foreach (string boneName in meshData.Bones)
                {
                    var boneInfoList = (sklBlocks.First() as SklBlock).BoneInfoList;

                    var matchingBone = boneInfoList.Where(b => b.BoneName == boneName).First();

                    Bone bone = new Bone();
                    bone.Name = boneName;

                    mesh.Bones.Add(bone);
                }

                // Add weights to those bones
                int weightsPerVert = (meshData.Weights.Count / meshData.Vertices.Count);
                for (int vNum = 0; vNum < meshData.Vertices.Count; ++vNum)
                {
                    for (int wNum = 0; wNum < weightsPerVert; ++wNum)
                    {
                        // Make sure the bone actually exists
                        if (mesh.BoneCount <= (wNum % weightsPerVert))
                            break;

                        VertexWeight vWeight;
                        vWeight.VertexID = vNum;
                        vWeight.Weight = meshData.Weights[wNum + (vNum * weightsPerVert)];
                        mesh.Bones[wNum % weightsPerVert].VertexWeights.Add(vWeight);
                    }
                }

                meshList.Add(mesh);
            }

            return meshList;
        }

        private static List<Material> GetMaterials(SrdFile srd)
        {
            var materialList = new List<Material>();

            var matBlocks = srd.Blocks.Where(b => b is MatBlock).ToList();
            var txiBlocks = srd.Blocks.Where(b => b is TxiBlock).ToList();

            foreach (MatBlock mat in matBlocks)
            {
                var matResources = mat.Children[0] as RsiBlock;

                Material material = new Material();
                material.Name = matResources.ResourceStringList[0];

                foreach (var pair in mat.MapTexturePairs)
                {
                    // Find the TXI block associated with the current map
                    TxiBlock matchingTxi = new TxiBlock();
                    foreach (TxiBlock txi in txiBlocks)
                    {
                        var txiResources = txi.Children[0] as RsiBlock;
                        if (txiResources.ResourceStringList[0] == pair.Value)
                        {
                            matchingTxi = txi;
                            break;
                        }
                    }

                    TextureSlot texSlot = new TextureSlot
                    {
                        FilePath = matchingTxi.TextureFilename,
                        Mapping = TextureMapping.FromUV,
                        UVIndex = 0,
                    };

                    // Determine map type
                    if (pair.Key.StartsWith("COLORMAP"))
                    {
                        if (matchingTxi.TextureFilename.StartsWith("lm"))
                            texSlot.TextureType = TextureType.Lightmap;
                        else
                            texSlot.TextureType = TextureType.Diffuse;
                    }
                    else if (pair.Key.StartsWith("NORMALMAP"))
                    {
                        texSlot.TextureType = TextureType.Normals;
                    }
                    else if (pair.Key.StartsWith("SPECULARMAP"))
                    {
                        texSlot.TextureType = TextureType.Specular;
                    }
                    else if (pair.Key.StartsWith("TRANSPARENCYMAP"))
                    {
                        texSlot.TextureType = TextureType.Opacity;
                    }
                    else if (pair.Key.StartsWith("REFLECTMAP"))
                    {
                        texSlot.TextureType = TextureType.Reflection;
                    }
                    else
                    {
                        Console.WriteLine($"WARNING: Texture map type {pair.Key} is not currently supported.");
                    }
                    texSlot.TextureIndex = material.GetMaterialTextureCount(texSlot.TextureType);

                    if (!material.AddMaterialTexture(texSlot))
                        Console.WriteLine($"WARNING: Adding map ({pair.Key}, {pair.Value}) did not update or create new data!");
                }

                materialList.Add(material);
            }

            return materialList;
        }
    }
}
