using Klyte.WriteTheCity.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheCity.Data
{
    [XmlRoot("PropLayoutData")]
    public class WTCPropLayoutData : WTCLibBaseData<WTCPropLayoutData, BoardDescriptorGeneralXml>
    {

        public override string SaveId => "K45_WTC_PropLayoutData";
    }
}
