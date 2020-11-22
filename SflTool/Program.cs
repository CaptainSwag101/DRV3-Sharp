using System;
using System.IO;
using Newtonsoft.Json;
using V3Lib.Resource.SFL;

namespace SflTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SFL Tool by CaptainSwag101\n" +
                "Version 1.1.0, built on 2020-11-22\n");

            if (args.Length == 0)
            {
                Console.WriteLine("ERROR: No targets specified.");
                return;
            }

            foreach (string arg in args)
            {
                FileInfo info = new FileInfo(arg);
                if (!info.Exists)
                {
                    Console.WriteLine($"ERROR: File \"{arg}\" does not exist, skipping.");
                    continue;
                }

                if (info.Extension.ToLowerInvariant() == ".sfl")
                {
                    // Convert SFL to JSON
                    using FileStream fs = new(info.FullName, FileMode.Open);
                    SFLScene sfl = new(fs);

                    string output = JsonConvert.SerializeObject(sfl, Formatting.Indented);
                    File.WriteAllText(info.FullName.Remove(info.FullName.Length - info.Extension.Length) + ".json", output);

                }
                else if (info.Extension.ToLowerInvariant() == ".json")
                {
                    // Convert JSON to SFL
                    
                }
                else
                {
                    Console.WriteLine($"ERROR: Invalid file extension \"{info.Extension}\".");
                    continue;
                }
            }
        }
    }
}
