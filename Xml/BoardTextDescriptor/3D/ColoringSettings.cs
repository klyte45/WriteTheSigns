using ColossalFramework;
using Klyte.Commons.Utils;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.WriteTheSigns.Xml
{
    public partial class BoardTextDescriptorGeneralXml
    {
        public class ColoringSettings
        {
            [XmlAttribute("useContrastColor")]
            public bool UseContrastColor { get => ColorSource == ColoringSource.Contrast; set => ColorSource = value ? ColoringSource.Contrast : ColoringSource.Fixed; }
            [XmlIgnore]
            public Color m_cachedColor = Color.clear;
            [XmlAttribute("color")]
            public string FixedColor { get => m_cachedColor == Color.clear ? null : ColorExtensions.ToRGB(m_cachedColor); set => m_cachedColor = value.IsNullOrWhiteSpace() ? Color.clear : (Color)ColorExtensions.FromRGB(value); }
            [XmlAttribute("colorSource")]
            public ColoringSource ColorSource { get; set; } = ColoringSource.Fixed;

            public enum ColoringSource
            {
                Fixed,
                Contrast,
                Parent
            }
        }
    }

}

