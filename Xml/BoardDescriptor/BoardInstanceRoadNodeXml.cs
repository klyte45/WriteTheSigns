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
                m_descriptor = null;
            }
        }

        private string m_propLayoutName;

        [XmlIgnore]
        private BoardDescriptorGeneralXml m_descriptor;

        [XmlIgnore]
        public BoardDescriptorGeneralXml Descriptor
        {
            get {
                if (m_descriptor == null && m_propLayoutName != null)
                {
                    m_descriptor = WTSPropLayoutData.Instance.Get(m_propLayoutName);
                    if (m_descriptor == null || m_descriptor.m_allowedRenderClass != Rendering.TextRenderingClass.RoadNodes)
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

        [XmlArray("AllowedRoadLevels")] [XmlArrayItem("Level")] public HashSet<ItemClass.Level> AllowedLevels { get; set; } = new HashSet<Level>();
        [XmlAttribute("spawnChance")] public byte SpawnChance { get; set; } = 255;
        [XmlAttribute("placeOnDistrictBorder")] public bool PlaceOnDistrictBorder { get; set; } = false;
        [XmlAttribute("placeOnTunnelBridgeStart")] public bool PlaceOnTunnelBridgeStart { get; set; } = false;
        [XmlAttribute("ignoreEmptyNameRoads")] public bool IgnoreEmptyNameRoads { get; set; } = true;
        [XmlAttribute("minRoadHalfWidth")] public float MinRoadHalfWidth { get; set; } = 0;
        [XmlAttribute("maxRoadHalfWidth")] public float MaxRoadHalfWidth { get; set; } = 999;


        [XmlAttribute("useDistrictColor")] public bool UseDistrictColor = false;

        [XmlArray("SelectedDistricts")] [XmlArrayItem("District")] public HashSet<ushort> SelectedDistricts { get; set; } = new HashSet<ushort>();
        [XmlAttribute("districtSelectionIsBlacklist")] public bool SelectedDistrictsIsBlacklist { get; set; } = true;
        [XmlAttribute("districtRestrictionOrder")] public DistrictRestrictionOrder DistrictRestrictionOrder { get; set; }



        public bool AllowsClass(ItemClass source) => AllowedLevels.Contains(source.m_level);
        public bool AllowsDistrict(byte districtId) => SelectedDistricts.Contains(districtId) != SelectedDistrictsIsBlacklist;
        public bool AllowsPark(byte districtId) => SelectedDistricts.Contains((ushort)(0x100 | districtId)) != SelectedDistrictsIsBlacklist;

        public bool Allows(byte park, byte district)
        {
            if (district == 0 && park > 0)
            {
                return AllowsPark(park);
            }
            if (park == 0)
            {
                return AllowsDistrict(district);
            }
            switch (DistrictRestrictionOrder)
            {
                case DistrictRestrictionOrder.ParksOrDistricts:
                default:
                    return AllowsDistrict(district) || AllowsPark(park);
                case DistrictRestrictionOrder.ParksAndDistricts:
                    return AllowsDistrict(district) && AllowsPark(park);
            }
        }

        public void ResetCacheDescriptor() => m_descriptor = null;
    }
    public enum TrafficDirectionRequired
    {
        NONE,
        INCOMING,
        OUTCOMING,
    }

    public enum DistrictRestrictionOrder
    {
        ParksOrDistricts,
        ParksAndDistricts
    }

    public enum ExitSideRequired
    {
        NONE,
        OUTSIDE,
        INSIDE
    }
    public enum RoadSide
    {
        LEFT,
        RIGHT,
        CENTER

    }
}
