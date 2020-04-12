using ColossalFramework;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{

    public abstract class BoardTextDescriptorParentXml<T> where T : BoardTextDescriptorParentXml<T>
    {
        [XmlIgnore]
        public Vector3 m_textRelativePosition;
        [XmlIgnore]
        public Vector3 m_textRelativeRotation = new Vector3(0, 180, 0);
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
        [XmlAttribute("textType")]
        public TextType m_textType = TextType.OwnName;
        [XmlAttribute("fixedText")]
        public string m_fixedText = null;
        [XmlAttribute("fixedTextLocaleCategory")]
        public string m_fixedTextLocaleKey = null;
        [XmlAttribute("fixedTextLocalized")]
        public bool m_isFixedTextLocalized = false;
        [XmlAttribute("nightEmissiveMultiplier")]
        public float m_nightEmissiveMultiplier = 0f;
        [XmlAttribute("dayEmissiveMultiplier")]
        public float m_dayEmissiveMultiplier = 0f;
        [XmlAttribute("textAlign")]
        public UIHorizontalAlignment m_textAlign = UIHorizontalAlignment.Center;
        [XmlAttribute("verticalAlign")]
        public UIVerticalAlignment m_verticalAlign = UIVerticalAlignment.Middle;
        [XmlAttribute("shader")]
        public string m_shader = null;



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
        public string ForceColor { get => m_defaultColor == Color.clear ? null : ColorExtensions.ToRGB(m_defaultColor); set => m_defaultColor = value.IsNullOrWhiteSpace() ? Color.clear : (Color) ColorExtensions.FromRGB(value); }

        [XmlIgnore]
        private BasicRenderInformation m_generatedFixedTextRenderInfo;
        [XmlIgnore]
        public BasicRenderInformation GeneratedFixedTextRenderInfo
        {
            get => m_generatedFixedTextRenderInfo;
            set {
                m_generatedFixedTextRenderInfo = value;
                GeneratedFixedTextRenderInfoTick = SimulationManager.instance.m_currentTickIndex;
            }
        }
        [XmlIgnore]
        public uint GeneratedFixedTextRenderInfoTick { get; private set; }

        public string Serialize()
        {
            var xmlser = new XmlSerializer(typeof(T));
            var settings = new XmlWriterSettings { Indent = false };
            using var textWriter = new StringWriter();
            using var xw = XmlWriter.Create(textWriter, settings);
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            xmlser.Serialize(xw, this, ns);
            return textWriter.ToString();
        }

        public static T Deserialize(string s)
        {
            var xmlser = new XmlSerializer(typeof(T));
            try
            {
                using TextReader tr = new StringReader(s);
                using var reader = XmlReader.Create(tr);
                if (xmlser.CanDeserialize(reader))
                {
                    return (T) xmlser.Deserialize(reader);
                }
                else
                {
                    LogUtils.DoErrorLog($"CAN'T DESERIALIZE BOARD DESCRIPTOR!\nText : {s}");
                }
            }
            catch (Exception e)
            {
                LogUtils.DoErrorLog($"CAN'T DESERIALIZE BOARD DESCRIPTOR!\nText : {s}\n{e.Message}\n{e.StackTrace}");
            }
            return null;
        }
    }


}
