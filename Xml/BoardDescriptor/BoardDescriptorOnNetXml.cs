using System.Xml;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Overrides
{

    [XmlRoot("onNetDescriptor")]
    public class BoardDescriptorOnNetXml
    {
        [XmlAttribute("inverted")]
        public bool m_invertSign = false;
        [XmlAttribute("segmentPosition")]
        public float m_segmentPosition = 0.5f;
        [XmlElement("BasicConfig")]
        public BoardDescriptorGeneralXml BasicConfig { get; private set; } = new BoardDescriptorGeneralXml();

    }


}
