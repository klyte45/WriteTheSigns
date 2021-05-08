using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using System.Linq;
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
        [XmlAttribute("defaultFont")]
        public string FontName { get; set; }

        private VehicleInfo m_cachedInfo;

        [XmlIgnore]
        internal VehicleInfo CachedInfo
        {
            get
            {
                if (m_cachedInfo is null && VehicleAssetName != null && (m_cachedInfo = VehiclesIndexes.instance.PrefabsLoaded.Values.Where(x => x.name == VehicleAssetName).FirstOrDefault()) is null)
                {
                    VehicleAssetName = null;
                }
                return m_cachedInfo;
            }
        }

        public bool IsValid() => !(TextDescriptors is null || CachedInfo is null || VehicleAssetName is null);

    }

}
