using ColossalFramework;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [XmlElement("TextParametersV3")]
        [Obsolete("Legacy Beta < 3! Compat only", true)]
        public SimpleNonSequentialList<string> Old_b3_TextParameters
        {
            get => null;
            set
            {
                foreach (var k in value?.Keys)
                {
                    if (k < m_textParameters.Length)
                    {
                        m_textParameters[k] = new TextParameterWrapper(value[k]);
                    }
                }
            }
        }
        [XmlElement("TextParametersV4")]
        public SimpleNonSequentialList<TextParameterXmlContainer> TextParameters
        {
            get
            {
                var res = new SimpleNonSequentialList<TextParameterXmlContainer>();
                for (int i = 0; i < m_textParameters.Length; i++)
                {
                    if (m_textParameters[i] != null)
                    {
                        res[i] = TextParameterXmlContainer.FromWrapper(m_textParameters[i]);
                    }
                }
                return res;
            }
            set
            {
                foreach (var k in value?.Keys)
                {
                    if (k < m_textParameters.Length)
                    {
                        m_textParameters[k] = value[k]?.ToWrapper();
                    }
                }
            }
        }

        public void SetTextParameter(int idx, string val)
        {
            if (m_textParameters == null)
            {
                m_textParameters = new TextParameterWrapper[TEXT_PARAMETERS_COUNT];
            }
            m_textParameters[idx] = val.IsNullOrWhiteSpace() ? null : new TextParameterWrapper(val);
        }
        public void DeleteTextParameter(int idx)
        {
            if (m_textParameters == null)
            {
                m_textParameters = new TextParameterWrapper[TEXT_PARAMETERS_COUNT];
            }
            m_textParameters[idx] = null;
        }

        public Dictionary<int, string[]> GetAllParametersUsed() => Descriptor?.TextDescriptors.Select(x => x.ToParameterKV()).Where(x => !(x is null)).GroupBy(x => x.First).ToDictionary(x => x.Key, x => x.Select(y => y.Second).ToArray());

        [XmlIgnore]
        public TextParameterWrapper[] m_textParameters = new TextParameterWrapper[TEXT_PARAMETERS_COUNT];

        public TextParameterWrapper GetTextParameter(int idx) => m_textParameters?[idx];
    }

    public class TextParameterXmlContainer
    {
        [XmlAttribute("value")]
        public string Value { get; set; }
        [XmlAttribute("isEmpty")]
        public bool IsEmpty { get; set; }

        public static TextParameterXmlContainer FromWrapper(TextParameterWrapper input) => new TextParameterXmlContainer
        {
            IsEmpty = input.IsEmpty,
            Value = input.ToString()
        };
        public TextParameterWrapper ToWrapper() => new TextParameterWrapper(IsEmpty ? null : Value);
    }
}
