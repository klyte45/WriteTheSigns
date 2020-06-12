using ColossalFramework.UI;
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
    public class ExportableBuildingGroupDescriptorXml : BuildingGroupDescriptorXml
    {

        [XmlElement("localLayout")]
        [Obsolete]
        public override SimpleXmlDictionary<string, BoardDescriptorGeneralXml> LocalLayouts
        {
            get {
                m_localLayouts = PropInstances.Select(x =>
                {
                    TryGetDescriptor(x.PropLayoutName, out BoardDescriptorGeneralXml desc);
                    return desc;
                }).Where(x => x?.SaveName != null).GroupBy(x => x.SaveName).Select(x => x.FirstOrDefault()).ToDictionary(x => x.SaveName, x => x);
                var res = new SimpleXmlDictionary<string, BoardDescriptorGeneralXml>();
                m_localLayouts.ForEach(x => res[x.Key] = x.Value);
                return res;
            }
            set => m_localLayouts = value;
        }


        private Dictionary<string, BoardDescriptorGeneralXml> m_localLayouts;

        public override BoardDescriptorGeneralXml GetDescriptorOf(int id)
        {
            if (m_descriptors == null || id >= m_descriptors.Length)
            {
                return null;
            }
            ref BoardDescriptorGeneralXml descriptor = ref m_descriptors[id];
            if (descriptor == null && m_propInstances[id].PropLayoutName != null)
            {
                if (!TryGetDescriptor(m_propInstances[id].PropLayoutName, out descriptor))
                {
                    m_propInstances[id].PropLayoutName = null;
                }
            }
            return descriptor;
        }

        private bool TryGetDescriptor(string layoutName, out BoardDescriptorGeneralXml descriptor)
        {
            descriptor = WTSPropLayoutData.Instance.Get(layoutName);
            return layoutName != null && (descriptor != null || m_localLayouts.TryGetValue(layoutName, out descriptor));
        }
    }
}