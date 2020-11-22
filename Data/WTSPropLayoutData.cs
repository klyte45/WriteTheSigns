using ColossalFramework;
using ColossalFramework.Globalization;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Xml;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Data
{
    [XmlRoot("PropLayoutData")]
    public class WTSPropLayoutData : WTSLibBaseData<WTSPropLayoutData, BoardDescriptorGeneralXml>
    {

        public override string SaveId => "K45_WTS_PropLayoutData";

        protected override void Save()
        {
            WTSRoadNodesData.Instance.ResetCacheDescriptors();
            base.Save();
        }

        public string[] FilterBy(string input, TextRenderingClass? renderClass) => 
            m_indexes
            .Where((x) => (renderClass == null || renderClass == m_savedDescriptorsSerialized[x.Value].m_allowedRenderClass) && input.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Key, input, CompareOptions.IgnoreCase) >= 0)
            .Select(x => x.Key)
            .OrderBy((x) => x.StartsWith("WTS://") ? "ZZ" + x : "AA" + x)
            .ToArray();

    }
}
