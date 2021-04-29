using Klyte.Commons.Interfaces;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{
    public class SeedInstanceHighwayShield : IIdentifiable
    {
        [XmlIgnore]
        public ushort SeedId { get; private set; }

        [XmlAttribute("seedId")]
        public long? Id { get => SeedId; set => SeedId = (ushort)(value ?? 0); }
        [XmlAttribute("shieldLayoutName")]
        public string ShieldLayoutName { get; set; }

    }
}
