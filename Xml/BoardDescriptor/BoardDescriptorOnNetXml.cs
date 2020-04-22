using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{

    [XmlRoot("onNetDescriptor")]
    public class BoardDescriptorOnNetXml : BoardInstanceXml
    {
        [XmlAttribute("inverted")]
        public bool m_invertSign = false;
        [XmlAttribute("segmentPosition")]
        public float m_segmentPosition = 0.5f;

    }


}
