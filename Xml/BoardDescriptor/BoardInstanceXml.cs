using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.WriteTheCity.Xml
{
    public class BoardInstanceXml
    {
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

        public BoardDescriptorGeneralXml Descriptor { get; internal set; } = new BoardDescriptorGeneralXml();
    }

}
