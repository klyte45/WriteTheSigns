using System.Linq;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Overrides
{
    public interface IBoardBunchContainer
    {
        [XmlIgnore]
        BasicRenderInformation NameSubInfo { get; set; }

        public bool HasAnyBoard();
    }

    public class IBoardBunchContainer<CC> : IBoardBunchContainer where CC : CacheControl
    {
        [XmlIgnore]
        internal CC[] m_boardsData;

        [XmlIgnore]
        public BasicRenderInformation NameSubInfo { get; set; }

        public bool HasAnyBoard() => (m_boardsData?.Where(y => y != null)?.Count() ?? 0) > 0;
    }


}
