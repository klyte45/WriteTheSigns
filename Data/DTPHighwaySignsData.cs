using ColossalFramework.UI;
using Klyte.Commons.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorHighwaySigns;

namespace Klyte.DynamicTextProps.Data
{

    [XmlRoot("DTPHighwaySignsData")]
    public class DTPHighwaySignsData : DTPBaseData<DTPHighwaySignsData, BoardBunchContainerHighwaySignXml, CacheControlHighwaySign>
    {
        public override int ObjArraySize => NetManager.MAX_SEGMENT_COUNT;

        
        public override string SaveId => "K45_DTP3_DTPHighwaySignsData";
    }

}
