using ColossalFramework;
using ColossalFramework.UI;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheCity.Rendering;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.WriteTheCity.Xml
{
    [XmlRoot("textDescriptor")]
    public class BoardTextDescriptorGeneralXml : ILibable
    {
        [XmlIgnore]
        public Vector3 m_textRelativePosition;
        [XmlIgnore]
        public Vector3 m_textRelativeRotation;

        [XmlAttribute("textScale")]
        public float m_textScale = 1f;
        [XmlAttribute("maxWidth")]
        public float m_maxWidthMeters = 0;
        [XmlAttribute("applyOverflowResizingOnY")]
        public bool m_applyOverflowResizingOnY = false;


        [XmlAttribute("useContrastColor")]
        public bool m_useContrastColor = true;
        [XmlIgnore]
        public Color m_defaultColor = Color.clear;
        [XmlAttribute("textAlign")]
        public UIHorizontalAlignment m_textAlign = UIHorizontalAlignment.Center;

        [XmlAttribute("textType")]
        public TextType m_textType = TextType.Fixed;
        [XmlAttribute("fixedText")]
        public string m_fixedText = "Text";


        [XmlAttribute("overrideFont")]
        public string m_overrideFont;

        [XmlAttribute("allCaps")]
        public bool m_allCaps = false;
        [XmlAttribute("prefix")]
        public string m_prefix = "";
        [XmlAttribute("suffix")]
        public string m_suffix = "";
        [XmlAttribute("cloneInvertHorizontalAlign")]
        public bool m_invertYCloneHorizontalAlign;
        [XmlAttribute("clone180DegY")]
        public bool m_create180degYClone;

        [XmlAttribute("relativePositionX")]
        public float RelPositionX { get => m_textRelativePosition.x; set => m_textRelativePosition.x = value; }
        [XmlAttribute("relativePositionY")]
        public float RelPositionY { get => m_textRelativePosition.y; set => m_textRelativePosition.y = value; }
        [XmlAttribute("relativePositionZ")]
        public float RelPositionZ { get => m_textRelativePosition.z; set => m_textRelativePosition.z = value; }

        [XmlAttribute("relativeRotationX")]
        public float RotationX { get => m_textRelativeRotation.x; set => m_textRelativeRotation.x = value; }
        [XmlAttribute("relativeRotationY")]
        public float RotationY { get => m_textRelativeRotation.y; set => m_textRelativeRotation.y = value; }
        [XmlAttribute("relativeRotationZ")]
        public float RotationZ { get => m_textRelativeRotation.z; set => m_textRelativeRotation.z = value; }

        [XmlAttribute("color")]
        public string ForceColor { get => m_defaultColor == Color.clear ? null : ColorExtensions.ToRGB(m_defaultColor); set => m_defaultColor = value.IsNullOrWhiteSpace() ? Color.clear : (Color)ColorExtensions.FromRGB(value); }

        [XmlAttribute("saveName")]
        public string SaveName { get; set; }
    }


}
