using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Libraries;
using Klyte.DynamicTextProps.Overrides;
using System;
using System.Linq;
using static Klyte.DynamicTextProps.Overrides.BoardDescriptorStreetSignXml;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorRoadNodes;

namespace Klyte.DynamicTextProps.UI
{

    internal class DTPStreetSignTab3 : DTPNoPropEditorTemplateTab<BoardGeneratorRoadNodes, BoardBunchContainerStreetPlateXml, CacheControlStreetPlate, BasicRenderInformation, BoardDescriptorStreetSignXml, BoardTextDescriptorSteetSignXml, DTPLibTextMeshStreetPlate, DTPLibStreetPropGroup>
    {
        private UICheckBox m_useDistrictColorCheck;
        private UIColorField m_propColorPicker;
        private UIDropDown m_qualifierExtractionDropdown;

        protected override BoardDescriptorStreetSignXml CurrentConfig
        {
            get => BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor;
            set => BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor = value;
        }

        protected override void BeforeCreatingTextScrollPanel()
        {
            m_useDistrictColorCheck = m_uiHelperHS.AddCheckboxLocale("K45_DTP_USE_DISTRICT_COLOR", BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.UseDistrictColor, OnChangeUseDistrictColor);
            KlyteMonoUtils.LimitWidth(m_useDistrictColorCheck.label, m_uiHelperHS.Self.width - 50);
            KlyteMonoUtils.LimitWidth(m_uiHelperHS.AddCheckboxLocale("K45_DTP_PLACE_ON_DISTRICT_BORDER", BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.PlaceOnDistrictBorder, SetPlaceOnDistrictBorder).label, m_uiHelperHS.Self.width - 50);
            m_propColorPicker = m_uiHelperHS.AddColorPicker(Locale.Get("K45_DTP_PROP_COLOR"), BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.PropColor, OnChangePropColor);
            AddDropdown(Locale.Get("K45_DTP_CUSTOM_NAME_EXTRACTION_QUALIFIER"), out m_qualifierExtractionDropdown, m_uiHelperHS, Enum.GetNames(typeof(RoadQualifierExtractionMode)).Select(x => Locale.Get($"K45_DTP_RoadQualifierExtractionMode", x)).ToArray(), SetRoadQualifierExtractionMode);

        }
        protected override void CleanDescriptor() => BoardGeneratorRoadNodes.Instance.CleanDescriptor();
        protected override void GenerateDefaultModels() => BoardGeneratorRoadNodes.GenerateDefaultSignModelAtLibrary();
        protected override TextType[] GetAvailableTextTypes() => AVAILABLE_TEXT_TYPES;
        protected override string GetFontLabelString() => Locale.Get("K45_DTP_FONT_ST_CORNERS");
        protected override string GetLibLocaleEntry() => "K45_DTP_STREET_SIGNS_LIB_TITLE";
        protected override string GetLocaleNameForContentTypes() => "K45_DTP_OWN_NAME_CONTENT";

        protected void SetRoadQualifierExtractionMode(int idx)
        {
            BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.RoadQualifierExtraction = (RoadQualifierExtractionMode) idx;
            BoardGeneratorRoadNodes.Instance.ClearCacheStreetName();
            BoardGeneratorRoadNodes.Instance.ClearCacheStreetQualifier();
        }

        private void OnChangeUseDistrictColor(bool isChecked)
        {
            BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.UseDistrictColor = isChecked;
            m_propColorPicker.parent.isVisible = !isChecked;
            BoardGeneratorRoadNodes.Instance.SoftReset();
        }
        private void SetPlaceOnDistrictBorder(bool isChecked)
        {
            BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.PlaceOnDistrictBorder = isChecked;
            BoardGeneratorRoadNodes.Instance.SoftReset();
        }
    }


}
