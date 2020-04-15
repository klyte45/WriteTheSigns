using ColossalFramework;
using Klyte.Commons.Utils;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{

    public class BoardDescriptorGeneralXml
    {
        [XmlAttribute("propName")]
        public string m_propName;
        [XmlIgnore]
        public Vector3 m_propPosition;
        [XmlIgnore]
        public Vector3 PropScale
        {
            get => new Vector3(ScaleX, ScaleY ?? ScaleX, ScaleZ ?? ScaleX);
            set {
                ScaleX = value.x;
                ScaleY = value.y;
                ScaleZ = value.z;
            }
        }
        [XmlIgnore]
        public Vector3 m_propRotation;
        [XmlElement("textDescriptor")]
        public BoardTextDescriptorGeneralXml[] m_textDescriptors = new BoardTextDescriptorGeneralXml[0];


        [XmlAttribute("positionX")]
        public float PropPositionX { get => m_propPosition.x; set => m_propPosition.x = value; }
        [XmlAttribute("positionY")]
        public float PropPositionY { get => m_propPosition.y; set => m_propPosition.y = value; }
        [XmlAttribute("positionZ")]
        public float PropPositionZ { get => m_propPosition.z; set => m_propPosition.z = value; }


        [XmlAttribute("rotationX")]
        public float PropRotationX { get => m_propRotation.x; set => m_propRotation.x = value; }
        [XmlAttribute("rotationY")]
        public float PropRotationY { get => m_propRotation.y; set => m_propRotation.y = value; }
        [XmlAttribute("rotationZ")]
        public float PropRotationZ { get => m_propRotation.z; set => m_propRotation.z = value; }

        [XmlAttribute("scaleX")]
        public float ScaleX = 1;
        [XmlAttribute("scaleY")]
        public float? ScaleY;
        [XmlAttribute("scaleZ")]
        public float? ScaleZ;


        [XmlIgnore]
        public Color FixedColor { get => m_cachedColor; set => m_cachedColor = value; }
        [XmlIgnore]
        private Color m_cachedColor;
        [XmlAttribute("fixedColor")]
        public string FixedColorStr { get => m_cachedColor == default ? null : ColorExtensions.ToRGB(FixedColor); set => FixedColor = value.IsNullOrWhiteSpace() ? default : (Color)ColorExtensions.FromRGB(value); }


        [XmlAttribute("fontName")]
        public string FontName { get; set; }


        public Matrix4x4 TextMatrixTranslation(int idx) => Matrix4x4.Translate(m_textDescriptors[idx].m_textRelativePosition);
        public Matrix4x4 TextMatrixRotation(int idx) => Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(m_textDescriptors[idx].m_textRelativeRotation), Vector3.one);

    }


}
