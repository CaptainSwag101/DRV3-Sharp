using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp
{
    internal interface IOperation
    {
        public string Name { get; } // Name of the operation for display/choice
        public string Description { get; }  // Text describing what that option does if the user chooses "Help"

        public void Perform(IOperationContext rawContext);  // All operations will be interactive so this can be a super basic function
    }
}
