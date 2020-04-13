using Klyte.DynamicTextProps.Overrides;
using System.Xml.Serialization;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorBuildings;

namespace Klyte.DynamicTextProps.Data
{

    [XmlRoot("DTPBuildingsData")]
    public class DTPBuildingsData : DTPBaseData<DTPBuildingsData, BoardBunchContainerBuilding>
    {
        public override int ObjArraySize => BuildingManager.MAX_BUILDING_COUNT;
        
        public override string SaveId => "K45_DTP3_DTPBuildingsData";
    }

}
