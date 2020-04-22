using Klyte.Commons.Interfaces;
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
            get => new ListWrapper<DESC>() { listVal = m_savedDescriptors.Values.ToList() };
            set {
                if (value != null)
                {
                    m_savedDescriptors = value.listVal.GroupBy(x => x.SaveName).Select(y => y.First()).ToDictionary(x => x.SaveName, x => x);
                }
            }
        }

        [XmlIgnore]
        private Dictionary<string, DESC> m_savedDescriptors = new Dictionary<string, DESC>();

        public void Add(string indexName, DESC descriptor)
        {
            descriptor.SaveName = indexName;
            m_savedDescriptors[indexName] = descriptor;
            Save();
        }
        public DESC Get(string indexName)
        {
            m_savedDescriptors.TryGetValue(indexName, out DESC descriptor);
            return descriptor;
        }

        public IEnumerable<string> List() => m_savedDescriptors.Keys;

        public void Remove(string indexName)
        {
            if (indexName != null)
            {
                bool removed = m_savedDescriptors.Remove(indexName);
                if (removed)
                {
                    Save();
                }
            }
        }
        protected abstract void Save();

        public void Replace(string key, DESC newFile)
        {
            Remove(key);
            Add(key, newFile);
        }

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