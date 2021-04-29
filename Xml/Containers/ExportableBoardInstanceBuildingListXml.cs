using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{
    public class ExportableBoardInstanceBuildingListXml : ILibable
    {
        public BoardInstanceBuildingXml[] Instances { get; set; }
        public SimpleXmlDictionary<string, BoardDescriptorGeneralXml> Layouts { get; set; }
        [XmlAttribute("saveName")]
        public string SaveName { get; set; }
    }
}