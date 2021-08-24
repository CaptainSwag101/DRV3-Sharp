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

using DRV3_Sharp_Library.Formats;
using DRV3_Sharp_Library.Formats.Archive.SPC;
using DRV3_Sharp_Library.Formats.Resource.SRD;
using DRV3_Sharp_Library.Formats.Text.STX;
using PowerArgs;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DRV3_Sharp
{
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    public class FileOperations
    {
        [HelpHook, ArgShortcut("-?"), ArgDescription("Shows this help")]
        public bool Help { get; set; }

        [ArgActionMethod, ArgDescription("Creates a new file at the specified path, with an optionally-specified format, and prompts the user to operate on the file.")]
        public void Create(
            [ArgRequired, ArgDescription("The file to create.")]string path,
            [ArgDescription("Overrides the data type inferred from the file extension."), ArgDefaultValue(null)] string? type
            )
        {
            if (type is null) type = ProgramUtils.DeduceTypeFromPath(path);

            Console.WriteLine($"(PLACEHOLDER) Pretending to create file of type {type} at {path}.");
        }

        [ArgActionMethod, ArgDescription("Opens the file at the specified path, with an optionally-specified format, and prompts the user to operate on the file.")]
        public void Open(
            [ArgRequired, ArgDescription("The file to open."), ArgExistingFile]string path,
            [ArgDescription("Overrides the data type inferred from the file extension."), ArgDefaultValue(null)]string? type
            )
        {
            if (type is null) type = ProgramUtils.DeduceTypeFromPath(path);

            Console.WriteLine($"Attempting to open file of type {type} at {path}.");

            using FileStream fs = new(path, FileMode.Open);

            switch (type)
            {
                case "spc":
                    {
                        SpcSerializer.Deserialize(fs, out SpcData loaded);
                        Program.LoadData(loaded, path);
                        break;
                    }

                case "stx":
                    {
                        StxSerializer.Deserialize(fs, out StxData loaded);
                        Program.LoadData(loaded, path);
                        break;
                    }

                case "srd":
                    {
                        // If the file doesn't end in .srd, make a secondary path that does so we can compute the accompanying file names correctly
                        string srdPathCorrectedExt = Path.ChangeExtension(path, "srd")!;

                        // Try and open the accompanying resource data files if they are present (not always)
                        FileStream? srdvStream = null;
                        try
                        {
                            srdvStream = new(srdPathCorrectedExt + 'v', FileMode.Open);
                        }
                        catch (FileNotFoundException ex)
                        { }

                        FileStream? srdiStream = null;
                        try
                        {
                            srdiStream = new(srdPathCorrectedExt + 'i', FileMode.Open);
                        }
                        catch (FileNotFoundException ex)
                        { }

                        SrdSerializer.Deserialize(fs, srdvStream, srdiStream, out SrdData loaded);
                        srdvStream?.Close();
                        srdiStream?.Close();

                        Program.LoadData(loaded, path);
                        break;
                    }
            }
        }
    }

    static class ProgramUtils
    {
        public static string DeduceTypeFromPath(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant();
        }
    }

    class Program
    {
        private static IDanganV3Data? _loadedData = null;
        private static string? _loadedDataPath = null;

        static void Main(string[] args)
        {
            // Setup text encoding so we can use Shift-JIS encoding for certain files later on
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Do initial setup
            Args.InvokeAction<FileOperations>(args);
        }

        public static void LoadData(IDanganV3Data data, string path)
        {
            _loadedData = data;
            _loadedDataPath = path;

            // Use PowerArgs to generate prompts to operate on whatever file was loaded


            Debugger.Break();
        }
    }
}
