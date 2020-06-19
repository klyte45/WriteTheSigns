using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Data
{

    [XmlRoot("WTSVehicleData")]
    public class WTSVehicleData : WTSBaseData<WTSVehicleData, BoardBunchContainerBuilding[]>
    {
        public override int ObjArraySize => VehicleManager.MAX_VEHICLE_COUNT;

        public override string SaveId => "K45_WTS_WTSVehicleData";

        public override int BoardCount => 0;

        public override int SubBoardCount => 0;

        [XmlElement]
        public SimpleXmlDictionary<string, LayoutDescriptorVehicleXml> CityDescriptors = new SimpleXmlDictionary<string, LayoutDescriptorVehicleXml>();
        [XmlIgnore]
        public SimpleXmlDictionary<string, LayoutDescriptorVehicleXml> GlobalDescriptors = new SimpleXmlDictionary<string, LayoutDescriptorVehicleXml>();
        [XmlIgnore]
        public SimpleXmlDictionary<string, LayoutDescriptorVehicleXml> AssetsDescriptors = new SimpleXmlDictionary<string, LayoutDescriptorVehicleXml>();

        public void CleanCache() => ResetBoards();
    }

}
