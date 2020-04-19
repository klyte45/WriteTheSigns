using ColossalFramework.UI;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheCity.Overrides;
using Klyte.WriteTheCity.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Klyte.WriteTheCity.Data
{
    public abstract class WTCBaseData<D, BBC> : DataExtensorBase<D> where D : WTCBaseData<D, BBC>, new() where BBC : IBoardBunchContainer
    {
        public BBC[] BoardsContainers { get; private set; }
        public abstract int ObjArraySize { get; }

        public override void LoadDefaults()
        {
            base.LoadDefaults();
            BoardsContainers = new BBC[ObjArraySize];
        }
        public void ResetBoards() => BoardSerialData = null;

        [XmlAttribute("defaultFont")]
        public virtual string DefaultFont { get; set; }

        [XmlElement("BoardsContainer")]
        public SimpleNonSequentialList<BBC> BoardSerialData
        {
            get {
                var temp = new SimpleNonSequentialList<BBC>();
                BoardsContainers.Select((x, i) => Tuple.New(i, x)).Where((x) => x?.Second?.HasAnyBoard() ?? false).ForEach(x => temp[x.First] = x.Second);
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
