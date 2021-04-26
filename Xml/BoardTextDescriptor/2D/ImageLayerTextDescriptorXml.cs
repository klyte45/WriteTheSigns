using Klyte.Commons.Interfaces;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using static Klyte.WriteTheSigns.ModShared.IBridgeADR;
using static Klyte.WriteTheSigns.Xml.BoardTextDescriptorGeneralXml;

namespace Klyte.WriteTheSigns.Xml
{
    [XmlRoot("textDescriptor2D")]
    public partial class ImageLayerTextDescriptorXml : ILibable
    {
        [XmlAttribute("textScale")]
        public float m_textScale = 1f;
        [XmlAttribute("spacingFactor")]
        internal float m_charSpacingFactor = 0.85f;
        [XmlAttribute("maxWidth")]
        public int m_maxWidthPixels = 0;
        [XmlAttribute("fixedHeight")]
        public int m_fixedHeightPixels = 0;
        [XmlAttribute("applyOverflowResizingOnY")]
        public bool m_applyOverflowResizingOnY = false;

        [XmlElement("offsetUV")]
        public Vector2 OffsetUV { get; set; } = Vector2.one / 2;
        [XmlElement("pivotUV")]
        public Vector2 PivotUV { get; set; } = Vector2.one / 2;

        [XmlAttribute("textContent")]
        public TextType m_textType = TextType.Fixed;

        [XmlAttribute("fixedText")]
        public string m_fixedText = "Text";
        [XmlIgnore]
        public TextParameterWrapper m_spriteParam;
        [XmlAttribute("spriteName")]
        public string SpriteParam
        {
            get => m_spriteParam?.ToString();
            set => m_spriteParam = new TextParameterWrapper(value);
        }

        [XmlAttribute("overrideFont")] public string m_overrideFont;
        [XmlAttribute("fontClass")] public FontClass m_fontClass = FontClass.Regular;

        [XmlAttribute("prefix")]
        public string m_prefix = "";
        [XmlAttribute("suffix")]
        public string m_suffix = "";

        [XmlAttribute("saveName")]
        public string SaveName { get; set; }

        [XmlElement("ColoringSettings")]
        public ColoringSettings ColoringConfig { get; set; } = new ColoringSettings { m_cachedColor = Color.white };

        public bool IsSpriteText()
        {
            switch (m_textType)
            {
                case TextType.LinesSymbols:
                case TextType.GameSprite:
                    return true;
            }
            return false;
        }

        public Vector2 GetScale(Vector2 textSize)
        {
            if (IsSpriteText())
            {
                return Vector2.one * m_textScale;
            }
            if (m_fixedHeightPixels > 0)
            {
                var heightMultiplier = m_fixedHeightPixels / textSize.y;
                var widthMultiplier = Mathf.Min(heightMultiplier, m_maxWidthPixels > 0 ? m_maxWidthPixels / textSize.x : heightMultiplier);

                return new Vector2(widthMultiplier, heightMultiplier);
            }
            else
            {
                var widthMultiplier = m_maxWidthPixels > 0 ? Mathf.Min(m_maxWidthPixels / textSize.x, m_textScale) : m_textScale;
                var heightMultiplier = m_applyOverflowResizingOnY ? widthMultiplier : m_textScale;

                return new Vector2(widthMultiplier, heightMultiplier);
            }
        }


        public Vector4 GetAreaSize(float shieldWidth, float shieldHeight, float textureWidth, float textureHeight)
        {
            var multiplier = GetScale(new Vector2(textureWidth, textureHeight));
            float textTargetHeight = textureHeight * multiplier.y;
            float textTargetWidth = textureWidth * multiplier.x;

            return new Vector4(
                   Mathf.Lerp(0, shieldWidth, OffsetUV.x) - Mathf.Lerp(0, textTargetWidth, PivotUV.x),
                   Mathf.Lerp(0, shieldHeight, OffsetUV.y) - Mathf.Lerp(0, textTargetHeight, PivotUV.y),
                   textTargetWidth,
                   textTargetHeight);
        }
        internal bool GetTargetText(AdrHighwayParameters parameters, out string text)
        {
            switch (m_textType)
            {
                case TextType.Fixed:
                    text = m_fixedText;
                    break;
                case TextType.GameSprite:
                    text = null;
                    break;
                case TextType.CityName:
                    text = SimulationManager.instance.m_metaData.m_CityName;
                    break;
                case TextType.HwCodeShort:
                    text = parameters?.shortCode;
                    break;
                case TextType.HwCodeLong:
                    text = parameters?.longCode;
                    break;
                case TextType.HwDettachedPrefix:
                    text = parameters?.detachedStr;
                    break;
                case TextType.HwIdentifierSuffix:
                    text = parameters?.hwIdentifier;
                    break;
                default:
                    text = null;
                    return false;
            }
            text = $"{m_prefix}{text}{m_suffix}";
            return true;
        }
    }

}

