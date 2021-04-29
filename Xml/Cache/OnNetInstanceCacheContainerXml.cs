using Klyte.Commons.Utils;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{
    [XmlRoot("onNetDescriptor")]
    public class OnNetInstanceCacheContainerXml : BoardInstanceOnNetXml
    {
        [XmlAttribute("targetSegment1")] public ushort m_targetSegment1;
        [XmlAttribute("targetSegment2")] public ushort m_targetSegment2;
        [XmlAttribute("targetSegment3")] public ushort m_targetSegment3;
        [XmlAttribute("targetSegment4")] public ushort m_targetSegment4;

        [XmlElement("cachedPos")]
        public Vector3Xml m_cachedPosition;
        [XmlElement("cachedRot")]
        public Vector3Xml m_cachedRotation;
        [XmlIgnore]
        public PropInfo m_simpleCachedProp;

        public override void OnChangeMatrixData()
        {
            base.OnChangeMatrixData();
            m_cachedPosition = null;
            m_cachedRotation = null;
        }
    }
}
