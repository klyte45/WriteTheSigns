using ColossalFramework;
using ColossalFramework.UI;
using FontStashSharp;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using System;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.WriteTheSigns.Xml
{
    [XmlRoot("textDescriptor")]
    public class BoardTextDescriptorGeneralXml : ILibable
    {
        [XmlAttribute("textScale")]
        public float m_textScale = 1f;
        [XmlAttribute("maxWidth")]
        public float m_maxWidthMeters = 0;
        [XmlAttribute("applyOverflowResizingOnY")]
        public bool m_applyOverflowResizingOnY = false;


        [XmlAttribute("textAlign")]
        public UIHorizontalAlignment m_textAlign = UIHorizontalAlignment.Center;
        [XmlAttribute("textContent")]
        public TextType m_textType = TextType.Fixed;
        [XmlAttribute("destinationReference")]
        public DestinationReference m_destinationRelative = DestinationReference.Self;
        [XmlAttribute("parameterIdx")]
        public int m_parameterIdx = 1;
        [XmlAttribute("fixedText")]
        public string m_fixedText = "Text";
        [XmlAttribute("spriteName")]
        public string m_spriteName = "";

        [XmlAttribute("overrideFont")] public string m_overrideFont;
        [XmlAttribute("fontClass")] public FontClass m_fontClass = FontClass.Regular;

        [XmlAttribute("allCaps")]
        public bool m_allCaps = false;
        [XmlAttribute("applyAbbreviations")]
        public bool m_applyAbbreviations = false;
        [XmlAttribute("prefix")]
        public string m_prefix = "";
        [XmlAttribute("suffix")]
        public string m_suffix = "";


        [XmlAttribute("saveName")]
        public string SaveName { get; set; }


        [XmlElement("PlacingSettings")]
        public PlacingSettings PlacingConfig { get; set; } = new PlacingSettings();

        [XmlElement("ColoringSettings")]
        public ColoringSettings ColoringConfig { get; set; } = new ColoringSettings();

        [XmlElement("IlluminationSettings")]
        public IlluminationSettings IlluminationConfig { get; set; } = new IlluminationSettings();

        [XmlElement("MultiItemSettings")]
        public SubItemSettings MultiItemSettings { get; set; } = new SubItemSettings();

        [XmlElement("BackgroundMeshSettings")]
        public BackgroundMesh BackgroundMeshSettings { get; set; } = new BackgroundMesh();

        [XmlElement("AnimationSettings")]
        public AnimationSettings AnimationSettings { get; set; } = new AnimationSettings();


        public bool IsTextRelativeToSegment()
        {
            switch (m_textType)
            {
                case TextType.District:
                case TextType.DistrictOrPark:
                case TextType.Park:
                case TextType.ParkOrDistrict:
                case TextType.PostalCode:
                case TextType.StreetCode:
                case TextType.StreetNameComplete:
                case TextType.StreetPrefix:
                case TextType.StreetSuffix:
                case TextType.DistanceFromReference:
                    return true;
            }
            return false;
        }
        public bool IsMultiItemText()
        {
            switch (m_textType)
            {
                case TextType.LinesSymbols:
                case TextType.LineFullName:
                    return true;
            }
            return false;
        }
        public bool IsSpriteText()
        {
            switch (m_textType)
            {
                case TextType.LinesSymbols:
                case TextType.GameSprite:
                    return true;
            }
            return false;
        }

        public class SubItemSettings
        {
            [XmlAttribute("subItemsPerRow")]
            public int SubItemsPerRow { get => m_subItemsPerRow; set => m_subItemsPerRow = Math.Max(1, Math.Min(value, 10)); }
            [XmlAttribute("subItemsPerColumn")]
            public int SubItemsPerColumn { get => m_subItemsPerColumn; set => m_subItemsPerColumn = Math.Max(1, Math.Min(value, 10)); }

            [XmlAttribute("verticalFirst")]
            public bool VerticalFirst { get; set; }
            [XmlAttribute("verticalAlign")]
            public UIVerticalAlignment VerticalAlign { get; set; } = UIVerticalAlignment.Top;


            [XmlElement("subItemSpacing")]
            public Vector2Xml SubItemSpacing { get; set; } = new Vector2Xml();
            [XmlIgnore]
            private int m_subItemsPerColumn;
            [XmlIgnore]
            private int m_subItemsPerRow;
        }
        public class PlacingSettings
        {
            [XmlAttribute("cloneInvertHorizontalAlign")]
            public bool m_invertYCloneHorizontalAlign;
            [XmlAttribute("clone180DegY")]
            public bool m_create180degYClone;
            [XmlAttribute("mirrored")]
            public bool m_mirrored;

            [XmlElement("position")]
            public Vector3Xml Position { get; set; } = new Vector3Xml();
            [XmlElement("rotation")]
            public Vector3Xml Rotation { get; set; } = new Vector3Xml();
        }
        public class ColoringSettings
        {
            [XmlAttribute("useContrastColor")]
            public bool m_useContrastColor = true;
            [XmlIgnore]
            public Color m_defaultColor = Color.clear;
            [XmlAttribute("color")]
            public string ForceColor { get => m_defaultColor == Color.clear ? null : ColorExtensions.ToRGB(m_defaultColor); set => m_defaultColor = value.IsNullOrWhiteSpace() ? Color.clear : (Color)ColorExtensions.FromRGB(value); }

        }

        public class IlluminationSettings
        {
            [XmlAttribute("type")]
            public MaterialType IlluminationType { get; set; } = MaterialType.OPAQUE;
            [XmlAttribute("strength")]
            public float IlluminationStrength { get; set; } = 1;
            [XmlAttribute("blinkType")]
            public BlinkType BlinkType { get; set; } = BlinkType.None;
            [XmlElement("customBlinkParams")]
            public Vector4Xml CustomBlink { get; set; } = new Vector4Xml();
            [XmlAttribute("requiredFlags")]
            public int m_requiredFlags;
            [XmlAttribute("forbiddenFlags")]
            public int m_forbiddenFlags;

        }

        public class BackgroundMesh
        {
            [XmlElement("size")]
            public Vector2Xml Size
            {
                get => m_size; set {
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
            set {
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
            set {
                cachedFrameArray = null;
                m_outerSpecularLevel = value;
            }
        }
        [XmlElement("backSize")]
        public Vector2Xml BackSize
        {
            get => m_backSize; set {
                cachedFrameArray = null; m_backSize = value;
            }
        }
        [XmlElement("backOffset")]
        public Vector2Xml BackOffset
        {
            get => m_backOffset; set {
                cachedFrameArray = null; m_backOffset = value;
            }
        }
        [XmlAttribute("frontDepth")]
        public float FrontDepth
        {
            get => m_frontDepth; set {
                cachedFrameArray = null; m_frontDepth = value;
            }
        }
        [XmlAttribute("glassTransparency")]
        public float GlassTransparency
        {
            get => m_glassTransparency; set {
                cachedFrameArray = null; m_glassTransparency = value;
            }
        }
        [XmlAttribute("glassSpecularLevel")]
        public float GlassSpecularLevel
        {
            get => m_glassSpecularLevel; set {
                cachedFrameArray = null; m_glassSpecularLevel = value;
            }
        }
        [XmlAttribute("backDepth")]
        public float BackDepth
        {
            get => m_backDepth; set {
                cachedFrameArray = null; m_backDepth = value;
            }
        }
        [XmlAttribute("frontBorderThickness")]
        public float FrontBorderThickness
        {
            get => m_frontBorderThickness; set {
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

    public class AnimationSettings
    {
        [XmlAttribute("extraDelayCycleFrames")]
        public int m_extraDelayCycleFrames;
        [XmlAttribute("itemCycleFramesDuration")]
        public int m_itemCycleFramesDuration = 400;
    }

    public enum BlinkType
    {
        None,
        Blink_050_050,
        MildFade_0125_0125,
        MediumFade_500_500,
        StrongBlaze_0125_0125,
        StrongFade_250_250,
        Blink_025_025,
        Custom
    }

    public enum FontClass
    {
        Regular,
        PublicTransport,
        ElectronicBoards,
        Stencil

    }

}

