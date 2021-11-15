using System;
using System.Collections.Generic;
using System.Linq;
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
                operationList.Add(new ExitOperation());

                // If an SPC file is loaded, add file-related operations
                if (loadedData != null)
                {
                    operationList.Insert(2, new SaveSpcOperation());
                    operationList.Insert(3, new InsertFileOperation());
                    operationList.Insert(4, new ExtractFileOperation());
                }

                return operationList;
            }
        }

        protected static SpcContext GetVerifiedContext(IOperationContext compare)
        {
            // Ensure that this is not somehow being called from the wrong context
            if (compare.GetType() != typeof(SpcContext))
                throw new InvalidOperationException($"This operation was called from an illegal context {compare.GetType()}, it should only be called from {typeof(SpcContext)}.");

            return (SpcContext)compare;
        }

        public SpcContext()
        { }

        public SpcContext(SpcData initialData, string initialDataPath)
        {
            loadedData = initialData;
            loadedDataPath = initialDataPath;
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

                context.loadedData = new();
                context.loadedDataPath = null;
                context.unsavedChanges = false;

                // Load the file now
                throw new NotImplementedException();
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
                throw new NotImplementedException();

                context.unsavedChanges = false;
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
                var context = GetVerifiedContext(rawContext);

                // Extract the file now
                throw new NotImplementedException();
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

        internal class ExitOperation : IOperation
        {
            public string Name => "Exit";

            public string Description => "Ends the current SPC operations.";

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
