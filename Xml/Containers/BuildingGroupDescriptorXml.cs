﻿using ColossalFramework.UI;
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
    [XmlRoot("buildingConfig")]
    public class BuildingGroupDescriptorXml : ILibable
    {

        [XmlAttribute("buildingName")]
        public string BuildingName { get; set; }
        [XmlElement("boardDescriptor")]
        public BoardInstanceBuildingXml[] PropInstances
        {
            get => m_propInstances;
            set
            {
                if (value != null)
                {
                    m_propInstances = value;
                    m_propInstances.ForEach((x) => x.ParentGroup = this);
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
        internal Dictionary<string, BoardDescriptorGeneralXml> m_localLayouts;

        [XmlIgnore]
        protected BoardInstanceBuildingXml[] m_propInstances = new BoardInstanceBuildingXml[0];

        [XmlElement("localLayout")]
        [Obsolete("Export only!", true)]
        public virtual SimpleXmlDictionary<string, BoardDescriptorGeneralXml> LocalLayouts
        {
            get => CaculateLocalLayouts();
            set { }
        }

        internal SimpleXmlDictionary<string, BoardDescriptorGeneralXml> CaculateLocalLayouts()
        {
            m_localLayouts = PropInstances.Select(x => WTSPropLayoutData.Instance.Get(x.PropLayoutName)).Where(x => x != null).GroupBy(x => x.SaveName).Select(x => x.FirstOrDefault()).ToDictionary(x => x.SaveName, x => x);
            var res = new SimpleXmlDictionary<string, BoardDescriptorGeneralXml>();
            m_localLayouts.ForEach(x => res[x.Key] = x.Value);
            return res;
        }
    }
}