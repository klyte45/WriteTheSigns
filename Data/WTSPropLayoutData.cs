using Klyte.WriteTheSigns.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Data
{
    [XmlRoot("PropLayoutData")]
    public class WTSPropLayoutData : WTSLibBaseData<WTSPropLayoutData, BoardDescriptorGeneralXml>
    {

        public override string SaveId => "K45_WTS_PropLayoutData";

        protected override void Save()
        {
            base.Save();
            WTSRoadNodesData.Instance.ResetCacheDescriptors();
        }
    }
}
