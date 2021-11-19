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
                operationList.Add(new StxOperation());
                operationList.Add(new SrdOperation());
                operationList.Add(new HelpOperation());
                operationList.Add(new CancelOperation());

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
        
        internal class StxOperation : IOperation
        {
            public string Name => "STX";

            public string Description => "The primary text file type used by DRV3.";

            public void Perform(IOperationContext rawContext)
            {
                _ = GetVerifiedContext(rawContext);

                Program.PopContext();   // Remove this context so if we exit the upcoming context, we fall back directly to RootContext
                Program.PushContext(new StxContext());
            }
        }
        
        internal class SrdOperation : IOperation
        {
            public string Name => "SRD";

            public string Description => "The primary resource container used by DRV3. (CURRENTLY UNFINISHED!)";

            public void Perform(IOperationContext rawContext)
            {
                _ = GetVerifiedContext(rawContext);

                Program.PopContext();   // Remove this context so if we exit the upcoming context, we fall back directly to RootContext
                Program.PushContext(new SrdContext());
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
