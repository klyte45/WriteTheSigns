using System.Xml.Serialization;

namespace Klyte.DynamicTextBoards.Overrides
{
    [XmlRoot("basicBoardDescriptor")]
    public class BoardDescriptorXml : BoardDescriptorParentXml<BoardDescriptorXml, BoardTextDescriptorXml> { }


}
