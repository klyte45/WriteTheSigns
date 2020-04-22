using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{
    [XmlRoot("mileageMarkerDescriptor")]
    public class BoardDescriptorMileageMarkerXml : BoardInstanceXml
    {

        [XmlAttribute("useMiles")]
        public bool UseMiles { get; set; }

    }


}
