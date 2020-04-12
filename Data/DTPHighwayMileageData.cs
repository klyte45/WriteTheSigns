using Klyte.DynamicTextProps.Overrides;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Data
{

    [XmlRoot("DTPHighwayMileageData")]
    public class DTPHighwayMileageData : DTPBaseData<DTPHighwayMileageData, IBoardBunchContainer<CacheControl>>
    {
        public override int ObjArraySize => NetManager.MAX_SEGMENT_COUNT;
        [XmlElement("CurrentDescriptor")]
        public BoardDescriptorMileageMarkerXml CurrentDescriptor { get; set; }

        public override string SaveId => "K45_DTP3_DTPHighwayMileageData";
    }

}
