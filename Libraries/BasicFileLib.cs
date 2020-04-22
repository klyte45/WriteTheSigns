using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Xml;
using System.IO;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Libraries
{
    public abstract class BasicFileLib<LIB, DESC> : BasicLib<LIB, DESC>
    where LIB : BasicFileLib<LIB, DESC>, new()
    where DESC : ILibable
    {
        private static LIB m_instance;
        public static LIB Instance
        {
            get {
                if (m_instance == null)
                {
                    m_instance = LoadInstance();
                }
                return m_instance;
            }
        }
        protected abstract string XmlName { get; }

        public static void Reload() => m_instance = null;
        private static string DefaultXmlFileBasePath => WTSController.FOLDER_NAME + Path.DirectorySeparatorChar;
        private string DefaultXmlFileBaseFullPath => $"{DefaultXmlFileBasePath}{XmlName}.xml";
        protected sealed override void Save() => File.WriteAllText(DefaultXmlFileBaseFullPath, XmlUtils.DefaultXmlSerialize<LIB>((LIB)this));
        protected static LIB LoadInstance()
        {
            var newVal = new LIB();
            if (File.Exists(newVal.DefaultXmlFileBaseFullPath))
            {
                return XmlUtils.DefaultXmlDeserialize<LIB>(File.ReadAllText(newVal.DefaultXmlFileBaseFullPath));
            }
            return newVal;
        }
    }

    [XmlRoot("LibPropSettings")] public class WTSLibPropSettings : BasicFileLib<WTSLibPropSettings, BoardDescriptorGeneralXml> { protected override string XmlName => "LibPropSettings"; }
    [XmlRoot("LibPropTextItem")] public class WTSLibPropTextItem : BasicFileLib<WTSLibPropTextItem, BoardTextDescriptorGeneralXml> { protected override string XmlName => "LibPropTextItem"; }
    [XmlRoot("LibRoadCornerRule")] public class WTSLibRoadCornerRule : BasicFileLib<WTSLibRoadCornerRule, BoardInstanceRoadNodeXml> { protected override string XmlName => "LibRoadCornerRule"; }

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