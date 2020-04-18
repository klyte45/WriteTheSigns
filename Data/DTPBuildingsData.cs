using System.Xml.Serialization;
using static Klyte.DynamicTextProps.Xml.BoardGeneratorBuildings;

namespace Klyte.DynamicTextProps.Data
{

    [XmlRoot("DTPBuildingsData")]
    public class DTPBuildingsData : DTPBaseData<DTPBuildingsData, BoardBunchContainerBuilding>
    {
        public override int ObjArraySize => BuildingManager.MAX_BUILDING_COUNT;

        public override string SaveId => "K45_DTP3_DTPBuildingsData";
    }

}
