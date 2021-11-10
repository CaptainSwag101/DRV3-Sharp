using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using V3Lib.CriWare;

namespace AcbTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("ACB Tool by CaptainSwag101\n" +
                "Version 1.0.0, built on 2020-08-03\n");

            if (args.Length != 2)
            {
                Console.WriteLine("ERROR: Invalid number of parameters. Please provide only an ACB and AWB path, respectively.");
                return;
            }

            AcbFile loadedAcb = new AcbFile();
            string loadedAcbPath = args[0];
            loadedAcb.Load(loadedAcbPath);

            AwbFile loadedAwb = new AwbFile();
            string loadedAwbPath = args[1];
            loadedAwb.Load(loadedAwbPath);

            FileInfo info = new FileInfo(loadedAcbPath);
            string outputDir = info.DirectoryName + Path.DirectorySeparatorChar + info.Name.Substring(0, info.Name.Length - info.Extension.Length);
            Directory.CreateDirectory(outputDir);

            List<string> cueValues = loadedAcb.Cues.Values.ToList();
            for (short cueNum = 0; cueNum < loadedAcb.Cues.Count; ++cueNum)
            {
                if (loadedAwb.AudioData.ContainsKey(cueNum))
                {
                    byte[] audioData = loadedAwb.AudioData[cueNum];
                    string outFilePath = outputDir + Path.DirectorySeparatorChar + cueValues[cueNum];

                    // Guess output file extension
                    if (audioData[0] == 0x80 && audioData[1] == 0x00)
                        outFilePath += ".adx";
                    else if (audioData[0..3] == Encoding.ASCII.GetBytes("HCA"))
                        outFilePath += ".hca";

                    using FileStream audioFile = new FileStream(outFilePath, FileMode.Create);
                    audioFile.Write(audioData);
                }
            }
        }
    }
}
