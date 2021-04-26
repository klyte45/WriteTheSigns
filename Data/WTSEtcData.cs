using ColossalFramework;
using Klyte.Commons.Interfaces;
using Klyte.WriteTheSigns.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Data
{
    [XmlRoot("WTSEtcData")]
    public class WTSEtcData : DataExtensionBase<WTSEtcData>
    {
        public override string SaveId => "K45_WTS_EtcData";

        [XmlElement("fontSettings")]
        public FontSettings FontSettings { get; set; } = new FontSettings();

        [XmlIgnore]
        private static readonly SavedInt m_temperatureUnit = new SavedInt(Settings.temperatureUnit, Settings.gameSettingsFile, DefaultSettings.temperatureUnit, true);

        //	this.m_String = StringUtils.SafeFormat((num != 0) ? "{0:0.0}°F" : "{0:0.0}°C", newVal);

        internal static string FormatTemp(float num) => StringUtils.SafeFormat((m_temperatureUnit != 0) ? kFormatFahrenheit : kFormatCelsius, (num * (m_temperatureUnit != 0 ? 1.8 : 1)) + (m_temperatureUnit != 0 ? 32 : 0));

        public const string kFormatCelsius = "{0:0}°C";
        public const string kFormatFahrenheit = "{0:0}°F";
    }
    public class FontSettings
    {
        private string m_publicTransportLineSymbolFont;
        private string highwayShieldsFont;

        [XmlAttribute("publicTransportFont")]
        public string PublicTransportLineSymbolFont
        {
            get => m_publicTransportLineSymbolFont; set
            {
                if (WriteTheSignsMod.Controller is null || m_publicTransportLineSymbolFont != value)
                {
                    m_publicTransportLineSymbolFont = value;
                    WriteTheSignsMod.Controller?.AtlasesLibrary?.PurgeAllLines();
                }
            }
        }

        [XmlAttribute("electronicFont")]
        public string ElectronicFont { get; set; }

        [XmlAttribute("stencilFont")]
        public string StencilFont { get; set; }

        [XmlAttribute("highwayShieldsFont")]
        public string HighwayShieldsFont
        {
            get => highwayShieldsFont; set
            {
                {
                    if (WriteTheSignsMod.Controller is null || highwayShieldsFont != value)
                    {
                        highwayShieldsFont = value;
                        if (LoadingManager.instance.m_loadingComplete)
                        {
                            WriteTheSignsMod.Controller?.HighwayShieldsAtlasLibrary?.PurgeShields();
                        }
                    }
                }
            }
        }

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
                case FontClass.HighwayShields:
                    return HighwayShieldsFont ?? WTSController.DEFAULT_FONT_KEY;
            }
            return null;
        }
    }
}
