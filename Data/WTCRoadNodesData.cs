using Klyte.WriteTheCity.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheCity.Data
{

    [XmlRoot("WTCRoadNodesData")]
    public class WTCRoadNodesData : WTCBaseData<WTCRoadNodesData, IBoardBunchContainer<CacheControlRoadNode>>
    {
        public override int ObjArraySize => NetManager.MAX_NODE_COUNT;

        public override string DefaultFont { get => CurrentDescriptor.Descriptor.FontName; set => CurrentDescriptor.Descriptor.FontName = value; }

        public override string SaveId => "K45_WTC3_WTCNetNodesData";

        [XmlElement("CurrentDescriptor")]
        public BoardInstanceRoadNodeXml CurrentDescriptor { get; set; } = new BoardInstanceRoadNodeXml();
    }

}
