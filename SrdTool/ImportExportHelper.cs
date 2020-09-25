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

            // This warning is unnecessary
            //if (vtxBlocks.Count != mshBlocks.Count)
            //{
            //    Console.WriteLine("WARNING: Vertex and Mesh block counts are not equal!");
            //}

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
            var extractedData = new List<(List<Vector3> Vertices, List<Vector3> Normals, List<Vector2> Texcoords, List<ushort[]> Indices, List<string> Bones, List<float> Weights)>();
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
                                        texcoord.Y = positionReader.ReadSingle();           // V, invert for non-glTF exports
                                        curTexcoordList.Add(texcoord);
                                    }
                                }
                                break;

                            case 1: // Bone weights?
                                {
                                    //while (positionReader.BaseStream.Position < subBlock.Offset + subBlock.Size)
                                    //    curWeightList.Add(positionReader.ReadSingle());
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
                                throw new NotImplementedException();
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
                        // We need to reverse the order of the indices to prevent the normals
                        // from becoming permanently flipped due to the clockwise/counter-clockwise
                        // order of the indices determining the face's direction
                        indices[3 - (i + 1)] = index;
                    }
                    curIndexList.Add(indices);
                }

                // Add the extracted data to our list
                extractedData.Add((curVertexList, curNormalList, curTexcoordList, curIndexList, vtx.BindBoneList, curWeightList));
            }

            // Now that we've extracted the data we need, convert it to Assimp equivalents
            for (int d = 0; d < extractedData.Count; ++d)
            {
                var (Vertices, Normals, Texcoords, Indices, Bones, Weights) = extractedData[d];
                var msh = mshBlocks[d] as MshBlock;
                var mshResources = msh.Children[0] as RsiBlock;

                Mesh mesh = new Mesh()
                {
                    Name = mshResources.ResourceStringList[0],
                    PrimitiveType = PrimitiveType.Triangle,
                    MaterialIndex = materials.IndexOf(materials.Where(m => m.Name == msh.MaterialName).First()),
                };

                // Add vertices
                foreach (var vertex in Vertices)
                {
                    Vector3D vec3D = new Vector3D(vertex.X, vertex.Y, vertex.Z);
                    mesh.Vertices.Add(vec3D);
                }

                // Add normals
                foreach (var normal in Normals)
                {
                    Vector3D vec3D = new Vector3D(normal.X, normal.Y, normal.Z);
                    mesh.Normals.Add(vec3D);
                }

                // Add UVs
                mesh.UVComponentCount[0] = 2;
                mesh.TextureCoordinateChannels[0] = new List<Vector3D>();
                foreach (var uv in Texcoords)
                {
                    Vector3D vec3D = new Vector3D(uv.X, uv.Y, 0.0f);
                    mesh.TextureCoordinateChannels[0].Add(vec3D);
                }

                // Add faces
                foreach (var indexArray in Indices)
                {
                    Face face = new Face();

                    foreach (ushort index in indexArray)
                    {
                        face.Indices.Add(index);
                    }

                    mesh.Faces.Add(face);
                }

                // Add bones
                /*
                foreach (string boneName in Bones)
                {
                    var boneInfoList = (sklBlocks.First() as SklBlock).BoneInfoList;

                    var matchingBone = boneInfoList.Where(b => b.BoneName == boneName).First();

                    Bone bone = new Bone();
                    bone.Name = boneName;

                    Assimp.Matrix4x4 offset;
                    offset.A1 = matchingBone.Matrix1[0][0];
                    offset.A2 = matchingBone.Matrix1[0][1];
                    offset.A3 = matchingBone.Matrix1[0][2];
                    offset.A4 = 0.0f;
                    offset.B1 = matchingBone.Matrix1[1][0];
                    offset.B2 = matchingBone.Matrix1[1][1];
                    offset.B3 = matchingBone.Matrix1[1][2];
                    offset.B4 = 0.0f;
                    offset.C1 = matchingBone.Matrix1[2][0];
                    offset.C2 = matchingBone.Matrix1[2][1];
                    offset.C3 = matchingBone.Matrix1[2][2];
                    offset.C4 = 0.0f;
                    offset.D1 = matchingBone.Matrix1[3][0];
                    offset.D2 = matchingBone.Matrix1[3][1];
                    offset.D3 = matchingBone.Matrix1[3][2];
                    offset.D4 = 1.0f;
                    bone.OffsetMatrix = offset;

                    // Add weights to bone
                    foreach (float w in Weights)
                    {
                        VertexWeight vWeight;
                        vWeight.VertexID = Weights.IndexOf(w);
                        vWeight.Weight = w;
                        bone.VertexWeights.Add(vWeight);
                    }

                    mesh.Bones.Add(bone);
                }
                */

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
