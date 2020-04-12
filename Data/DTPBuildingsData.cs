using Klyte.DynamicTextProps.Overrides;
using System.Xml.Serialization;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorBuildings;

namespace Klyte.DynamicTextProps.Data
{

    [XmlRoot("DTPBuildingsData")]
    public class DTPBuildingsData : DTPBaseData<DTPBuildingsData, BoardBunchContainerBuilding, CacheControl>
    {
        public override int ObjArraySize => BuildingManager.MAX_BUILDING_COUNT;

        [XmlElement("GlobalConfiguration")]
        public BoardGeneratorBuildingConfigXml GlobalConfiguration { get; set; }

        public override string SaveId => "K45_DTP3_DTPBuildingsData";
    }

}
