using FontStashSharp;
using Klyte.DynamicTextProps.Overrides;
using SpriteFontPlus.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpriteFontPlus
{
    public class DynamicSpriteFont
    {
        internal struct TextureEnumerator : IEnumerable<Texture2D>
        {
            private readonly FontSystem _font;

            public TextureEnumerator(FontSystem font) => _font = font;

            public IEnumerator<Texture2D> GetEnumerator()
            {
                foreach (FontAtlas atlas in _font.Atlases)
                {
                    yield return atlas.Texture;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private readonly FontSystem _fontSystem;

        public IEnumerable<Texture2D> Textures => new TextureEnumerator(_fontSystem);

        public int Size
        {
            get => _fontSystem.FontSize;
            set => _fontSystem.FontSize = value;
        }

        public float Spacing
        {
            get => _fontSystem.Spacing;
            set => _fontSystem.Spacing = value;
        }

        public bool UseKernings
        {
            get => _fontSystem.UseKernings;

            set => _fontSystem.UseKernings = value;
        }

        public int? DefaultCharacter
        {
            get => _fontSystem.DefaultCharacter;

            set => _fontSystem.DefaultCharacter = value;
        }

        public event EventHandler CurrentAtlasFull
        {
            add {
                _fontSystem.CurrentAtlasFull += value;
            }

            remove {
                _fontSystem.CurrentAtlasFull -= value;
            }
        }

        private DynamicSpriteFont(byte[] ttf, int defaultSize, int textureWidth, int textureHeight, int blur)
        {
            _fontSystem = new FontSystem(textureWidth, textureHeight, blur)
            {
                FontSize = defaultSize
            };

            _fontSystem.AddFontMem(ttf);
        }

        public float DrawString(BasicRenderInformation batch, string text, Vector2 pos, Color color) => DrawString(batch, text, pos, color, Vector2.one);

        public float DrawString(BasicRenderInformation bri, string text, Vector2 pos, Color color, Vector2 scale)
        {
            _fontSystem.Color = color;
            _fontSystem.Scale = scale;

            float result = _fontSystem.DrawText(bri, pos.x, pos.y, text);

            _fontSystem.Scale = Vector2.one;

            return result;
        }

        public void AddTtf(byte[] ttf) => _fontSystem.AddFontMem(ttf);

        public void AddTtf(Stream ttfStream) => AddTtf(ttfStream.ToByteArray());

        public Vector2 MeasureString(string text)
        {
            var bounds = new FontStashSharp.Bounds();
            _fontSystem.TextBounds(0, 0, text, ref bounds);

            return new Vector2(bounds.X2, bounds.Y2);
        }

        public Rect GetTextBounds(Vector2 position, string text)
        {
            var bounds = new FontStashSharp.Bounds();
            _fontSystem.TextBounds(position.x, position.y, text, ref bounds);

            return new Rect((int)bounds.X, (int)bounds.Y, (int)(bounds.X2 - bounds.X), (int)(bounds.Y2 - bounds.Y));
        }

        public void Reset(int width, int height) => _fontSystem.Reset(width, height);

        public void Reset() => _fontSystem.Reset();

        public static DynamicSpriteFont FromTtf(byte[] ttf, int defaultSize, int textureWidth = 1024, int textureHeight = 1024, int blur = 0) => new DynamicSpriteFont(ttf, defaultSize, textureWidth, textureHeight, blur);

        public static DynamicSpriteFont FromTtf(Stream ttfStream, int defaultSize, int textureWidth = 1024, int textureHeight = 1024, int blur = 0) => FromTtf(ttfStream.ToByteArray(), defaultSize, textureWidth, textureHeight, blur);
    }
}