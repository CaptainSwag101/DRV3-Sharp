using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SpcTool
{
    class SpcFile
    {
        public List<SpcSubfile> Subfiles = new List<SpcSubfile>();
        private byte[] Unknown1;
        private int Unknown2;

        public void Load(string filepath)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(File.ReadAllBytes(filepath)));

            // Verify the magic value, it could either be "CPS." (the one we want) or "$CFH" (most files in the console version, unusable for now)
            string magic = new ASCIIEncoding().GetString(reader.ReadBytes(4));
            if (magic == "$CFH")
            {
                // decompress using SRD method first, then resume
                return;
            }

            if (magic != "CPS.")
            {
                Console.WriteLine("ERROR: Not a valid SPC file, magic number invalid.");
                return;
            }

            // Read the first set of data
            Unknown1 = reader.ReadBytes(0x24);
            int fileCount = reader.ReadInt32();
            Unknown2 = reader.ReadInt32();
            reader.BaseStream.Seek(0x10, SeekOrigin.Current);

            // Verify file table header, should be "Root"
            if (!new ASCIIEncoding().GetString(reader.ReadBytes(4)).Equals("Root"))
            {
                Console.WriteLine("ERROR: Not a valid SPC file, table header invalid.");
                return;
            }
            reader.BaseStream.Seek(0x0C, SeekOrigin.Current);

            // For each subfile in the table, read the corresponding data
            for (int i = 0; i < fileCount; ++i)
            {
                SpcSubfile subfile = new SpcSubfile();
                subfile.CompressionFlag = reader.ReadInt16();
                subfile.UnknownFlag = reader.ReadInt16();
                subfile.CurrentSize = reader.ReadInt32();
                subfile.OriginalSize = reader.ReadInt32();

                int nameLength = reader.ReadInt32();
                reader.BaseStream.Seek(0x10, SeekOrigin.Current);
                int namePadding = (0x10 - (nameLength + 1) % 0x10) % 0x10;
                subfile.Name = new ASCIIEncoding().GetString(reader.ReadBytes(nameLength));
                reader.BaseStream.Seek(namePadding + 1, SeekOrigin.Current);    // Discard the null terminator

                int dataPadding = (0x10 - subfile.CurrentSize % 0x10) % 0x10;
                subfile.Data = reader.ReadBytes(subfile.CurrentSize);
                reader.BaseStream.Seek(dataPadding, SeekOrigin.Current);

                Subfiles.Add(subfile);
            }

            reader.Close();
        }

        public void Save(string filepath)
        {
            BinaryWriter writer = new BinaryWriter(new FileStream(filepath, FileMode.OpenOrCreate));

            writer.Write(new ASCIIEncoding().GetBytes("CPS."));
            writer.Write(Unknown1);
            writer.Write(Subfiles.Count);
            writer.Write(Unknown2);
            writer.Write(new byte[0x10]);
            writer.Write(new ASCIIEncoding().GetBytes("Root"));
            writer.Write(new byte[0x0C]);

            foreach (SpcSubfile subfile in Subfiles)
            {
                writer.Write(subfile.CompressionFlag);
                writer.Write(subfile.UnknownFlag);
                writer.Write(subfile.CurrentSize);
                writer.Write(subfile.OriginalSize);
                writer.Write(subfile.Name.Length);
                writer.Write(new byte[0x10]);

                int namePadding = (0x10 - (subfile.Name.Length + 1) % 0x10) % 0x10;
                writer.Write(new ASCIIEncoding().GetBytes(subfile.Name));
                writer.Write(new byte[namePadding + 1]);

                int dataPadding = (0x10 - subfile.CurrentSize % 0x10) % 0x10;
                writer.Write(subfile.Data);
                writer.Write(new byte[dataPadding]);
            }

            writer.Close();
        }
    }
}
