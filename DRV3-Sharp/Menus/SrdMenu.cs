using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assimp;
using DRV3_Sharp_Library.Formats.Data.SRD;
using DRV3_Sharp_Library.Formats.Data.SRD.Resources;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tga;
using AssimpNode = Assimp.Node;
using Node = DRV3_Sharp_Library.Formats.Data.SRD.Blocks.Node;

namespace DRV3_Sharp.Menus;

internal sealed class SrdMenu : IMenu
{
    private SrdData? loadedData = null;
    private FileInfo? loadedDataInfo = null;
    public string HeaderText => "You can choose from the following options:";
    public int FocusedEntry { get; set; }

    public MenuEntry[] AvailableEntries
    {
        get
        {
            List<MenuEntry> entries = new()
            {
                // Add always-available entries
                new("Load", "Load an SRD file, pulling data from associated SRDI and SRDV binary files.", Load),
                new("Help", "View descriptions of currently-available operations.", Help),
                new("Back", "Return to the previous menu.", Program.PopMenu)
            };

            if (loadedData is not null)
            {
                // Add loaded-data specific entries
                entries.Insert(0, new("Export Textures", "Export all texture resources within the resource data.", ExtractTextures));
                entries.Insert(1, new("Export Models", "Export all 3D geometry within the resource data.", ExtractModels));
            }

            return entries.ToArray();
        }
    }

    private void Load()
    {
        var paths = Utils.ParsePathsFromConsole("Type the file you wish to load, or drag-and-drop it onto this window: ", true, false);
        if (paths?[0] is not FileInfo fileInfo)
        {
            Console.Write("Unable to find the path specified.");
            Utils.PromptForEnterKey(false);
            return;
        }
        
        // TODO: Check with the user if there are existing loaded files before clearing the list
        loadedData = null;
        loadedDataInfo = null;
        
        // Determine the expected paths of the accompanying SRDI and SRDV files.
        int lengthNoExtension = (fileInfo.FullName.Length - fileInfo.Extension.Length);
        string noExtension = fileInfo.FullName[..lengthNoExtension];
        FileInfo srdiInfo = new(noExtension + ".srdi");
        FileInfo srdvInfo = new(noExtension + ".srdv");
        
        // Initialize appropriate FileStreams based on which files exist.
        FileStream fs = new(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        FileStream? srdi = null;
        if (srdiInfo.Exists) srdi = new FileStream(srdiInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        FileStream? srdv = null;
        if (srdvInfo.Exists) srdv = new FileStream(srdvInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        
        // Deserialize the SRD data with available resource streams
        SrdSerializer.Deserialize(fs, srdi, srdv, out SrdData data);
        loadedData = data;
        loadedDataInfo = fileInfo;
            
        Console.Write($"Loaded the SRD file successfully.");
        Utils.PromptForEnterKey(false);
    }

    private void ExtractTextures()
    {
        if (loadedData is null || loadedDataInfo is null) return;

        var successfulExports = 0;
        foreach (var resource in loadedData!.Resources)
        {
            if (resource is not TextureResource texture) continue;
            
            string outputPath = Path.Combine(loadedDataInfo.DirectoryName!, texture.Name);
            using FileStream fs = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            if (texture.Name.EndsWith(".tga"))
            {
                texture.ImageMipmaps[0].Save(fs, new TgaEncoder());
            }
            else if (texture.Name.EndsWith(".bmp"))
            {
                texture.ImageMipmaps[0].Save(fs, new BmpEncoder());
            }
            else if (texture.Name.EndsWith(".png"))
            {
                texture.ImageMipmaps[0].Save(fs, new PngEncoder());
            }
            ++successfulExports;
        }
        
        Console.Write($"Exported {successfulExports} textures successfully.");
        Utils.PromptForEnterKey(false);
    }

    private void ExtractModels()
    {
        // Loop through the various resources in the SRD and construct an Assimp scene
        // and associated meshes, trees, etc.
        
        // First, let's populate some lists of the various resources we will use.
        List<MaterialResource> materialResources = new();
        List<MeshResource> meshResources = new();
        List<TextureInstanceResource> textureInstanceResources = new();
        List<VertexResource> vertexResources = new();
        foreach (var resource in loadedData!.Resources)
        {
            switch (resource)
            {
                case MaterialResource mat:
                    materialResources.Add(mat);
                    break;
                case MeshResource mesh:
                    meshResources.Add(mesh);
                    break;
                case TextureInstanceResource txi:
                    textureInstanceResources.Add(txi);
                    break;
                case VertexResource vertex:
                    vertexResources.Add(vertex);
                    break;
            }
        }
        
        // Second, let's generate our material data from MAT and TXI resources.
        List<Material> constructedMaterials = new();
        foreach (MaterialResource materialResource in materialResources)
        {
            Material constructedMaterial = new();
            constructedMaterial.Name = materialResource.Name;
            
            foreach ((string mapName, string textureName) in materialResource.MapTexturePairs)
            {
                // Find the TXI resource associated with the current map
                var matchingTexture = textureInstanceResources.First(txi => txi.LinkedMaterialName == textureName);

                TextureSlot texSlot = new()
                {
                    FilePath = matchingTexture.LinkedTextureName,
                    Mapping = TextureMapping.FromUV,
                    UVIndex = 0,
                };

                // Determine map type
                if (mapName.StartsWith("COLORMAP"))
                {
                    if (matchingTexture.LinkedTextureName.StartsWith("lm"))
                        texSlot.TextureType = TextureType.Lightmap;
                    else
                        texSlot.TextureType = TextureType.Diffuse;
                }
                else if (mapName.StartsWith("NORMALMAP"))
                {
                    texSlot.TextureType = TextureType.Normals;
                }
                else if (mapName.StartsWith("SPECULARMAP"))
                {
                    texSlot.TextureType = TextureType.Specular;
                }
                else if (mapName.StartsWith("TRANSPARENCYMAP"))
                {
                    texSlot.TextureType = TextureType.Opacity;
                }
                else if (mapName.StartsWith("REFLECTMAP"))
                {
                    texSlot.TextureType = TextureType.Reflection;
                }
                else
                {
                    Console.WriteLine($"WARNING: Texture map type {mapName} is not currently supported.");
                }
                texSlot.TextureIndex = constructedMaterial.GetMaterialTextureCount(texSlot.TextureType);

                if (!constructedMaterial.AddMaterialTexture(texSlot))
                    Console.WriteLine($"WARNING: Adding map ({mapName}, {textureName}) did not update or create new data!");
            }
            
            constructedMaterials.Add(constructedMaterial);
        }
        
        // Third, let's generate our 3D geometry meshes, from MSH resources and their associated VTX resources.
        List<Mesh> constructedMeshes = new();
        if (meshResources.Count != vertexResources.Count)
        {
            throw new InvalidDataException("The number of meshes did not match the number of vertices.");
        }
        
        // Iterate through the meshes and construct Assimp meshes based on them and their vertices.
        foreach (MeshResource meshResource in meshResources)
        {
            VertexResource linkedVertexResource = vertexResources.First(r => r.Name == meshResource.LinkedVertexName);
            
            Mesh assimpMesh = new();
            assimpMesh.Name = meshResource.Name;
            assimpMesh.PrimitiveType = PrimitiveType.Triangle;
            assimpMesh.MaterialIndex = constructedMaterials.IndexOf(constructedMaterials.First(mat =>
                mat.Name == meshResource.LinkedMaterialName));

            foreach (var vertex in linkedVertexResource.Vertices)
            {
                assimpMesh.Vertices.Add(new Vector3D(vertex.X, vertex.Y, vertex.Z));
            }

            foreach (var normal in linkedVertexResource.Normals)
            {
                assimpMesh.Normals.Add(new Vector3D(normal.X, normal.Y, normal.Z));
            }

            foreach (var index in linkedVertexResource.Indices)
            {
                Face face = new();
                face.Indices.Add(index.Item1);
                face.Indices.Add(index.Item2);
                face.Indices.Add(index.Item3);
                assimpMesh.Faces.Add(face);
            }

            assimpMesh.UVComponentCount[0] = 2;
            assimpMesh.TextureCoordinateChannels[0] = new();
            foreach (var uv in linkedVertexResource.TextureCoords)
            {
                Vector3D texCoord3d = new(uv.X, uv.Y, 0.0f);
                assimpMesh.TextureCoordinateChannels[0].Add(texCoord3d);
            }
            
            // Finally add the constructed Assimp mesh to the list.
            constructedMeshes.Add(assimpMesh);
        }

        Scene scene = new();
        SceneResource? sceneResource = loadedData!.Resources.First(r => r is SceneResource) as SceneResource;
        if (sceneResource is null)
        {
            Console.Write("ERROR: The current SRD file contains no scene, meaning it does not contain any 3D model data.");
            Utils.PromptForEnterKey();
            return;
        }

        scene.Clear();
        scene.RootNode = new AssimpNode(sceneResource.Name);
        scene.Meshes.AddRange(constructedMeshes);
        scene.Materials.AddRange(constructedMaterials);

        foreach (var treeName in sceneResource.LinkedTreeNames)
        {
            TreeResource tree = loadedData!.Resources.First(r => r is TreeResource tre && tre.Name == treeName) as TreeResource ?? throw new InvalidOperationException();

            // Perform a depth-first traverse through the tree to create all the Assimp nodes.
            AssimpNode treeRoot = DepthFirstTreeNodeConversion(tree.RootNode, meshResources);
            
            scene.RootNode.Children.Add(treeRoot);
        }

        AssimpContext context = new();
        var exportFormats = context.GetSupportedExportFormats();
        foreach (var format in exportFormats)
        {
            if (format.FileExtension != "gltf") continue;

            string exportName = $"{loadedDataInfo!.FullName}";
            context.ExportFile(scene, $"{exportName}.{format.FileExtension}", format.FormatId);
            break;
        }
    }

    private AssimpNode DepthFirstTreeNodeConversion(Node inputNode, List<MeshResource> meshResources)
    {
        AssimpNode outputNode = new(inputNode.Name);

        for (var meshNum = 0; meshNum < meshResources.Count; ++meshNum)
        {
            if (meshResources[meshNum].Name == inputNode.Name)
            {
                outputNode.MeshIndices.Add(meshNum);
            }
        }
        
        // Shortcut out if we have reached a leaf node.
        if (inputNode.Children is null) return outputNode;
        
        // Recursively traverse all child nodes.
        foreach (var child in inputNode.Children)
        {
            outputNode.Children.Add(DepthFirstTreeNodeConversion(child, meshResources));
        }

        return outputNode;
    }

    private void Help()
    {
        Utils.PrintMenuDescriptions(AvailableEntries);
    }
}