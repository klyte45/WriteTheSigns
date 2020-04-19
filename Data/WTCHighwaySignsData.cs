using Klyte.WriteTheCity.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheCity.Data
{

    [XmlRoot("WTCHighwaySignsData")]
    public class WTCHighwaySignsData : WTCBaseData<WTCHighwaySignsData, BoardBunchContainerOnNetXml>
    {
        public override int ObjArraySize => NetManager.MAX_SEGMENT_COUNT;


        public override string SaveId => "K45_WTC3_WTCHighwaySignsData";
    }

}
