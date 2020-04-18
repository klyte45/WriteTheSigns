using Klyte.DynamicTextProps.Xml;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Data
{

    [XmlRoot("DTPHighwaySignsData")]
    public class DTPHighwaySignsData : DTPBaseData<DTPHighwaySignsData, BoardBunchContainerOnNetXml>
    {
        public override int ObjArraySize => NetManager.MAX_SEGMENT_COUNT;


        public override string SaveId => "K45_DTP3_DTPHighwaySignsData";
    }

}
