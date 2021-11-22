/*
    DRV3-Sharp, a free and open-source toolkit
    for working with files and assets from Danganronpa V3.

    Copyright (C) 2020-2021  James Pelster
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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DRV3_Sharp_Library.Formats.Archive.SPC;

namespace DRV3_Sharp.Contexts
{
    internal class SpcContext : IOperationContext
    {
        private SpcData? loadedData;
        private string? loadedDataPath;
        private bool unsavedChanges = false;

        public List<IOperation> PossibleOperations
        {
            get
            {
                List<IOperation> operationList = new();

                // Add always-available operations
                operationList.Add(new NewSpcOperation());
                operationList.Add(new LoadSpcOperation());
                operationList.Add(new HelpOperation());
                operationList.Add(new BackOperation());

                // If an SPC file is loaded, add file-related operations
                if (loadedData is not null)
                {
                    operationList.Insert(2, new SaveSpcOperation());
                    operationList.Insert(3, new ListFileOperation());
                    operationList.Insert(4, new InsertFileOperation());
                    operationList.Insert(5, new ExtractFileOperation());
                }

                return operationList;
            }
        }

        public SpcContext()
        { }

        public SpcContext(SpcData initialData, string initialDataPath)
        {
            loadedData = initialData;
            loadedDataPath = initialDataPath;
        }

        protected static SpcContext GetVerifiedContext(IOperationContext compare)
        {
            // Ensure that this is not somehow being called from the wrong context
            if (compare.GetType() != typeof(SpcContext))
                throw new InvalidOperationException($"This operation was called from an illegal context {compare.GetType()}, it should only be called from {typeof(SpcContext)}.");

            return (SpcContext)compare;
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

        internal class NewSpcOperation : IOperation
        {
            public string Name => "New SPC";

            public string Description => "Creates a new, empty SPC archive to be populated.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                if (!context.ConfirmIfUnsavedChanges()) return;

                context.loadedData = new();
                context.loadedDataPath = null;
                context.unsavedChanges = false;
            }
        }

        internal class LoadSpcOperation : IOperation
        {
            public string Name => "Load SPC";

            public string Description => "Load an existing SPC archive file.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                if (!context.ConfirmIfUnsavedChanges()) return;

                // Get the file path
                string? path = Utils.GetPathFromUser("Enter the full path of the file to load (or drag and drop it) and press Enter:", true);
                if (path is null) return;

                // Load the file now that we've verified it exists
                using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                SpcSerializer.Deserialize(fs, out context.loadedData);
                context.loadedDataPath = path;
                context.unsavedChanges = false;
            }
        }

        internal class SaveSpcOperation : IOperation
        {
            public string Name => "Save SPC";

            public string Description => "Save the currently-loaded SPC archive file.";

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

                using FileStream fs = new(context.loadedDataPath, FileMode.Create, FileAccess.Write, FileShare.None);
                SpcSerializer.Serialize(context.loadedData!, fs);   // It shouldn't be possible to invoke this operation while context.loadedData is null
                fs.Flush();

                context.unsavedChanges = false;
            }
        }

        internal class ListFileOperation : IOperation
        {
            public string Name => "List Files";

            public string Description => "List all files currently stored in the archive.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                int maxFiles = context.loadedData!.FileCount;
                if (maxFiles > 0)
                {
                    List<(string name, string description)> displayList = new();
                    for (int i = 0; i < maxFiles; ++i)
                    {
                        ArchivedFile entry = context.loadedData.Files[i];

                        StringBuilder descriptionBuilder = new();
                        descriptionBuilder.Append($"\tIs Compressed: {entry.IsCompressed}\n");
                        descriptionBuilder.Append($"\tOriginal Size: {entry.OriginalSize}\n");
                        descriptionBuilder.Append($"\tArchived Size: {entry.Data.Length}\n");
                        descriptionBuilder.Append($"\tUnknown Flag: {entry.UnknownFlag}\n");

                        displayList.Add((entry.Name, descriptionBuilder.ToString()));
                    }
                    Utils.DisplayDescriptiveList(displayList);
                }
                else
                {
                    Console.WriteLine("This archive contains no files.");
                }

                Console.WriteLine("Press any key to continue...");
                _ = Console.ReadKey(true);
            }
        }

        internal class InsertFileOperation : IOperation
        {
            public string Name => "Insert File";

            public string Description => "Insert a file into the currently-loaded SPC archive.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                // Get the file path to insert
                string? path = Utils.GetPathFromUser("Enter the full path of the file to insert (or drag and drop it) and press Enter:", true);
                if (path is null) return;

                // Check if a file by that name already exists in the archive
                string fileName = new FileInfo(path).Name;
                int? foundIndex = null;
                for (int i = 0; i < context.loadedData!.FileCount; ++i)
                {
                    if (context.loadedData!.Files[i].Name == fileName)
                    {
                        Console.Write("A file with the same name already exists in the archive! Overwrite? (y/N): ");

                        var key = Console.ReadKey(false).Key;
                        if (key == ConsoleKey.Y)
                        {
                            foundIndex = i;
                            break;
                        }
                        else
                        {
                            return;
                        }
                    }
                }

                using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                byte[] fileData = new byte[fs.Length];
                fs.Read(fileData, 0, (int)fs.Length);

                // Try to compress the file
                byte[] compressedData = SpcCompressor.Compress(fileData);

                // Prepare to generate the new ArchivedFile
                ArchivedFile newFile;

                // If the compressed data is smaller, use it. Otherwise, don't bother
                if (compressedData.Length < fileData.Length)
                    newFile = new(fileName, compressedData, 4, true, fileData.Length);
                else
                    newFile = new(fileName, fileData, 4, false, fileData.Length);

                // If the file already exists, replace that one
                if (foundIndex is not null)
                {
                    context.loadedData!.Files[(int)foundIndex] = newFile;
                }
                else
                {
                    context.loadedData!.Files.Add(newFile);
                }

                context.unsavedChanges = true;
            }
        }

        internal class ExtractFileOperation : IOperation
        {
            public string Name => "Extract File";

            public string Description => "Extract a file from the currently-loaded SPC archive.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                Program.PushContext(new SpcExtractContext(context.loadedData!));
            }
        }

        internal class HelpOperation : IOperation
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

        internal class BackOperation : IOperation
        {
            public string Name => "Back";

            public string Description => "Ends the current SPC operations and returns to the previous screen.";

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
