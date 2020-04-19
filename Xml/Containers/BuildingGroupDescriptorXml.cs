using Klyte.Commons.Interfaces;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheCity.Xml
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

        [XmlAttribute("versionWTCLastEdit")]
        public string VersionWTCLastEdit { get; } = WriteTheCityMod.FullVersion;

        [XmlAttribute("versionWTCCreation")]
        public string VersionWTCCreation { get; private set; } = WriteTheCityMod.FullVersion;
    }
}