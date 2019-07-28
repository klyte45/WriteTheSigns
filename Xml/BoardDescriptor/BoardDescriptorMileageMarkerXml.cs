using ColossalFramework;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Libraries;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{
    [XmlRoot("boardDescriptorMileageMarker")]
    public class BoardDescriptorMileageMarkerXml : BoardDescriptorParentXml<BoardDescriptorMileageMarkerXml, BoardTextDescriptorMileageMarkerXml>, ILibable
    {
        [XmlAttribute("fontName")]
        public string FontName { get; set; }

        [XmlAttribute("saveName")]
        public string SaveName { get; set; }

        [XmlIgnore]
        public Color PropColor { get => m_cachedColor == default ? Color.white : m_cachedColor; set => m_cachedColor = value; }
        [XmlIgnore]
        private Color m_cachedColor;

        [XmlAttribute("propColor")]
        public string PropColorStr { get => m_cachedColor == default ? null : ColorExtensions.ToRGB(PropColor); set => m_cachedColor = value.IsNullOrWhiteSpace() ? default : (Color) ColorExtensions.FromRGB(value); }
    }


}
