using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Overrides;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using static Klyte.Commons.Utils.XmlUtils;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorRoadNodes;

namespace Klyte.DynamicTextProps.Libraries
{
    public abstract class BasicLib<LIB, DESC>
        where LIB : BasicLib<LIB, DESC>, new()
        where DESC : ILibable
    {
        protected abstract string XmlName { get; }
        private static string DefaultXmlFileBasePath => DynamicTextPropsMod.FOLDER_NAME + Path.DirectorySeparatorChar;
        private string DefaultXmlFileBaseFullPath => $"{DefaultXmlFileBasePath}{XmlName}.xml";

        private static LIB m_instance;
        public static LIB Instance
        {
            get {
                if (m_instance == null)
                {
                    m_instance = new LIB();
                    if (File.Exists(m_instance.DefaultXmlFileBaseFullPath))
                    {
                        m_instance = XmlUtils.DefaultXmlDeserialize<LIB>(File.ReadAllText(m_instance.DefaultXmlFileBaseFullPath));
                    }
                }
                return m_instance;
            }
        }

        public static void Reload() => m_instance = null;

        [XmlElement("descriptorsData")]
        public ListWrapper<DESC> SavedDescriptorsSerialized
        {
            get => new ListWrapper<DESC>() { listVal = m_savedDescriptors.Values.ToList() };
            set {
                if (value != null)
                {
                    m_savedDescriptors = value.listVal.ToDictionary(x => x.SaveName, x => x);
                }
            }
        }

        [XmlIgnore]
        private Dictionary<string, DESC> m_savedDescriptors = new Dictionary<string, DESC>();

        public void Add(string indexName, DESC descriptor)
        {
            descriptor.SaveName = indexName;
            m_savedDescriptors[indexName] = descriptor;
            Save();
        }
        public DESC Get(string indexName)
        {
            m_savedDescriptors.TryGetValue(indexName, out DESC descriptor);
            return descriptor;
        }

        public IEnumerable<string> List() => m_savedDescriptors.Keys;

        public void Remove(string indexName)
        {
            bool removed = m_savedDescriptors.Remove(indexName);
            if (removed)
            {
                Save();
            }
        }

        private void Save() => File.WriteAllText(DefaultXmlFileBaseFullPath, XmlUtils.DefaultXmlSerialize<LIB>((LIB) this));

    }

    public interface ILibable
    {
        string SaveName { get; set; }
    }


    [XmlRoot("LibTextMesh")] public class DTPLibTextMeshHighwaySigns : BasicLib<DTPLibTextMeshHighwaySigns, BoardGeneratorHighwaySigns.BoardTextDescriptorHighwaySignsXml> { protected override string XmlName => "LibTextMesh"; }
    [XmlRoot("LibTextStreetPlate")] public class DTPLibTextMeshStreetPlate : BasicLib<DTPLibTextMeshStreetPlate, BoardTextDescriptorSteetSignXml> { protected override string XmlName => "LibTextStreetPlate"; }
    [XmlRoot("LibTextMileageMarker")] public class DTPLibTextMeshMileageMarker : BasicLib<DTPLibTextMeshMileageMarker, BoardTextDescriptorMileageMarkerXml> { protected override string XmlName => "LibTextMileageMarker"; }
    [XmlRoot("LibPropSingle")] public class DTPLibPropSingle : BasicLib<DTPLibPropSingle, BoardGeneratorHighwaySigns.BoardDescriptorHigwaySignXml> { protected override string XmlName => "LibPropSingle"; }
    [XmlRoot("LibPropGroup")] public class DTPLibPropGroup : BasicLib<DTPLibPropGroup, BoardGeneratorHighwaySigns.BoardBunchContainerHighwaySignXml> { protected override string XmlName => "LibPropGroup"; }
    [XmlRoot("LibStreetPropGroup")] public class DTPLibStreetPropGroup : BasicLib<DTPLibStreetPropGroup, BoardDescriptorStreetSignXml> { protected override string XmlName => "LibStreetPropGroup"; }
    [XmlRoot("LibMileageMarkerPropGroup")] public class DTPLibMileageMarkerGroup : BasicLib<DTPLibMileageMarkerGroup, BoardDescriptorMileageMarkerXml> { protected override string XmlName => "LibMileageMarkerPropGroup"; }

}