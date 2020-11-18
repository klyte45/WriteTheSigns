using Klyte.Commons.Interfaces;
using Klyte.WriteTheSigns.Data;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{

    [XmlRoot("onNetDescriptor")]
    public class BoardInstanceOnNetXml : BoardInstanceXml, ILibable
    {
        [XmlAttribute("inverted")]
        public bool InvertSign
        {
            get => m_invertSign; set {
                m_invertSign = value;
                OnChangeMatrixData();
            }
        }
        [XmlAttribute("segmentPosition")]
        public float SegmentPosition
        {
            get => m_segmentPosition; set {
                m_segmentPosition = value;
                OnChangeMatrixData();
            }
        }
        [XmlAttribute("targetSegment1")]
        public ushort m_targetSegment1;
        [XmlAttribute("targetSegment2")]
        public ushort m_targetSegment2;
        [XmlAttribute("targetSegment3")]
        public ushort m_targetSegment3;
        [XmlAttribute("targetSegment4")]
        public ushort m_targetSegment4;

        [XmlAttribute("propLayoutName")]
        public string PropLayoutName
        {
            get => Descriptor?.SaveName;
            set {
                m_propLayoutName = value;
                Descriptor = WTSPropLayoutData.Instance.Get(m_propLayoutName);
            }
        }

        private string m_propLayoutName;

        [XmlIgnore]
        private BoardDescriptorGeneralXml m_descriptor;
        private float m_segmentPosition = 0.5f;
        private bool m_invertSign = false;

        [XmlIgnore]
        public BoardDescriptorGeneralXml Descriptor
        {
            get {
                if (m_descriptor == null && m_propLayoutName != null)
                {
                    m_descriptor = WTSPropLayoutData.Instance.Get(m_propLayoutName);
                    if (m_descriptor == null || m_descriptor.m_allowedRenderClass != Rendering.TextRenderingClass.PlaceOnNet)
                    {
                        m_propLayoutName = null;
                    }
                }
                return m_descriptor;
            }
            internal set {
                m_propLayoutName = value?.SaveName;
                m_descriptor = WTSPropLayoutData.Instance.Get(m_propLayoutName);
            }
        }

        [XmlAttribute("saveName")]
        public string SaveName { get; set; }
    }

}
