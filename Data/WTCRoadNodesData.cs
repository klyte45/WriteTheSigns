using Klyte.WriteTheCity.Xml;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Klyte.WriteTheCity.Data
{

    [XmlRoot("WTCRoadNodesData")]
    public class WTCRoadNodesData : WTCBaseData<WTCRoadNodesData, IBoardBunchContainer<CacheControlRoadNode>>
    {
        public override int ObjArraySize => NetManager.MAX_NODE_COUNT;

        [XmlElement("DefaultFont")]
        public override string DefaultFont { get; set; } = WTCController.DEFAULT_FONT_KEY;

        [XmlIgnore]
        public override string SaveId => "K45_WTC3_WTCNetNodesData";

        [XmlElement("CurrentDescriptor")]
        public List<BoardInstanceRoadNodeXml> CurrentDescriptorOrder { get; set; } = new List<BoardInstanceRoadNodeXml>();

        [XmlAttribute("roadQualifierExtraction")]
        public RoadQualifierExtractionMode RoadQualifierExtraction { get; set; } = RoadQualifierExtractionMode.NONE;

        [XmlAttribute("abbreviationFile")]
        public string AbbreviationFile { get; set; } = "";
    }

}
