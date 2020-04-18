using ColossalFramework;
using Klyte.Commons.Utils;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.DynamicTextProps.Xml
{

    public class BoardDescriptorGeneralXml
    {
        [XmlAttribute("propName")]
        public string m_propName;

        [XmlIgnore]
        public Color FixedColor { get => m_cachedColor; set => m_cachedColor = value; }
        [XmlIgnore]
        private Color m_cachedColor;
        [XmlAttribute("fixedColor")]
        public string FixedColorStr { get => m_cachedColor == default ? null : ColorExtensions.ToRGB(FixedColor); set => FixedColor = value.IsNullOrWhiteSpace() ? default : (Color)ColorExtensions.FromRGB(value); }

        [XmlAttribute("fontName")]
        public string FontName { get; set; }

        [XmlElement("textDescriptor")]
        public BoardTextDescriptorGeneralXml[] m_textDescriptors = new BoardTextDescriptorGeneralXml[0];


        public Matrix4x4 TextMatrixTranslation(int idx) => Matrix4x4.Translate(m_textDescriptors[idx].m_textRelativePosition);
        public Matrix4x4 TextMatrixRotation(int idx) => Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(m_textDescriptors[idx].m_textRelativeRotation), Vector3.one);

    }


}
