using ColossalFramework;
using ColossalFramework.UI;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Rendering;
using System;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{
    [XmlRoot("textDescriptor")]
    public partial class BoardTextDescriptorGeneralXml : ILibable
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
        [Obsolete("Use textContent")]
        public TextType m_textType = TextType.Fixed;
        [XmlAttribute("textContentV2")]
        public TextContent textContent = TextContent.None;
        [XmlAttribute("destinationReference")]
        public DestinationReference m_destinationRelative = DestinationReference.Self;
        [XmlAttribute("parameterIdx")]
        public int m_parameterIdx = 0;
        [XmlIgnore]
        private TextParameterWrapper parameterValue;

        [XmlAttribute("defaultParameterValue")]
        public string DefaultParameterValueAsString
        {
            get => parameterValue?.ToString().TrimToNull();
            set => parameterValue = value.IsNullOrWhiteSpace() ? null : new TextParameterWrapper(value);
        }

        public void SetDefaultParameterValueAsString(string value, TextRenderingClass renderingClass) => parameterValue = value.IsNullOrWhiteSpace() ? null : new TextParameterWrapper(value);

        [XmlAttribute("parameterDisplayName")]
        public string ParameterDisplayName { get; set; }

        [XmlIgnore]
        public TextParameterWrapper DefaultParameterValue => parameterValue;

        [XmlAttribute("fixedText")]
        public string m_fixedText = "Text";
        [XmlIgnore]
        public TextParameterWrapper m_spriteParam;

        [XmlAttribute("spriteNameV2")]
        public string SpriteParam
        {
            get => m_spriteParam?.ToString();
            set => m_spriteParam = new TextParameterWrapper(value);
        }
        [XmlAttribute("spriteName")]
        public string SpriteParam_Legacy
        {
            get => null;
            set
            {
                if (!(value is null))
                {
                    m_spriteParam = new TextParameterWrapper($"image://<ROOT>/{value}");
                }
            }
        }

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
                case TextType.StreetNameComplete:
                case TextType.StreetPrefix:
                case TextType.StreetSuffix:
                case TextType.DistanceFromReference:
                case TextType.HwShield:
                case TextType.HwCodeLong:
                case TextType.HwCodeShort:
                case TextType.HwDettachedPrefix:
                case TextType.HwIdentifierSuffix:
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
                case TextType.HwShield:
                    return true;
            }
            return false;
        }

        public bool IsParameter() => m_textType == TextType.ParameterizedText || m_textType == TextType.ParameterizedGameSprite || m_textType == TextType.ParameterizedGameSpriteIndexed;

        public Tuple<int, string> ToParameterKV()
        {
            string paramType;
            switch (m_textType)
            {
                case TextType.ParameterizedGameSprite:
                    paramType = "folder:// or assetFolder://"; break;
                case TextType.ParameterizedGameSpriteIndexed:
                    paramType = "image:// or assetImage://"; break;
                case TextType.ParameterizedText:
                    paramType = "var:// or any string value"; break;
                default:
                    return null;
            }
            return Tuple.New(m_parameterIdx, $"<color yellow>{(ParameterDisplayName.IsNullOrWhiteSpace() ? SaveName : ParameterDisplayName)}</color>\n\t{paramType}{(DefaultParameterValue is null ? "" : $"\n\tdef: <color cyan>{DefaultParameterValueAsString}</color>")}");
        }


        #region Convert to common
#pragma warning disable CS0618 // O tipo ou membro é obsoleto

        public void UpdateContentType(TextRenderingClass renderingClass, ref int lastParamUsed)
        {
            if (textContent != TextContent.None)
            {
                return;
            }

            switch (renderingClass)
            {
                case TextRenderingClass.RoadNodes:
                    ToCommonDescriptorRoad(ref lastParamUsed);
                    break;
                case TextRenderingClass.Buildings:
                    ToCommonDescriptorBuilding(ref lastParamUsed);
                    break;
                case TextRenderingClass.PlaceOnNet:
                    ToCommonDescriptorOnNet(ref lastParamUsed);
                    break;
                case TextRenderingClass.Vehicle:
                    ToCommonDescriptorVehicle(ref lastParamUsed);
                    break;
            }
        }

        internal void ToCommonDescriptorBuilding(ref int lastParamUsed)
        {
            //TextType.Fixed,
            //TextType.GameSprite,
            //TextType.ParameterizedGameSprite,
            //TextType.ParameterizedGameSpriteIndexed,
            //TextType.ParameterizedText,
            //TextType.OwnName,
            //TextType.LinesSymbols,
            //TextType.LineFullName,
            //TextType.NextStopLine, // Next Station Line 1
            //TextType.PrevStopLine, // Previous Station Line 2
            //TextType.LastStopLine, // Line Destination (Last stop before get back) 3
            //TextType.PlatformNumber,
            //TextType.TimeTemperature,
            //TextType.CityName

            switch (m_textType)
            {
                case TextType.Fixed:
                    textContent = TextContent.ParameterizedText;
                    SetDefaultParameterValueAsString(m_fixedText, TextRenderingClass.Buildings);
                    m_parameterIdx = ++lastParamUsed;
                    break;
                case TextType.GameSprite:
                    textContent = TextContent.ParameterizedSpriteSingle;
                    SetDefaultParameterValueAsString(m_spriteParam.ToString(), TextRenderingClass.Buildings);
                    m_parameterIdx = ++lastParamUsed;
                    break;
                case TextType.ParameterizedText:
                    textContent = TextContent.ParameterizedText;
                    SetDefaultParameterValueAsString(DefaultParameterValueAsString, TextRenderingClass.Buildings);
                    break;
                case TextType.ParameterizedGameSprite:
                    textContent = TextContent.ParameterizedSpriteFolder;
                    SetDefaultParameterValueAsString(DefaultParameterValueAsString, TextRenderingClass.Buildings);
                    break;
                case TextType.ParameterizedGameSpriteIndexed:
                    textContent = TextContent.ParameterizedSpriteSingle;
                    SetDefaultParameterValueAsString(DefaultParameterValueAsString, TextRenderingClass.Buildings);
                    break;
                case TextType.OwnName:
                    ToParameterStringText(ref lastParamUsed, VariableBuildingSubType.OwnName);
                    break;
                case TextType.LinesSymbols:
                    textContent = TextContent.LinesSymbols;
                    break;
                case TextType.LineFullName:
                    textContent = TextContent.LinesNameList;
                    break;
                case TextType.NextStopLine:
                    ToParameterStringText(ref lastParamUsed, VariableBuildingSubType.NextStopLine);
                    break;
                case TextType.PrevStopLine:
                    ToParameterStringText(ref lastParamUsed, VariableBuildingSubType.PrevStopLine);
                    break;
                case TextType.LastStopLine:
                    ToParameterStringText(ref lastParamUsed, VariableBuildingSubType.LastStopLine);
                    break;
                case TextType.PlatformNumber:
                    ToParameterStringText(ref lastParamUsed, VariableBuildingSubType.PlatformNumber);
                    break;
                case TextType.TimeTemperature:
                    textContent = TextContent.TimeTemperature;
                    break;
                case TextType.CityName:
                    ToParameterStringText(ref lastParamUsed, VariableCitySubType.CityName, TextRenderingClass.Buildings);
                    break;
            }



        }

        internal void ToCommonDescriptorRoad(ref int lastParamUsed)
        {
            //    TextType.Fixed,
            //    TextType.GameSprite,
            //    TextType.StreetPrefix,
            //    TextType.StreetSuffix,
            //    TextType.StreetNameComplete,
            //    TextType.DistanceFromReference,
            //    TextType.PostalCode,
            //    TextType.District,
            //    TextType.Park,
            //    TextType.DistrictOrPark,
            //    TextType.ParkOrDistrict,
            //    TextType.CityName
            switch (m_textType)
            {
                case TextType.Fixed:
                    textContent = TextContent.ParameterizedText;
                    SetDefaultParameterValueAsString(m_fixedText, TextRenderingClass.PlaceOnNet);
                    m_parameterIdx = ++lastParamUsed;
                    break;
                case TextType.GameSprite:
                    textContent = TextContent.ParameterizedSpriteSingle;
                    SetDefaultParameterValueAsString(m_spriteParam.ToString(), TextRenderingClass.RoadNodes);
                    m_parameterIdx = ++lastParamUsed;
                    break;
                case TextType.StreetPrefix:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.StreetPrefix);
                    break;
                case TextType.StreetSuffix:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.StreetSuffix);
                    break;
                case TextType.StreetNameComplete:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.StreetNameComplete);
                    break;
                case TextType.District:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.District);
                    break;
                case TextType.Park:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.Park);
                    break;
                case TextType.DistrictOrPark:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.DistrictOrPark);
                    break;
                case TextType.ParkOrDistrict:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.DistrictOrPark);
                    break;
                case TextType.PostalCode:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.PostalCode);
                    break;
                case TextType.CityName:
                    ToParameterStringText(ref lastParamUsed, VariableCitySubType.CityName, TextRenderingClass.RoadNodes);
                    break;
                case TextType.DistanceFromReference:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.DistanceFromReferenceKilometers);
                    break; ;
            }

        }
        internal void ToCommonDescriptorOnNet(ref int lastParamUsed)
        {

            //TextType.Fixed,
            //    TextType.GameSprite,
            //    TextType.ParameterizedGameSprite,
            //    TextType.ParameterizedGameSpriteIndexed,
            //    TextType.ParameterizedText,
            //    TextType.HwShield,
            //    TextType.StreetPrefix,
            //    TextType.StreetSuffix,
            //    TextType.StreetNameComplete,
            //    TextType.PostalCode,
            //    TextType.District,
            //    TextType.Park,
            //    TextType.DistrictOrPark,
            //    TextType.ParkOrDistrict,
            //    TextType.TimeTemperature,
            //    TextType.CityName
            switch (m_textType)
            {
                case TextType.Fixed:
                    textContent = TextContent.ParameterizedText;
                    SetDefaultParameterValueAsString(m_fixedText, TextRenderingClass.PlaceOnNet);
                    m_parameterIdx = ++lastParamUsed;
                    break;
                case TextType.StreetPrefix:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.StreetPrefix);
                    break;
                case TextType.StreetSuffix:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.StreetSuffix);
                    break;
                case TextType.StreetNameComplete:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.StreetNameComplete);
                    break;
                case TextType.District:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.District);
                    break;
                case TextType.Park:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.Park);
                    break;
                case TextType.DistrictOrPark:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.DistrictOrPark);
                    break;
                case TextType.ParkOrDistrict:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.DistrictOrPark);
                    break;
                case TextType.LinesSymbols:
                    textContent = TextContent.LinesSymbols;
                    break;
                case TextType.Direction:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.Direction);
                    break;
                case TextType.HwShield:
                    textContent = TextContent.HwShield;
                    break;
                case TextType.PostalCode:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.PostalCode);
                    break;
                case TextType.GameSprite:
                    textContent = TextContent.ParameterizedSpriteSingle;
                    SetDefaultParameterValueAsString(m_spriteParam.ToString(), TextRenderingClass.PlaceOnNet);
                    m_parameterIdx = ++lastParamUsed;
                    break;
                case TextType.ParameterizedText:
                    textContent = TextContent.ParameterizedText;
                    SetDefaultParameterValueAsString(DefaultParameterValueAsString, TextRenderingClass.PlaceOnNet);
                    break;
                case TextType.TimeTemperature:
                    textContent = TextContent.TimeTemperature;
                    break;
                case TextType.ParameterizedGameSprite:
                    textContent = TextContent.ParameterizedSpriteFolder;
                    SetDefaultParameterValueAsString(DefaultParameterValueAsString, TextRenderingClass.PlaceOnNet);
                    break;
                case TextType.ParameterizedGameSpriteIndexed:
                    textContent = TextContent.ParameterizedSpriteSingle;
                    SetDefaultParameterValueAsString(DefaultParameterValueAsString, TextRenderingClass.PlaceOnNet);
                    break;
                case TextType.CityName:
                    ToParameterStringText(ref lastParamUsed, VariableCitySubType.CityName, TextRenderingClass.PlaceOnNet);
                    break;
                case TextType.HwCodeShort:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.HwCodeShort);
                    break;
                case TextType.HwCodeLong:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.HwCodeLong);
                    break;
                case TextType.HwDettachedPrefix:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.HwDettachedPrefix);
                    break;
                case TextType.HwIdentifierSuffix:
                    ToParameterStringText(ref lastParamUsed, VariableSegmentTargetSubType.HwIdentifierSuffix);
                    break;
            }

        }
        internal void ToCommonDescriptorVehicle(ref int lastParamUsed)
        {
            //TextType.Fixed,
            //TextType.GameSprite,
            //TextType.LinesSymbols,
            //TextType.OwnName,
            //TextType.LineIdentifier,
            //TextType.NextStopLine,
            //TextType.PrevStopLine,
            //TextType.LastStopLine,
            //TextType.LineFullName,
            //TextType.CityName,
            switch (m_textType)
            {
                case TextType.Fixed:
                    textContent = TextContent.ParameterizedText;
                    SetDefaultParameterValueAsString(m_fixedText, TextRenderingClass.PlaceOnNet);
                    m_parameterIdx = ++lastParamUsed;
                    break;
                case TextType.GameSprite:
                    textContent = TextContent.ParameterizedSpriteSingle;
                    SetDefaultParameterValueAsString(m_spriteParam.ToString(), TextRenderingClass.RoadNodes);
                    m_parameterIdx = ++lastParamUsed;
                    break;
                case TextType.LinesSymbols:
                    textContent = TextContent.LinesSymbols;
                    break;
                case TextType.CityName:
                    ToParameterStringText(ref lastParamUsed, VariableCitySubType.CityName, TextRenderingClass.RoadNodes);
                    break;
                case TextType.OwnName:
                    ToParameterStringText(ref lastParamUsed, VariableVehicleSubType.OwnNumber);
                    break;
                case TextType.LineIdentifier:
                    ToParameterStringText(ref lastParamUsed, VariableVehicleSubType.LineIdentifier);
                    break;
                case TextType.NextStopLine:
                    ToParameterStringText(ref lastParamUsed, VariableVehicleSubType.NextStopLine);
                    break;
                case TextType.PrevStopLine:
                    ToParameterStringText(ref lastParamUsed, VariableVehicleSubType.PrevStopLine);
                    break;
                case TextType.LastStopLine:
                    ToParameterStringText(ref lastParamUsed, VariableVehicleSubType.LastStopLine);
                    break;
                case TextType.LineFullName:
                    ToParameterStringText(ref lastParamUsed, VariableVehicleSubType.LineFullName);
                    break;
            }

        }

        private void ToParameterStringText(ref int lastParamUsed, VariableSegmentTargetSubType targetType)
        {
            textContent = TextContent.ParameterizedText;
            if (m_destinationRelative == 0)
            {
                SetDefaultParameterValueAsString($"var://CurrentSegment/{targetType}/{(m_allCaps ? "U" : "")}{(m_applyAbbreviations ? "A" : "")}/{m_prefix}/{m_suffix}", TextRenderingClass.PlaceOnNet);
            }
            else
            {
                SetDefaultParameterValueAsString($"var://SegmentTarget/{(int)m_destinationRelative}/{targetType}/{(m_allCaps ? "U" : "")}{(m_applyAbbreviations ? "A" : "")}/{m_prefix}/{m_suffix}", TextRenderingClass.PlaceOnNet);
            }
            m_parameterIdx = ++lastParamUsed;
        }
        private void ToParameterStringText(ref int lastParamUsed, VariableBuildingSubType targetType)
        {
            textContent = TextContent.ParameterizedText;
            SetDefaultParameterValueAsString($"var://CurrentBuilding/{targetType}/{(m_allCaps ? "U" : "")}{(m_applyAbbreviations ? "A" : "")}/{m_prefix}/{m_suffix}", TextRenderingClass.Buildings);
            m_parameterIdx = ++lastParamUsed;
        }
        private void ToParameterStringText(ref int lastParamUsed, VariableCitySubType targetType, TextRenderingClass renderingClass)
        {
            textContent = TextContent.ParameterizedText;
            SetDefaultParameterValueAsString($"var://CityData/{targetType}/{(m_allCaps ? "U" : "")}{(m_applyAbbreviations ? "A" : "")}/{m_prefix}/{m_suffix}", renderingClass);
            m_parameterIdx = ++lastParamUsed;
        }
        private void ToParameterStringText(ref int lastParamUsed, VariableVehicleSubType targetType)
        {
            textContent = TextContent.ParameterizedText;
            if (targetType.GetCommandLevel().descriptionKey == "COMMON_STRINGFORMAT")
            {
                SetDefaultParameterValueAsString($"var://CurrentVehicle/{targetType}/{(m_allCaps ? "U" : "")}{(m_applyAbbreviations ? "A" : "")}/{m_prefix}/{m_suffix}", TextRenderingClass.Vehicle);
            }
            else
            {
                SetDefaultParameterValueAsString($"var://CurrentVehicle/{targetType}/{m_prefix}/{m_suffix}", TextRenderingClass.Vehicle);
            }

            m_parameterIdx = ++lastParamUsed;
        }

#pragma warning restore CS0618 // O tipo ou membro é obsoleto
        #endregion

    }

}

