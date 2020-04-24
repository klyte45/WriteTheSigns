using Klyte.Commons.Utils;
using StbTrueTypeSharp;

namespace FontStashSharp
{
    internal class Font
    {
        private float AscentBase, DescentBase, LineHeightBase, FontHeight;

        public float Ascent { get; private set; }
        public float Descent { get; private set; }
        public float LineHeight { get; private set; }
        public float Scale { get; private set; }

        public FontInfo _font = new FontInfo();

        public void Recalculate(float size)
        {
            Ascent = AscentBase * size;
            Descent = DescentBase * size;
            LineHeight = LineHeightBase * size;
            Scale = _font.stbtt_ScaleForPixelHeight(size);
        }
        public void RecalculateBasedOnHeight(float height)
        {
            float tempScale = _font.stbtt_ScaleForPixelHeight(100);
            int aIdx = GetGlyphIndex(char.ConvertToUtf32("Á", 0));
            int jIdx = GetGlyphIndex(char.ConvertToUtf32("j", 0));
            int x0 = 0, y0 = 0, x1 = 0, y1 = 0;

            _font.stbtt_GetGlyphBitmapBox(aIdx, tempScale, tempScale, ref x0, ref y0, ref x1, ref y1);
            int maxY = y1;
            _font.stbtt_GetGlyphBitmapBox(jIdx, tempScale, tempScale, ref x0, ref y0, ref x1, ref y1);
            int minY = y0;

            float size = height / (maxY - minY) * 45;
            Ascent = AscentBase * size;
            Descent = DescentBase * size;
            LineHeight = LineHeightBase * size;
            Scale = _font.stbtt_ScaleForPixelHeight(size);
        }

        public int GetGlyphIndex(int codepoint) => _font.stbtt_FindGlyphIndex(codepoint);

        public void BuildGlyphBitmap(int glyph, float scale, ref int advance, ref int lsb, ref int x0, ref int y0, ref int x1, ref int y1)
        {
            _font.stbtt_GetGlyphHMetrics(glyph, ref advance, ref lsb);
            _font.stbtt_GetGlyphBitmapBox(glyph, scale, scale, ref x0, ref y0, ref x1, ref y1);
        }

        public void RenderGlyphBitmap(FakePtr<byte> output, int outWidth, int outHeight, int outStride, int glyph) => _font.stbtt_MakeGlyphBitmap(output, outWidth, outHeight, outStride, Scale, Scale, glyph);

        public static Font FromMemory(byte[] data)
        {
            var font = new Font();

            if (font._font.stbtt_InitFont(data, 0) == 0)
            {
                throw new FontCreationException("stbtt_InitFont failed");
            }

            font._font.stbtt_GetFontVMetrics(out int ascent, out int descent, out int lineGap);

            font.FontHeight = ascent - descent;
            font.AscentBase = ascent / font.FontHeight;
            font.DescentBase = descent / font.FontHeight;
            font.LineHeightBase = (font.FontHeight + lineGap) / font.FontHeight;

            return font;
        }
    }
}
