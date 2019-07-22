using Klyte.DynamicTextBoards.Libraries;
using System.Xml.Serialization;
using static Klyte.DynamicTextBoards.Overrides.BoardGeneratorRoadNodes;

namespace Klyte.DynamicTextBoards.Overrides
{
    public class BoardDescriptorStreetSignXml : BoardDescriptorParentXml<BoardDescriptorStreetSignXml, BoardTextDescriptorSteetSignXml>, ILibable
    {
        [XmlAttribute("saveName")]
        public string SaveName { get; set; }
    }


}
