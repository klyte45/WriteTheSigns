using ColossalFramework.UI;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Overrides;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Klyte.DynamicTextProps.Data
{
    public abstract class DTPBaseData<D, BBC, CC> : DataExtensorBase<D> where D : DTPBaseData<D, BBC, CC>, new() where BBC : IBoardBunchContainer<CC> where CC : CacheControl
    {
        public BBC[] BoardsContainers { get; private set; }
        public abstract int ObjArraySize { get; }

        public override void LoadDefaults()
        {
            base.LoadDefaults();
            BoardsContainers = new BBC[ObjArraySize];
        }
        public void ResetBoards() => BoardSerialData = null;

        [XmlArray("BoardsContainer")]
        [XmlArrayItem("Board")]
        public SimpleNonSequentialList<BBC> BoardSerialData
        {
            get {
                var temp = new SimpleNonSequentialList<BBC>();
                BoardsContainers.Select((x, i) => Tuple.New(i, x)).Where((x) => x?.Second?.m_boardsData?.Where(y => y != null).Count() > 0).ForEach(x => temp[x.First] = x.Second);
                return temp;
            }
            set {
                BoardsContainers = new BBC[ObjArraySize];
                if (value != null)
                {
                    foreach (KeyValuePair<long, BBC> item in value)
                    {
                        LogUtils.DoLog($"item: {item}");
                        if (item.Key == 0)
                        {
                            continue;
                        }
                        BoardsContainers[item.Key] = item.Value;
                    }
                }
            }
        }

    }

}
