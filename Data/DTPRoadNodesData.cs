using Klyte.DynamicTextProps.Overrides;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Data
{

    [XmlRoot("DTPNetNodesData")]
    public class DTPNetNodesData : DTPBaseData<DTPNetNodesData, IBoardBunchContainer<CacheControlStreetPlate>>
    {
        public override int ObjArraySize => NetManager.MAX_NODE_COUNT;

        public override string DefaultFont { get => CurrentDescriptor.BasicConfig.FontName; set => CurrentDescriptor.BasicConfig.FontName = value; }

        public override string SaveId => "K45_DTP3_DTPNetNodesData";

        [XmlElement("CurrentDescriptor")]
        public BoardDescriptorNetNodesXml CurrentDescriptor { get; set; } = new BoardDescriptorNetNodesXml();
    }

}
