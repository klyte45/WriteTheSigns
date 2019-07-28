using Klyte.DynamicTextProps.Libraries;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Overrides
{

    public partial class BoardGeneratorHighwaySigns
    {
        public class BoardTextDescriptorHighwaySignsXml : BoardTextDescriptorParentXml<BoardTextDescriptorHighwaySignsXml>, ILibable
        {
            [XmlAttribute("nameContent")]
            public OwnNameContent m_ownTextContent;
            [XmlIgnore]
            public OwnNameContent m_cachedTextContent;
            [XmlAttribute("overrideFont")]
            public string m_overrideFont;

            [XmlAttribute("saveName")]
            public string SaveName { get; set; }
        }

    }
    public class BoardTextDescriptorXml : BoardTextDescriptorParentXml<BoardTextDescriptorXml> { }
    public partial class BoardGeneratorRoadNodes
    {
        public class BoardTextDescriptorSteetSignXml : BoardTextDescriptorParentXml<BoardTextDescriptorSteetSignXml>, ILibable
        {
            [XmlAttribute("saveName")]
            public string SaveName { get; set; }
        }

    }

}
