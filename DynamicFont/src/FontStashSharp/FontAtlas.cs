using StbTrueTypeSharp;
using System;
using System.Linq;
using UnityEngine;

namespace FontStashSharp
{
    internal class FontAtlas
    {
        public int Width
        {
            get; private set;
        }

        public int Height
        {
            get; private set;
        }

        public int NodesNumber
        {
            get; private set;
        }

        public FontAtlasNode[] Nodes
        {
            get; private set;
        }

        public Texture2D Texture
        {
            get; set;
        }
        private Material m_material;
        public Material Material
        {
            get {
                if (m_material == null)
                {
                    m_material = new Material(Shader.Find("Custom/Buildings/Building/NoBase"))
                    {
                        mainTexture = Texture
                    };
                }
                return m_material;
            }
        }

        public FontAtlas(int w, int h, int count)
        {
            Width = w;
            Height = h;
            Nodes = new FontAtlasNode[count];
            Nodes[0].X = 0;
            Nodes[0].Y = 0;
            Nodes[0].Width = w;
            NodesNumber++;
        }

        public void InsertNode(int idx, int x, int y, int w)
        {
            if (NodesNumber + 1 > Nodes.Length)
            {
                FontAtlasNode[] oldNodes = Nodes;
                int newLength = Nodes.Length == 0 ? 8 : Nodes.Length * 2;
                Nodes = new FontAtlasNode[newLength];
                for (int i = 0; i < oldNodes.Length; ++i)
                {
                    Nodes[i] = oldNodes[i];
                }
            }

            for (int i = NodesNumber; i > idx; i--)
            {
                Nodes[i] = Nodes[i - 1];
            }

            Nodes[idx].X = x;
            Nodes[idx].Y = y;
            Nodes[idx].Width = w;
            NodesNumber++;
        }

        public void RemoveNode(int idx)
        {
            if (NodesNumber == 0)
            {
                return;
            }

            for (int i = idx; i < NodesNumber - 1; i++)
            {
                Nodes[i] = Nodes[i + 1];
            }

            NodesNumber--;
        }

        public void Expand(int w, int h)
        {
            if (w > Width)
            {
                InsertNode(NodesNumber, Width, 0, w - Width);
            }

            Width = w;
            Height = h;
        }

        public void Reset(int w, int h)
        {
            Width = w;
            Height = h;
            NodesNumber = 0;
            Nodes[0].X = 0;
            Nodes[0].Y = 0;
            Nodes[0].Width = w;
            NodesNumber++;
        }

        public bool AddSkylineLevel(int idx, int x, int y, int w, int h)
        {
            InsertNode(idx, x, y + h, w);
            for (int i = idx + 1; i < NodesNumber; i++)
            {
                if (Nodes[i].X < Nodes[i - 1].X + Nodes[i - 1].Width)
                {
                    int shrink = Nodes[i - 1].X + Nodes[i - 1].Width - Nodes[i].X;
                    Nodes[i].X += shrink;
                    Nodes[i].Width -= shrink;
                    if (Nodes[i].Width <= 0)
                    {
                        RemoveNode(i);
                        i--;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            for (int i = 0; i < NodesNumber - 1; i++)
            {
                if (Nodes[i].Y == Nodes[i + 1].Y)
                {
                    Nodes[i].Width += Nodes[i + 1].Width;
                    RemoveNode(i + 1);
                    i--;
                }
            }

            return true;
        }

        public int RectFits(int i, int w, int h)
        {
            int x = Nodes[i].X;
            int y = Nodes[i].Y;
            if (x + w > Width)
            {
                return -1;
            }

            int spaceLeft = w;
            while (spaceLeft > 0)
            {
                if (i == NodesNumber)
                {
                    return -1;
                }

                y = Math.Max(y, Nodes[i].Y);
                if (y + h > Height)
                {
                    return -1;
                }

                spaceLeft -= Nodes[i].Width;
                ++i;
            }

            return y;
        }

        public bool AddRect(int rw, int rh, ref int rx, ref int ry)
        {
            int besth = Height;
            int bestw = Width;
            int besti = -1;
            int bestx = -1;
            int besty = -1;
            for (int i = 0; i < NodesNumber; i++)
            {
                int y = RectFits(i, rw, rh);
                if (y != -1)
                {
                    if (y + rh < besth || y + rh == besth && Nodes[i].Width < bestw)
                    {
                        besti = i;
                        bestw = Nodes[i].Width;
                        besth = y + rh;
                        bestx = Nodes[i].X;
                        besty = y;
                    }
                }
            }

            if (besti == -1)
            {
                return false;
            }

            if (!AddSkylineLevel(besti, bestx, besty, rw, rh))
            {
                return false;
            }

            rx = bestx;
            ry = besty;
            return true;
        }

        public void RenderGlyph(FontGlyph glyph)
        {
            int pad = glyph.Pad;

            // Render glyph to byte buffer
            byte[] buffer = new byte[Mathf.RoundToInt(glyph.Bounds.width) * Mathf.RoundToInt(glyph.Bounds.height)];
            Array.Clear(buffer, 0, buffer.Length);

            int g = glyph.Index;
            var dst = new FakePtr<byte>(buffer, pad + (pad * Mathf.RoundToInt(glyph.Bounds.width)));
            glyph.Font.RenderGlyphBitmap(dst,
               Mathf.RoundToInt(glyph.Bounds.width) - pad * 2,
               Mathf.RoundToInt(glyph.Bounds.height) - pad * 2,
               Mathf.RoundToInt(glyph.Bounds.width),
                g);

            if (glyph.Blur > 0)
            {
                Blur(buffer, Mathf.RoundToInt(glyph.Bounds.width), Mathf.RoundToInt(glyph.Bounds.height), Mathf.RoundToInt(glyph.Bounds.width), glyph.Blur);
            }

            // Byte buffer to RGBA
            var colorBuffer = new Color[Mathf.RoundToInt(glyph.Bounds.width) * Mathf.RoundToInt(glyph.Bounds.height)];
            for (int i = 0; i < colorBuffer.Length; ++i)
            {
                byte c = buffer[i];
                colorBuffer[i].r = colorBuffer[i].g = colorBuffer[i].b = colorBuffer[i].a = c;
            }

            // Write to texture
            if (Texture == null)
            {
                Texture = new Texture2D(Width, Height);
                Texture.SetPixels(new Color[Width * Height].Select(x => Color.clear).ToArray());
            }

            Texture.SetPixels(Mathf.RoundToInt(glyph.Bounds.x), Mathf.RoundToInt(glyph.Bounds.y), Mathf.RoundToInt(glyph.Bounds.width), Mathf.RoundToInt(glyph.Bounds.height), colorBuffer, 0);
            IsDirty = true;
        }

        public bool IsDirty { get; private set; } = false;

        public void UpdateMaterial()
        {
            if (IsDirty)
            {
                Material.mainTexture = Texture;
                Material.SetTexture("_MainTex", Texture);
                Texture2D aciTex = Material.GetTexture("_ACIMap") as Texture2D ?? new Texture2D(Texture.width, Texture.height);
                aciTex.SetPixels(Texture.GetPixels().Select(x => new Color(1 - x.a, 0, 1 - x.a / 2, 1)).ToArray());
                aciTex.Apply();
                Material.SetTexture("_ACIMap", aciTex);

                IsDirty = false;
            }
        }

        private void Blur(byte[] dst, int w, int h, int dstStride, int blur)
        {
            int alpha = 0;
            float sigma = 0;
            if (blur < 1)
            {
                return;
            }

            sigma = blur * 0.57735f;
            alpha = (int)((1 << 16) * (1.0f - Math.Exp(-2.3f / (sigma + 1.0f))));
            var ptr = new FakePtr<byte>(dst);
            BlurRows(ptr, w, h, dstStride, alpha);
            BlurCols(ptr, w, h, dstStride, alpha);
            BlurRows(ptr, w, h, dstStride, alpha);
            BlurCols(ptr, w, h, dstStride, alpha);
        }

        private static void BlurCols(FakePtr<byte> dst, int w, int h, int dstStride, int alpha)
        {
            int x = 0;
            int y = 0;
            for (y = 0; y < h; y++)
            {
                int z = 0;
                for (x = 1; x < w; x++)
                {
                    z += (alpha * ((dst[x] << 7) - z)) >> 16;
                    dst[x] = (byte)(z >> 7);
                }

                dst[w - 1] = 0;
                z = 0;
                for (x = w - 2; x >= 0; x--)
                {
                    z += (alpha * ((dst[x] << 7) - z)) >> 16;
                    dst[x] = (byte)(z >> 7);
                }

                dst[0] = 0;
                dst += dstStride;
            }
        }

        private static void BlurRows(FakePtr<byte> dst, int w, int h, int dstStride, int alpha)
        {
            int x = 0;
            int y = 0;
            for (x = 0; x < w; x++)
            {
                int z = 0;
                for (y = dstStride; y < h * dstStride; y += dstStride)
                {
                    z += (alpha * ((dst[y] << 7) - z)) >> 16;
                    dst[y] = (byte)(z >> 7);
                }

                dst[(h - 1) * dstStride] = 0;
                z = 0;
                for (y = (h - 2) * dstStride; y >= 0; y -= dstStride)
                {
                    z += (alpha * ((dst[y] << 7) - z)) >> 16;
                    dst[y] = (byte)(z >> 7);
                }

                dst[0] = 0;
                dst++;
            }
        }
    }
}