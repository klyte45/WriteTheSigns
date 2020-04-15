using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Overrides
{
    [XmlRoot("mileageMarkerDescriptor")]
    public class BoardDescriptorMileageMarkerXml
    {

        [XmlAttribute("useMiles")]
        public bool UseMiles { get; set; }
        [XmlElement("BasicConfig")]
        public BoardDescriptorGeneralXml BasicConfig { get; private set; } = new BoardDescriptorGeneralXml();

    }


}
