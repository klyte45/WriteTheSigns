using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Data
{
    [XmlRoot("WTSHighwayShieldsData")]
    public class WTSHighwayShieldsData : DataExtensionBase<WTSHighwayShieldsData>
    {
        public override string SaveId => "K45_WTS_WTSHighwayShieldsData";
        [XmlElement]
        public SimpleXmlDictionary<string, HighwayShieldDescriptor> CityDescriptors = new SimpleXmlDictionary<string, HighwayShieldDescriptor>();
        [XmlIgnore]
        public SimpleXmlDictionary<string, HighwayShieldDescriptor> GlobalDescriptors = new SimpleXmlDictionary<string, HighwayShieldDescriptor>();        
    }

}
