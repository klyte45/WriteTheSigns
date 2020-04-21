using System.Xml.Serialization;

namespace Klyte.WriteTheCity.Xml
{
    [XmlRoot("roadCornerDescriptor")]
    public class BoardInstanceRoadNodeXml : BoardInstanceXml
    {
        [XmlAttribute("placeOnDistrictBorder")]
        public bool PlaceOnDistrictBorder { get; set; } = true;        

        [XmlAttribute("useDistrictColor")]
        public bool UseDistrictColor = false;
    }


}
