using ColossalFramework.UI;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using System;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{

    public class OnNetGroupDescriptorXml : IBoardBunchContainer
    {
        [XmlIgnore]
        internal OnNetInstanceCacheContainerXml[] BoardsData { get; set; }
        [XmlIgnore]
        public bool cached = false;
        [XmlElement("BoardsData")]
        public SimpleXmlList<OnNetInstanceCacheContainerXml> BoardsDataExportable
        {
            get => new SimpleXmlList<OnNetInstanceCacheContainerXml>(BoardsData);
            set => BoardsData = value.ToArray();
        }
        public bool HasAnyBoard() => (BoardsData?.Where(y => y != null)?.Count() ?? 0) > 0;
        [XmlIgnore]
        [Obsolete]
        public SimpleXmlDictionary<string, BoardDescriptorGeneralXml> LocalLayouts
        {
            get {
                var m_localLayouts = BoardsData.Select(x => WTSPropLayoutData.Instance.Get(x.PropLayoutName)).Where(x => x != null).GroupBy(x => x.SaveName).Select(x => x.FirstOrDefault()).ToDictionary(x => x.SaveName, x => x);
                var res = new SimpleXmlDictionary<string, BoardDescriptorGeneralXml>();
                m_localLayouts.ForEach(x => res[x.Key] = x.Value);
                return res;
            }
            set { }
        }

        [XmlIgnore]
        private long m_districtUpdated;
        [XmlIgnore]
        private long m_parkUpdated;
    }
    public class ExportableBoardInstanceOnNetListXml : ILibable
    {
        public BoardInstanceOnNetXml[] Instances { get; set; }
        public SimpleXmlDictionary<string, BoardDescriptorGeneralXml> Layouts { get; set; }
        [XmlAttribute("saveName")]
        public string SaveName { get; set; }
    }

}
