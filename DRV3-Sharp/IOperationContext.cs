using DRV3_Sharp_Library.Formats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp
{
    internal interface IOperationContext
    {
        public List<IOperation> PossibleOperations { get; } // This is dynamic based on the current state of the current context
    }
}
