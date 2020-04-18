using Klyte.Commons.Interfaces;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Xml
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

        [XmlAttribute("stopMappingThresold")]
        public float StopMappingThresold { get; set; } = 1f;

        [XmlAttribute("versionDTPLastEdit")]
        public string VersionDTPLastEdit { get; } = DynamicTextPropsMod.FullVersion;

        [XmlAttribute("versionDTPCreation")]
        public string VersionDTPCreation { get; private set; } = DynamicTextPropsMod.FullVersion;
    }
}