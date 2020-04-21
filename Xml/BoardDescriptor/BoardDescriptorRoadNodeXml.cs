using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using static ItemClass;

namespace Klyte.WriteTheCity.Xml
{
    [XmlRoot("roadCornerDescriptor")]
    public class BoardInstanceRoadNodeXml : BoardInstanceXml
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

        [XmlAttribute("useDistrictColor")]
        public bool UseDistrictColor = false;

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
    }


}
