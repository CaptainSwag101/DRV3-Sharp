using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp_Library.Formats.Resource.SRD.Blocks
{
    public class RsfBlock : ISrdBlock
    {
        #region Block Constants
        private const int MAGIC_1 = 20110331;
        private const int MAGIC_2 = 20110401;
        #endregion

        #region Public Properties
        public string BlockType { get { return @"$RSF"; } }
        public int Unknown04 { get; private set; }
        public int Unknown08 { get; private set; }
        public int Unknown0C { get; private set; }
        public string FolderName { get; set; }
        #endregion

        #region Public Methods
        public RsfBlock()
        {
            FolderName = "";
            Unknown04 = MAGIC_1;
            Unknown08 = MAGIC_2;
            Unknown0C = 0;
        }

        public List<string> GetBlockInfo()
        {
            throw new NotImplementedException();
        }

        public static void Deserialize(MemoryStream mainDataStream, out RsfBlock rsf)
        {
            rsf = new();

            BinaryReader reader = new(mainDataStream, Encoding.GetEncoding("shift-jis"), true);

            int folderNameOffset = reader.ReadInt32();
            if (folderNameOffset != 16) throw new InvalidDataException($"{nameof(folderNameOffset)} was {folderNameOffset} when it is usually 16. This means we are skipping data that we need to read. Please send this file the the program's author!");
            rsf.Unknown04 = reader.ReadInt32();
            if (rsf.Unknown04 != MAGIC_1) throw new InvalidDataException($"The first magic number for the $RSF block was not {MAGIC_1}. Please send this file to the program's author!");
            rsf.Unknown08 = reader.ReadInt32();
            if (rsf.Unknown08 != MAGIC_2) throw new InvalidDataException($"The second magic number for the $RSF block was not {MAGIC_2}. Please send this file to the program's author!");
            rsf.Unknown0C = reader.ReadInt32();

            reader.BaseStream.Seek(folderNameOffset, SeekOrigin.Begin);
            rsf.FolderName = Utils.ReadNullTerminatedString(reader, Encoding.GetEncoding("shift-jis"));
        }
        #endregion
    }
}
