using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Libraries;
using Klyte.DynamicTextProps.Overrides;
using Klyte.DynamicTextProps.Utils;
using System.Linq;
using UnityEngine;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorRoadNodes;

namespace Klyte.DynamicTextProps.UI
{

    internal class DTPStreetSignTab2 : DTPXmlEditorParentTab<BoardGeneratorRoadNodes, BoardBunchContainerStreetPlateXml, CacheControlStreetPlate, BasicRenderInformation, BoardDescriptorStreetSignXml, BoardTextDescriptorSteetSignXml, DTPLibTextMeshStreetPlate>
    {

        private UICheckBox m_useContrastColorTextCheckbox;

        private UICheckBox m_useDistrictColorCheck;
        private UIColorField m_propColorPicker;

        protected override BoardTextDescriptorSteetSignXml[] CurrentSelectedDescriptorArray
        {
            get => BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors;
            set => BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors = value;
        }

        #region Awake

        protected override string GetFontLabelString() => Locale.Get("K45_DTP_FONT_ST_CORNERS");

        protected override void AwakePropEditor(out UIScrollablePanel scrollTabs, out UIHelperExtension referenceHelper)
        {
            BoardGeneratorRoadNodes.GenerateDefaultSignModelAtLibrary();

            m_loadPropGroup = AddLibBox<DTPLibStreetPropGroup, BoardDescriptorStreetSignXml>(Locale.Get("K45_DTP_STREET_SIGNS_LIB_TITLE"), m_uiHelperHS,
                        out _, null,
                        out _, null,
                        out _, null,
                        (x) =>
                        {
                            BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor = XmlUtils.DefaultXmlDeserialize<BoardDescriptorStreetSignXml>(XmlUtils.DefaultXmlSerialize(x));
                            Start();
                        },
                () => BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor);

            var buttonErase = (UIButton) m_uiHelperHS.AddButton(Locale.Get("K45_DTP_ERASE_CURRENT_CONFIG"), DoDeleteGroup);
            KlyteMonoUtils.LimitWidth(buttonErase, m_uiHelperHS.Self.width - 20, true);
            buttonErase.color = Color.red;

            AddDropdown(Locale.Get("K45_DTP_PROP_MODEL_SELECT"), out m_propsDropdown, m_uiHelperHS, new string[0], SetPropModel);
            m_useDistrictColorCheck = m_uiHelperHS.AddCheckboxLocale("K45_DTP_USE_DISTRICT_COLOR", BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.UseDistrictColor, OnChangeUseDistrictColor);
            KlyteMonoUtils.LimitWidth(m_useDistrictColorCheck.label, m_uiHelperHS.Self.width - 50);
            KlyteMonoUtils.LimitWidth(m_uiHelperHS.AddCheckboxLocale("K45_DTP_PLACE_ON_DISTRICT_BORDER", BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.PlaceOnDistrictBorder, SetPlaceOnDistrictBorder).label, m_uiHelperHS.Self.width - 50);
            m_propColorPicker = m_uiHelperHS.AddColorPicker(Locale.Get("K45_DTP_PROP_COLOR"), BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.PropColor, OnChangePropColor);

            KlyteMonoUtils.CreateHorizontalScrollPanel(m_uiHelperHS.Self, out scrollTabs, out _, m_uiHelperHS.Self.width - 20, 40, Vector3.zero);
            referenceHelper = m_uiHelperHS;

        }

        protected override void OnTextTabStripChanged() => OnChangeTabTexts(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors?.Length ?? 0);
        protected override void DoInTextCommonTabGroupUI(UIHelperExtension groupTexts) => m_useContrastColorTextCheckbox = groupTexts.AddCheckboxLocale("K45_DTP_USE_CONTRAST_COLOR", false, SetUseContrastColor);



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
        private void OnChangePropColor(Color c)
        {
            BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.PropColor = c;
            BoardGeneratorRoadNodes.Instance.SoftReset();
        }


        public void ReloadGroupLib() => ReloadLib<DTPLibStreetPropGroup, BoardDescriptorStreetSignXml>(m_loadPropGroup);
        public void ReloadTextLib() => ReloadLib<DTPLibTextMeshStreetPlate, BoardTextDescriptorSteetSignXml>(m_loadTextDD);

        private static void ReloadLib<LIB, DESC>(UIDropDown loadDD)
            where LIB : BasicLib<LIB, DESC>, new()
            where DESC : ILibable => loadDD.items = BasicLib<LIB, DESC>.Instance.List().ToArray();

        private void DoDeleteGroup()
        {
            BoardGeneratorRoadNodes.Instance.CleanDescriptor();
            m_propsDropdown.selectedIndex = -1;
            OnChangeTabTexts(-1);

        }

        protected override void OnStart()
        {
            m_propsDropdown.selectedIndex = BoardGeneratorHighwaySigns.Instance.LoadedProps.IndexOf(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_propName ?? "") + 1;
            m_useDistrictColorCheck.isChecked = BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.UseDistrictColor;
            m_propColorPicker.parent.isVisible = !m_useDistrictColorCheck.isChecked;
            BoardGeneratorRoadNodes.Instance.ChangeFont(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.FontName);
            DTPUtils.ReloadFontsOf<BoardGeneratorRoadNodes>(m_fontSelect);
            OnChangeUseDistrictColor(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.UseDistrictColor);
            OnChangePropColor(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.PropColor);
            OnChangeTabTexts(-1);
        }

        protected override void SetPropModel(int idx)
        {
            if (!m_isLoading && idx >= 0)
            {
                BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_propName = idx == 0 ? null : BoardGeneratorHighwaySigns.Instance.LoadedProps[idx - 1];
                BoardGeneratorRoadNodes.Instance.SoftReset();
            }
        }

        protected override void OnChangeCustomText(BoardTextDescriptorSteetSignXml descriptor) => descriptor.GeneratedFixedTextRenderInfo = null;

        protected override void ReloadTabInfoText()
        {

            m_isLoading = true;
            EnsureTabQuantityTexts(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors?.Length ?? -1);
            ConfigureTabsShownText(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors?.Length ?? 0);
            m_pseudoTabTextsContainer.Self.isVisible = CurrentTabText >= 0;
            if (CurrentTabText >= 0)
            {
                LoadTabTextInfo(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors[CurrentTabText]);
                m_loadPropGroup.items = DTPLibTextMeshStreetPlate.Instance.List().ToArray();
            }
            m_isLoading = false;
            ReloadTextLib();
            ReloadGroupLib();
        }

        protected override void AfterLoadingTabTextInfo(BoardTextDescriptorSteetSignXml descriptor) => m_useContrastColorTextCheckbox.isChecked = descriptor?.m_useContrastColor ?? false;
        protected override void OnDropdownTextTypeSelectionChanged(int idx) => m_customText.parent.isVisible = BoardGeneratorBuildings.AVAILABLE_TEXT_TYPES[idx] == TextType.Fixed;
        protected override void OnLoadTextLibItem() => ReloadTabInfoText();
        protected override void PostAwake() { }
        protected override string GetLocaleNameForContentTypes() => "K45_DTP_OWN_NAME_CONTENT";
        protected override bool isTextEditionAvailable() => true;
        protected override void ReloadTabInfo() { }
        protected override TextType[] GetAvailableTextTypes() => AVAILABLE_TEXT_TYPES;

        #endregion

    }


}
