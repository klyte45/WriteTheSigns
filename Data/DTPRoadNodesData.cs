using Klyte.DynamicTextProps.Xml;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Data
{

    [XmlRoot("DTPNetNodesData")]
    public class DTPNetNodesData : DTPBaseData<DTPNetNodesData, IBoardBunchContainer<CacheControlRoadNode>>
    {
        public override int ObjArraySize => NetManager.MAX_NODE_COUNT;

        public override string DefaultFont { get => CurrentDescriptor.FontName; set => CurrentDescriptor.FontName = value; }

        public override string SaveId => "K45_DTP3_DTPNetNodesData";

        [XmlElement("CurrentDescriptor")]
        public BoardDescriptorGeneralXml CurrentDescriptor { get; set; } = new BoardDescriptorGeneralXml();
    }

}
