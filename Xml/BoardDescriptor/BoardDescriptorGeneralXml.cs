using ColossalFramework;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Rendering;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.WriteTheSigns.Xml
{
    public class BoardDescriptorGeneralXml : ILibable, IKeyGetter<string>
    {
        [XmlAttribute("propName")]
        public string m_propName;

        [XmlAttribute("availability")]
        public TextRenderingClass m_allowedRenderClass = TextRenderingClass.RoadNodes;

        [XmlIgnore]
        public Color? FixedColor { get => m_cachedColor; set => m_cachedColor = value; }
        [XmlIgnore]
        private Color? m_cachedColor;
        [XmlAttribute("fixedColor")]
        public string FixedColorStr { get => m_cachedColor == null ? null : ColorExtensions.ToRGB(FixedColor ?? Color.clear); set => FixedColor = value.IsNullOrWhiteSpace() ? null : (Color?)ColorExtensions.FromRGB(value); }

        [XmlAttribute("fontName")]
        public string FontName { get; set; }
        [XmlIgnore]
        public string OriginalSaveName
        {
            get {
                string result = m_originalSaveName;
                m_originalSaveName = null;
                return result;
            }
            private set => m_originalSaveName = value;
        }

        [XmlAttribute("saveName")]
        public string SaveName
        {
            get => m_saveName;
            set {
                if (m_saveName != null && m_originalSaveName == null)
                {
                    m_originalSaveName = m_saveName;
                }
                m_saveName = value;
            }
        }

        [XmlElement("textDescriptor")]
        public BoardTextDescriptorGeneralXml[] TextDescriptors { get; set; } = new BoardTextDescriptorGeneralXml[0];

        [XmlIgnore]
        private string m_saveName;

        private string m_originalSaveName;

        public string GetKeyString() => m_saveName;
    }

}
