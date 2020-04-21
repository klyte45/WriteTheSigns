using Klyte.Commons.Interfaces;
using Klyte.WriteTheCity.Data;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using static ItemClass;

namespace Klyte.WriteTheCity.Xml
{
    [XmlRoot("roadCornerDescriptor")]
    public class BoardInstanceRoadNodeXml : BoardInstanceXml, ILibable
    {
        [XmlArray("ReferenceSelection")]
        [XmlArrayItem("ItemClass")]
        public List<ItemClassSimpleDefinition> ReferenceSelection { get; set; } = new List<ItemClassSimpleDefinition>();

        [XmlAttribute("selectionAsBlacklist")]
        public bool AsBlackList { get; set; }

        [XmlAttribute("placeOnDistrictBorder")]
        public bool PlaceOnDistrictBorder { get; set; } = true;

        [XmlAttribute("applyAbreviationsOnFullName")]
        public bool ApplyAbreviationsOnFullName { get; set; } = true;
        [XmlAttribute("applyAbreviationsOnSuffix")]
        public bool ApplyAbreviationsOnSuffix { get; set; } = true;
        [XmlAttribute("spawnChance")]
        public byte SpawnChance { get; set; } = 255;
        [XmlAttribute("saveName")]
        public string SaveName { get; set; }

        [XmlAttribute("useDistrictColor")]
        public bool UseDistrictColor = false;

        [XmlAttribute("propLayoutName")]
        public string PropLayoutName
        {
            get => Descriptor?.SaveName;
            set {
                m_propLayoutName = value;
                Descriptor = WTCPropLayoutData.Instance.Get(m_propLayoutName);
            }
        }

        private string m_propLayoutName;

        [XmlIgnore]
        private BoardDescriptorGeneralXml m_descriptor;

        [XmlIgnore]
        public override BoardDescriptorGeneralXml Descriptor
        {
            get {
                if (m_descriptor == null && m_propLayoutName != null)
                {
                    m_descriptor = WTCPropLayoutData.Instance.Get(m_propLayoutName);
                    m_propLayoutName = null;
                }
                return m_descriptor;
            }
            internal set {
                m_propLayoutName = value?.SaveName;
                m_descriptor = WTCPropLayoutData.Instance.Get(m_propLayoutName);
            }
        }

        public bool AllowsClass(ItemClass source) => ReferenceSelection.Where(x => x.IsSame(source)).Count() == 0 == AsBlackList;

    }
    public struct ItemClassSimpleDefinition
    {
        public Service m_service;
        public SubService m_subService;
        public Level m_level;
        public Layer m_layer;
        public string name;

        public static ItemClassSimpleDefinition FromItemClass(ItemClass from)
        {
            return new ItemClassSimpleDefinition
            {
                m_layer = from.m_layer,
                m_subService = from.m_subService,
                m_service = from.m_service,
                m_level = from.m_level,
                name = from.name,
            };
        }

        public bool IsSame(ItemClass from)
        {
            return m_layer == from.m_layer &&
                m_subService == from.m_subService &&
                m_service == from.m_service &&
                m_level == from.m_level &&
                name == from.name;
        }

        public override string ToString() => name;
    }


}
