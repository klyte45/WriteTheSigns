using ColossalFramework.UI;
using Klyte.Commons.Interfaces;
using SpriteFontPlus;
using System;
using System.Collections.Generic;
using System.Linq;
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
                    WriteTheSignsMod.Controller?.SpriteRenderingRules?.PurgeAllLines();
                }
            }
        }

        [XmlElement("alias")]
        [Obsolete]
        public AliasEntry[] FontAliases
        {
            get => Aliases.Select(x => new AliasEntry
            {
                Original = x.Key,
                Target = x.Value
            }).ToArray();
            set {
                if (value != null)
                {
                    Aliases.Clear();
                    value.ForEach(x =>
                    {
                        if (x.Original != null && x.Target != null)
                        {
                            Aliases[x.Original] = x.Target;
                        }
                    });
                }
            }
        }


        internal Dictionary<string, string> Aliases => FontServer.instance.Aliases;


        public class AliasEntry
        {
            [XmlAttribute("original")]
            public string Original { get; set; }
            [XmlAttribute("target")]
            public string Target { get; set; }
        }
    }



}
