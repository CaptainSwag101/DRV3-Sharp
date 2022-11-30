/*
    DRV3-Sharp, a free and open-source toolkit
    for working with files and assets from Danganronpa V3.

    Copyright (C) 2020-2022  James Pelster
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DRV3_Sharp_Library.Formats.Resource.SRD;
using Scarlet;
using Scarlet.IO;
using SixLabors.ImageSharp;

namespace DRV3_Sharp.Contexts
{
    internal sealed class SrdContext : IOperationContext
    {
        private SrdData? loadedData;
        private string? loadedDataPath;
        private bool unsavedChanges = false;

        public List<IOperation> PossibleOperations
        {
            get
            {
                List<IOperation> operationList = new()
                {
                    // Add always-available operations
                    new NewSrdOperation(),
                    new LoadSrdOperation(),
                    new HelpOperation(),
                    new BackOperation()
                };

                // If an SRD file is loaded, add file-related operations
                if (loadedData is not null)
                {
                    operationList.Insert(2, new SaveSrdOperation());
                    operationList.Insert(3, new ListBlocksOperation());
                    //operationList.Insert(4, new ExtractTexturesOperation());
                    //operationList.Insert(5, new ExtractFontOperation());
                }

                return operationList;
            }
        }

        public SrdContext()
        { }

        public SrdContext(SrdData initialData, string initialDataPath)
        {
            loadedData = initialData;
            loadedDataPath = initialDataPath;
        }

        private static SrdContext GetVerifiedContext(IOperationContext compare)
        {
            // Ensure that this is not somehow being called from the wrong context
            if (compare.GetType() != typeof(SrdContext))
                throw new InvalidOperationException($"This operation was called from an illegal context {compare.GetType()}, it should only be called from {typeof(SrdContext)}.");

            return (SrdContext)compare;
        }

        public bool ConfirmIfUnsavedChanges()
        {
            if (unsavedChanges)
            {
                ConsoleColor fgColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("You have unsaved changes pending! These changes WILL BE LOST if you continue!");
                Console.ForegroundColor = fgColor;
                Console.Write("Are you sure you want to continue? (y/N) ");

                var key = Console.ReadKey(false).Key;
                if (key == ConsoleKey.Y)
                    return true;
                else
                    return false;
            }
            else
            {
                return true;
            }
        }

        private sealed class NewSrdOperation : IOperation
        {
            public string Name => "New SRD";

            public string Description => "Creates a new, empty SRD resource container to be populated.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                if (!context.ConfirmIfUnsavedChanges()) return;

                context.loadedData = new();
                context.loadedDataPath = null;
                context.unsavedChanges = false;
            }
        }

        private sealed class LoadSrdOperation : IOperation
        {
            public string Name => "Load SRD";

            public string Description => "Load an existing SRD resource container, and any accompanying binary files.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                if (!context.ConfirmIfUnsavedChanges()) return;

                // Get the file path
                string? path = Utils.GetPathFromUser("Enter the full path of the file to load (or drag and drop it) and press Enter:", true);
                if (path is null) return;

                // Load the file now that we've verified it exists
                string srdiPath = Path.ChangeExtension(path, "srdi");
                string srdvPath = Path.ChangeExtension(path, "srdv");
                using FileStream srdStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                FileStream? srdiStream = null;
                FileStream? srdvStream = null;
                if (File.Exists(srdiPath)) srdiStream = new(srdiPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (File.Exists(srdvPath)) srdvStream = new(srdvPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                SrdSerializer.Deserialize(srdStream, srdiStream, srdvStream, out context.loadedData);
                srdiStream?.Close();
                srdvStream?.Close();

                context.loadedDataPath = path;
                context.unsavedChanges = false;
            }
        }

        private sealed class SaveSrdOperation : IOperation
        {
            public string Name => "Save SRD";

            public string Description => "Save the currently-loaded SRD resource container, and any accompanying binary files.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                // Save the file now
                if (string.IsNullOrWhiteSpace(context.loadedDataPath))
                {
                    string? path = Utils.GetPathFromUser("Enter the full path where the file should be saved (or drag and drop it) and press Enter:", false);
                    if (path is null) return;

                    context.loadedDataPath = path;
                }

                using FileStream srdStream = new(context.loadedDataPath, FileMode.Create, FileAccess.Write, FileShare.None);
                using MemoryStream srdiStream = new();
                using MemoryStream srdvStream = new();
                SrdSerializer.Serialize(context.loadedData!, srdStream, srdvStream, srdiStream);    // It shouldn't be possible to invoke this operation while context.loadedData is null

                // If we need SRDI or SRDV, create and write them too
                if (srdiStream.Length > 0)
                {
                    string srdiPath = Path.ChangeExtension(context.loadedDataPath, "srdi");
                    using FileStream srdiFile = new(srdiPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    srdiFile.Write(srdiStream.ToArray());
                }
                if (srdvStream.Length > 0)
                {
                    string srdvPath = Path.ChangeExtension(context.loadedDataPath, "srdv");
                    using FileStream srdvFile = new(srdvPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    srdvFile.Write(srdvStream.ToArray());
                }

                context.unsavedChanges = false;
            }
        }

        private sealed class ListBlocksOperation : IOperation
        {
            public string Name => "List Blocks";

            public string Description => "Display a descriptive list of all discrete data blocks in this resource archive.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                StringBuilder sb = new();
                foreach (ISrdBlock block in context.loadedData!.Blocks)
                {
                    foreach (string line in block.GetBlockInfo())
                    {
                        sb.Append($"{line}\n");
                    }
                    sb.Append('\n');
                }
                Console.WriteLine(sb.ToString());

                Console.WriteLine("Press any key to continue...");
                _ = Console.ReadKey(true);
            }
        }

        private sealed class ExtractTexturesOperation : IOperation
        {
            public string Name => "Extract Textures";

            public string Description => "Extract one or more textures from the resource container.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                Console.WriteLine("Not yet implemented...");

                Console.WriteLine("Press any key to continue...");
                _ = Console.ReadKey(true);

                //List<ISrdBlock> txrBlocks = context.loadedData!.Blocks.Where(block => block is TxrBlock).ToList();
                //if (txrBlocks.Count == 0)
                //{
                //    Console.WriteLine("This file contains no textures.");
                //    return;
                //}

                //// Save the textures in the same location as the SRD file
                //if (string.IsNullOrWhiteSpace(context.loadedDataPath))
                //{
                //    string? path = Utils.GetPathFromUser("Enter the full path where the file should be saved (or drag and drop it) and press Enter:", false);
                //    if (path is null) return;

                //    context.loadedDataPath = path;
                //}
                //string srdDirPath = Utils.GetEnclosingDirectory(context.loadedDataPath!)!;

                //// Check if there is an RSF block in the file. If so, extract contents to an inner folder
                //string extractDirPath;
                //List<ISrdBlock> rsfBlocks = context.loadedData!.Blocks.Where(block => block is RsfBlock).ToList();
                //if (rsfBlocks.Count > 0)
                //{
                //    RsfBlock rsf = (rsfBlocks.First() as RsfBlock)!;
                //    extractDirPath = Path.Combine(srdDirPath, rsf.FolderName);
                //}
                //else
                //{
                //    extractDirPath = srdDirPath;
                //}

                //// Iterate through all TXR blocks and extract their contents
                //foreach (TxrBlock block in txrBlocks)
                //{
                //    Console.WriteLine($"Extracting {block.TextureFilename}");

                //    // Here's the hard part: Converting the texture format into something we can work with
                //    if (block.Format == TextureFormat.ARGB8888)
                //    {
                //        // Do stuff
                //    }
                //}
            }
        }

        private sealed class ExtractFontOperation : IOperation
        {
            public string Name => "Extract Font";

            public string Description => "Extract font bounding box table and character mapping info from the resource container.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                Console.WriteLine("Not yet implemented...");

                Console.WriteLine("Press any key to continue...");
                _ = Console.ReadKey(true);

                //List<ISrdBlock> txrBlocks = context.loadedData!.Blocks.Where(block => block is TxrBlock).ToList();
                //if (txrBlocks.Count == 0)
                //{
                //    Console.WriteLine("This file contains no fonts.");
                //    return;
                //}

                //// Save the fonts in the same location as the SRD file
                //if (string.IsNullOrWhiteSpace(context.loadedDataPath))
                //{
                //    string? path = Utils.GetPathFromUser("Enter the full path where the file should be saved (or drag and drop it) and press Enter:", false);
                //    if (path is null) return;

                //    context.loadedDataPath = path;
                //}
                //string srdDirPath = Utils.GetEnclosingDirectory(context.loadedDataPath!)!;

                //// Check if there is an RSF block in the file. If so, extract contents to an inner folder
                //string extractDirPath;
                //List<ISrdBlock> rsfBlocks = context.loadedData!.Blocks.Where(block => block is RsfBlock).ToList();
                //if (rsfBlocks.Count > 0)
                //{
                //    RsfBlock rsf = (rsfBlocks.First() as RsfBlock)!;
                //    extractDirPath = Path.Combine(srdDirPath, rsf.FolderName);
                //}
                //else
                //{
                //    extractDirPath = srdDirPath;
                //}

                //// Iterate through all TXR blocks and extract their font mapping data, if it is present
                //foreach (TxrBlock block in txrBlocks)
                //{

                //}
            }
        }

        private sealed class HelpOperation : IOperation
        {
            public string Name => "Help";

            public string Description => "Displays information about the operations you can currently perform.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                var operations = context.PossibleOperations;
                List<(string name, string description)> displayList = new();
                foreach (IOperation op in operations)
                {
                    displayList.Add((op.Name, $"\t{op.Description}"));
                }
                Utils.DisplayDescriptiveList(displayList);

                Console.WriteLine("Press any key to continue...");
                _ = Console.ReadKey(true);
            }
        }

        private sealed class BackOperation : IOperation
        {
            public string Name => "Back";

            public string Description => "Ends the current SRD operations and returns to the previous screen.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                if (!context.ConfirmIfUnsavedChanges()) return;

                // Pop this context off the program's context stack
                Program.PopContext();
            }
        }
    }
}
