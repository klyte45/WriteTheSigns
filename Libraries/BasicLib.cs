using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheCity.Xml;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using static Klyte.Commons.Utils.XmlUtils;

namespace Klyte.WriteTheCity.Libraries
{
    public abstract class BasicLib<LIB, DESC>
        where LIB : BasicLib<LIB, DESC>, new()
        where DESC : ILibable
    {
        protected abstract string XmlName { get; }
        private static string DefaultXmlFileBasePath => WTCController.FOLDER_NAME + Path.DirectorySeparatorChar;
        private string DefaultXmlFileBaseFullPath => $"{DefaultXmlFileBasePath}{XmlName}.xml";

        private static LIB m_instance;
        public static LIB Instance
        {
            get {
                if (m_instance == null)
                {
                    m_instance = new LIB();
                    if (File.Exists(m_instance.DefaultXmlFileBaseFullPath))
                    {
                        m_instance = XmlUtils.DefaultXmlDeserialize<LIB>(File.ReadAllText(m_instance.DefaultXmlFileBaseFullPath));
                    }
                }
                return m_instance;
            }
        }

        public static void Reload() => m_instance = null;

        [XmlElement("descriptorsData")]
        public ListWrapper<DESC> SavedDescriptorsSerialized
        {
            get => new ListWrapper<DESC>() { listVal = m_savedDescriptors.Values.ToList() };
            set {
                if (value != null)
                {
                    m_savedDescriptors = value.listVal.ToDictionary(x => x.SaveName, x => x);
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
            bool removed = m_savedDescriptors.Remove(indexName);
            if (removed)
            {
                Save();
            }
        }

        private void Save() => File.WriteAllText(DefaultXmlFileBaseFullPath, XmlUtils.DefaultXmlSerialize<LIB>((LIB)this));

    }

    [XmlRoot("LibPropSettings")] public class WTCLibPropSettings : BasicLib<WTCLibPropSettings, BoardDescriptorGeneralXml> { protected override string XmlName => "LibPropSettings"; }
    [XmlRoot("LibPropTextItem")] public class WTCLibPropTextItem : BasicLib<WTCLibPropTextItem, BoardDescriptorGeneralXml> { protected override string XmlName => "LibPropTextItem"; }

    //#region Mileage Marker
    //[XmlRoot("LibMileageMarkerProp")] public class WTCLibMileageMarkerGroup : BasicLib<WTCLibMileageMarkerGroup, BoardDescriptorMileageMarkerXml> { protected override string XmlName => "LibMileageMarkerProp"; }
    //[XmlRoot("LibMileageMarkerText")] public class WTCLibTextMeshMileageMarker : BasicLib<WTCLibTextMeshMileageMarker, BoardTextDescriptorMileageMarkerXml> { protected override string XmlName => "LibMileageMarkerText"; }
    //#endregion

    //#region Corner Signs
    //[XmlRoot("LibCornerSignProp")] public class WTCLibStreetPropGroup : BasicLib<WTCLibStreetPropGroup, BoardDescriptorStreetSignXml> { protected override string XmlName => "LibCornerSignProp"; }
    //[XmlRoot("LibCornerSignText")] public class WTCLibTextMeshStreetPlate : BasicLib<WTCLibTextMeshStreetPlate, BoardTextDescriptorSteetSignXml> { protected override string XmlName => "LibCornerSignText"; }
    //#endregion

    //#region In Segment props
    //[XmlRoot("LibSegmentPropGroup")] public class WTCLibPropGroupHigwaySigns : BasicLib<WTCLibPropGroupHigwaySigns, BoardBunchContainerHighwaySignXml> { protected override string XmlName => "LibSegmentPropGroup"; }
    //[XmlRoot("LibSegmentProp")] public class WTCLibPropSingleHighwaySigns : BasicLib<WTCLibPropSingleHighwaySigns, BoardDescriptorHigwaySignXml> { protected override string XmlName => "LibSegmentProp"; }
    //[XmlRoot("LibSegmentText")] public class WTCLibTextMeshHighwaySigns : BasicLib<WTCLibTextMeshHighwaySigns, BoardTextDescriptorHighwaySignsXml> { protected override string XmlName => "LibSegmentText"; }
    //#endregion

    //#region Building extra props
    //[XmlRoot("LibBuildingsPropGroup")] public class WTCLibPropGroupBuildingSigns : BasicLib<WTCLibPropGroupBuildingSigns, BuildingGroupDescriptorXml> { protected override string XmlName => "LibBuildingsPropGroup"; }
    //[XmlRoot("LibBuildingsProp")] public class WTCLibPropSingleBuildingSigns : BasicLib<WTCLibPropSingleBuildingSigns, BoardDescriptorBuildingXml> { protected override string XmlName => "LibBuildingsProp"; }
    //[XmlRoot("LibBuildingsText")] public class WTCLibTextMeshBuildingSigns : BasicLib<WTCLibTextMeshBuildingSigns, BoardTextDescriptorBuildingsXml> { protected override string XmlName => "LibBuildingsText"; }
    //#endregion
}