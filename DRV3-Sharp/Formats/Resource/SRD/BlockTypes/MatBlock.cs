using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DRV3_Sharp.Formats.Resource.SRD.BlockTypes
{
    record MatBlock : ISrdBlock
    {
        public uint Unknown10;
        public float Unknown14;
        public float Unknown18;
        public float Unknown1C;
        public ushort Unknown20;
        public ushort Unknown22;
        public List<(string, string)> MapTexturePairs = new();
        public List<(string Name, byte[] Data)> ExtraMaterialInfo = new();
        public string MaterialName;
        public string[] MaterialShaderReferences;

        public MatBlock(byte[] mainData, byte[] subData)
        {
            // Read main data
            using BinaryReader reader = new(new MemoryStream(mainData));

            Unknown10 = reader.ReadUInt32();
            Unknown14 = reader.ReadSingle();
            Unknown18 = reader.ReadSingle();
            Unknown1C = reader.ReadSingle();
            Unknown20 = reader.ReadUInt16();
            Unknown22 = reader.ReadUInt16();
            ushort stringMapStart = reader.ReadUInt16();
            ushort stringMapCount = reader.ReadUInt16();

            reader.BaseStream.Seek(stringMapStart, SeekOrigin.Begin);
            for (int m = 0; m < stringMapCount; ++m)
            {
                ushort textureNameOffset = reader.ReadUInt16();
                ushort mapNameOffset = reader.ReadUInt16();

                long oldPos = reader.BaseStream.Position;

                reader.BaseStream.Seek(textureNameOffset, SeekOrigin.Begin);
                string textureName = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);
                reader.BaseStream.Seek(mapNameOffset, SeekOrigin.Begin);
                string mapName = Utils.ReadNullTerminatedString(reader, Encoding.ASCII);

                MapTexturePairs.Add((mapName, textureName));

                reader.BaseStream.Seek(oldPos, SeekOrigin.Begin);
            }

            // Decode the RSI sub-block
            RsiBlock rsi;
            using MemoryStream subBlockStream = new(subData);
            BlockSerializer.Deserialize(subBlockStream, null, null, out ISrdBlock block);
            if (block is RsiBlock) rsi = (RsiBlock)block;
            else throw new InvalidDataException("The first sub-block was not an RSI block.");

            // Copy over extra shader/material info from the RSI data
            ExtraMaterialInfo = rsi.LocalResourceData;

            // Material name is the first RSI resource string
            if (rsi.ResourceStrings.Count > 0)
                MaterialName = rsi.ResourceStrings[0];
            else
                throw new InvalidDataException("The MAT's resource sub-block did not contain the material name.");

            MaterialShaderReferences = new string[rsi.ResourceStrings.Count - 1];
            if (rsi.ResourceStrings.Count > 1)
            {
                rsi.ResourceStrings.CopyTo(1, MaterialShaderReferences, 0, rsi.ResourceStrings.Count - 1);
            }
        }
    }
}
