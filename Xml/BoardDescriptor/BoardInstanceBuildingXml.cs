using ColossalFramework;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{

    public class BoardInstanceBuildingXml : BoardInstanceXml, ILibable
    {
        public const int TEXT_PARAMETERS_COUNT = 10;


        [XmlArray("platformOrder")]
        [XmlArrayItem("p")]
        public int[] m_platforms = new int[0];
        [XmlAttribute("showIfNoLine")]
        public bool m_showIfNoLine = true;

        [XmlElement("arrayRepeatOffset")]
        public Vector3Xml ArrayRepeat
        {
            get => m_arrayRepeat; set
            {
                if (value != m_arrayRepeat)
                {
                    OnChangeMatrixData();
                }
                m_arrayRepeat = value;
            }
        }
        [XmlIgnore]
        private int m_arrayRepeatTimes = 0;
        [XmlAttribute("arrayRepeatTimes")]
        public int ArrayRepeatTimes
        {
            get => m_arrayRepeatTimes;
            set
            {
                if (value != m_arrayRepeatTimes)
                {
                    OnChangeMatrixData();
                }
                m_arrayRepeatTimes = value;
            }
        }

        [XmlAttribute("coloringMode")]
        public ColoringMode ColorModeProp { get; set; } = ColoringMode.Fixed;
        [XmlAttribute("useFixedIfMultiline")]
        public bool UseFixedIfMultiline { get; set; } = true;

        [XmlAttribute("saveName")]
        public string SaveName { get; set; }

        [XmlAttribute("propLayoutName")]
        public string PropLayoutName
        {
            get => m_propLayoutName; set
            {
                m_propLayoutName = value;
                m_descriptor = null;
            }
        }
        [XmlIgnore]
        private string m_propLayoutName;
        [XmlIgnore]
        private BoardDescriptorGeneralXml m_descriptor;

        [XmlIgnore]
        public BoardDescriptorGeneralXml Descriptor
        {
            get
            {
                if (m_descriptor == null && m_propLayoutName != null)
                {
                    if (!TryGetDescriptor(m_propLayoutName, out m_descriptor) || m_descriptor.m_allowedRenderClass != Rendering.TextRenderingClass.Buildings)
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
                m_descriptor = null;
            }
        }
        private bool TryGetDescriptor(string layoutName, out BoardDescriptorGeneralXml descriptor)
        {
            descriptor = WTSPropLayoutData.Instance.Get(layoutName);
            return layoutName != null && (descriptor != null || (ParentGroup?.m_localLayouts?.TryGetValue(layoutName, out descriptor) ?? false));
        }

        [XmlAttribute("subBuildingIdxPivotReference")]
        public int SubBuildingPivotReference { get; set; } = -1;

        private string simplePropName;
        [XmlAttribute("simplePropName")]
        public string SimplePropName
        {
            get => simplePropName; set
            {
                simplePropName = value;
                m_cachedSimpleProp = value is null ? null : PrefabCollection<PropInfo>.FindLoaded(value);
            }
        }
        [XmlIgnore]
        public PropInfo CachedSimpleProp
        {
            get => m_cachedSimpleProp;
            internal set
            {
                SimplePropName = value?.name;
                m_cachedSimpleProp = value;
            }
        }
        [XmlIgnore]
        private PropInfo m_cachedSimpleProp;
        private Vector3Xml m_arrayRepeat = new Vector3Xml();

        [XmlIgnore]
        internal BuildingGroupDescriptorXml ParentGroup
        {
            private get; set;
        }

        [XmlElement("CustomParameters")]
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

        public override PrefabInfo TargetAssetParameter => Descriptor?.CachedProp;

        public override TextRenderingClass RenderingClass => TextRenderingClass.Buildings;

        public override string DescriptorOverrideFont => Descriptor?.FontName;

        public void SetTextParameter(int idx, string val)
        {
            if (m_textParameters == null)
            {
                m_textParameters = new TextParameterWrapper[TEXT_PARAMETERS_COUNT];
            }
            m_textParameters[idx] = val.IsNullOrWhiteSpace() ? null : new TextParameterWrapper(val);
        }

        public Dictionary<int, string[]> GetAllParametersUsed() => Descriptor?.TextDescriptors.Select(x => x.ToParameterKV()).Where(x => !(x is null)).GroupBy(x => x.First).ToDictionary(x => x.Key, x => x.Select(y => y.Second).ToArray());

        [XmlIgnore]
        public TextParameterWrapper[] m_textParameters = new TextParameterWrapper[TEXT_PARAMETERS_COUNT];

        public override TextParameterWrapper GetParameter(int idx) => m_textParameters?[idx];
    }
}