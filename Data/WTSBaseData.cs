using ICities;
using Klyte.Commons.Interfaces;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Data
{
    public abstract class WTSBaseData<D, CC> : DataExtensionBase<D> where D : WTSBaseData<D, CC>, new()
    {
        private CC[,,] m_boardsContainers;

        [XmlIgnore]
        public ref CC[,,] BoardsContainers
        {
            get {
                if (m_boardsContainers == null)
                {
                    ResetBoards();
                }
                return ref m_boardsContainers;
            }
        }
        [XmlIgnore]
        public abstract int ObjArraySize { get; }
        [XmlIgnore]
        public abstract int BoardCount { get; }
        [XmlIgnore]
        public abstract int SubBoardCount { get; }

        public override void LoadDefaults(ISerializableData serializableData)
        {
            base.LoadDefaults(serializableData);
            m_boardsContainers = new CC[ObjArraySize, BoardCount, SubBoardCount];
        }
        protected virtual void ResetBoards() => m_boardsContainers = new CC[ObjArraySize, BoardCount, SubBoardCount];

        [XmlAttribute("defaultFont")]
        public virtual string DefaultFont { get; set; }

    }

}
