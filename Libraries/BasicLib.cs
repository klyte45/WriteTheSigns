using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Overrides;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using static Klyte.Commons.Utils.XmlUtils;

namespace Klyte.DynamicTextProps.Libraries
{
    public abstract class BasicLib<LIB, DESC>
        where LIB : BasicLib<LIB, DESC>, new()
        where DESC : ILibable
    {
        protected abstract string XmlName { get; }
        private static string DefaultXmlFileBasePath => DTPController.FOLDER_NAME + Path.DirectorySeparatorChar;
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

    #region Mileage Marker
    [XmlRoot("LibMileageMarkerProp")] public class DTPLibMileageMarkerGroup : BasicLib<DTPLibMileageMarkerGroup, BoardDescriptorMileageMarkerXml> { protected override string XmlName => "LibMileageMarkerProp"; }
    [XmlRoot("LibMileageMarkerText")] public class DTPLibTextMeshMileageMarker : BasicLib<DTPLibTextMeshMileageMarker, BoardTextDescriptorMileageMarkerXml> { protected override string XmlName => "LibMileageMarkerText"; }
    #endregion

    #region Corner Signs
    [XmlRoot("LibCornerSignProp")] public class DTPLibStreetPropGroup : BasicLib<DTPLibStreetPropGroup, BoardDescriptorStreetSignXml> { protected override string XmlName => "LibCornerSignProp"; }
    [XmlRoot("LibCornerSignText")] public class DTPLibTextMeshStreetPlate : BasicLib<DTPLibTextMeshStreetPlate, BoardTextDescriptorSteetSignXml> { protected override string XmlName => "LibCornerSignText"; }
    #endregion

    #region In Segment props
    [XmlRoot("LibSegmentPropGroup")] public class DTPLibPropGroupHigwaySigns : BasicLib<DTPLibPropGroupHigwaySigns, BoardBunchContainerHighwaySignXml> { protected override string XmlName => "LibSegmentPropGroup"; }
    [XmlRoot("LibSegmentProp")] public class DTPLibPropSingleHighwaySigns : BasicLib<DTPLibPropSingleHighwaySigns, BoardDescriptorHigwaySignXml> { protected override string XmlName => "LibSegmentProp"; }
    [XmlRoot("LibSegmentText")] public class DTPLibTextMeshHighwaySigns : BasicLib<DTPLibTextMeshHighwaySigns, BoardTextDescriptorHighwaySignsXml> { protected override string XmlName => "LibSegmentText"; }
    #endregion

    #region Building extra props
    [XmlRoot("LibBuildingsPropGroup")] public class DTPLibPropGroupBuildingSigns : BasicLib<DTPLibPropGroupBuildingSigns, BuildingGroupDescriptorXml> { protected override string XmlName => "LibBuildingsPropGroup"; }
    [XmlRoot("LibBuildingsProp")] public class DTPLibPropSingleBuildingSigns : BasicLib<DTPLibPropSingleBuildingSigns, BoardDescriptorBuildingXml> { protected override string XmlName => "LibBuildingsProp"; }
    [XmlRoot("LibBuildingsText")] public class DTPLibTextMeshBuildingSigns : BasicLib<DTPLibTextMeshBuildingSigns, BoardTextDescriptorBuildingsXml> { protected override string XmlName => "LibBuildingsText"; }
    #endregion
}