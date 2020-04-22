using Klyte.Commons.Interfaces;
using Klyte.WriteTheSigns.Data;
using System.Collections.Generic;
using System.Xml.Serialization;
using static ItemClass;

namespace Klyte.WriteTheSigns.Xml
{
    [XmlRoot("roadCornerDescriptor")]
    public class BoardInstanceRoadNodeXml : BoardInstanceXml, ILibable
    {
        [XmlAttribute("saveName")] public string SaveName { get; set; }

        [XmlAttribute("propLayoutName")]
        public string PropLayoutName
        {
            get => Descriptor?.SaveName;
            set {
                m_propLayoutName = value;
                Descriptor = WTSPropLayoutData.Instance.Get(m_propLayoutName);
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
                    m_descriptor = WTSPropLayoutData.Instance.Get(m_propLayoutName);
                    m_propLayoutName = null;
                }
                return m_descriptor;
            }
            internal set {
                m_propLayoutName = value?.SaveName;
                m_descriptor = WTSPropLayoutData.Instance.Get(m_propLayoutName);
            }
        }

        [XmlArray("ReferenceSelection")] [XmlArrayItem("ItemClass")] public HashSet<ItemClass.Level> AllowedLevels { get; set; } = new HashSet<Level>();
        [XmlAttribute("spawnChance")] public byte SpawnChance { get; set; } = 255;
        [XmlAttribute("placeOnDistrictBorder")] public bool PlaceOnDistrictBorder { get; set; } = true;
        [XmlAttribute("placeOnMidSegment")] public bool PlaceOnSegmentInsteadOfCorner { get; set; } = false;
        [XmlAttribute("placeOnlyIfOutboundTraffic")] public bool PlaceOnlyIfOutboundTraffic { get; set; } = false;



        [XmlAttribute("useDistrictColor")] public bool UseDistrictColor = false;
        [XmlAttribute("applyAbreviationsOnFullName")] public bool ApplyAbreviationsOnFullName { get; set; } = true;
        [XmlAttribute("applyAbreviationsOnSuffix")] public bool ApplyAbreviationsOnSuffix { get; set; } = true;

        [XmlAttribute("selectedDistricts")] public HashSet<ushort> SelectedDistricts { get; set; } = new HashSet<ushort>();
        [XmlAttribute("districtSelectionIsBlacklist")] public bool SelectedDistrictsIsBlacklist { get; set; } = true;

        public bool AllowsClass(ItemClass source) => AllowedLevels.Contains(source.m_level);
        public bool AllowsDistrict(byte districtId) => SelectedDistricts.Contains(districtId) == SelectedDistrictsIsBlacklist;
        public bool AllowsPark(byte districtId) => SelectedDistricts.Contains((ushort)(0x100 | districtId)) == SelectedDistrictsIsBlacklist;

    }


}
