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
                Console.WriteLine("Enter the full path of the file to load (or drag and drop it) and press Enter: ");
                string? path = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(path))
                {
                    Console.WriteLine("The specified path is null or invalid.");
                    Console.WriteLine("Press any key to continue...");
                    _ = Console.ReadKey(true);
                    return;
                }

                // Trim leading and trailing quotation marks (which are often added during drag-and-drop)
                path = path.Trim('"');

                if (!File.Exists(path))
                {
                    Console.WriteLine("The specified file does not exist.");
                    Console.WriteLine("Press any key to continue...");
                    _ = Console.ReadKey(true);
                    return;
                }

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
                    Console.WriteLine("Please specify where to save the file:");
                    string? path = Console.ReadLine();
                    if (path is null)
                    {
                        Console.WriteLine("The specified path is null.");
                        Console.WriteLine("Press any key to continue...");
                        _ = Console.ReadKey(true);
                        return;
                    }

                    // Ensure the path isn't a directory
                    if (new FileInfo(path).Attributes.HasFlag(FileAttributes.Directory))
                    {
                        Console.WriteLine("The specified path is a directory.");
                        Console.WriteLine("Press any key to continue...");
                        _ = Console.ReadKey(true);
                        return;
                    }

                    using FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
                    SpcSerializer.Serialize(context.loadedData!, fs);   // It shouldn't be possible to invoke this operation while context.loadedData is null
                    fs.Flush();
                }

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
                    for (int i = 0; i < maxFiles; ++i)
                    {
                        ArchivedFile entry = context.loadedData.Files[i];

                        // Preserve original foreground color in the case of a custom-themed terminal
                        ConsoleColor origForeground = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine(entry.Name);
                        Console.ForegroundColor = origForeground;
                        Console.Write("\tIs Compressed: ");
                        Console.WriteLine(entry.IsCompressed);
                        Console.Write("\tOriginal Size: ");
                        Console.WriteLine(entry.OriginalSize);
                        Console.Write("\tArchived Size: ");
                        Console.WriteLine(entry.Data.Length);
                        Console.Write("\tUnknown Flag: ");
                        Console.WriteLine(entry.UnknownFlag);
                        Console.WriteLine();
                    }
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

                // Insert the file now
                throw new NotImplementedException();

                context.unsavedChanges = true;
            }
        }

        internal class ExtractFileOperation : IOperation
        {
            public string Name => "Extract File";

            public string Description => "Extract a file from the currently-loaded SPC archive.";

            public void Perform(IOperationContext rawContext)
            {
                _ = GetVerifiedContext(rawContext);

                Program.PushContext(new SpcExtractContext());
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
                foreach (IOperation op in operations)
                {
                    // Preserve original foreground color in the case of a custom-themed terminal
                    ConsoleColor origForeground = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(op.Name);
                    Console.ForegroundColor = origForeground;
                    Console.Write("\t");
                    Console.Write(op.Description);
                    Console.WriteLine();
                }

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
