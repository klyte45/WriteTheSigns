using ColossalFramework;
using FontStashSharp;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpriteFontPlus
{
    public class FontServer : SingletonLite<FontServer>
    {
        private Dictionary<string, DynamicSpriteFont> m_fontRegistered = new Dictionary<string, DynamicSpriteFont>();

        private float m_targetHeight = 100;

        private float m_qualityMultiplier = 1f;

        private int DefaultTextureSize => WTSController.DefaultTextureSizeFont;


        public event Action<string> EventTextureReloaded;

        private bool m_currentlyReloadingAll = false;

        private int FontSizeEffective => Mathf.RoundToInt(m_targetHeight * m_qualityMultiplier);
        public Vector2 ScaleEffective => Vector2.one / m_qualityMultiplier;


        public void ResetCollection() => m_fontRegistered = new Dictionary<string, DynamicSpriteFont>();
        public void SetOverallSize(float f)
        {
            m_targetHeight = f;
            OnChangeSizeParam();
        }

        public void ResetOverallSize() => SetOverallSize(120);

        public void SetQualityMultiplier(float f)
        {
            m_qualityMultiplier = f;
            OnChangeSizeParam();
        }

        public void ResetQualityMultiplier() => SetQualityMultiplier(1);

        private void OnChangeSizeParam()
        {
            foreach (DynamicSpriteFont font in m_fontRegistered.Values)
            {
                font.Height = FontSizeEffective;
                font.Reset(DefaultTextureSize, DefaultTextureSize);
            }
        }

        public bool RegisterFont(string name, byte[] fontData, int textureSize)
        {
            try
            {
                if (name == null)
                {
                    LogUtils.DoErrorLog($"RegisterFont: FONT NAME CANNOT BE NULL!!");
                    return false;
                }

                if (m_fontRegistered.ContainsKey(name))
                {
                    m_fontRegistered[name].Reset(1, 1);
                }
                m_fontRegistered[name] = DynamicSpriteFont.FromTtf(fontData, name, FontSizeEffective, textureSize, textureSize);
            }
            catch (FontCreationException)
            {
                LogUtils.DoErrorLog($"RegisterFont: Error creating the font \"{name}\"... Invalid data!");
                return false;
            }
            return true;
        }
        public void ClearFonts() => m_fontRegistered.Clear();

        public DynamicSpriteFont this[string idx] => idx != null && m_fontRegistered.TryGetValue(idx, out DynamicSpriteFont value) ? value : null;

        public IEnumerable<string> GetAllFonts() => m_fontRegistered.Keys;
    }
}