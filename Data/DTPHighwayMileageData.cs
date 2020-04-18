using Klyte.DynamicTextProps.Xml;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Data
{

    [XmlRoot("DTPHighwayMileageData")]
    public class DTPHighwayMileageData : DTPBaseData<DTPHighwayMileageData, IBoardBunchContainer<CacheControl>>
    {
        public override string DefaultFont { get => CurrentDescriptor.FontName; set => CurrentDescriptor.FontName = value; }
        public override int ObjArraySize => NetManager.MAX_SEGMENT_COUNT;
        [XmlElement("CurrentDescriptor")]
        public BoardDescriptorGeneralXml CurrentDescriptor { get; set; } = new BoardDescriptorGeneralXml();

        public override string SaveId => "K45_DTP3_DTPHighwayMileageData";
    }

}
