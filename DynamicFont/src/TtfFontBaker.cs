using SpriteFontPlus.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static StbTrueTypeSharp.PackContext;

namespace SpriteFontPlus
{
    public static class TtfFontBaker
    {
        public static TtfFontBakerResult Bake(Stream ttfStream, float fontPixelHeight, int bitmapWidth, int bitmapHeight, IEnumerable<CharacterRange> characterRanges) => Bake(ttfStream.ToByteArray(), fontPixelHeight, bitmapWidth, bitmapHeight, characterRanges);

        public static TtfFontBakerResult Bake(byte[] ttf, float fontPixelHeight,
            int bitmapWidth, int bitmapHeight,
            IEnumerable<CharacterRange> characterRanges)
        {
            if (ttf == null || ttf.Length == 0)
            {
                throw new ArgumentNullException(nameof(ttf));
            }

            if (fontPixelHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fontPixelHeight));
            }

            if (bitmapWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bitmapWidth));
            }

            if (bitmapHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bitmapHeight));
            }

            if (characterRanges == null)
            {
                throw new ArgumentNullException(nameof(characterRanges));
            }

            if (!characterRanges.Any())
            {
                throw new ArgumentException("characterRanges must have a least one value.");
            }

            byte[] pixels;
            var glyphs = new Dictionary<int, GlyphInfo>();
            var fontInfo = new StbTrueTypeSharp.FontInfo();
            if (fontInfo.stbtt_InitFont(ttf, 0) == 0)
            {
                throw new Exception("Failed to init font.");
            }

            float scaleFactor = fontInfo.stbtt_ScaleForPixelHeight(fontPixelHeight);

            fontInfo.stbtt_GetFontVMetrics(out int ascent, out int descent, out int lineGap);

            pixels = new byte[bitmapWidth * bitmapHeight];
            var pc = new StbTrueTypeSharp.PackContext();
            pc.stbtt_PackBegin(pixels, bitmapWidth,
                bitmapHeight, bitmapWidth, 1);

            foreach (CharacterRange range in characterRanges)
            {
                if (range.Start > range.End)
                {
                    continue;
                }

                var cd = new stbtt_packedchar[range.End - range.Start + 1];
                for (int i = 0; i < cd.Length; ++i)
                {
                    cd[i] = new stbtt_packedchar();
                }

                pc.stbtt_PackFontRange(ttf, 0, fontPixelHeight,
                    range.Start,
                    range.End - range.Start + 1,
                    cd);

                for (int i = 0; i < cd.Length; ++i)
                {
                    float yOff = cd[i].yoff;
                    yOff += ascent * scaleFactor;

                    var glyphInfo = new GlyphInfo
                    {
                        X = cd[i].x0,
                        Y = cd[i].y0,
                        Width = cd[i].x1 - cd[i].x0,
                        Height = cd[i].y1 - cd[i].y0,
                        XOffset = (int)cd[i].xoff,
                        YOffset = (int)Math.Round(yOff),
                        XAdvance = (int)Math.Round(cd[i].xadvance)
                    };

                    glyphs[(char)(i + range.Start)] = glyphInfo;
                }
            }

            return new TtfFontBakerResult(glyphs, fontPixelHeight, pixels, bitmapWidth, bitmapHeight);
        }
    }
}