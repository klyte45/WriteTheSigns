using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Overrides
{
    [XmlRoot("basicBoardDescriptor")]
    public class BoardDescriptorXml : BoardDescriptorParentXml<BoardDescriptorXml, BoardTextDescriptorXml> { }


}
