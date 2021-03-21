using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{
    [XmlRoot("vehicleDescriptors")]
    public class ExportableLayoutDescriptorVehicleXml
    {
        [XmlElement("vehicleDescriptor")]
        public LayoutDescriptorVehicleXml[] Descriptors { get; set; }
    }

}
