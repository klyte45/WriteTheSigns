using Klyte.Commons.Interfaces;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using static Klyte.Commons.Utils.XmlUtils;

namespace Klyte.DynamicTextProps.Overrides
{

    public class BoardBunchContainerHighwaySignXml : IBoardBunchContainer<CacheControlHighwaySign>, ILibable
    {
        [XmlIgnore]
        public bool cached = false;
        [XmlElement("descriptors")]
        public ListWrapper<BoardDescriptorOnNetXml> Descriptors
        {
            get => new ListWrapper<BoardDescriptorOnNetXml> { listVal = m_boardsData.Select(x => x.descriptor).ToList() };
            set => m_boardsData = value.listVal.Select(x => new CacheControlHighwaySign { descriptor = x }).ToArray();
        }

        [XmlAttribute("saveName")]
        public string SaveName { get; set; }
    }


}
