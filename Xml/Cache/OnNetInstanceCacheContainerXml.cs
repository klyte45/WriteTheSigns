using Klyte.Commons.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{
    [XmlRoot("onNetDescriptor")]
    public class OnNetInstanceCacheContainerXml : BoardInstanceOnNetXml
    {
        [XmlAttribute("targetSegment1")] [Obsolete("Pre-beta 4 fields", true)] public ushort? m_targetSegment1 { get => null; set { if (value != null) { m_targets[1] = value ?? 0; } } }
        [XmlAttribute("targetSegment2")] [Obsolete("Pre-beta 4 fields", true)] public ushort? m_targetSegment2 { get => null; set { if (value != null) { m_targets[2] = value ?? 0; } } }
        [XmlAttribute("targetSegment3")] [Obsolete("Pre-beta 4 fields", true)] public ushort? m_targetSegment3 { get => null; set { if (value != null) { m_targets[3] = value ?? 0; } } }
        [XmlAttribute("targetSegment4")] [Obsolete("Pre-beta 4 fields", true)] public ushort? m_targetSegment4 { get => null; set { if (value != null) { m_targets[4] = value ?? 0; } } }

        [XmlElement("targetSegments")]
        public SimpleNonSequentialList<ushort> m_targets = new SimpleNonSequentialList<ushort>();

        [XmlIgnore]
        public List<Vector3Xml> m_cachedPositions;
        [XmlIgnore]
        public List<Vector3Xml> m_cachedRotations;
        [XmlIgnore]
        public PropInfo m_simpleCachedProp => m_simpleProp;

        public override void OnChangeMatrixData()
        {
            base.OnChangeMatrixData();
            m_cachedPositions = null;
            m_cachedRotations = null;
        }

        public ushort GetTargetSegment(int id) => m_targets.TryGetValue(id, out ushort value) ? value : (ushort)0;
        public void SetTargetSegment(int id, ushort value) => m_targets[id] = value;


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

        public TextParameterWrapper SetTextParameter(int idx, string val)
        {
            if (m_textParameters == null)
            {
                m_textParameters = new TextParameterWrapper[TEXT_PARAMETERS_COUNT];
            }
            return m_textParameters[idx] = new TextParameterWrapper(val);
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
        public Dictionary<int, List<BoardTextDescriptorGeneralXml>> GetAllParametersUsedWithData() => Descriptor?.TextDescriptors.Where(x => x.IsParameter()).GroupBy(x => x.m_parameterIdx).ToDictionary(x => x.Key, x => x.ToList());

        [XmlIgnore]
        public TextParameterWrapper[] m_textParameters = new TextParameterWrapper[TEXT_PARAMETERS_COUNT];

        public override TextParameterWrapper GetParameter(int idx) => m_textParameters?[idx];
    }
}
