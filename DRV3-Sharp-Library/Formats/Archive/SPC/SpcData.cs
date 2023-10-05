using System;
using System.Collections.Generic;

namespace DRV3_Sharp_Library.Formats.Archive.SPC;

public class ArchivedFile
{
     public string Name;
     public short UnknownFlag;
     public int OriginalSize;
     private Memory<byte> _rawData;

     public ArchivedFile(string name, short unknownFlag, int originalSize, bool isCompressed, byte[] rawData)
     {
          Name = name;
          UnknownFlag = unknownFlag;
          OriginalSize = originalSize;
          IsCompressed = isCompressed;
          _rawData = rawData;
     }

     public ArchivedFile()
     {
          Name = string.Empty;
          UnknownFlag = 4;
          OriginalSize = 0;
          IsCompressed = false;
          _rawData = Array.Empty<byte>();
     }

     public bool IsCompressed
     {
          get;
          private set;
     }

     public Memory<byte> Data
     {
          get
          {
               return IsCompressed ? SpcCompressor.Decompress(_rawData.Span) : _rawData;
          }

          set
          {
               byte[] compressedData = SpcCompressor.Compress(value.Span);
               if (compressedData.Length >= value.Length)
               {
                    _rawData = value;
                    IsCompressed = false;
               }
               else
               {
                    _rawData = compressedData;
                    IsCompressed = true;
               }
          }
     }
};

public class SpcData : IDanganV3Data
{
     public int Unknown2C;
     public List<ArchivedFile> Files;

     public SpcData(int unknown2C, List<ArchivedFile> files)
     {
          Unknown2C = unknown2C;
          Files = files;
     }
}