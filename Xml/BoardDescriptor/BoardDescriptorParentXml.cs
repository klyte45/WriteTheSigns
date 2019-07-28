using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{

    public abstract class BoardDescriptorParentXml<BD, BTD>
        where BD : BoardDescriptorParentXml<BD, BTD>
        where BTD : BoardTextDescriptorParentXml<BTD>
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
        public BTD[] m_textDescriptors = new BTD[0];


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


        public Matrix4x4 TextMatrixTranslation(int idx) => Matrix4x4.Translate(m_textDescriptors[idx].m_textRelativePosition);
        public Matrix4x4 TextMatrixRotation(int idx) => Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(m_textDescriptors[idx].m_textRelativeRotation), Vector3.one);

    }


}
