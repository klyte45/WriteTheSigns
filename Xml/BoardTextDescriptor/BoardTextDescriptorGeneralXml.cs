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
        [XmlAttribute("destinationRelative")]
        public DestinationReference m_destinationRelative = DestinationReference.Self;
        [XmlAttribute("targetNodeRelative")]
        public int m_targetNodeRelative = 0;
        [XmlAttribute("fixedText")]
        public string m_fixedText = "Text";

        [XmlAttribute("overrideFont")] public string m_overrideFont;

        [XmlAttribute("allCaps")]
        public bool m_allCaps = false;
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

        [XmlElement("MultiItemSettings")]
        public SubItemSettings MultiItemSettings { get; set; } = new SubItemSettings();

        public bool IsTextRelativeToSegment()
        {
            switch (m_textType)
            {
                case TextType.District:
                case TextType.DistrictOrPark:
                case TextType.Park:
                case TextType.ParkOrDistrict:
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
                    return true;
            }
            return false;
        }

        public class SubItemSettings
        {
            [XmlAttribute("subItemsPerRow")]
            public int SubItemsPerRow { get => m_subItemsPerRow; set => m_subItemsPerRow = Math.Max(1, value); }

            [XmlAttribute("subItemsPerColumn")]
            public int SubItemsPerColumn { get => m_subItemsPerColumn; set => m_subItemsPerColumn = Math.Max(1, value); }

            [XmlAttribute("verticalFirst")]
            public bool VerticalFirst { get; set; }

            [XmlAttribute("subItemSpacingX")]
            public float SubItemSpacingX { get => m_subItemSpacing.x; set => m_subItemSpacing.x = value; }
            [XmlAttribute("subItemSpacingY")]
            public float SubItemSpacingY { get => m_subItemSpacing.y; set => m_subItemSpacing.y = value; }

            [XmlIgnore]
            public Vector2 m_subItemSpacing;
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


            [XmlIgnore]
            public Vector3 m_textRelativePosition;
            [XmlIgnore]
            public Vector3 m_textRelativeRotation;
        }
        public class ColoringSettings
        {
            [XmlAttribute("useContrastColor")]
            public bool m_useContrastColor = true;
            [XmlIgnore]
            public Color m_defaultColor = Color.clear;
            [XmlIgnore]
            public Vector4 m_customBlink;
            [XmlAttribute("color")]
            public string ForceColor { get => m_defaultColor == Color.clear ? null : ColorExtensions.ToRGB(m_defaultColor); set => m_defaultColor = value.IsNullOrWhiteSpace() ? Color.clear : (Color)ColorExtensions.FromRGB(value); }
            [XmlAttribute("appearenceType")]
            public MaterialType MaterialType { get; set; } = MaterialType.OPAQUE;
            [XmlAttribute("illuminationStrength")]
            public float IlluminationStrength { get; set; } = 1;
            [XmlAttribute("blinkType")]
            public BlinkType BlinkType { get; set; } = BlinkType.None;


            [XmlAttribute("customBlinkX")]
            public float CustomBlinkX { get => m_customBlink.x; set => m_customBlink.x = value; }
            [XmlAttribute("customBlinkY")]
            public float CustomBlinkY { get => m_customBlink.y; set => m_customBlink.y = value; }
            [XmlAttribute("customBlinkZ")]
            public float CustomBlinkZ { get => m_customBlink.z; set => m_customBlink.z = value; }
            [XmlAttribute("customBlinkW")]
            public float CustomBlinkW { get => m_customBlink.w; set => m_customBlink.w = value; }
        }
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


}
