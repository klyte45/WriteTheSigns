using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{
    public class OnNetInstanceCacheContainerXml : BoardInstanceOnNetXml
    {
        [XmlElement("cachedPos")]
        public Vector3Xml m_cachedPosition;
        [XmlElement("cachedRot")]
        public Vector3Xml m_cachedRotation;
        [XmlIgnore]
        public PropInfo m_cachedProp;
        [XmlIgnore]
        public ushort? m_cachedDistrictParkId;
        [XmlIgnore]
        public ushort? m_cachedDistrictId;
        [XmlAttribute("cachedParkId")]
        public ushort CachedDistrictParkId
        {
            get {
                if ((m_parkUpdated < WTSOnNetData.Instance.m_lastUpdateDistrict || m_cachedDistrictParkId == null) && m_cachedPosition != null)
                {
                    m_cachedDistrictParkId = DistrictManager.instance.GetPark(m_cachedPosition);
                    m_parkUpdated = WTSOnNetData.Instance.m_lastUpdateDistrict;
                }
                return m_cachedDistrictParkId ?? 0;
            }
        }
        [XmlAttribute("cachedDistrictId")]
        public ushort CachedDistrictId
        {
            get {
                if ((m_districtUpdated < WTSOnNetData.Instance.m_lastUpdateDistrict || m_cachedDistrictId == null) && m_cachedPosition != null)
                {
                    m_cachedDistrictId = DistrictManager.instance.GetDistrict(m_cachedPosition);
                    m_districtUpdated = WTSOnNetData.Instance.m_lastUpdateDistrict;
                }
                return m_cachedDistrictId ?? 0;
            }
        }
        [XmlIgnore]
        private long m_districtUpdated;
        [XmlIgnore]
        private long m_parkUpdated;

        public override void OnChangeMatrixData()
        {
            m_cachedPosition = null;
            m_cachedRotation = null;
            m_cachedDistrictId = null;
            m_cachedDistrictParkId = null;
        }
    }
}
