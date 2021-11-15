using DRV3_Sharp_Library.Formats.Archive.SPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp.Contexts
{
    internal class SpcExtractContext : IOperationContext
    {
        private SpcData loadedSpc;

        public List<IOperation> PossibleOperations
        {
            get
            {
                List<IOperation> operationList = new();

                // Add "back" and "extract all" operations first
                operationList.Add(new BackOperation());
                operationList.Add(new ExtractAllOperation(loadedSpc));

                foreach (ArchivedFile file in loadedSpc.Files)
                {
                    operationList.Add(new ExtractFileOperation(file));
                }

                return operationList;
            }
        }

        public SpcExtractContext(SpcData spc)
        {
            loadedSpc = spc;
        }

        internal class BackOperation : IOperation
        {
            public string Name => "Back";

            public string Description => "";

            public void Perform(IOperationContext rawContext)
            {
                Program.PopContext();
            }
        }

        internal class ExtractAllOperation : IOperation
        {
            private SpcData spcToExtract;

            public string Name => "Extract All";

            public string Description => "";

            public ExtractAllOperation(SpcData spc)
            {
                spcToExtract = spc;
            }

            public void Perform(IOperationContext rawContext)
            {
                foreach (ArchivedFile file in spcToExtract.Files)
                {
                    byte[] data;

                    // If the file is compressed, decompress it first
                    if (file.IsCompressed)
                        data = SpcCompressor.Decompress(file.Data);
                    else
                        data = file.Data;

                    // TODO: Properly get the location to extract the file
                    using FileStream fs = new(file.Name, FileMode.Create, FileAccess.Write, FileShare.None);
                    fs.Write(data);
                    fs.Flush();
                }
            }
        }

        internal class ExtractFileOperation : IOperation
        {
            private ArchivedFile fileToExtract;

            public string Name => fileToExtract.Name;

            public string Description => "";

            public ExtractFileOperation(ArchivedFile file)
            {
                fileToExtract = file;
            }

            public void Perform(IOperationContext rawContext)
            {
                byte[] data;

                // If the file is compressed, decompress it first
                if (fileToExtract.IsCompressed)
                    data = SpcCompressor.Decompress(fileToExtract.Data);
                else
                    data = fileToExtract.Data;

                // TODO: Properly get the location to extract the file
                using FileStream fs = new(fileToExtract.Name, FileMode.Create, FileAccess.Write, FileShare.None);
                fs.Write(data);
                fs.Flush();
            }
        }
    }
}
