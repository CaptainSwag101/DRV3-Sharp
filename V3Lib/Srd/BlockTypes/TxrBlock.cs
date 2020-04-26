using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V3Lib.Srd.BlockTypes
{
    public enum TextureFormat
    {
        Unknown     = 0x00,
        ARGB8888    = 0x01,
        BGR565      = 0x02,
        BGRA4444    = 0x05,
        DXT1RGB     = 0x0F,
        DXT5        = 0x11,
        BC5         = 0x14,
        BC4         = 0x16,
        BPTC        = 0x1C
    }

    public sealed class TxrBlock : Block
    {
        public int Unknown10;
        public ushort Swizzle;
        public ushort DisplayWidth;
        public ushort DisplayHeight;
        public ushort Scanline;
        public TextureFormat Format;
        public byte Unknown1D;
        public byte Palette;
        public byte PaletteId;

        public override void DeserializeData(byte[] rawData)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(rawData));

            Unknown10 = reader.ReadInt32();
            Swizzle = reader.ReadUInt16();
            DisplayWidth = reader.ReadUInt16();
            DisplayHeight = reader.ReadUInt16();
            Scanline = reader.ReadUInt16();
            Format = (TextureFormat)reader.ReadByte();
            Unknown1D = reader.ReadByte();
            Palette = reader.ReadByte();
            PaletteId = reader.ReadByte();

            reader.Close();
            reader.Dispose();
        }

        public override byte[] SerializeData()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(ms);

            writer.Write(Unknown10);
            writer.Write(Swizzle);
            writer.Write(DisplayWidth);
            writer.Write(DisplayHeight);
            writer.Write(Scanline);
            writer.Write((int)Format);
            writer.Write(Unknown1D);
            writer.Write(Palette);
            writer.Write(PaletteId);

            byte[] result = ms.ToArray();
            writer.Close();
            writer.Dispose();
            return result;
        }

        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"{nameof(Unknown10)}: {Unknown10}\n");
            sb.Append($"Swizzle: {Swizzle}\n");
            sb.Append($"Display Width: {DisplayWidth}\n");
            sb.Append($"Display Height: {DisplayHeight}\n");
            sb.Append($"Scanline: {Scanline}\n");
            sb.Append($"Format: {Format}\n");
            sb.Append($"{nameof(Unknown1D)}: {Unknown1D}\n");
            sb.Append($"Palette: {Palette}\n");
            sb.Append($"Palette ID: {PaletteId}");

            return sb.ToString();
        }
    }
}
