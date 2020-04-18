using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Xml
{
    [XmlRoot("roadCornerDescriptor")]
    public class BoardDescriptorRoadNodeXml : BoardInstanceXml
    {
        [XmlAttribute("placeOnDistrictBorder")]
        public bool PlaceOnDistrictBorder { get; set; } = true;


        [XmlAttribute("roadQualifierExtraction")]
        public RoadQualifierExtractionMode RoadQualifierExtraction { get; set; } = RoadQualifierExtractionMode.NONE;

        [XmlAttribute("abbreviationFile")]
        public string AbbreviationFile { get; set; } = "";

        [XmlAttribute("useDistrictColor")]
        public bool UseDistrictColor = false;
    }

    public enum RoadQualifierExtractionMode
    {
        NONE,
        START,
        END
    }


}
