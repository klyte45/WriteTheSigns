using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{
    public class TextParameterXmlContainer
    {
        [XmlAttribute("value")]
        public string Value { get; set; }
        [XmlAttribute("isEmpty")]
        public bool IsEmpty { get; set; }

        public static TextParameterXmlContainer FromWrapper(TextParameterWrapper input) => new TextParameterXmlContainer
        {
            IsEmpty = input.IsEmpty,
            Value = input.ToString()
        };
        public TextParameterWrapper ToWrapper() => new TextParameterWrapper(IsEmpty ? null : Value);
    }
}
