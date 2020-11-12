using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Rsct
{
    public class RsctFile
    {
        public List<(string String, uint Unknown)> Strings = new List<(string String, uint Unknown)>();

        public void Load(string rsctPath)
        {
            using var reader = new BinaryReader(new FileStream(rsctPath, FileMode.Open), Encoding.Unicode);

            // Verify the magic value, it should be "RSCT"
            string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (magic != "RSCT")
            {
                Console.WriteLine($"ERROR: Invalid magic value, expected \"RSCT\" but got \"{magic}\".");
                return;
            }

            // Skip 4 bytes of padding
            reader.ReadUInt32();

            int stringCount = reader.ReadInt32();
            int unknown0C = reader.ReadInt32(); // always 0x00000014?
            int stringDataStart = reader.ReadInt32();

            for (int i = 0; i < stringCount; ++i)
            {
                uint unk = reader.ReadUInt32();
                uint stringOffset = reader.ReadUInt32();

                long lastPos = reader.BaseStream.Position;
                reader.BaseStream.Seek(stringOffset, SeekOrigin.Begin);

                uint strLen = reader.ReadUInt32();
                string str = Utils.ReadNullTerminatedString(reader, Encoding.Unicode);

                Strings.Add((str, unk));

                reader.BaseStream.Seek(lastPos, SeekOrigin.Begin);
            }
        }

        public void Save(string rsctPath)
        {
            using var writer = new BinaryWriter(new FileStream(rsctPath, FileMode.Create));

            writer.Write(Encoding.ASCII.GetBytes("RSCT"));
            writer.Write((uint)0);

            writer.Write(Strings.Count);
            writer.Write((int)14);  // unknown0C

            using var strInfoStream = new MemoryStream();
            using var strInfoWriter = new BinaryWriter(strInfoStream);
            using var stringStream = new MemoryStream();
            using var stringWriter = new BinaryWriter(stringStream, Encoding.Unicode);
            foreach (var strTuple in Strings)
            {
                strInfoWriter.Write(strTuple.Unknown);
                strInfoWriter.Write((uint)(writer.BaseStream.Position + (8 * Strings.Count) + stringStream.Position));

                stringWriter.Write(Encoding.Unicode.GetByteCount(strTuple.String));
                stringWriter.Write(strTuple.String);
                stringWriter.Write((ushort)0);
            }

            writer.Write((uint)(writer.BaseStream.Position + 4 + strInfoStream.Length));    // stringDataStart
            writer.Write(strInfoStream.ToArray());
            writer.Write(stringStream.ToArray());
        }
    }
}
