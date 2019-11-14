﻿using ColossalFramework;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{
    public class BoardDescriptorBuildingXml : BoardDescriptorParentXml<BoardDescriptorBuildingXml, BoardTextDescriptorBuildingsXml>, ILibable

    {
        [XmlArray("platformOrder")]
        [XmlArrayItem("p")]
        public int[] m_platforms = new int[0];
        [XmlAttribute("showIfNoLine")]
        public bool m_showIfNoLine = true;


        [XmlIgnore]
        private Vector3 m_arrayRepeat;
        [XmlIgnore]
        public Vector3 ArrayRepeat { get => m_arrayRepeat; set => m_arrayRepeat = value; }

        [XmlAttribute("arrayRepeatX")]
        public float ArrayRepeatX { get => m_arrayRepeat.x; set => m_arrayRepeat.x = value; }
        [XmlAttribute("arrayRepeatY")]
        public float ArrayRepeatY { get => m_arrayRepeat.y; set => m_arrayRepeat.y = value; }
        [XmlAttribute("arrayRepeatZ")]
        public float ArrayRepeatZ { get => m_arrayRepeat.z; set => m_arrayRepeat.z = value; }

        [XmlAttribute("arrayRepeatTimes")]
        public int m_arrayRepeatTimes = 0;

        [XmlAttribute("coloringMode")]
        public ColoringMode ColorModeProp
        {
            get {
                if (m_colorMode == null)
                {
                    if (FixedColor == default)
                    {
                        m_colorMode = ColoringMode.ByPlatform;
                    }
                    else
                    {
                        m_colorMode = ColoringMode.Fixed;
                    }
                }
                return m_colorMode ?? ColoringMode.Fixed;
            }
            set => m_colorMode = value;
        }


        [XmlIgnore]
        private ColoringMode? m_colorMode;

        [XmlIgnore]
        public Color FixedColor { get => m_cachedColor; set => SetDistrictColor(value); }
        [XmlIgnore]
        private Color m_cachedColor;

        [XmlAttribute("fixedColor")]
        public string FixedColorStr { get => m_cachedColor == default ? null : ColorExtensions.ToRGB(FixedColor); set => SetDistrictColor(value.IsNullOrWhiteSpace() ? default : (Color) ColorExtensions.FromRGB(value)); }
        [XmlAttribute("saveName")]
        public string SaveName { get; set; }

        private void SetDistrictColor(Color c)
        {
            DistrictManagerOverrides.OnDistrictChanged();
            m_cachedColor = c;
        }
    }

    public enum ColoringMode
    {
        ByPlatform,
        Fixed,
        ByDistrict,
        FromBuilding
    }
}
