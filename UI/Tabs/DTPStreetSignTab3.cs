using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Data;
using Klyte.DynamicTextProps.Libraries;
using Klyte.DynamicTextProps.Overrides;
using Klyte.DynamicTextProps.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using static Klyte.DynamicTextProps.Overrides.BoardDescriptorStreetSignXml;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorRoadNodes;

namespace Klyte.DynamicTextProps.UI
{

    internal class DTPStreetSignTab3 : DTPNoPropEditorTemplateTab<BoardGeneratorRoadNodes, IBoardBunchContainer<CacheControlStreetPlate>, DTPRoadNodesData, BoardDescriptorStreetSignXml, BoardTextDescriptorSteetSignXml, DTPLibTextMeshStreetPlate, DTPLibStreetPropGroup>
    {
        private UICheckBox m_useDistrictColorCheck;
        private UIColorField m_propColorPicker;
        private UIDropDown m_qualifierExtractionDropdown;
        private UIDropDown m_abbreviationDropdown;
        private bool m_loadingAbbreviations;

        protected override BoardDescriptorStreetSignXml CurrentConfig
        {
            get => DTPRoadNodesData.Instance.CurrentDescriptor;
            set => DTPRoadNodesData.Instance.CurrentDescriptor = value;
        }

        protected override void BeforeCreatingTextScrollPanel()
        {
            m_useDistrictColorCheck = m_uiHelperHS.AddCheckboxLocale("K45_DTP_USE_DISTRICT_COLOR", DTPRoadNodesData.Instance.CurrentDescriptor.UseDistrictColor, OnChangeUseDistrictColor);
            KlyteMonoUtils.LimitWidth(m_useDistrictColorCheck.label, m_uiHelperHS.Self.width - 50);
            KlyteMonoUtils.LimitWidth(m_uiHelperHS.AddCheckboxLocale("K45_DTP_PLACE_ON_DISTRICT_BORDER", DTPRoadNodesData.Instance.CurrentDescriptor.PlaceOnDistrictBorder, SetPlaceOnDistrictBorder).label, m_uiHelperHS.Self.width - 50);
            m_propColorPicker = m_uiHelperHS.AddColorPicker(Locale.Get("K45_DTP_PROP_COLOR"), DTPRoadNodesData.Instance.CurrentDescriptor.PropColor, OnChangePropColor);
            m_propColorPicker.parent.isVisible = !DTPRoadNodesData.Instance.CurrentDescriptor.UseDistrictColor;
            AddDropdown(Locale.Get("K45_DTP_CUSTOM_NAME_EXTRACTION_QUALIFIER"), out m_qualifierExtractionDropdown, m_uiHelperHS, Enum.GetNames(typeof(RoadQualifierExtractionMode)).Select(x => Locale.Get($"K45_DTP_RoadQualifierExtractionMode", x)).ToArray(), SetRoadQualifierExtractionMode);


            AddDropdown(Locale.Get("K45_DTP_ABBREVIATION_FILE"), out m_abbreviationDropdown, m_uiHelperHS, new string[0], SetAbbreviationFile);
            AddRefreshButtonForDropdown(m_abbreviationDropdown, () =>
            {
                m_loadingAbbreviations = true;
                DynamicTextPropsMod.Controller.ReloadAbbreviationFiles();
                var abbreviationOptions = new List<string>()
                    {
                        Locale.Get("K45_DTP_NO_ABBREVIATION_FILE_OPTION")
                    };
                abbreviationOptions.AddRange(DynamicTextPropsMod.Controller.AbbreviationFiles.Keys.OrderBy(x => x));
                m_abbreviationDropdown.items = abbreviationOptions.ToArray();
                m_abbreviationDropdown.selectedIndex = abbreviationOptions.IndexOf(DTPRoadNodesData.Instance.CurrentDescriptor.AbbreviationFile);
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
            DTPRoadNodesData.Instance.CurrentDescriptor.RoadQualifierExtraction = (RoadQualifierExtractionMode)idx;
            RenderUtils.ClearCacheStreetName();
            RenderUtils.ClearCacheStreetQualifier();
        }

        protected void SetAbbreviationFile(int idx)
        {
            if (!m_loadingAbbreviations)
            {
                DTPRoadNodesData.Instance.CurrentDescriptor.AbbreviationFile = idx <= 0 ? null : m_abbreviationDropdown.items[idx];
                RenderUtils.ClearCacheStreetName();
                RenderUtils.ClearCacheStreetQualifier();
            }
        }

        private void OnChangeUseDistrictColor(bool isChecked)
        {
            DTPRoadNodesData.Instance.CurrentDescriptor.UseDistrictColor = isChecked;
            m_propColorPicker.parent.isVisible = !isChecked;
            BoardGeneratorRoadNodes.Instance.SoftReset();
        }
        private void SetPlaceOnDistrictBorder(bool isChecked)
        {
            DTPRoadNodesData.Instance.CurrentDescriptor.PlaceOnDistrictBorder = isChecked;
            BoardGeneratorRoadNodes.Instance.SoftReset();
        }
    }


}
