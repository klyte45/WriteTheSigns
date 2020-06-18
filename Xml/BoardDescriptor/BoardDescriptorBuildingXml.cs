using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{

    public class BoardInstanceBuildingXml : BoardInstanceXml, ILibable

    {
        [XmlArray("platformOrder")]
        [XmlArrayItem("p")]
        public int[] m_platforms = new int[0];
        [XmlAttribute("showIfNoLine")]
        public bool m_showIfNoLine = true;

        [XmlElement("arrayRepeatOffset")]
        public Vector3Xml ArrayRepeat { get; set; } = new Vector3Xml();


        [XmlAttribute("arrayRepeatTimes")]
        public int m_arrayRepeatTimes = 0;

        [XmlAttribute("coloringMode")]
        public ColoringMode ColorModeProp { get; set; } = ColoringMode.Fixed;
        [XmlAttribute("useFixedIfMultiline")]
        public bool UseFixedIfMultiline { get; set; } = true;

        [XmlAttribute("saveName")]
        public string SaveName { get; set; }

        [XmlAttribute("propLayoutName")]
        public string PropLayoutName { get; set; }

        [XmlAttribute("subBuildingIdxPivotReference")]
        public int SubBuildingPivotReference { get; set; } = -1;

    }

    public class ExportableBoardInstanceBuildingListXml : ILibable
    {
        public BoardInstanceBuildingXml[] Instances { get; set; }
        public SimpleXmlDictionary<string, BoardDescriptorGeneralXml> Layouts { get; set; }
        [XmlAttribute("saveName")]
        public string SaveName { get; set; }
    }

    public enum ColoringMode
    {
        ByPlatform,
        Fixed,
        ByDistrict,
        FromBuilding
    }
}