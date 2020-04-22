using Klyte.Commons.Interfaces;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
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

        [XmlAttribute("versionWTSLastEdit")]
        public string VersionWTSLastEdit { get; } = WriteTheSignsMod.FullVersion;

        [XmlAttribute("versionWTSCreation")]
        public string VersionWTSCreation { get; private set; } = WriteTheSignsMod.FullVersion;
    }
}