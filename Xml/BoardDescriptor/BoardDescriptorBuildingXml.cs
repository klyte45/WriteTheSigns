using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{

    public class BoardDescriptorBuildingXml : IBoardDescriptor

    {
        [XmlArray("platformOrder")]
        [XmlArrayItem("p")]
        public int[] m_platforms = new int[0];
        [XmlAttribute("showIfNoLine")]
        public bool m_showIfNoLine = true;


        [XmlIgnore]
        private Vector3 m_arrayRepeat;
        [XmlIgnore]
        public Vector3 ArrayRepeat { get => m_arrayRepeat; set => m_arrayRepeat = value; }

        [XmlAttribute("arrayRepeatX")]
        public float ArrayRepeatX { get => m_arrayRepeat.x; set => m_arrayRepeat.x = value; }
        [XmlAttribute("arrayRepeatY")]
        public float ArrayRepeatY { get => m_arrayRepeat.y; set => m_arrayRepeat.y = value; }
        [XmlAttribute("arrayRepeatZ")]
        public float ArrayRepeatZ { get => m_arrayRepeat.z; set => m_arrayRepeat.z = value; }

        [XmlAttribute("arrayRepeatTimes")]
        public int m_arrayRepeatTimes = 0;

        [XmlAttribute("coloringMode")]
        public ColoringMode? ColorModeProp
        {
            get => m_colorMode;
            set => m_colorMode = value;
        }

        [XmlIgnore]
        private ColoringMode? m_colorMode;

        [XmlElement("BasicConfig")]
        public BoardDescriptorGeneralXml BasicConfig { get; private set; } = new BoardDescriptorGeneralXml();
    }

    public enum ColoringMode
    {
        ByPlatform,
        Fixed,
        ByDistrict,
        FromBuilding
    }
}