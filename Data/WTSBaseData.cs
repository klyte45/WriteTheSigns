using Klyte.Commons.Interfaces;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Data
{
    public abstract class WTSBaseData<D, CC> : DataExtensorBase<D> where D : WTSBaseData<D, CC>, new()
    {
        [XmlIgnore]
        public CC[,,] BoardsContainers { get; private set; }
        [XmlIgnore]
        public abstract int ObjArraySize { get; }
        [XmlIgnore]
        public abstract int BoardCount { get; }
        [XmlIgnore]
        public abstract int SubBoardCount { get; }

        public override void LoadDefaults()
        {
            base.LoadDefaults();
            BoardsContainers = new CC[ObjArraySize, BoardCount, SubBoardCount];
        }
        public virtual void ResetBoards() => BoardsContainers = new CC[ObjArraySize, BoardCount, SubBoardCount];

        [XmlAttribute("defaultFont")]
        public virtual string DefaultFont { get; set; }

    }

}
