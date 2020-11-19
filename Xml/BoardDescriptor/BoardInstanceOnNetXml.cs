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

        [XmlAttribute("propLayoutName")]
        public string PropLayoutName
        {
            get => Descriptor?.SaveName;
            set {
                m_propLayoutName = value;
                m_descriptor = null;
            }
        }

        private string m_propLayoutName;


        [XmlAttribute("simplePropName")]
        public string m_simplePropName;

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



        [XmlIgnore]
        public PropInfo SimpleProp
        {
            get {
                if (m_simplePropName != null && m_simpleProp?.name != m_simplePropName)
                {
                    m_simpleProp = PrefabCollection<PropInfo>.FindLoaded(m_simplePropName);
                    if (m_simpleProp == null)
                    {
                        m_simplePropName = null;
                    }
                }
                return m_simpleProp;
            }
            internal set {
                m_simplePropName = value?.name;
                m_simpleProp = null;
            }
        }
        [XmlIgnore]
        private PropInfo m_simpleProp;

        [XmlAttribute("saveName")]
        public string SaveName { get; set; }
    }

}
