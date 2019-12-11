using System;
using System.IO;
using System.Linq;
using V3Lib.CriWare;

namespace AcbTool
{
    class Program
    {
        static void Main(string[] args)
        {
            AcbFile loadedAcb = new AcbFile();
            string loadedAcbPath = @"D:\Games\Persona Modding\AcbEditor\bgm.acb";
            //string loadedAcbPath = @"D:\Games\SteamLibrary\steamapps\common\Danganronpa V3 Killing Harmony\data\win\game_resident\game_resident\JINGLE.acb";
            //string loadedAcbPath = @"D:\Games\SteamLibrary\steamapps\common\Danganronpa V3 Killing Harmony\data\win\game_resident\voice_resident_ENG\VOICE_ENG.acb";
            loadedAcb.Load(loadedAcbPath);

            AwbFile loadedAwb = new AwbFile();
            string loadedAwbPath = @"D:\Games\Persona Modding\AcbEditor\bgm.awb";
            //string loadedAwbPath = @"D:\Games\SteamLibrary\steamapps\common\Danganronpa V3 Killing Harmony\data\win\sound\JINGLE.awb";
            //string loadedAwbPath = @"D:\Games\SteamLibrary\steamapps\common\Danganronpa V3 Killing Harmony\data\win\sound\VOICE_ENG.awb";
            loadedAwb.Load(loadedAwbPath);

            FileInfo info = new FileInfo(loadedAcbPath);
            string outputDir = info.DirectoryName + Path.DirectorySeparatorChar + info.Name.Substring(0, info.Name.Length - info.Extension.Length);
            Directory.CreateDirectory(outputDir);

            foreach (var cue in loadedAcb.Cues)
            {
                if (loadedAwb.AudioData.ContainsKey((short)loadedAcb.Cues.Keys.ToList().IndexOf(cue.Key)))
                {
                    string outFilePath = outputDir + Path.DirectorySeparatorChar + cue.Value;
                    byte[] audioData = loadedAwb.AudioData[(short)loadedAcb.Cues.Keys.ToList().IndexOf(cue.Key)];

                    // Guess output file extension
                    if (audioData[0] == 0x80 && audioData[1] == 0x00)
                        outFilePath += ".adx";
                    else if (audioData[0] == (byte)'H' && audioData[1] == (byte)'C' && (char)audioData[2] == (byte)'A')
                        outFilePath += ".hca";

                    using FileStream audioFile = new FileStream(outFilePath, FileMode.Create);
                    audioFile.Write(audioData);
                }
            }
        }
    }
}
