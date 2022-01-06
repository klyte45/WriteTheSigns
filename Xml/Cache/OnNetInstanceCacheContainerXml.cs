using Klyte.Commons.Utils;
using System.Collections.Generic;
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
        public List<Vector3Xml> m_cachedPositions;
        [XmlElement("cachedRot")]
        public List<Vector3Xml> m_cachedRotations;
        [XmlIgnore]
        public PropInfo m_simpleCachedProp => m_simpleProp;

        public override void OnChangeMatrixData()
        {
            base.OnChangeMatrixData();
            m_cachedPositions = null;
            m_cachedRotations = null;
        }

        public ushort GetTargetSegment(int id)
        {
            switch (id)
            {
                case 1:
                    return m_targetSegment1;
                case 2:
                    return m_targetSegment2;
                case 3:
                    return m_targetSegment3;
                case 4:
                    return m_targetSegment4;
                default:
                    return 0;
            }
        }
    }
}
