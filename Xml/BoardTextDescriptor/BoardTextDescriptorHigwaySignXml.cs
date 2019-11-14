using Klyte.Commons.Interfaces;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Overrides
{

    public partial class BoardGeneratorHighwaySigns
    {
        public class BoardTextDescriptorHighwaySignsXml : BoardTextDescriptorParentXml<BoardTextDescriptorHighwaySignsXml>, ILibable
        {
            [XmlAttribute("overrideFont")]
            public string m_overrideFont;

            [XmlAttribute("saveName")]
            public string SaveName { get; set; }
            [XmlIgnore]
            public TextType? m_cachedType;
        }

    }
}
