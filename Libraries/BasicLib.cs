using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.DynamicTextBoards.Overrides;
using Klyte.Commons.Utils;
using Klyte.DynamicTextBoards.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using ICities;
using System.IO;
using ColossalFramework.IO;
using Klyte.DynamicTextBoards.Overrides;
using static Klyte.Commons.Utils.XmlUtils;

namespace Klyte.DynamicTextBoards.Libraries
{
    public abstract class BasicLib<LIB, DESC>
        where LIB : BasicLib<LIB, DESC>, new()
        where DESC : ILibable
    {
        protected abstract string XmlName { get; }
        private static string defaultXmlFileBasePath => DynamicTextBoardsMod.FOLDER_NAME + Path.DirectorySeparatorChar;
        private string defaultXmlFileBaseFullPath => $"{defaultXmlFileBasePath}{XmlName}.xml";

        private static LIB m_instance;
        public static LIB Instance
        {
            get {
                if (m_instance == null)
                {
                    m_instance = new LIB();
                    if (File.Exists(m_instance.defaultXmlFileBaseFullPath))
                    {
                        m_instance = XmlUtils.DefaultXmlDeserialize<LIB>(File.ReadAllText(m_instance.defaultXmlFileBaseFullPath));
                    }
                }
                return m_instance;
            }
        }

        public static void Reload()
        {
            m_instance = null;
        }

        [XmlElement("descriptorsData")]
        public ListWrapper<DESC> SavedDescriptorsSerialized
        {
            get {
                return new ListWrapper<DESC>() { listVal = m_SavedDescriptors.Values.ToList() };
            }
            set {
                if (value != null) m_SavedDescriptors = value.listVal.ToDictionary(x => x.SaveName, x => x);
            }
        }

        [XmlIgnore]
        private Dictionary<string, DESC> m_SavedDescriptors = new Dictionary<string, DESC>();

        public void Add(string indexName, DESC descriptor)
        {
            descriptor.SaveName = indexName;
            m_SavedDescriptors[indexName] = descriptor;
            Save();
        }
        public DESC Get(string indexName)
        {
            m_SavedDescriptors.TryGetValue(indexName, out DESC descriptor);
            return descriptor;
        }

        public IEnumerable<string> List()
        {
            return m_SavedDescriptors.Keys;
        }

        public void Remove(string indexName)
        {
            var removed = m_SavedDescriptors.Remove(indexName);
            if (removed) Save();
        }

        private void Save()
        {
            File.WriteAllText(defaultXmlFileBaseFullPath, XmlUtils.DefaultXmlSerialize<LIB>((LIB)this));
        }

    }

    public interface ILibable
    {
        string SaveName { get; set; }
    }


    [XmlRoot("LibTextMesh")] public class DTBLibTextMesh : BasicLib<DTBLibTextMesh, BoardGeneratorHighwaySigns.BoardTextDescriptorHigwaySign> { protected override string XmlName => "LibTextMesh"; }
    [XmlRoot("LibPropSingle")] public class DTBLibPropSingle : BasicLib<DTBLibPropSingle, BoardGeneratorHighwaySigns.BoardDescriptorHigwaySign> { protected override string XmlName => "LibPropSingle"; }
    [XmlRoot("LibPropGroup")] public class DTBLibPropGroup : BasicLib<DTBLibPropGroup, BoardGeneratorHighwaySigns.BoardBunchContainerHighwaySign> { protected override string XmlName => "LibPropGroup"; }

}