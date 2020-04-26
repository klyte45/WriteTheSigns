using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using static Klyte.Commons.Utils.XmlUtils;

namespace Klyte.WriteTheSigns.Libraries
{
    public abstract class BasicLib<LIB, DESC>
        where LIB : BasicLib<LIB, DESC>, new()
        where DESC : ILibable
    {

        [XmlElement("descriptorsData")]
        public ListWrapper<DESC> SavedDescriptorsSerialized
        {
            get => new ListWrapper<DESC>() { listVal = m_savedDescriptorsSerialized.ToList() };
            set {
                m_savedDescriptorsSerialized = value.listVal.ToArray();
                UpdateIndex();
            }
        }

        private void UpdateIndex() => m_indexes = m_savedDescriptorsSerialized.Select((x, y) => Tuple.New(x.SaveName, y)).ToDictionary(x => x.First, (x) => x.Second);

        [XmlIgnore]
        private Dictionary<string, int> m_indexes = new Dictionary<string, int>();

        private DESC[] m_savedDescriptorsSerialized;

        public void Add(string indexName, ref DESC descriptor)
        {
            descriptor.SaveName = indexName;
            if (!m_indexes.TryGetValue(indexName, out int idxArray))
            {
                m_savedDescriptorsSerialized = (m_savedDescriptorsSerialized ?? new DESC[0]).Union(new DESC[] { descriptor }).ToArray();
            }
            else
            {
                m_savedDescriptorsSerialized[idxArray] = descriptor;

            }
            UpdateIndex();
            Save();
        }

        private DESC m_nullDesc = default;

        public ref DESC Get(string indexName)
        {
            if (m_indexes.TryGetValue(indexName, out int idxArray))
            {
                return ref m_savedDescriptorsSerialized[idxArray];
            }
            else
            {
                m_nullDesc = default;
                return ref m_nullDesc;
            }
        }

        public IEnumerable<string> List() => m_indexes.Keys;
        public IEnumerable<string> ListWhere(Func<DESC, bool> filter) => m_savedDescriptorsSerialized.Where(x => filter(x)).Select(x => x.SaveName);

        public void Remove(string indexName)
        {
            if (indexName != null)
            {
                if (m_indexes.TryGetValue(indexName, out int idxArray))
                {
                    m_savedDescriptorsSerialized = m_savedDescriptorsSerialized.Where(x => x.SaveName != indexName).ToArray();
                    UpdateIndex();
                    Save();
                }
            }
        }
        protected abstract void Save();
    }

    //#region Mileage Marker
    //[XmlRoot("LibMileageMarkerProp")] public class WTSLibMileageMarkerGroup : BasicLib<WTSLibMileageMarkerGroup, BoardDescriptorMileageMarkerXml> { protected override string XmlName => "LibMileageMarkerProp"; }
    //[XmlRoot("LibMileageMarkerText")] public class WTSLibTextMeshMileageMarker : BasicLib<WTSLibTextMeshMileageMarker, BoardTextDescriptorMileageMarkerXml> { protected override string XmlName => "LibMileageMarkerText"; }
    //#endregion

    //#region Corner Signs
    //[XmlRoot("LibCornerSignProp")] public class WTSLibStreetPropGroup : BasicLib<WTSLibStreetPropGroup, BoardDescriptorStreetSignXml> { protected override string XmlName => "LibCornerSignProp"; }
    //[XmlRoot("LibCornerSignText")] public class WTSLibTextMeshStreetPlate : BasicLib<WTSLibTextMeshStreetPlate, BoardTextDescriptorSteetSignXml> { protected override string XmlName => "LibCornerSignText"; }
    //#endregion

    //#region In Segment props
    //[XmlRoot("LibSegmentPropGroup")] public class WTSLibPropGroupHigwaySigns : BasicLib<WTSLibPropGroupHigwaySigns, BoardBunchContainerHighwaySignXml> { protected override string XmlName => "LibSegmentPropGroup"; }
    //[XmlRoot("LibSegmentProp")] public class WTSLibPropSingleHighwaySigns : BasicLib<WTSLibPropSingleHighwaySigns, BoardDescriptorHigwaySignXml> { protected override string XmlName => "LibSegmentProp"; }
    //[XmlRoot("LibSegmentText")] public class WTSLibTextMeshHighwaySigns : BasicLib<WTSLibTextMeshHighwaySigns, BoardTextDescriptorHighwaySignsXml> { protected override string XmlName => "LibSegmentText"; }
    //#endregion

    //#region Building extra props
    //[XmlRoot("LibBuildingsPropGroup")] public class WTSLibPropGroupBuildingSigns : BasicLib<WTSLibPropGroupBuildingSigns, BuildingGroupDescriptorXml> { protected override string XmlName => "LibBuildingsPropGroup"; }
    //[XmlRoot("LibBuildingsProp")] public class WTSLibPropSingleBuildingSigns : BasicLib<WTSLibPropSingleBuildingSigns, BoardDescriptorBuildingXml> { protected override string XmlName => "LibBuildingsProp"; }
    //[XmlRoot("LibBuildingsText")] public class WTSLibTextMeshBuildingSigns : BasicLib<WTSLibTextMeshBuildingSigns, BoardTextDescriptorBuildingsXml> { protected override string XmlName => "LibBuildingsText"; }
    //#endregion
}