using Klyte.WriteTheSigns.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Data
{

    [XmlRoot("WTSHighwaySignsData")]
    public class WTSHighwaySignsData : WTSBaseData<WTSHighwaySignsData, BoardBunchContainerOnNetXml>
    {
        public override int ObjArraySize => NetManager.MAX_SEGMENT_COUNT;


        public override string SaveId => "K45_WTS_WTSHighwaySignsData";

        public override int BoardCount => 1;

        public override int SubBoardCount => 1;
    }

}
