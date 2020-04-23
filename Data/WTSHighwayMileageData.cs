using Klyte.WriteTheSigns.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Data
{

    [XmlRoot("WTSHighwayMileageData")]
    public class WTSHighwayMileageData : WTSBaseData<WTSHighwayMileageData, IBoardBunchContainer>
    {
        public override string DefaultFont { get => CurrentDescriptor.FontName; set => CurrentDescriptor.FontName = value; }
        public override int ObjArraySize => NetManager.MAX_SEGMENT_COUNT;
        [XmlElement("CurrentDescriptor")]
        public BoardDescriptorGeneralXml CurrentDescriptor { get; set; } = new BoardDescriptorGeneralXml();

        public override string SaveId => "K45_WTS_WTSHighwayMileageData";

        public override int BoardCount => 1;

        public override int SubBoardCount => 1;
    }

}
