using Klyte.DynamicTextProps.Libraries;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Overrides
{
    public partial class BoardGeneratorRoadNodes
    {
        public class BoardTextDescriptorSteetSignXml : BoardTextDescriptorParentXml<BoardTextDescriptorSteetSignXml>, ILibable
        {
            [XmlAttribute("saveName")]
            public string SaveName { get; set; }
        }

    }

}
