using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Spc
{
    public class SpcFile
    {
        public List<SpcSubfile> Subfiles = new List<SpcSubfile>();
        private byte[] Unknown1;
        private int Unknown2;

        public void Load(string spcPath)
        {
            using BinaryReader reader = new BinaryReader(new FileStream(spcPath, FileMode.Open));

            // Verify the magic value, it could either be "CPS." (the one we want) or "$CFH" (most files in the console version, unusable for now)
            string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
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
            if (!Encoding.ASCII.GetString(reader.ReadBytes(4)).Equals("Root"))
            {
                Console.WriteLine("ERROR: Not a valid SPC file, table header invalid.");
                return;
            }
            reader.BaseStream.Seek(0x0C, SeekOrigin.Current);

            // For each subfile in the table, read the corresponding data
            for (int i = 0; i < fileCount; ++i)
            {
                SpcSubfile subfile = new SpcSubfile
                {
                    CompressionFlag = reader.ReadInt16(),
                    UnknownFlag = reader.ReadInt16(),
                    CurrentSize = reader.ReadInt32(),
                    OriginalSize = reader.ReadInt32()
                };

                int nameLength = reader.ReadInt32();
                reader.BaseStream.Seek(0x10, SeekOrigin.Current);
                int namePadding = (0x10 - (nameLength + 1) % 0x10) % 0x10;
                subfile.Name = Encoding.GetEncoding("shift-jis").GetString(reader.ReadBytes(nameLength));
                reader.BaseStream.Seek(namePadding + 1, SeekOrigin.Current);    // Discard the null terminator

                int dataPadding = (0x10 - subfile.CurrentSize % 0x10) % 0x10;
                subfile.Data = reader.ReadBytes(subfile.CurrentSize);
                reader.BaseStream.Seek(dataPadding, SeekOrigin.Current);

                Subfiles.Add(subfile);
            }
        }

        public void Save(string spcPath)
        {
            using BinaryWriter writer = new BinaryWriter(new FileStream(spcPath, FileMode.Create));

            writer.Write(Encoding.ASCII.GetBytes("CPS."));
            writer.Write(Unknown1);
            writer.Write(Subfiles.Count);
            writer.Write(Unknown2);
            writer.Write(new byte[0x10]);
            writer.Write(Encoding.ASCII.GetBytes("Root"));
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
                writer.Write(Encoding.GetEncoding("shift-jis").GetBytes(subfile.Name));
                writer.Write(new byte[namePadding + 1]);

                int dataPadding = (0x10 - subfile.CurrentSize % 0x10) % 0x10;
                writer.Write(subfile.Data);
                writer.Write(new byte[dataPadding]);
            }
        }

        /// <summary>
        /// Extracts a specified subfile from the SPC archive into the given directory.
        /// </summary>
        /// <param name="filename">The name of the subfile to extract.</param>
        /// <param name="outputLocation">The directory to save the file into.</param>
        /// <param name="decompress">Whether the subfile should be decompressed before extracting. Unless you know what you're doing, leave this set to "true".</param>
        public void ExtractSubfile(string filename, string outputLocation, bool decompress = true)
        {
            foreach (SpcSubfile subfile in Subfiles)
            {
                if (filename == subfile.Name)
                {
                    outputLocation.TrimEnd('\\');
                    outputLocation.TrimEnd('/');

                    if (decompress)
                    {
                        subfile.Decompress();
                    }

                    using FileStream output = new FileStream(outputLocation + Path.DirectorySeparatorChar + filename, FileMode.Create);
                    output.Write(subfile.Data);
                    output.Close();

                    return;
                }
            }

            Console.WriteLine($"ERROR: Unable to find a subfile called \"{filename}\".");
        }

        /// <summary>
        /// Inserts a file into the SPC archive. If a file with the same name already exists within the archive, it will be replaced.
        /// </summary>
        /// <param name="filename">The path of the file to be inserted into the SPC archive.</param>
        /// <param name="compress">Whether the subfile should be compressed before inserting. Unless you know what you're doing, leave this set to "true".</param>
        public void InsertSubfile(string filename, bool compress = true)
        {
            FileInfo info = new FileInfo(filename);

            if (!info.Exists)
            {
                Console.WriteLine($"ERROR: Target file\"{info.FullName}\" does not exist.");
                return;
            }

            // Check if a subfile already exists with the specified name
            int existingIndex = -1;
            for (int s = 0; s < Subfiles.Count; ++s)
            {
                if (info.Name == Subfiles[s].Name)
                {
                    Console.WriteLine("The specified file already exists within the SPC archive. Overwrite? (y/N)");
                    string yesNo = Console.ReadLine().ToLowerInvariant();
                    if (yesNo.StartsWith("y"))
                    {
                        existingIndex = s;
                        break;
                    }

                    return;
                }
            }

            using BinaryReader reader = new BinaryReader(new FileStream(filename, FileMode.Open));
            int subfileSize = (int)reader.BaseStream.Length;
            SpcSubfile subfileToInject = new SpcSubfile
            {
                CompressionFlag = 1,
                UnknownFlag = (short)(subfileSize > ushort.MaxValue ? 8 : 4),   // seems like this flag might relate to size? This is a BIG guess though.
                CurrentSize = subfileSize,
                OriginalSize = subfileSize,
                Name = info.Name,
                Data = reader.ReadBytes(subfileSize)
            };
            reader.Close();

            if (compress)
            {
                subfileToInject.Compress();
            }

            // Check if a subfile already exists with the specified name and replace
            if (existingIndex != -1)
            {
                Subfiles[existingIndex] = subfileToInject;
                return;
            }

            // We should only reach this code if there is not an existing subfile with the same name
            Subfiles.Add(subfileToInject);
        }
    }
}
