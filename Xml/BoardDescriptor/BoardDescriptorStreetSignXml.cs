using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Overrides
{
    [XmlRoot("roadCornerDescriptor")]
    public class BoardDescriptorNetNodesXml: IBoardDescriptor
    {
        [XmlAttribute("placeOnDistrictBorder")]
        public bool PlaceOnDistrictBorder { get; set; } = true;


        [XmlAttribute("roadQualifierExtraction")]
        public RoadQualifierExtractionMode RoadQualifierExtraction { get; set; } = RoadQualifierExtractionMode.NONE;

        [XmlAttribute("abbreviationFile")]
        public string AbbreviationFile { get; set; } = "";

        [XmlAttribute("useDistrictColor")]
        public bool UseDistrictColor = false;
        [XmlElement("BasicConfig")]
        public BoardDescriptorGeneralXml BasicConfig { get; private set; } = new BoardDescriptorGeneralXml();

    }

    public enum RoadQualifierExtractionMode
    {
        NONE,
        START,
        END
    }


}
