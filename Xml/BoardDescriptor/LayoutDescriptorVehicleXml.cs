using Klyte.Commons.Interfaces;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{
    [XmlRoot("vehicleDescriptor")]
    public class LayoutDescriptorVehicleXml : BoardInstanceXml, ILibable
    {
        [XmlIgnore]
        public string SaveName { get; set; }

        [XmlAttribute("vehicleAssetName")]
        public string VehicleAssetName { get => SaveName; set => SaveName = value; }

        [XmlElement("textDescriptor")]
        public BoardTextDescriptorGeneralXml[] TextDescriptors { get; set; } = new BoardTextDescriptorGeneralXml[0];
        [XmlElement("blackSubmesh")]
        public int[] BlackSubmeshes { get; set; } = new int[0];
        [XmlAttribute("defaultFont")]
        public string FontName { get; set; }


        [XmlIgnore]
        internal bool SubmeshesUpdated { get; set; }
    }

}
