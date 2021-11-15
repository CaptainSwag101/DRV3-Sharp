using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp.Contexts
{
    internal class RootContext : IOperationContext
    {
        public List<IOperation> PossibleOperations
        {
            get
            {
                List<IOperation> operationList = new();

                // Add always-available operations
                operationList.Add(new SelectContextOperation());
                operationList.Add(new HelpOperation());
                operationList.Add(new ExitOperation());

                return operationList;
            }
        }

        protected static RootContext GetVerifiedContext(IOperationContext compare)
        {
            // Ensure that this is not somehow being called from the wrong context
            if (compare.GetType() != typeof(RootContext))
                throw new InvalidOperationException($"This operation was called from an illegal context {compare.GetType()}, it should only be called from {typeof(RootContext)}.");

            return (RootContext)compare;
        }

        internal class SelectContextOperation : IOperation
        {
            public string Name => "Select Initial Context";

            public string Description => "Select the context/filetype which you'd like to start working with.";

            public void Perform(IOperationContext rawContext)
            {
                _ = GetVerifiedContext(rawContext);

                Program.PushContext(new SelectTypeContext());
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

            public string Description => "Exits the program.";

            public void Perform(IOperationContext rawContext)
            {
                _ = GetVerifiedContext(rawContext);

                // Pop this context off the program's context stack
                Program.PopContext();
            }
        }
    }
}
