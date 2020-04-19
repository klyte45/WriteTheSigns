using Klyte.WriteTheCity.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheCity.Data
{

    [XmlRoot("WTCHighwayMileageData")]
    public class WTCHighwayMileageData : WTCBaseData<WTCHighwayMileageData, IBoardBunchContainer<CacheControl>>
    {
        public override string DefaultFont { get => CurrentDescriptor.FontName; set => CurrentDescriptor.FontName = value; }
        public override int ObjArraySize => NetManager.MAX_SEGMENT_COUNT;
        [XmlElement("CurrentDescriptor")]
        public BoardDescriptorGeneralXml CurrentDescriptor { get; set; } = new BoardDescriptorGeneralXml();

        public override string SaveId => "K45_WTC3_WTCHighwayMileageData";
    }

}
