using Klyte.Commons.Interfaces;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Rendering;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{

    [XmlRoot("onNetDescriptor")]
    public class BoardInstanceOnNetXml : BoardInstanceXml, ILibable
    {
        public const int TEXT_PARAMETERS_COUNT = 10;

        [XmlAttribute("inverted")]
        public bool InvertSign
        {
            get => m_invertSign; set
            {
                m_invertSign = value;
                OnChangeMatrixData();
            }
        }
        [XmlAttribute("segmentPosition")]
        public float SegmentPosition
        {
            get => m_segmentPosition; set
            {
                m_segmentPosition = value;
                OnChangeMatrixData();
            }
        }
        [XmlAttribute("segmentPositionStart")]
        public float SegmentPositionStart
        {
            get => m_segmentPositionStart; set
            {
                m_segmentPositionStart = value;
                OnChangeMatrixData();
            }
        }
        [XmlAttribute("segmentPositionEnd")]
        public float SegmentPositionEnd
        {
            get => m_segmentPositionEnd; set
            {
                m_segmentPositionEnd = value;
                OnChangeMatrixData();
            }
        }
        [XmlAttribute("segmentPositionsRepeatCount")]
        public ushort SegmentPositionRepeatCount
        {
            get => m_segmentPositionRepeat; set
            {
                m_segmentPositionRepeat = value;
                OnChangeMatrixData();
            }
        }
        [XmlAttribute("segmentPositionsRepeating")]
        public bool SegmentPositionRepeating
        {
            get => m_segmentRepeatItem; set
            {
                m_segmentRepeatItem = value;
                OnChangeMatrixData();
            }
        }

        [XmlAttribute("propLayoutName")]
        public string PropLayoutName
        {
            get => Descriptor?.SaveName;
            set
            {
                m_propLayoutName = value;
                m_descriptor = null;
                OnChangeMatrixData();
            }
        }

        private string m_propLayoutName;



        [XmlIgnore]
        private BoardDescriptorGeneralXml m_descriptor;
        private float m_segmentPosition = 0.5f;
        private float m_segmentPositionStart = 0f;
        private float m_segmentPositionEnd = 1f;
        private ushort m_segmentPositionRepeat = 1;
        private bool m_segmentRepeatItem = false;
        private bool m_invertSign = false;

        [XmlIgnore]
        public BoardDescriptorGeneralXml Descriptor
        {
            get
            {
                if (m_descriptor == null && m_propLayoutName != null)
                {
                    m_descriptor = WTSPropLayoutData.Instance.Get(m_propLayoutName);
                    if (m_descriptor == null || m_descriptor.m_allowedRenderClass != Rendering.TextRenderingClass.PlaceOnNet)
                    {
                        m_propLayoutName = null;
                    }
                    OnChangeMatrixData();
                }
                return m_descriptor;
            }
            internal set
            {
                m_propLayoutName = value?.SaveName;
                m_descriptor = WTSPropLayoutData.Instance.Get(m_propLayoutName);
            }
        }


        [XmlAttribute("simplePropName")]
        public string m_simplePropName;
        [XmlIgnore]
        public PropInfo SimpleProp
        {
            get
            {
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
            internal set
            {
                m_simplePropName = value?.name;
                m_simpleProp = value;
            }
        }
        [XmlIgnore]
        protected PropInfo m_simpleProp;

        [XmlAttribute("saveName")]
        public string SaveName { get; set; }

        public override PrefabInfo TargetAssetParameter => Descriptor?.CachedProp;

        public override TextRenderingClass RenderingClass => TextRenderingClass.PlaceOnNet;

        public override string DescriptorOverrideFont => Descriptor?.FontName;

        public override TextParameterWrapper GetParameter(int idx) => null;
    }
}
