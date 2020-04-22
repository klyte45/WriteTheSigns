using Klyte.WriteTheSigns.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Data
{

    [XmlRoot("WTSRoadNodesData")]
    public class WTSRoadNodesData : WTSBaseData<WTSRoadNodesData, IBoardBunchContainer<CacheControlRoadNode>>
    {
        private BoardInstanceRoadNodeXml[] m_currentDescriptorOrder = new BoardInstanceRoadNodeXml[0];

        public override int ObjArraySize => NetManager.MAX_NODE_COUNT;

        [XmlElement("DefaultFont")]
        public override string DefaultFont { get; set; }

        [XmlIgnore]
        public override string SaveId => "K45_WTS_WTSNetNodesData";

        [XmlElement("CurrentDescriptor")]
        public BoardInstanceRoadNodeXml[] CurrentDescriptorOrder
        {
            get {
                if (m_currentDescriptorOrder == null)
                {
                    m_currentDescriptorOrder = new BoardInstanceRoadNodeXml[0];
                }
                return m_currentDescriptorOrder;
            }
            set => m_currentDescriptorOrder = value;
        }
        [XmlAttribute("roadQualifierExtraction")]
        public RoadQualifierExtractionMode RoadQualifierExtraction { get; set; } = RoadQualifierExtractionMode.NONE;

        [XmlAttribute("abbreviationFile")]
        public string AbbreviationFile { get; set; }
    }

}
