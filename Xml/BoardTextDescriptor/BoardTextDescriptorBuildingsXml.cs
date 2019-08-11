using Klyte.DynamicTextProps.Libraries;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Overrides
{
    public class BoardTextDescriptorBuildingsXml : BoardTextDescriptorParentXml<BoardTextDescriptorBuildingsXml>, ILibable
    {

        [XmlAttribute("overrideFont")]
        public string m_overrideFont;

        [XmlAttribute("saveName")]
        public string SaveName { get; set; }

        [XmlIgnore]
        public TextType? m_cachedType;
    }
}
