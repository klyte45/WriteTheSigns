using ColossalFramework;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Libraries;
using System.Xml.Serialization;
using UnityEngine;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorRoadNodes;

namespace Klyte.DynamicTextProps.Overrides
{
    [XmlRoot("streetSignDescriptor")]
    public class BoardDescriptorStreetSignXml : BoardDescriptorParentXml<BoardDescriptorStreetSignXml, BoardTextDescriptorSteetSignXml>, ILibable
    {
        [XmlAttribute("fontName")]
        public string FontName { get; set; }

        [XmlAttribute("saveName")]
        public string SaveName { get; set; }

        [XmlAttribute("useDistrictColor")]
        public bool UseDistrictColor = false;
        [XmlIgnore]
        public Color PropColor { get => m_cachedColor == default ? Color.white : m_cachedColor; set => m_cachedColor = value; }
        [XmlIgnore]
        private Color m_cachedColor;

        [XmlAttribute("propColor")]
        public string PropColorStr { get => m_cachedColor == default ? null : ColorExtensions.ToRGB(PropColor); set => m_cachedColor = value.IsNullOrWhiteSpace() ? default : (Color) ColorExtensions.FromRGB(value); }
    }


}
