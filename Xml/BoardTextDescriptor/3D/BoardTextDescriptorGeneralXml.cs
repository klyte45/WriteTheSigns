using ColossalFramework;
using ColossalFramework.UI;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
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
        public TextType m_textType = TextType.Fixed;
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
    }

}

