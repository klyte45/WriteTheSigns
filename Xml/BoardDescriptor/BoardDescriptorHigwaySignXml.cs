using Klyte.Commons.Utils;
using Klyte.Commons.Interfaces;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{

    public partial class BoardGeneratorHighwaySigns
    {
        [XmlRoot("highwaySignDescriptor")]
        public class BoardDescriptorHigwaySignXml : BoardDescriptorParentXml<BoardDescriptorHigwaySignXml, BoardTextDescriptorHighwaySignsXml>, ILibable
        {
            [XmlAttribute("inverted")]
            public bool m_invertSign = false;
            [XmlAttribute("segmentPosition")]
            public float m_segmentPosition = 0.5f;
            [XmlIgnore]
            public Color m_color = Color.white;
            [XmlAttribute("color")]
            public string ColorStr
            {
                get => ColorExtensions.ToRGB(m_color);
                set => m_color = ColorExtensions.FromRGB(value);
            }
            [XmlAttribute("saveName")]
            public string SaveName { get; set; }
        }

    }
}
