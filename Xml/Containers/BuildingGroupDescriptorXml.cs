using Klyte.DynamicTextProps.Libraries;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Overrides
{
    [XmlRoot("buildingConfig")]
    public class BuildingGroupDescriptorXml : ILibable
    {

        [XmlAttribute("buildingName")]
        public string BuildingName { get; set; }
        [XmlElement("boardDescriptor")]
        public BoardDescriptorBuildingXml[] BoardDescriptors { get; set; }
        [XmlAttribute("saveName")]
        public string SaveName { get; set; }
    }
}