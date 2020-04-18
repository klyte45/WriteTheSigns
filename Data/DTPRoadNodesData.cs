using Klyte.DynamicTextProps.Xml;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Data
{

    [XmlRoot("DTPNetNodesData")]
    public class DTPNetNodesData : DTPBaseData<DTPNetNodesData, IBoardBunchContainer<CacheControlRoadNode>>
    {
        public override int ObjArraySize => NetManager.MAX_NODE_COUNT;

        public override string DefaultFont { get => CurrentDescriptor.Descriptor.FontName; set => CurrentDescriptor.Descriptor.FontName = value; }

        public override string SaveId => "K45_DTP3_DTPNetNodesData";

        [XmlElement("CurrentDescriptor")]
        public BoardDescriptorRoadNodeXml CurrentDescriptor { get; set; } = new BoardDescriptorRoadNodeXml();
    }

}
