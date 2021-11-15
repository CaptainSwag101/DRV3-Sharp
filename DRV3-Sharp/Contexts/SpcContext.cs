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

        public List<IOperation> PossibleOperations
        {
            get
            {
                List<IOperation> operationList = new();

                // If an SPC file is loaded, add file-related operations
                if (loadedData != null)
                {
                    
                }

                // Add always-available operations
                operationList.Add(new HelpOperation());
                operationList.Add(new ExitOperation());

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

        private class HelpOperation : IOperation
        {
            public string Name => "Help";

            public string Description => "Displays information about the operations you can currently perform.";

            public void Perform(IOperationContext rawContext)
            {
                // Ensure that this is not somehow being called from the wrong context
                if (rawContext is not SpcContext context)
                    throw new InvalidOperationException($"This operation was called from an illegal context {rawContext.GetType()}, it should only be called from {typeof(SpcContext)}.");

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

        private class ExitOperation : IOperation
        {
            public string Name => "Exit";

            public string Description => "Ends the current SPC operations.";

            public void Perform(IOperationContext rawContext)
            {
                // Ensure that this is not somehow being called from the wrong context
                if (rawContext is not SpcContext context)
                    throw new InvalidOperationException($"This operation was called from an illegal context {rawContext.GetType()}, it should only be called from {typeof(SpcContext)}.");

                Program.PopContext();
            }
        }
    }
}
