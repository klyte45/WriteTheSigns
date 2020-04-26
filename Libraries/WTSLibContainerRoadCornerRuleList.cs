using Klyte.Commons.Interfaces;
using Klyte.WriteTheSigns.Xml;
using System.Xml.Serialization;
using static Klyte.Commons.Utils.XmlUtils;

namespace Klyte.WriteTheSigns.Libraries
{
    public class WTSLibContainerRoadCornerRuleList : ILibable
    {
        [XmlAttribute("saveName")]
        public string SaveName { get; set; }
        [XmlElement("DescriptorRulesOrder")]
        public ListWrapper<BoardInstanceRoadNodeXml> DescriptorRulesOrderXml { get; set; }
    }

}