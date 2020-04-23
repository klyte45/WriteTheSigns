using Klyte.Commons.Interfaces;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using static Klyte.Commons.Utils.XmlUtils;

namespace Klyte.WriteTheSigns.Xml
{

    public class BoardBunchContainerOnNetXml : IBoardBunchContainer, ILibable
    {
        [XmlIgnore]
        private CacheControlOnNet[] m_boardsData;
        [XmlIgnore]
        public bool cached = false;
        [XmlElement("descriptors")]
        public ListWrapper<BoardDescriptorOnNetXml> Descriptors
        {
            get => new ListWrapper<BoardDescriptorOnNetXml> { listVal = m_boardsData.Select(x => x.descriptor).ToList() };
            set => m_boardsData = value.listVal.Select(x => new CacheControlOnNet { descriptor = x }).ToArray();
        }

        [XmlAttribute("saveName")]
        public string SaveName { get; set; }

        public bool HasAnyBoard() => (m_boardsData?.Where(y => y != null)?.Count() ?? 0) > 0;
    }


}
