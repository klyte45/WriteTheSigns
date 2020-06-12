using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Xml;
using System.Xml.Serialization;
using static Klyte.WriteTheSigns.Xml.BoardGeneratorBuildings;

namespace Klyte.WriteTheSigns.Data
{

    [XmlRoot("WTSBuildingsData")]
    public class WTSBuildingsData : WTSBaseData<WTSBuildingsData, BoardBunchContainerBuilding[]>
    {
        public override int ObjArraySize => BuildingManager.MAX_BUILDING_COUNT;

        public override string SaveId => "K45_WTS_WTSBuildingsData";

        public override int BoardCount => 1;

        public override int SubBoardCount => 1;

        [XmlElement]
        public SimpleXmlDictionary<string, BuildingGroupDescriptorXml> CityDescriptors = new SimpleXmlDictionary<string, BuildingGroupDescriptorXml>();
        [XmlIgnore]
        public SimpleXmlDictionary<string, ExportableBuildingGroupDescriptorXml> GlobalDescriptors = new SimpleXmlDictionary<string, ExportableBuildingGroupDescriptorXml>();
        [XmlIgnore]
        public SimpleXmlDictionary<string, ExportableBuildingGroupDescriptorXml> AssetsDescriptors = new SimpleXmlDictionary<string, ExportableBuildingGroupDescriptorXml>();

        public void CleanCache() => ResetBoards();
    }

}
