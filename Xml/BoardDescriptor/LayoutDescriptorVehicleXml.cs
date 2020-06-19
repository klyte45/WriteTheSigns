using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{
    [XmlRoot("vehicleDescriptor")]
    public class LayoutDescriptorVehicleXml : BoardInstanceXml
    {

        [XmlAttribute("vehicleAssetName")]
        public string VehicleAssetName { get; set; }

        [XmlElement("textDescriptor")]
        public BoardTextDescriptorGeneralXml[] TextDescriptors { get; set; } = new BoardTextDescriptorGeneralXml[0];
    }

}
