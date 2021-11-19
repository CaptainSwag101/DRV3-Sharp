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
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using DRV3_Sharp_Library.Formats.Text.STX;

namespace DRV3_Sharp.Contexts
{
    internal class StxContext : IOperationContext
    {
        private StxData? loadedData;
        private string? loadedDataPath;
        private bool unsavedChanges = false;

        public List<IOperation> PossibleOperations
        {
            get
            {
                List<IOperation> operationList = new();

                // Add always-available operations
                operationList.Add(new NewStxOperation());
                operationList.Add(new LoadStxOperation());
                operationList.Add(new ConvertFromJsonOperation());
                operationList.Add(new HelpOperation());
                operationList.Add(new BackOperation());

                // If an STX file is loaded, add file-related operations
                if (loadedData is not null)
                {
                    operationList.Insert(2, new SaveStxOperation());
                    operationList.Insert(4, new ConvertToJsonOperation());
                }

                return operationList;
            }
        }

        public StxContext()
        { }

        public StxContext(StxData initialData, string initialDataPath)
        {
            loadedData = initialData;
            loadedDataPath = initialDataPath;
        }

        protected static StxContext GetVerifiedContext(IOperationContext compare)
        {
            // Ensure that this is not somehow being called from the wrong context
            if (compare.GetType() != typeof(StxContext))
                throw new InvalidOperationException($"This operation was called from an illegal context {compare.GetType()}, it should only be called from {typeof(StxContext)}.");

            return (StxContext)compare;
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

        internal class NewStxOperation : IOperation
        {
            public string Name => "New STX";

            public string Description => "Creates a new, empty STX text file to be populated.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                if (!context.ConfirmIfUnsavedChanges()) return;

                context.loadedData = new();
                context.loadedDataPath = null;
                context.unsavedChanges = false;
            }
        }

        internal class LoadStxOperation : IOperation
        {
            public string Name => "Load STX";

            public string Description => "Load an existing STX text file.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                if (!context.ConfirmIfUnsavedChanges()) return;

                // Get the file path
                string? path = Utils.GetPathFromUser("Enter the full path of the file to load (or drag and drop it) and press Enter:");
                if (path is null) return;

                // Load the file now that we've verified it exists
                using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                StxSerializer.Deserialize(fs, out context.loadedData);
                context.loadedDataPath = path;
                context.unsavedChanges = false;
            }
        }

        internal class SaveStxOperation : IOperation
        {
            public string Name => "Save STX";

            public string Description => "Save the currently-loaded STX text file.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                // Save the file now
                if (string.IsNullOrWhiteSpace(context.loadedDataPath))
                {
                    string? path = Utils.GetPathFromUser("Enter the full path where the file should be saved (or drag and drop it) and press Enter:");
                    if (path is null) return;

                    context.loadedDataPath = path;
                }

                using FileStream fs = new(context.loadedDataPath, FileMode.Create, FileAccess.Write, FileShare.None);
                StxSerializer.Serialize(context.loadedData!, fs);   // It shouldn't be possible to invoke this operation while context.loadedData is null
                fs.Flush();

                context.unsavedChanges = false;
            }
        }

        internal class ConvertFromJsonOperation : IOperation
        {
            public string Name => "Convert From JSON";

            public string Description => "Parses a properly-formatted JSON file and creates STX data based on its contents.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                if (!context.ConfirmIfUnsavedChanges()) return;

                string? path = Utils.GetPathFromUser("Enter the full path of the file to load (or drag and drop it) and press Enter:");
                if (path is null) return;

                context.loadedData = new();
                context.loadedDataPath = null;

                using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                using StreamReader reader = new(fs, Encoding.UTF8);
                string jsonIn = reader.ReadToEnd();
                var tableStringParagraphList = JsonSerializer.Deserialize<List<List<List<string>>>>(jsonIn);
                if (tableStringParagraphList is null)
                {
                    Console.WriteLine("Something went wrong while deserializing the JSON file!");
                    return;
                }

                foreach (var tableEntry in tableStringParagraphList)
                {
                    StringTable table = new(8, new());  // TODO: This value of 8 for the UnknownData is just a guess!
                    foreach (var paragraphList in tableEntry)
                    {
                        StringBuilder lineBuilder = new();
                        lineBuilder.AppendJoin("\n", paragraphList);
                        table.Strings.Add(lineBuilder.ToString());
                    }
                    context.loadedData.Tables.Add(table);
                }

                context.unsavedChanges = false;
            }
        }

        internal class ConvertToJsonOperation : IOperation
        {
            public string Name => "Convert To JSON";

            public string Description => "Creates a human-readable JSON file containing the STX text strings and other data.";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                List<List<List<string>>> tableStringParagraphList = new();
                foreach (var table in context.loadedData!.Tables)
                {
                    List<List<string>> stringParagraphList = new();
                    foreach (string line in table.Strings)
                    {
                        List<string> paragraphList = new();
                        string[] split = line.Split('\n');
                        foreach (string p in split)
                        {
                            paragraphList.Add(p.TrimEnd('\r')); // Some languages use carriage returns, others don't. Screw consistency, amirite?
                        }
                        stringParagraphList.Add(paragraphList);
                    }
                    tableStringParagraphList.Add(stringParagraphList);
                }

                JsonSerializerOptions jsonOptions = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                string jsonString = JsonSerializer.Serialize(tableStringParagraphList, jsonOptions);

                string? path = Utils.GetPathFromUser("Enter the full path where the file should be saved (or drag and drop it) and press Enter:");
                if (path is null) return;

                using FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
                using StreamWriter writer = new(fs, Encoding.UTF8);
                writer.Write(jsonString);
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

            public string Description => "Ends the current STX operations and returns to the previous screen.";

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
