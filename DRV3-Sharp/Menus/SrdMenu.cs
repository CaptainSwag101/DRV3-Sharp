using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assimp;
using DRV3_Sharp_Library.Formats.Data.SRD;
using DRV3_Sharp_Library.Formats.Data.SRD.Resources;
using SixLabors.ImageSharp.Formats.Bmp;
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

    private async void ExtractTextures()
    {
        if (loadedData is null || loadedDataInfo is null) return;

        var successfulExports = 0;
        foreach (var resource in loadedData!.Resources)
        {
            if (resource is not TextureResource texture) continue;
            
            string outputPath = Path.Combine(loadedDataInfo.DirectoryName!, texture.Name.Replace(".tga", ".bmp"));
            await using FileStream fs = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            await texture.ImageMipmaps[0].SaveAsync(fs, new BmpEncoder());
            ++successfulExports;
        }
        
        Console.Write($"Exported {successfulExports} textures successfully.");
        Utils.PromptForEnterKey(false);
    }

    private void ExtractModels()
    {
        // Loop through the various resources in the SRD and construct an Assimp scene
        // and associated meshes, trees, etc.
        
        // First, let's generate our 3D geometry meshes, from MSH blocks and their associated VTX blocks.
        List<Mesh> constructedMeshes = new();
        List<MeshResource> meshResources = new();
        List<VertexResource> vertexResources = new();
        foreach (var resource in loadedData!.Resources)
        {
            switch (resource)
            {
                case MeshResource mesh:
                    meshResources.Add(mesh);
                    break;
                case VertexResource vertex:
                    vertexResources.Add(vertex);
                    break;
            }
        }

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
            assimpMesh.MaterialIndex = 0;   // TODO: Map this to a real material and not just the placeholder later

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
            
            // Finally add the constructed Assimp mesh to the list.
            constructedMeshes.Add(assimpMesh);
        }

        Scene scene = new();
        SceneResource sceneResource = loadedData!.Resources.First(r => r is SceneResource) as SceneResource ?? throw new InvalidOperationException();

        scene.Clear();
        scene.RootNode = new AssimpNode(sceneResource.Name);
        scene.Meshes.AddRange(constructedMeshes);
        // This placeholder material is mandatory or else we get an access violation error when trying to save the model. Thanks, Assimp!
        Material placeholderMaterial = new();
        placeholderMaterial.Name = "PlaceholderMaterial";
        scene.Materials.Add(placeholderMaterial);

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