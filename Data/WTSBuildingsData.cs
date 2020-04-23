using System.Xml.Serialization;
using static Klyte.WriteTheSigns.Xml.BoardGeneratorBuildings;

namespace Klyte.WriteTheSigns.Data
{

    [XmlRoot("WTSBuildingsData")]
    public class WTSBuildingsData : WTSBaseData<WTSBuildingsData, BoardBunchContainerBuilding>
    {
        public override int ObjArraySize => BuildingManager.MAX_BUILDING_COUNT;

        public override string SaveId => "K45_WTS_WTSBuildingsData";

        public override int BoardCount => 1;

        public override int SubBoardCount => 1;
    }

}
