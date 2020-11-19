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

        public override void OnChangeMatrixData()
        {
            m_cachedPosition = null;
            m_cachedRotation = null;
        }
    }
}
