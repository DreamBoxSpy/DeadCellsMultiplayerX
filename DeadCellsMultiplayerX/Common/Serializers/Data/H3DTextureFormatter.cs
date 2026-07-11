using dc.h3d.mat;
using MessagePack;
using MessagePack.Formatters;
using dc.hl.types;
using CoreLibrary.Core.Extensions;
using System.Diagnostics;

namespace DeadCellsMultiplayerX.Common.Serializers.Data
{
    public class H3DTextureFormatter : IMessagePackFormatter<Texture?>
    {
        private static readonly dc.hxd.PixelFormat[] FormatLookup =
        {
            new dc.hxd.PixelFormat.ARGB(),
            new dc.hxd.PixelFormat.BGRA(),
            new dc.hxd.PixelFormat.RGBA(),
            new dc.hxd.PixelFormat.RGBA16F(),
            new dc.hxd.PixelFormat.RGBA32F(),
            new dc.hxd.PixelFormat.R8(),
            new dc.hxd.PixelFormat.R16F(),
            new dc.hxd.PixelFormat.R32F(),
            new dc.hxd.PixelFormat.RG8(),
            new dc.hxd.PixelFormat.RG16F(),
            new dc.hxd.PixelFormat.RG32F(),
            new dc.hxd.PixelFormat.RGB8(),
            new dc.hxd.PixelFormat.RGB16F(),
            new dc.hxd.PixelFormat.RGB32F(),
            new dc.hxd.PixelFormat.SRGB(),
            new dc.hxd.PixelFormat.SRGB_ALPHA(),
            new dc.hxd.PixelFormat.RGB10A2(),
            new dc.hxd.PixelFormat.RG11B10UF()
        };

        private static readonly TextureFlags[] FlagLookup =
        {
            new TextureFlags.Target(),
            new TextureFlags.Cube(),
            new TextureFlags.MipMapped(),
            new TextureFlags.ManualMipMapGen(),
            new TextureFlags.IsNPOT(),
            new TextureFlags.NoAlloc(),
            new TextureFlags.Dynamic(),
            new TextureFlags.AlphaPremultiplied(),
            new TextureFlags.WasCleared(),
            new TextureFlags.Loading(),
            new TextureFlags.Serialize(),
            new TextureFlags.IsArray()
        };

        public Texture? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            reader.ReadArrayHeader();

            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            int formatIndex = reader.ReadInt32();
            int flagsInt = reader.ReadInt32();


            var format = GetPixelFormat((HXDFormatters)formatIndex);
            var flagsArray = FlagsIntToArray(flagsInt);
            return new Texture(width, height, flagsArray, format, null);
        }

        public void Serialize(ref MessagePackWriter writer, Texture? value, MessagePackSerializerOptions options)
        {
            if (value == null) { writer.WriteNil(); return; }

            writer.WriteArrayHeader(4);
            writer.Write(value.width);
            writer.Write(value.height);
            writer.Write(checked((byte)value.format.RawIndex));
            writer.Write(value.flags);
        }
        private static dc.hxd.PixelFormat GetPixelFormat(HXDFormatters format)
        {
            if ((byte)format >= FormatLookup.Length)
                throw new ArgumentOutOfRangeException(nameof(format), format, "不支持的像素格式");
            return FormatLookup[(byte)format];
        }


        private static ArrayObj FlagsIntToArray(int flags)
        {
            var list = new List<TextureFlags>();
            for (int i = 0; i < FlagLookup.Length; i++)
            {
                if ((flags & (1 << i)) != 0)
                    list.Add(FlagLookup[i]);
            }
            return list.ToArrayObj();
        }

    }


    public enum HXDFormatters : byte
    {
        // Token: 0x04000630 RID: 1584
        ARGB,
        // Token: 0x04000631 RID: 1585
        BGRA,
        // Token: 0x04000632 RID: 1586
        RGBA,
        // Token: 0x04000633 RID: 1587
        RGBA16F,
        // Token: 0x04000634 RID: 1588
        RGBA32F,
        // Token: 0x04000635 RID: 1589
        R8,
        // Token: 0x04000636 RID: 1590
        R16F,
        // Token: 0x04000637 RID: 1591
        R32F,
        // Token: 0x04000638 RID: 1592
        RG8,
        // Token: 0x04000639 RID: 1593
        RG16F,
        // Token: 0x0400063A RID: 1594
        RG32F,
        // Token: 0x0400063B RID: 1595
        RGB8,
        // Token: 0x0400063C RID: 1596
        RGB16F,
        // Token: 0x0400063D RID: 1597
        RGB32F,
        // Token: 0x0400063E RID: 1598
        SRGB,
        // Token: 0x0400063F RID: 1599
        SRGB_ALPHA,
        // Token: 0x04000640 RID: 1600
        RGB10A2,
        // Token: 0x04000641 RID: 1601
        RG11B10UF
    }
}