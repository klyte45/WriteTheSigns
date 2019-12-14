using ColossalFramework;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using System.Xml.Serialization;
using UnityEngine;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorRoadNodes;

namespace Klyte.DynamicTextProps.Overrides
{
    [XmlRoot("streetSignDescriptor")]
    public class BoardDescriptorStreetSignXml : BoardDescriptorParentXml<BoardDescriptorStreetSignXml, BoardTextDescriptorSteetSignXml>, ILibable, IFontConfigContainer, IPropParamsContainer
    {
        [XmlAttribute("fontName")]
        public string FontName { get; set; }
        [XmlAttribute("placeOnDistrictBorder")]
        public bool PlaceOnDistrictBorder { get; set; } = true;

        [XmlAttribute("saveName")]
        public string SaveName { get; set; }

        [XmlAttribute("roadQualifierExtraction")]
        public RoadQualifierExtractionMode RoadQualifierExtraction { get; set; } = RoadQualifierExtractionMode.NONE;

        [XmlAttribute("useDistrictColor")]
        public bool UseDistrictColor = false;
        [XmlIgnore]
        public Color PropColor { get => m_cachedColor == default ? Color.white : m_cachedColor; set => m_cachedColor = value; }
        [XmlIgnore]
        private Color m_cachedColor;

        [XmlAttribute("color")]
        public string PropColorStr { get => m_cachedColor == default ? null : ColorExtensions.ToRGB(PropColor); set => m_cachedColor = value.IsNullOrWhiteSpace() ? default : (Color) ColorExtensions.FromRGB(value); }
        [XmlIgnore]
        public string PropName { get => m_propName; set => m_propName = value; }

        public enum RoadQualifierExtractionMode
        {
            NONE,
            START,
            END
        }
    }


}
