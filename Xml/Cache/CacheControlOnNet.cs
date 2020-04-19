using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.WriteTheCity.Xml
{

    public class CacheControlOnNet : CacheControl
    {
        [XmlElement("descriptor")]
        public BoardDescriptorOnNetXml descriptor;
        [XmlIgnore]
        public Vector3 cachedPosition;
        [XmlIgnore]
        public Vector3 cachedRotation;
    }


}
