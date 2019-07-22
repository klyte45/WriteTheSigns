using System.Xml.Serialization;

namespace Klyte.DynamicTextBoards.Overrides
{
    public class IBoardBunchContainer<CC, BRI> where CC : CacheControl where BRI : BasicRenderInformation
    {
        [XmlIgnore]
        internal BRI m_nameSubInfo;
        [XmlIgnore]
        internal CC[] m_boardsData;
    }


}
