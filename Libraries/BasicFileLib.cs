using Klyte.Commons.Interfaces;
using Klyte.Commons.Libraries;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Libraries
{


    [XmlRoot("LibPropSettings")] public class WTSLibPropSettings : LibBaseFile<WTSLibPropSettings, BoardDescriptorGeneralXml> { protected override string XmlName => "LibPropSettings"; }
    [XmlRoot("LibPropTextItem")] public class WTSLibPropTextItem : LibBaseFile<WTSLibPropTextItem, BoardTextDescriptorGeneralXml> { protected override string XmlName => "LibPropTextItem"; }
    [XmlRoot("LibRoadCornerRule")] public class WTSLibRoadCornerRule : LibBaseFile<WTSLibRoadCornerRule, BoardInstanceRoadNodeXml> { protected override string XmlName => "LibRoadCornerRule"; }
    [XmlRoot("LibRoadCornerRuleList")] public class WTSLibRoadCornerRuleList : LibBaseFile<WTSLibRoadCornerRuleList, ILibableAsContainer<BoardInstanceRoadNodeXml>> { protected override string XmlName => "LibRoadCornerRuleList"; }
    [XmlRoot("LibBuildingPropLayoutList")] public class WTSLibBuildingPropLayoutList : LibBaseFile<WTSLibBuildingPropLayoutList, ExportableBoardInstanceBuildingListXml> { protected override string XmlName => "LibBuildingPropLayoutList"; }
    [XmlRoot("LibBuildingPropLayout")] public class WTSLibBuildingPropLayout : LibBaseFile<WTSLibBuildingPropLayout, BoardInstanceBuildingXml> { protected override string XmlName => "LibBuildingPropLayout"; }

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