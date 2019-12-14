using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Libraries;
using Klyte.DynamicTextProps.Overrides;
using System;
using System.Collections.Generic;
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
        private UIDropDown m_abbreviationDropdown;
        private bool m_loadingAbbreviations;

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
            m_propColorPicker.parent.isVisible = !BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.UseDistrictColor;
            AddDropdown(Locale.Get("K45_DTP_CUSTOM_NAME_EXTRACTION_QUALIFIER"), out m_qualifierExtractionDropdown, m_uiHelperHS, Enum.GetNames(typeof(RoadQualifierExtractionMode)).Select(x => Locale.Get($"K45_DTP_RoadQualifierExtractionMode", x)).ToArray(), SetRoadQualifierExtractionMode);


            AddDropdown(Locale.Get("K45_DTP_ABBREVIATION_FILE"), out m_abbreviationDropdown, m_uiHelperHS, new string[0], SetAbbreviationFile);
            AddRefreshButtonForDropdown(m_abbreviationDropdown, () =>
            {
                m_loadingAbbreviations = true;
                DynamicTextPropsMod.Instance.Controller.ReloadAbbreviationFiles();
                var abbreviationOptions = new List<string>()
                    {
                        Locale.Get("K45_DTP_NO_ABBREVIATION_FILE_OPTION")
                    };
                abbreviationOptions.AddRange(DynamicTextPropsMod.Instance.Controller.AbbreviationFiles.Keys.OrderBy(x => x));
                m_abbreviationDropdown.items = abbreviationOptions.ToArray();
                m_abbreviationDropdown.selectedIndex = abbreviationOptions.IndexOf(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.AbbreviationFile);
                if (m_abbreviationDropdown.selectedIndex == -1)
                {
                    m_abbreviationDropdown.selectedIndex = 0;
                }

                m_loadingAbbreviations = false;
            });
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

        protected void SetAbbreviationFile(int idx)
        {
            if (!m_loadingAbbreviations)
            {
                BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.AbbreviationFile = idx <= 0 ? null : m_abbreviationDropdown.items[idx];
                BoardGeneratorRoadNodes.Instance.ClearCacheStreetName();
                BoardGeneratorRoadNodes.Instance.ClearCacheStreetQualifier();
            }
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
