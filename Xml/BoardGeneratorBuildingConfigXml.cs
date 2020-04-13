using System.Xml;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Legacy
{
    [XmlRoot("BGBuildingConfig")]
    public class BoardGeneratorBuildingConfigXmlLegacy
    {
        [XmlAttribute("defaultFont")]
        public string DefaultFont { get; set; } = "";
    }
}