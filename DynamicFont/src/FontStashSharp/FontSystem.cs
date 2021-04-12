using ColossalFramework;
using ColossalFramework.UI;
using SpriteFontPlus.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FontStashSharp
{
    internal class FontSystem
    {
        private readonly Dictionary<int, Dictionary<int, FontGlyph>> _glyphs = new Dictionary<int, Dictionary<int, FontGlyph>>();

        private readonly List<Font> _fonts = new List<Font>();
        private float _ith;
        private float _itw;
        private FontAtlas _currentAtlas;
        private Vector2 _size;
        private int _fontHeight;

        private Dictionary<string, BasicRenderInformation> m_textCache = new Dictionary<string, BasicRenderInformation>();

        public int FontHeight
        {
            get => _fontHeight;
            set
            {
                _fontHeight = value;
                foreach (Font f in _fonts)
                {
                    f.RecalculateBasedOnHeight(_fontHeight);
                }
            }
        }

        public Color Color;
        public readonly int Blur;
        public float Spacing;
        public Vector2 Scale;
        public bool UseKernings = true;

        public long LastUpdateAtlas { get; private set; }

        public int? DefaultCharacter = ' ';

        public FontAtlas CurrentAtlas
        {
            get
            {
                if (_currentAtlas == null)
                {
                    _currentAtlas = new FontAtlas(Mathf.RoundToInt(_size.x), Mathf.RoundToInt(_size.y), 256);
                    Atlases.Add(_currentAtlas);
                    LastUpdateAtlas = DateTime.Now.Ticks;
                }

                return _currentAtlas;
            }
        }

        public List<FontAtlas> Atlases { get; } = new List<FontAtlas>();

        public event Action CurrentAtlasFull;

        public FontSystem(int width, int height, int blur = 0)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (blur < 0 || blur > 20)
            {
                throw new ArgumentOutOfRangeException(nameof(blur));
            }

            Blur = blur;

            _size = new Vector2(width, height);

            _itw = 1.0f / _size.x;
            _ith = 1.0f / _size.y;
            ClearState();
        }

        public void ClearState()
        {
            FontHeight = 100;
            Color = Color.white;
            Spacing = 0;
            m_lastCoroutineStep = 0;
        }

        public void AddFontMem(byte[] data)
        {
            var font = Font.FromMemory(data);
            font.RecalculateBasedOnHeight(FontHeight);
            _fonts.Add(font);
        }

        private Dictionary<int, FontGlyph> GetGlyphsCollection(int size)
        {
            if (_glyphs.TryGetValue(size, out Dictionary<int, FontGlyph> result))
            {
                return result;
            }

            result = new Dictionary<int, FontGlyph>();
            _glyphs[size] = result;
            return result;
        }

        public BasicRenderInformation DrawText(MonoBehaviour referenceGO, float x, float y, string str, Vector3 scale, UIHorizontalAlignment alignment = UIHorizontalAlignment.Center)
        {
            BasicRenderInformation bri;
            if (string.IsNullOrEmpty(str))
            {
                if (!m_textCache.TryGetValue("", out bri))
                {
                    bri = new BasicRenderInformation
                    {
                        m_mesh = new Mesh(),
                        m_generatedMaterial = CurrentAtlas.Material
                    };
                    m_textCache[""] = bri;
                }
                return bri;
            }
            if (m_textCache.TryGetValue(str, out bri))
            {
                return bri;
            }
            else
            {
                m_textCache[str] = null;
                referenceGO.StartCoroutine(WriteTextureCoroutine(x, y, str, scale, alignment));
                return null;
            }

        }

        private static uint m_lastCoroutineStep = 0;
        private static int m_coroutineCounter = 0;

        private bool CheckCoroutineCount()
        {
            if (m_lastCoroutineStep != SimulationManager.instance.m_currentTickIndex)
            {
                m_lastCoroutineStep = SimulationManager.instance.m_currentTickIndex;
                m_coroutineCounter = 0;
            }
            if (m_coroutineCounter > 1)
            {
                return false;
            }
            m_coroutineCounter++;
            return true;
        }

        private string ToFilteredString(Dictionary<int, FontGlyph> glyphs, string str, out bool hasResetted)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            var result = "";
            hasResetted = false;
            for (int i = 0; i < str.Length; i++)
            {
                var code = char.ConvertToUtf32(str, i);
                var glyph = GetGlyph(glyphs, code, out hasResetted, true);
                if (hasResetted)
                {
                    return null;
                }
                if (glyph is null)
                {
                    if (!char.ConvertFromUtf32(code).IsNormalized(System.Text.NormalizationForm.FormKD))
                    {
                        var normalizedStr = char.ConvertFromUtf32(code).Normalize(System.Text.NormalizationForm.FormKD);
                        for (int j = 0; j < normalizedStr.Length; j++)
                        {
                            var codeJ = normalizedStr[j];
                            var glyphJ = GetGlyph(glyphs, codeJ, out hasResetted, true);
                            if (hasResetted)
                            {
                                return null;
                            }
                            if (!(glyphJ is null))
                            {
                                result += normalizedStr[j];
                            }
                        }
                    }
                    if (char.IsHighSurrogate(str[i]))
                    {
                        i++;
                    }
                }
                else
                {
                    result += str[i];
                    if (char.IsHighSurrogate(str[i]))
                    {
                        result += str[++i];
                    }
                }
            }
            return result.Trim();
        }



        private IEnumerator WriteTextureCoroutine(float x, float y, string strOr, Vector3 scale, UIHorizontalAlignment alignment)
        {
            yield return 0;
            while (!CheckCoroutineCount())
            {
                yield return 0;
            }


            var bri = new BasicRenderInformation
            {
                m_YAxisOverflows = new RangeVector { min = float.MaxValue, max = float.MinValue },
            };

            Dictionary<int, FontGlyph> glyphs = GetGlyphsCollection(FontHeight);
            var str = ToFilteredString(glyphs, strOr, out bool hasResetted);
            if (hasResetted)
            {
                yield break;
            }
            if (string.IsNullOrEmpty(str))
            {
                if (m_textCache.TryGetValue("", out bri) && m_textCache.TryGetValue(strOr, out BasicRenderInformation currentVal3) && currentVal3 == null)
                {
                    m_textCache[strOr] = bri;
                }
                else
                {
                    m_textCache.Remove(strOr);
                }
            }

            // Determine ascent and lineHeight from first character
            float ascent = 0, lineHeight = 0;
            for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
            {
                int codepoint = char.ConvertToUtf32(str, i);

                FontGlyph glyph = GetGlyph(glyphs, codepoint, out _);
                if (glyph == null)
                {
                    continue;
                }

                ascent = glyph.Font.Ascent;
                lineHeight = glyph.Font.LineHeight;
                break;
            }

            var q = new FontGlyphSquad();

            float originX = 0.0f;
            float originY = 0.0f;

            originY += ascent;
            yield return 0;
            while (!CheckCoroutineCount())
            {
                yield return 0;
            }

            var uirenderData = UIRenderData.Obtain();
            try
            {
                long lastUpdateAtlasAtStart = LastUpdateAtlas;
                uirenderData.Clear();
                PoolList<Vector3> vertices = uirenderData.vertices;
                PoolList<Vector3> normals = uirenderData.normals;
                PoolList<Color32> colors = uirenderData.colors;
                PoolList<Vector2> uvs = uirenderData.uvs;
                PoolList<int> triangles = uirenderData.triangles;



                FontGlyph prevGlyph = null;
                for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
                {
                    int codepoint = char.ConvertToUtf32(str, i);

                    if (codepoint == '\n')
                    {
                        originX = 0.0f;
                        originY += lineHeight;
                        prevGlyph = null;
                        continue;
                    }

                    FontGlyph glyph = GetGlyph(glyphs, codepoint, out hasResetted);
                    if (hasResetted)
                    {
                        yield break;
                    }
                    if (glyph == null)
                    {
                        continue;
                    }

                    GetQuad(glyph, prevGlyph, Spacing, ref originX, ref originY, ref q);
                    bri.m_YAxisOverflows.min = Mathf.Min(bri.m_YAxisOverflows.min, glyph.YOffset - glyph.Font.Descent + glyph.Font.Ascent);
                    bri.m_YAxisOverflows.max = Mathf.Max(bri.m_YAxisOverflows.max, glyph.Bounds.height + glyph.YOffset - glyph.Font.Ascent + glyph.Font.Descent);

                    q.X0 = (int)(q.X0 * -scale.x);
                    q.X1 = (int)(q.X1 * -scale.x);
                    q.Y0 = (int)(q.Y0 * scale.y);
                    q.Y1 = (int)(q.Y1 * scale.y);

                    var destRect = new Rect((int)(x + q.X0),
                                                (int)(y + q.Y0),
                                                (int)(q.X1 - q.X0),
                                                (int)(q.Y1 - q.Y0));

                    DrawChar(glyph, vertices, normals, triangles, uvs, colors, Color.white, Color.white, destRect);

                    prevGlyph = glyph;
                }
                if (bri.m_mesh == null)
                {
                    bri.m_mesh = new Mesh();
                }
                bri.m_YAxisOverflows.min *= scale.y;
                bri.m_YAxisOverflows.max *= scale.y;
                bri.m_mesh.Clear();
                bri.m_mesh.vertices = AlignVertices(vertices, alignment);
                bri.m_mesh.normals = normals.ToArray();
                bri.m_mesh.colors32 = colors.Select(z => new Color32(z.a, z.a, z.a, z.a)).ToArray();
                bri.m_mesh.uv = uvs.ToArray();
                bri.m_mesh.triangles = triangles.ToArray();
                bri.m_materialGeneratedTick = LastUpdateAtlas;
                bri.m_fontBaseLimits = new RangeVector { min = prevGlyph.Font.Descent, max = prevGlyph.Font.Ascent };
                bri.m_refText = str;
            }
            finally
            {
                uirenderData.Release();
            }
            yield return 0;
            while (!CheckCoroutineCount())
            {
                yield return 0;
            }


            bri.m_mesh.RecalculateNormals();
            SolveTangents(bri.m_mesh);
            _currentAtlas.UpdateMaterial();

            bri.m_generatedMaterial = _currentAtlas.Material;

            bri.m_sizeMetersUnscaled = bri.m_mesh.bounds.size;
            if (m_textCache.TryGetValue(strOr, out BasicRenderInformation currentVal) && currentVal == null)
            {
                m_textCache[strOr] = bri;
            }
            else
            {
                m_textCache.Remove(strOr);
            }
            yield break;
        }

        private Vector3[] AlignVertices(PoolList<Vector3> points, UIHorizontalAlignment alignment)
        {
            if (points.Count == 0)
            {
                return points.ToArray();
            }

            var max = new Vector3(points.Select(x => x.x).Max(), points.Select(x => x.y).Max(), points.Select(x => x.z).Max());
            var min = new Vector3(points.Select(x => x.x).Min(), points.Select(x => x.y).Min(), points.Select(x => x.z).Min());
            Vector3 offset = default;
            switch (alignment)
            {
                case UIHorizontalAlignment.Left:
                    offset = min;
                    break;
                case UIHorizontalAlignment.Center:
                    offset = (max + min) / 2;
                    break;
                case UIHorizontalAlignment.Right:
                    offset = max;
                    break;
            }

            return points.Select(x => x - offset).ToArray();
        }

        private void DrawChar(FontGlyph glyph, PoolList<Vector3> vertices, PoolList<Vector3> normals, PoolList<int> triangles, PoolList<Vector2> uvs, PoolList<Color32> colors, Color overrideColor, Color bottomColor, Rect bounds)
        {
            AddTriangleIndices(vertices, triangles);
            vertices.Add(new Vector2(bounds.right, 1 - bounds.bottom));
            vertices.Add(new Vector2(bounds.left, 1 - bounds.bottom));
            vertices.Add(new Vector2(bounds.left, 1 - bounds.top));
            vertices.Add(new Vector2(bounds.right, 1 - bounds.top));
            Color32 item3 = overrideColor.linear;
            Color32 item4 = bottomColor.linear;
            colors.Add(item3);
            colors.Add(item3);
            colors.Add(item4);
            colors.Add(item4);
            AddUVCoords(uvs, glyph);
        }

        public static void SolveTangents(Mesh mesh)
        {
            mesh.tangents = mesh.vertices.Select(x => new Vector4(0, 0, 1, -1)).ToArray();
            mesh.RecalculateNormals();
        }



        private static void AddTriangleIndices(PoolList<Vector3> verts, PoolList<int> triangles)
        {
            int count = verts.Count;
            int[] array = kTriangleIndices;
            for (int i = 0; i < array.Length; i++)
            {
                triangles.Add(count + array[i]);
            }
        }
        private static int[] kTriangleIndices = new int[]{
                0,
                1,
                3,
                3,
                1,
                2
        };
        private void AddUVCoords(PoolList<Vector2> uvs, FontGlyph glyph)
        {
            uvs.Add(new Vector2(glyph.Bounds.right / _currentAtlas.Width, glyph.Bounds.bottom / _currentAtlas.Height));
            uvs.Add(new Vector2(glyph.Bounds.left / _currentAtlas.Width, glyph.Bounds.bottom / _currentAtlas.Height));
            uvs.Add(new Vector2(glyph.Bounds.left / _currentAtlas.Width, glyph.Bounds.top / _currentAtlas.Height));
            uvs.Add(new Vector2(glyph.Bounds.right / _currentAtlas.Width, glyph.Bounds.top / _currentAtlas.Height));
        }
        public float TextBounds(float x, float y, string str, ref Bounds bounds)
        {
            if (string.IsNullOrEmpty(str))
            {
                return 0.0f;
            }

            Dictionary<int, FontGlyph> glyphs = GetGlyphsCollection(FontHeight);

            // Determine ascent and lineHeight from first character
            float ascent = 0, lineHeight = 0;
            for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
            {
                int codepoint = char.ConvertToUtf32(str, i);
                FontGlyph glyph = GetGlyph(glyphs, codepoint, out _);
                if (glyph == null)
                {
                    continue;
                }

                ascent = glyph.Font.Ascent;
                lineHeight = glyph.Font.LineHeight;
                break;
            }


            var q = new FontGlyphSquad();
            float startx = 0;
            float advance = 0;

            y += ascent;

            float minx, maxx, miny, maxy;
            minx = maxx = x;
            miny = maxy = y;
            startx = x;

            FontGlyph prevGlyph = null;

            for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
            {
                int codepoint = char.ConvertToUtf32(str, i);

                if (codepoint == '\n')
                {
                    x = startx;
                    y += lineHeight;
                    prevGlyph = null;
                    continue;
                }

                FontGlyph glyph = GetGlyph(glyphs, codepoint, out _);

                if (glyph == null)
                {
                    continue;
                }

                GetQuad(glyph, prevGlyph, Spacing, ref x, ref y, ref q);
                if (q.X0 < minx)
                {
                    minx = q.X0;
                }

                if (x > maxx)
                {
                    maxx = x;
                }

                if (q.Y0 < miny)
                {
                    miny = q.Y0;
                }

                if (q.Y1 > maxy)
                {
                    maxy = q.Y1;
                }

                prevGlyph = glyph;
            }

            advance = x - startx;

            bounds.X = minx;
            bounds.Y = miny;
            bounds.X2 = maxx;
            bounds.Y2 = maxy;

            return advance;
        }

        public void Reset(float width, float height)
        {
            Atlases.Clear();

            _glyphs.Clear();

            m_textCache.Clear();

            if (width == _size.x && height == _size.y)
            {
                return;
            }

            _size = new Vector2(width, height);
            _itw = 1.0f / _size.x;
            _ith = 1.0f / _size.y;
        }

        public void Reset() => Reset(_size.x, _size.y);

        private int GetCodepointIndex(int codepoint, out Font font)
        {
            font = null;

            int g = 0;
            foreach (Font f in _fonts)
            {
                g = f.GetGlyphIndex(codepoint);
                if (g != 0)
                {
                    font = f;
                    break;
                }
            }

            return g;
        }

        private FontGlyph GetGlyphWithoutBitmap(Dictionary<int, FontGlyph> glyphs, int codepoint)
        {
            if (glyphs.TryGetValue(codepoint, out FontGlyph glyph))
            {
                return glyph;
            }

            int g = GetCodepointIndex(codepoint, out Font font);
            if (g == 0)
            {
                return null;
            }

            int advance = 0, lsb = 0, x0 = 0, y0 = 0, x1 = 0, y1 = 0;
            font.BuildGlyphBitmap(g, font.Scale, ref advance, ref lsb, ref x0, ref y0, ref x1, ref y1);

            int pad = FontGlyph.PadFromBlur(Blur);
            int gw = x1 - x0 + pad * 2;
            int gh = y1 - y0 + pad * 2;

            glyph = new FontGlyph
            {
                Font = font,
                Codepoint = codepoint,
                Height = FontHeight,
                Blur = Blur,
                Index = g,
                Bounds = new Rect(0, 0, gw, gh),
                XAdvance = (int)(font.Scale * advance * 10.0f),
                XOffset = x0 - pad,
                YOffset = y0 - pad
            };

            glyphs[codepoint] = glyph;

            return glyph;
        }

        private FontGlyph GetGlyphInternal(Dictionary<int, FontGlyph> glyphs, int codepoint, out bool hasResetted)
        {
            hasResetted = false;
            FontGlyph glyph = GetGlyphWithoutBitmap(glyphs, codepoint);
            if (glyph == null)
            {
                return null;
            }

            if (glyph.Atlas != null)
            {
                return glyph;
            }

            FontAtlas currentAtlas = CurrentAtlas;
            int gx = 0, gy = 0;
            int gw = Mathf.RoundToInt(glyph.Bounds.width);
            int gh = Mathf.RoundToInt(glyph.Bounds.height);
            if (!currentAtlas.AddRect(gw, gh, ref gx, ref gy))
            {
                CurrentAtlasFull?.Invoke();
                do
                {
                    // This code will force creation of new atlas with 4x size
                    _currentAtlas = null;
                    if (_size.x * _size.y < 8192 * 8192)
                    {
                        _size *= 2;
                    }
                    else
                    {
                        throw new Exception(string.Format("Could not add rect to the newly created atlas. gw={0}, gh={1} - MAP REACHED 8K * 8K LIMIT!", gw, gh));
                    }
                    glyphs.Clear();
                    glyphs[codepoint] = glyph;

                    currentAtlas = CurrentAtlas;
                    m_textCache.Clear();

                    hasResetted = true;
                    // Try to add again
                } while (!currentAtlas.AddRect(gw, gh, ref gx, ref gy));
            }

            glyph.Bounds.x = gx;
            glyph.Bounds.y = gy;

            currentAtlas.RenderGlyph(glyph);

            glyph.Atlas = currentAtlas;

            return glyph;
        }

        private FontGlyph GetGlyph(Dictionary<int, FontGlyph> glyphs, int codepoint, out bool hasResetted, bool ignoreDefaultChar = false)
        {
            FontGlyph result = GetGlyphInternal(glyphs, codepoint, out hasResetted);
            if (!ignoreDefaultChar && result == null && DefaultCharacter != null)
            {
                result = GetGlyphInternal(glyphs, DefaultCharacter.Value, out hasResetted);
            }

            return result;
        }

        private void GetQuad(FontGlyph glyph, FontGlyph prevGlyph, float spacing, ref float x, ref float y, ref FontGlyphSquad q)
        {
            if (prevGlyph != null)
            {
                float adv = 0;
                if (UseKernings && glyph.Font == prevGlyph.Font)
                {
                    adv = prevGlyph.GetKerning(glyph) * glyph.Font.Scale;
                }

                x += (int)(adv + spacing + 0.5f);
            }

            float rx = x + glyph.XOffset;
            float ry = y + glyph.YOffset;
            q.X0 = rx;
            q.Y0 = ry;
            q.X1 = rx + glyph.Bounds.width;
            q.Y1 = ry + glyph.Bounds.height;
            q.S0 = glyph.Bounds.x * _itw;
            q.T0 = glyph.Bounds.y * _ith;
            q.S1 = glyph.Bounds.xMax * _itw;
            q.T1 = glyph.Bounds.yMax * _ith;

            x += (int)(glyph.XAdvance / 10.0f + 0.5f);
        }



        public Texture2D WriteTexture2D(string str)
        {
            Dictionary<int, FontGlyph> glyphs = GetGlyphsCollection(FontHeight);
            // Determine ascent and lineHeight from first character
            float ascent = 0, lineHeight = 0;
            for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
            {
                int codepoint = char.ConvertToUtf32(str, i);

                FontGlyph glyph = GetGlyph(glyphs, codepoint, out _);
                if (glyph == null)
                {
                    continue;
                }

                ascent = glyph.Font.Ascent;
                lineHeight = glyph.Font.LineHeight;
                break;
            }

            var q = new FontGlyphSquad();

            float originX = 0.0f;
            float originY = 0.0f;

            originY += ascent;

            var bounds = new Bounds();
            TextBounds(0, 0, str, ref bounds);

            var targetTexture = new Texture2D(Mathf.CeilToInt((bounds.X2 - bounds.X)), Mathf.CeilToInt((bounds.Y2 - bounds.Y)), TextureFormat.ARGB32, false);
            targetTexture.SetPixels(new Color[targetTexture.width * targetTexture.height]);

            FontGlyph prevGlyph = null;
            for (int i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1)
            {
                int codepoint = char.ConvertToUtf32(str, i);

                if (codepoint == '\n')
                {
                    originX = 0.0f;
                    originY += lineHeight;
                    prevGlyph = null;
                    continue;
                }

                FontGlyph glyph = GetGlyphWithoutBitmap(glyphs, codepoint);
                if (glyph == null)
                {
                    continue;
                }

                GetQuad(glyph, prevGlyph, Spacing, ref originX, ref originY, ref q);

                q.X0 = (int)(q.X0);
                q.X1 = (int)(q.X1);
                q.Y0 = (int)(q.Y0);
                q.Y1 = (int)(q.Y1);


                Color[] arr = CurrentAtlas.GetGlyphColors(glyph);

                MergeTextures(targetTexture, arr, Mathf.RoundToInt(q.X0 - bounds.X), Mathf.RoundToInt(q.Y0 - bounds.Y), (int)(q.X1 - q.X0), (int)(q.Y1 - q.Y0), false, true);

                prevGlyph = glyph;
            }

            targetTexture.Apply();

            return targetTexture;
        }
        internal static void MergeTextures(Texture2D tex, Color[] colors, int startX, int startY, int sizeX, int sizeY, bool swapXY = false, bool flipVertical = false, bool flipHorizontal = false, bool plain = false)
        {
            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    Color orPixel = tex.GetPixel(startX + i, startY + j);
                    Color newPixel = colors[((flipVertical ? sizeY - j - 1 : j) * (swapXY ? 1 : sizeX)) + ((flipHorizontal ? sizeX - i - 1 : i) * (swapXY ? sizeY : 1))];

                    if (plain && newPixel.a != 1)
                    {
                        continue;
                    }

                    tex.SetPixel(startX + i, startY + j, Color.Lerp(orPixel, newPixel, newPixel.a));
                }
            }
        }

    }
    public enum MaterialType
    {
        OPAQUE,
        DAYNIGHT,
        FLAGS,
        BRIGHT
    }
}