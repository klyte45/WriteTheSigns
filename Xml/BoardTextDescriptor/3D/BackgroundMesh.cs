using ColossalFramework;
using Klyte.Commons.Utils;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.WriteTheSigns.Xml
{
    public partial class BoardTextDescriptorGeneralXml
    {
        public class BackgroundMesh
        {
            [XmlElement("size")]
            public Vector2Xml Size
            {
                get => m_size; set
                {
                    FrameMeshSettings.cachedFrameArray = null;
                    m_size = value;
                }
            }

            [XmlIgnore]
            private Vector2Xml m_size = new Vector2Xml();
            [XmlIgnore]
            public Color BackgroundColor { get => m_cachedColor; set => m_cachedColor = value; }
            [XmlIgnore]
            private Color m_cachedColor;
            [XmlAttribute("color")]
            public string BgColorStr { get => m_cachedColor == null ? null : ColorExtensions.ToRGB(BackgroundColor); set => BackgroundColor = value.IsNullOrWhiteSpace() ? default : ColorExtensions.FromRGB(value); }

            [XmlAttribute("useFrame")]
            public bool UseFrame { get; set; } = false;
            [XmlElement("frane")]
            [XmlElement("frame")]
            public FrameMesh FrameMeshSettings { get; set; } = new FrameMesh();
        }
    }

}

