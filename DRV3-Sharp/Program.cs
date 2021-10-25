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

using DRV3_Sharp.Operators;
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
    static class ProgramUtils
    {
        public static string DeduceTypeFromPath(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant();
        }
    }

    class Program
    {
        private static IDanganV3Data? loadedData;
        private static string? loadedDataPath;
        private static bool unsavedChanges = false;
        private static bool shouldExit = false;

        static void Main(string[] args)
        {
            // Setup text encoding so we can use Shift-JIS encoding for certain files later on
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Do initial startup
        }

        public static IDanganV3Data? GetData()
        {
            return loadedData;
        }

        public static void LoadData(IDanganV3Data data, string path)
        {
            loadedData = data;
            loadedDataPath = path;

            // Do stuff for each filetype
        }

        public static void SaveData()
        {
            if (loadedData is null) throw new InvalidOperationException($"The current file data is null.");
            if (loadedDataPath is null) throw new InvalidOperationException($"The current file path is null.");

            using FileStream fs = new(loadedDataPath, FileMode.Create);

            if (loadedData is SpcData)
            {
                SpcSerializer.Serialize((SpcData)loadedData, fs);
            }
            else if (loadedData is StxData)
            {
                StxSerializer.Serialize((StxData)loadedData, fs);
            }
            else if (loadedData is SrdData)
            {
                // If the file doesn't end in .srd, make a secondary path that does so we can compute the accompanying file names correctly
                string srdPathCorrectedExt = Path.ChangeExtension(loadedDataPath, "srd")!;

                FileStream srdvStream = new(srdPathCorrectedExt + 'v', FileMode.Create);
                FileStream srdiStream = new(srdPathCorrectedExt + 'i', FileMode.Create);

                SrdSerializer.Serialize((SrdData)loadedData, fs, srdvStream, srdiStream);
            }

            SetUnsavedChanges(false);
        }

        public static void SetUnsavedChanges(bool state)
        {
            unsavedChanges = state;
        }

        public static void PrepareToExit()
        {
            if (unsavedChanges)
            {
                string? response = null;
                while (true)
                {
                    Console.Write("There are unsaved changes! Are you sure you want to exit? (Y/N) ");
                    response = Console.ReadLine()!.ToLowerInvariant();

                    if (response.StartsWith('n'))
                        return;
                    else if (response.StartsWith('y'))
                        break;
                }
            }

            shouldExit = true;
        }
    }
}
