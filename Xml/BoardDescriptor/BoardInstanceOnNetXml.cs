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
        public TextParameterWrapper[] m_textParameters = new TextParameterWrapper[TEXT_PARAMETERS_COUNT];

        public TextParameterWrapper GetTextParameter(int idx) => m_textParameters?[idx];


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
        private PropInfo m_simpleProp;

        [XmlAttribute("saveName")]
        public string SaveName { get; set; }
        [XmlAttribute("textParameter1")] [Obsolete("Legacy Beta 1. Compat only!", true)] public string TextParameter1 { get => null; set => SetTextParameter(1, value); }
        [XmlAttribute("textParameter2")] [Obsolete("Legacy Beta 1. Compat only!", true)] public string TextParameter2 { get => null; set => SetTextParameter(2, value); }
        [XmlAttribute("textParameter3")] [Obsolete("Legacy Beta 1. Compat only!", true)] public string TextParameter3 { get => null; set => SetTextParameter(3, value); }
        [XmlAttribute("textParameter4")] [Obsolete("Legacy Beta 1. Compat only!", true)] public string TextParameter4 { get => null; set => SetTextParameter(4, value); }
        [XmlAttribute("textParameter5")] [Obsolete("Legacy Beta 1. Compat only!", true)] public string TextParameter5 { get => null; set => SetTextParameter(5, value); }
        [XmlAttribute("textParameter6")] [Obsolete("Legacy Beta 1. Compat only!", true)] public string TextParameter6 { get => null; set => SetTextParameter(6, value); }
        [XmlAttribute("textParameter7")] [Obsolete("Legacy Beta 1. Compat only!", true)] public string TextParameter7 { get => null; set => SetTextParameter(7, value); }
        [XmlAttribute("textParameter8")] [Obsolete("Legacy Beta 1. Compat only!", true)] public string TextParameter8 { get => null; set => SetTextParameter(8, value); }


        [XmlElement("TextParameters")]
        [Obsolete("Legacy Beta < 2.1! Compat only", true)]
        public SimpleNonSequentialList<string> Old_b2_TextParameters
        {
            get => null;
            set
            {
                foreach (var k in value?.Keys)
                {
                    if (k < m_textParameters.Length)
                    {
                        m_textParameters[k] = new TextParameterWrapper(value[k], true);
                    }
                }
            }
        }

        [XmlElement("TextParametersV3")]
        [Obsolete("Export only!", true)]
        public SimpleNonSequentialList<string> TextParameters
        {
            get
            {
                var res = new SimpleNonSequentialList<string>();
                for (int i = 0; i < m_textParameters.Length; i++)
                {
                    if (m_textParameters[i] != null)
                    {
                        res[i] = m_textParameters[i].ToString();
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
                        m_textParameters[k] = new TextParameterWrapper(value[k]);
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

        public Dictionary<int, string[]> GetAllParametersUsed()
        {
            if (!(Descriptor is null))
            {
                return Descriptor.TextDescriptors.Select(x => x.ToParameterKV()).Where(x => !(x is null)).GroupBy(x => x.First).ToDictionary(x => x.Key, x => x.Select(y => y.Second).ToArray());
            }
            return null;
        }
    }
}
