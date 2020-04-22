using Klyte.WriteTheSigns.Xml;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Data
{

    [XmlRoot("WTSRoadNodesData")]
    public class WTSRoadNodesData : WTSBaseData<WTSRoadNodesData, IBoardBunchContainer<CacheControlRoadNode>>
    {
        public override int ObjArraySize => NetManager.MAX_NODE_COUNT;

        [XmlElement("DefaultFont")]
        public override string DefaultFont { get; set; }

        [XmlIgnore]
        public override string SaveId => "K45_WTS_WTSNetNodesData";

        [XmlElement("CurrentDescriptor")]
        public List<BoardInstanceRoadNodeXml> CurrentDescriptorOrder { get; set; } = new List<BoardInstanceRoadNodeXml>();

        [XmlAttribute("roadQualifierExtraction")]
        public RoadQualifierExtractionMode RoadQualifierExtraction { get; set; } = RoadQualifierExtractionMode.NONE;

        [XmlAttribute("abbreviationFile")]
        public string AbbreviationFile { get; set; }
    }

}
