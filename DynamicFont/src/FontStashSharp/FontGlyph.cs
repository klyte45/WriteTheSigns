using System.Collections.Generic;
using UnityEngine;

namespace FontStashSharp
{
    internal class FontGlyph
    {
        private readonly Dictionary<int, int> _kernings = new Dictionary<int, int>();

        public Font Font;
        public FontAtlas Atlas;
        public int Codepoint;
        public int Index;
        public int Size;
        public int Blur;
        public Rect Bounds;
        public int XAdvance;
        public int XOffset;
        public int YOffset;

        public int Pad => PadFromBlur(Blur);

        public int GetKerning(FontGlyph nextGlyph)
        {
            if (_kernings.TryGetValue(nextGlyph.Index, out int result))
            {
                return result;
            }
            result = Font._font.stbtt_GetGlyphKernAdvance(Index, nextGlyph.Index);
            _kernings[nextGlyph.Index] = result;

            return result;
        }

        public static int PadFromBlur(int blur) => blur + 2;
    }
}
