using System.Xml.Serialization;
using static Klyte.WriteTheCity.Xml.BoardGeneratorBuildings;

namespace Klyte.WriteTheCity.Data
{

    [XmlRoot("WTCBuildingsData")]
    public class WTCBuildingsData : WTCBaseData<WTCBuildingsData, BoardBunchContainerBuilding>
    {
        public override int ObjArraySize => BuildingManager.MAX_BUILDING_COUNT;

        public override string SaveId => "K45_WTC3_WTCBuildingsData";
    }

}
