using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Data
{

    [XmlRoot("WTSOnNetData")]
    public class WTSOnNetData : DataExtensorBase<WTSOnNetData>
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
                LoadDefaults();
                foreach (var kv in value.Keys)
                {
                    m_boardsContainers[kv] = value[kv];
                }
            }
        }

        public override string SaveId => "K45_WTS_WTSOnNetData";

        public override void LoadDefaults()
        {
            base.LoadDefaults();
            m_boardsContainers = new OnNetGroupDescriptorXml[NetManager.MAX_SEGMENT_COUNT];
        }

        [XmlAttribute("defaultFont")]
        public virtual string DefaultFont { get; set; }


        [XmlIgnore]
        private byte?[] m_cachedDistrictParkId = new byte?[NetManager.MAX_SEGMENT_COUNT];
        [XmlIgnore]
        private byte?[] m_cachedDistrictId = new byte?[NetManager.MAX_SEGMENT_COUNT];
        public ushort GetCachedDistrictParkId(ushort segmentId)
        {
            if ( m_cachedDistrictParkId[segmentId] == null)
            {
                m_cachedDistrictParkId[segmentId] = DistrictManager.instance.GetPark(NetManager.instance.m_segments.m_buffer[segmentId].m_middlePosition);
            }
            return m_cachedDistrictParkId[segmentId] ?? 0;
        }
        public ushort GetCachedDistrictId(ushort segmentId)
        {
            if ( m_cachedDistrictId[segmentId] == null)
            {
                m_cachedDistrictId[segmentId] = DistrictManager.instance.GetDistrict(NetManager.instance.m_segments.m_buffer[segmentId].m_middlePosition);
            }
            return m_cachedDistrictId[segmentId] ?? 0;
        }

        public void ResetDistrictCache()
        {
            m_cachedDistrictParkId = new byte?[NetManager.MAX_SEGMENT_COUNT];
            m_cachedDistrictId = new byte?[NetManager.MAX_SEGMENT_COUNT];
        }

    }

}
