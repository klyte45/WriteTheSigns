using Klyte.WriteTheSigns.Xml;
using System.Linq;
using System.Xml.Serialization;
using static Klyte.Commons.Utils.XmlUtils;

namespace Klyte.WriteTheSigns.Data
{

    [XmlRoot("WTSRoadNodesData")]
    public class WTSRoadNodesData : WTSBaseData<WTSRoadNodesData, CacheRoadNodeItem>
    {
        private BoardInstanceRoadNodeXml[] m_currentDescriptorOrder = new BoardInstanceRoadNodeXml[0];

        public override int ObjArraySize => NetManager.MAX_NODE_COUNT;
        public override int BoardCount => 8;
        public override int SubBoardCount => 4;

        [XmlAttribute("DefaultFont")]
        public override string DefaultFont { get; set; }

        [XmlIgnore]
        public override string SaveId => "K45_WTS_WTSNetNodesData";

        [XmlElement("DescriptorRulesOrder")]
        public ListWrapper<BoardInstanceRoadNodeXml> DescriptorRulesOrderXml
        {
            get => new ListWrapper<BoardInstanceRoadNodeXml> { listVal = DescriptorRulesOrder.ToList() };
            set => m_currentDescriptorOrder = value.listVal.ToArray();
        }
        [XmlIgnore]
        public BoardInstanceRoadNodeXml[] DescriptorRulesOrder
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
