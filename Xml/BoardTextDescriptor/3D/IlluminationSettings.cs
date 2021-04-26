using FontStashSharp;
using Klyte.Commons.Utils;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{
    public partial class BoardTextDescriptorGeneralXml
    {
        public class IlluminationSettings
        {
            [XmlAttribute("type")]
            public MaterialType IlluminationType { get; set; } = MaterialType.OPAQUE;
            [XmlAttribute("strength")]
            public float IlluminationStrength { get; set; } = 1;
            [XmlAttribute("blinkType")]
            public BlinkType BlinkType { get; set; } = BlinkType.None;
            [XmlElement("customBlinkParams")]
            public Vector4Xml CustomBlink { get; set; } = new Vector4Xml();
            [XmlAttribute("requiredFlags")]
            public int m_requiredFlags;
            [XmlAttribute("forbiddenFlags")]
            public int m_forbiddenFlags;

        }
    }

}

