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

using DRV3_Sharp.Formats.Archive.SPC;
using DRV3_Sharp.Formats.Resource.SRD;
using System;
using System.IO;
using System.Text;

namespace DRV3_Sharp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Setup text encoding so we can use Shift-JIS encoding for certain files later on
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Console.WriteLine("Hello World!");

            string srdPath = @"C:\Users\James\Desktop\Games\DRV3 Tests\Windows\SRD Tests\ID001_sch_1F\model.srd";
            //string srdPath = @"C:\Users\James\Desktop\Games\DRV3 Tests\Windows\SRD Tests\v3_font00_JP\v3_font00.stx";
            using FileStream srdStream = new(srdPath, FileMode.Open);

            // If the file doesn't end in .srd, make a secondary path that does so we can compute the accompanying file names correctly
            string srdPath2 = Path.ChangeExtension(srdPath!, "srd")!;

            // Try and open the accompanying resource data files if they are present (not always)
            FileStream? srdvStream = null;
            try
            {
                srdvStream = new(srdPath2 + 'v', FileMode.Open);
            }
            catch (FileNotFoundException ex)
            { }

            FileStream? srdiStream = null;
            try
            {
                srdiStream = new(srdPath2 + 'i', FileMode.Open);
            }
            catch (FileNotFoundException ex)
            { }

            SrdSerializer.Deserialize(srdStream, srdvStream, srdiStream, out SrdData testSrd);
            srdvStream?.Close();
            srdiStream?.Close();
            return;

            //SpcData testSpc = new();
            //using FileStream outputTestStream = new(@"C:\Users\James\Desktop\Games\DRV3 Tests\Program Tests\test.spc", FileMode.Create);
            //SpcSerializer.Serialize(testSpc, outputTestStream);
            //return;
        }
    }
}
