using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{
    public class AnimationSettings
    {
        [XmlAttribute("extraDelayCycleFrames")]
        public int m_extraDelayCycleFrames;
        [XmlAttribute("itemCycleFramesDuration")]
        public int m_itemCycleFramesDuration = 400;
    }

}

