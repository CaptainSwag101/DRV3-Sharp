using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.CriWare
{
    public class AwbFile
    {
        public Dictionary<short, byte[]> AudioData = new Dictionary<short, byte[]>();

        public void Load(string awbPath)
        {
            using BinaryReader reader = new BinaryReader(new FileStream(awbPath, FileMode.Open));

            string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (magic != "AFS2")
            {
                //Invalid AWB file
                return;
            }

            uint unknown = reader.ReadUInt32();
            int numEntries = reader.ReadInt32();
            int alignment = reader.ReadInt32();

            List<short> ids = new List<short>();
            List<int> ends = new List<int>();

            for (int entry = 0; entry < numEntries; ++entry)
            {
                ids.Add(reader.ReadInt16());
            }

            int headerEnd = reader.ReadInt32();
            for (int entry = 0; entry < numEntries; ++entry)
            {
                ends.Add(reader.ReadInt32());
            }

            int audioStart = 0;
            int audioEnd = headerEnd;
            for (int entry = 0; entry < numEntries; ++entry)
            {
                audioStart = audioEnd;
                if ((audioEnd % alignment) > 0)
                {
                    audioStart += (alignment - (audioEnd % alignment));
                }
                audioEnd = ends[entry];

                reader.BaseStream.Seek(audioStart, SeekOrigin.Begin);
                byte[] audioData = reader.ReadBytes(audioEnd - audioStart);

                AudioData.Add(ids[entry], audioData);
            }
        }
    }
}
