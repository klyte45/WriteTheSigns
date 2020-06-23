using Klyte.Commons.Interfaces;
using Klyte.WriteTheSigns.Xml;
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

        [XmlAttribute("publicTransportFont")]
        public string PublicTransportLineSymbolFont
        {
            get => m_publicTransportLineSymbolFont; set {
                if (m_publicTransportLineSymbolFont != value)
                {
                    m_publicTransportLineSymbolFont = value;
                    WriteTheSignsMod.Controller?.SpriteRenderingRules?.PurgeAllLines();
                }
            }
        }

        [XmlAttribute("electronicFont")]
        public string ElectronicFont { get; set; }

        [XmlAttribute("stencilFont")]
        public string StencilFont { get; set; }

        internal string GetTargetFont(FontClass fontClass)
        {
            switch (fontClass)
            {
                case FontClass.Regular:
                    return null;
                case FontClass.PublicTransport:
                    return m_publicTransportLineSymbolFont ?? WTSController.DEFAULT_FONT_KEY;
                case FontClass.ElectronicBoards:
                    return ElectronicFont ?? WTSController.DEFAULT_FONT_KEY;
                case FontClass.Stencil:
                    return StencilFont ?? WTSController.DEFAULT_FONT_KEY;
            }
            return null;
        }
    }



}
