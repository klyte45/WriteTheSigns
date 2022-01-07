using ICities;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Data
{

    [XmlRoot("WTSOnNetData")]
    public class WTSOnNetData : DataExtensionBase<WTSOnNetData>
    {
        [XmlIgnore]
        public OnNetGroupDescriptorXml[] m_boardsContainers = new OnNetGroupDescriptorXml[NetManager.MAX_SEGMENT_COUNT];
        [XmlElement("BoardContainers")]
        public SimpleNonSequentialList<OnNetGroupDescriptorXml> BoardContainersExport
        {
            get {
                var res = new SimpleNonSequentialList<OnNetGroupDescriptorXml>();
                for (int i = 0; i < m_boardsContainers.Length; i++)
                {
                    if (m_boardsContainers[i] != null && m_boardsContainers[i].HasAnyBoard())
                    {
                        res[i] = m_boardsContainers[i];
                    }
                }
                return res;
            }

            set {
                LoadDefaults(null);
                foreach (var kv in value.Keys)
                {
                    m_boardsContainers[kv] = value[kv];
                }
            }
        }

        public override string SaveId => "K45_WTS_WTSOnNetData";

        public override void LoadDefaults(ISerializableData serializableData)
        {
            base.LoadDefaults(serializableData);
            m_boardsContainers = new OnNetGroupDescriptorXml[NetManager.MAX_SEGMENT_COUNT];
        }

        [XmlAttribute("defaultFont")]
        public virtual string DefaultFont { get; set; }
     

    }

}
