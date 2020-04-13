using Klyte.DynamicTextProps.Overrides;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Data
{

    [XmlRoot("DTPRoadNodesData")]
    public class DTPRoadNodesData : DTPBaseData<DTPRoadNodesData, IBoardBunchContainer<CacheControlStreetPlate>>
    {
        public override int ObjArraySize => NetManager.MAX_NODE_COUNT;

        public override string DefaultFont { get => CurrentDescriptor.FontName; set => CurrentDescriptor.FontName = value; }

        public override string SaveId => "K45_DTP3_DTPRoadNodesData";

        [XmlElement("CurrentDescriptor")]
        public BoardDescriptorStreetSignXml CurrentDescriptor { get; set; } = new BoardDescriptorStreetSignXml();
    }

}
