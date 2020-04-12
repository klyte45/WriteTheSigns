using Klyte.Commons.Interfaces;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Overrides
{
    public class BoardTextDescriptorSteetSignXml : BoardTextDescriptorParentXml<BoardTextDescriptorSteetSignXml>, ILibable
    {
        [XmlAttribute("saveName")]
        public string SaveName { get; set; }
    }



}
