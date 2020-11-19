using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{

    public class BoardInstanceBuildingXml : BoardInstanceXml, ILibable

    {
        [XmlArray("platformOrder")]
        [XmlArrayItem("p")]
        public int[] m_platforms = new int[0];
        [XmlAttribute("showIfNoLine")]
        public bool m_showIfNoLine = true;

        [XmlElement("arrayRepeatOffset")]
        public Vector3Xml ArrayRepeat
        {
            get => m_arrayRepeat; set {
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
            set {
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
            get => m_propLayoutName; set {
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
            get {
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
            internal set {
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

        [XmlAttribute("simplePropName")]
        public string m_simplePropName;
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
        private Vector3Xml m_arrayRepeat = new Vector3Xml();

        [XmlIgnore]
        internal BuildingGroupDescriptorXml ParentGroup
        {
            private get; set;
        }
    }

    public class ExportableBoardInstanceBuildingListXml : ILibable
    {
        public BoardInstanceBuildingXml[] Instances { get; set; }
        public SimpleXmlDictionary<string, BoardDescriptorGeneralXml> Layouts { get; set; }
        [XmlAttribute("saveName")]
        public string SaveName { get; set; }
    }

    public enum ColoringMode
    {
        ByPlatform,
        Fixed,
        ByDistrict,
        FromBuilding
    }
}