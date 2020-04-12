using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Overrides
{
    public class IBoardBunchContainer<CC> where CC : CacheControl
    {
        [XmlIgnore]
        internal BasicRenderInformation m_nameSubInfo;
        [XmlIgnore]
        internal CC[] m_boardsData;
    }


}
