using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheCity.Xml;
using System.IO;
using System.Xml.Serialization;

namespace Klyte.WriteTheCity.Libraries
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
        private static string DefaultXmlFileBasePath => WTCController.FOLDER_NAME + Path.DirectorySeparatorChar;
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

    [XmlRoot("LibPropSettings")] public class WTCLibPropSettings : BasicFileLib<WTCLibPropSettings, BoardDescriptorGeneralXml> { protected override string XmlName => "LibPropSettings"; }
    [XmlRoot("LibPropTextItem")] public class WTCLibPropTextItem : BasicFileLib<WTCLibPropTextItem, BoardTextDescriptorGeneralXml> { protected override string XmlName => "LibPropTextItem"; }

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