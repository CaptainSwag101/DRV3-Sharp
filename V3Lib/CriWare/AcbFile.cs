using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.CriWare
{
    public class AcbFile
    {
        public Dictionary<short, string> Cues = new Dictionary<short, string>();

        public void Load(string acbPath)
        {
            //using BinaryReader reader = new BinaryReader(new FileStream(acbPath, FileMode.Open));
            byte[] data = File.ReadAllBytes(acbPath);

            UtfTable mainTable = new UtfTable();
            mainTable.Load(data);

            UtfTable cueTable = new UtfTable();
            cueTable.Load((byte[])mainTable.Contents[0]["CueNameTable"]);
            
            foreach (var column in cueTable.Contents)
            {
                Cues.Add((short)column["CueIndex"], (string)column["CueName"]);
            }
        }
    }
}
