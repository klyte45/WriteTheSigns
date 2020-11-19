using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
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


        [XmlAttribute("textParameter1")] public string m_textParameter1;
        [XmlAttribute("textParameter2")] public string m_textParameter2;
        [XmlAttribute("textParameter3")] public string m_textParameter3;
        [XmlAttribute("textParameter4")] public string m_textParameter4;
        [XmlAttribute("textParameter5")] public string m_textParameter5;
        [XmlAttribute("textParameter6")] public string m_textParameter6;
        [XmlAttribute("textParameter7")] public string m_textParameter7;
        [XmlAttribute("textParameter8")] public string m_textParameter8;

        public string GetTextParam(int idx)
        {
            switch (idx)
            {
                case 1: return m_textParameter1;
                case 2: return m_textParameter2;
                case 3: return m_textParameter3;
                case 4: return m_textParameter4;
                case 5: return m_textParameter5;
                case 6: return m_textParameter6;
                case 7: return m_textParameter7;
                case 8: return m_textParameter8;
            }
            return null;
        }

        [XmlElement("cachedPos")]
        public Vector3Xml m_cachedPosition;
        [XmlElement("cachedRot")]
        public Vector3Xml m_cachedRotation;
        [XmlIgnore]
        public PropInfo m_cachedProp;

        public override void OnChangeMatrixData()
        {
            base.OnChangeMatrixData();
            m_cachedPosition = null;
            m_cachedRotation = null;
        }
    }
}
