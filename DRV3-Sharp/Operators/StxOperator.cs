using PowerArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp.Operators
{
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    class StxOperator
    {
        [HelpHook, ArgShortcut("-?"), ArgDescription("Shows this help")]
        public bool Help { get; set; }

        [ArgActionMethod, ArgDescription("Saves the file to disk.")]
        public void Save()
        {
            Program.SaveData();
        }

        [ArgActionMethod, ArgDescription("Exits the program.")]
        public void Exit()
        {
            Program.PrepareToExit();
        }
    }
}
