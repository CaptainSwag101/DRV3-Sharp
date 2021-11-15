using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp.Contexts
{
    internal class SelectTypeContext : IOperationContext
    {
        public List<IOperation> PossibleOperations
        {
            get
            {
                List<IOperation> operationList = new();

                // Populate with file types (and help and exit options)
                operationList.Add(new SpcOperation());
                operationList.Add(new HelpOperation());

                return operationList;
            }
        }

        protected static SelectTypeContext GetVerifiedContext(IOperationContext compare)
        {
            // Ensure that this is not somehow being called from the wrong context
            if (compare.GetType() != typeof(SelectTypeContext))
                throw new InvalidOperationException($"This operation was called from an illegal context {compare.GetType()}, it should only be called from {typeof(SelectTypeContext)}.");

            return (SelectTypeContext)compare;
        }

        internal class SpcOperation : IOperation
        {
            public string Name => "SPC";

            public string Description => "The primary archive type used by DRV3.";

            public void Perform(IOperationContext rawContext)
            {
                _ = GetVerifiedContext(rawContext);

                Program.PopContext();   // Remove this context so if we exit the upcoming context, we fall back directly to RootContext
                Program.PushContext(new SpcContext());
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

        internal class CancelOperation : IOperation
        {
            public string Name => "Cancel";

            public string Description => "Cancels context selection.";

            public void Perform(IOperationContext rawContext)
            {
                _ = GetVerifiedContext(rawContext);

                // Pop this context off the program's context stack
                Program.PopContext();
            }
        }
    }
}
