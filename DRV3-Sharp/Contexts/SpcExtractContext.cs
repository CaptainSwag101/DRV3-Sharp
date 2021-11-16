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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DRV3_Sharp_Library.Formats.Archive.SPC;

namespace DRV3_Sharp.Contexts
{
    internal class SpcExtractContext : IOperationContext
    {
        private readonly SpcData loadedData;

        public List<IOperation> PossibleOperations
        {
            get
            {
                List<IOperation> operationList = new();

                // Add "back" and "extract all" operations first
                operationList.Add(new BackOperation());
                operationList.Add(new ExtractAllOperation());

                foreach (ArchivedFile file in loadedData.Files)
                {
                    operationList.Add(new ExtractFileOperation(file));
                }

                return operationList;
            }
        }

        public SpcExtractContext(SpcData spc)
        {
            loadedData = spc;
        }

        protected static SpcExtractContext GetVerifiedContext(IOperationContext compare)
        {
            // Ensure that this is not somehow being called from the wrong context
            if (compare.GetType() != typeof(SpcExtractContext))
                throw new InvalidOperationException($"This operation was called from an illegal context {compare.GetType()}, it should only be called from {typeof(SpcExtractContext)}.");

            return (SpcExtractContext)compare;
        }

        internal class BackOperation : IOperation
        {
            public string Name => "Back";

            public string Description => "";

            public void Perform(IOperationContext rawContext)
            {
                _ = GetVerifiedContext(rawContext);

                Program.PopContext();
            }
        }

        internal class ExtractAllOperation : IOperation
        {
            public string Name => "Extract All";

            public string Description => "";

            public void Perform(IOperationContext rawContext)
            {
                var context = GetVerifiedContext(rawContext);

                foreach (ArchivedFile file in context.loadedData.Files)
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

                // Since we've extracted all files at once, the user probably doesn't want to stick around
                Program.PopContext();
            }
        }

        internal class ExtractFileOperation : IOperation
        {
            private readonly ArchivedFile fileToExtract;

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
