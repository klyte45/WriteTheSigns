using System.Xml;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Overrides
{
    [XmlRoot("buildingConfig")]
    public class BuildingConfigurationSerializeXml<BD, BTD>
        where BD : BoardDescriptorParentXml<BD, BTD>
        where BTD : BoardTextDescriptorParentXml<BTD>
    {
        [XmlAttribute("buildingName")]
        public string m_buildingName;
        [XmlElement("boardDescriptor")]
        public BD[] m_boardDescriptors;
    }


}
