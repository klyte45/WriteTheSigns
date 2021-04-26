using ColossalFramework;
using Klyte.Commons.Utils;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.WriteTheSigns.Xml
{
    public class FrameMesh
    {

        [XmlIgnore]
        public Color OutsideColor { get => m_cachedOutsideColor; set => m_cachedOutsideColor = value; }
        [XmlIgnore]
        private Color m_cachedOutsideColor = Color.gray;
        [XmlAttribute("color")]
        public string OutsideColorStr { get => m_cachedOutsideColor == null ? null : ColorExtensions.ToRGB(OutsideColor); set => OutsideColor = value.IsNullOrWhiteSpace() ? default : ColorExtensions.FromRGB(value); }

        [XmlIgnore]
        public Color GlassColor
        {
            get => m_cachedGlassColor;
            set
            {
                cachedFrameArray = null;
                m_cachedGlassColor = value;
            }
        }
        [XmlIgnore]
        private Color m_cachedGlassColor = Color.gray;
        [XmlAttribute("glassColor")]
        public string GlassColorStr { get => m_cachedGlassColor == null ? null : ColorExtensions.ToRGB(GlassColor); set => GlassColor = value.IsNullOrWhiteSpace() ? default : ColorExtensions.FromRGB(value); }

        [XmlAttribute("inheritColor")]
        public bool InheritColor { get; set; } = false;
        [XmlAttribute("specularLevel")]
        public float OuterSpecularLevel
        {
            get => m_outerSpecularLevel;
            set
            {
                cachedFrameArray = null;
                m_outerSpecularLevel = value;
            }
        }
        [XmlElement("backSize")]
        public Vector2Xml BackSize
        {
            get => m_backSize; set
            {
                cachedFrameArray = null; m_backSize = value;
            }
        }
        [XmlElement("backOffset")]
        public Vector2Xml BackOffset
        {
            get => m_backOffset; set
            {
                cachedFrameArray = null; m_backOffset = value;
            }
        }
        [XmlAttribute("frontDepth")]
        public float FrontDepth
        {
            get => m_frontDepth; set
            {
                cachedFrameArray = null; m_frontDepth = value;
            }
        }
        [XmlAttribute("glassTransparency")]
        public float GlassTransparency
        {
            get => m_glassTransparency; set
            {
                cachedFrameArray = null; m_glassTransparency = value;
            }
        }
        [XmlAttribute("glassSpecularLevel")]
        public float GlassSpecularLevel
        {
            get => m_glassSpecularLevel; set
            {
                cachedFrameArray = null; m_glassSpecularLevel = value;
            }
        }
        [XmlAttribute("backDepth")]
        public float BackDepth
        {
            get => m_backDepth; set
            {
                cachedFrameArray = null; m_backDepth = value;
            }
        }
        [XmlAttribute("frontBorderThickness")]
        public float FrontBorderThickness
        {
            get => m_frontBorderThickness; set
            {
                cachedFrameArray = null; m_frontBorderThickness = value;
            }
        }
        [XmlIgnore]
        public Vector3[] cachedFrameArray;
        [XmlIgnore]
        public Mesh meshOuterContainer;
        [XmlIgnore]
        public Mesh meshInnerContainer;
        [XmlIgnore]
        public Mesh meshGlass;
        [XmlIgnore]
        public Texture2D cachedGlassMain;
        [XmlIgnore]
        public Texture2D cachedGlassXYS;
        [XmlIgnore]
        public Texture2D cachedOuterXYS;
        [XmlIgnore]
        private Vector2Xml m_backSize = new Vector2Xml();
        [XmlIgnore]
        private Vector2Xml m_backOffset = new Vector2Xml();
        [XmlIgnore]
        private float m_frontDepth = .01f;
        [XmlIgnore]
        private float m_backDepth = .5f;
        [XmlIgnore]
        private float m_frontBorderThickness = .01f;
        [XmlIgnore]
        private float m_glassTransparency = 0.62f;
        [XmlIgnore]
        private float m_glassSpecularLevel = 0.26f;
        [XmlIgnore]
        private float m_outerSpecularLevel = 0.1f;

        ~FrameMesh()
        {
            UnityEngine.Object.Destroy(cachedGlassMain);
            UnityEngine.Object.Destroy(cachedGlassXYS);
            UnityEngine.Object.Destroy(cachedOuterXYS);
        }
    }

}

