using System.Xml;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Overrides
{
    [XmlRoot("BGBuildingConfig")]
    public class BoardGeneratorBuildingConfigXml
    {
        [XmlAttribute("defaultFont")]
        public string DefaultFont { get; set; } = "";
    }
}