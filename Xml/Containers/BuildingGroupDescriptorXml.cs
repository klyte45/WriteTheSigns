using ColossalFramework.UI;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using System;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{
    [XmlRoot("buildingConfig")]
    public class BuildingGroupDescriptorXml : ILibable
    {

        [XmlAttribute("buildingName")]
        public string BuildingName { get; set; }
        [XmlElement("boardDescriptor")]
        public BoardInstanceBuildingXml[] PropInstances
        {
            get => m_propInstances;
            set {
                if (value != null)
                {
                    m_propInstances = value;
                    m_descriptors = new BoardDescriptorGeneralXml[PropInstances.Length];
                }
            }
        }
        [XmlAttribute("saveName")]
        public string SaveName { get; set; }

        [XmlAttribute("stopMappingThresold")]
        public float StopMappingThresold { get; set; } = 1f;

        [XmlAttribute("versionWTSLastEdit")]
        public string VersionWTSLastEdit { get; } = WriteTheSignsMod.FullVersion;

        [XmlAttribute("versionWTSCreation")]
        public string VersionWTSCreation { get; private set; } = WriteTheSignsMod.FullVersion;



        [XmlIgnore]
        protected BoardDescriptorGeneralXml[] m_descriptors = new BoardDescriptorGeneralXml[0];
        protected BoardInstanceBuildingXml[] m_propInstances = new BoardInstanceBuildingXml[0];

        public virtual BoardDescriptorGeneralXml GetDescriptorOf(int id)
        {
            if (m_descriptors == null || id >= m_descriptors.Length)
            {
                return null;
            }
            ref BoardDescriptorGeneralXml descriptor = ref m_descriptors[id];
            if (descriptor?.SaveName != m_propInstances[id].PropLayoutName && m_propInstances[id].PropLayoutName != null)
            {
                descriptor = WTSPropLayoutData.Instance.Get(m_propInstances[id].PropLayoutName);
                if (descriptor == null)
                {
                    m_propInstances[id].PropLayoutName = null;
                }
            }
            return descriptor;
        }

        [XmlElement("localLayout")]
        [Obsolete]
        public virtual SimpleXmlDictionary<string, BoardDescriptorGeneralXml> LocalLayouts
        {
            get {
                var m_localLayouts = PropInstances.Select(x => WTSPropLayoutData.Instance.Get(x.PropLayoutName)).Where(x => x != null).GroupBy(x => x.SaveName).Select(x => x.FirstOrDefault()).ToDictionary(x => x.SaveName, x => x);
                var res = new SimpleXmlDictionary<string, BoardDescriptorGeneralXml>();
                m_localLayouts.ForEach(x => res[x.Key] = x.Value);
                return res;
            }
            set { }
        }
    }
}