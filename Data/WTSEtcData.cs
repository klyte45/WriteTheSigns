using Klyte.Commons.Interfaces;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Data
{
    [XmlRoot("WTSEtcData")]
    public class WTSEtcData : DataExtensorBase<WTSEtcData>
    {
        public override string SaveId => "K45_WTS_EtcData";

        [XmlElement("fontSettings")]
        public FontSettings FontSettings { get; set; } = new FontSettings();



    }
    public class FontSettings
    {
        private string m_publicTransportLineSymbolFont;

        [XmlAttribute("lineFont")]
        public string PublicTransportLineSymbolFont
        {
            get => m_publicTransportLineSymbolFont; set {
                if (m_publicTransportLineSymbolFont != value)
                {
                    m_publicTransportLineSymbolFont = value;
                    WriteTheSignsMod.Controller?.TransportLineRenderingRules?.PurgeAllLines();
                }
            }
        }
    }



}
