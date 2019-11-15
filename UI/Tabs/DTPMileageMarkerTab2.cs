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
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorHighwayMileage;

namespace Klyte.DynamicTextProps.UI
{

    internal class DTPMileageMarkerTab2 : DTPXmlEditorParentTab<BoardGeneratorHighwayMileage, IBoardBunchContainer<CacheControl, BasicRenderInformation>, CacheControl, BasicRenderInformation, BoardDescriptorMileageMarkerXml, BoardTextDescriptorMileageMarkerXml, DTPLibTextMeshMileageMarker>
    {
        private UICheckBox m_useContrastColorTextCheckbox;

        protected override BoardTextDescriptorMileageMarkerXml[] CurrentSelectedDescriptorArray { get => BoardGeneratorHighwayMileage.LoadedMileageMarkerConfig.m_textDescriptors; set => BoardGeneratorHighwayMileage.LoadedMileageMarkerConfig.m_textDescriptors = value; }

        protected override string GetFontLabelString() => Locale.Get("K45_DTP_FONT_MILEAGE_MARKES");

        #region Awake
        protected override void AwakePropEditor(out UIScrollablePanel scrollTabs, out UIHelperExtension referenceHelper)
        {
            BoardGeneratorHighwayMileage.GenerateDefaultSignModelAtLibrary();

            m_loadPropGroup = AddLibBox<DTPLibMileageMarkerGroup, BoardDescriptorMileageMarkerXml>(Locale.Get("K45_DTP_MILEAGE_MARKERS_LIB_TITLE"), m_uiHelperHS,
                        out _, null,
                        out _, null,
                        out _, null,
                        (x) =>
                        {
                            BoardGeneratorHighwayMileage.LoadedMileageMarkerConfig = XmlUtils.DefaultXmlDeserialize<BoardDescriptorMileageMarkerXml>(XmlUtils.DefaultXmlSerialize(x));
                            Start();
                        },
                () => BoardGeneratorHighwayMileage.LoadedMileageMarkerConfig);

            var buttonErase = (UIButton) m_uiHelperHS.AddButton(Locale.Get("K45_DTP_ERASE_CURRENT_CONFIG"), DoDeleteGroup);
            KlyteMonoUtils.LimitWidth(buttonErase, m_uiHelperHS.Self.width - 20, true);
            buttonErase.color = Color.red;

            AddDropdown(Locale.Get("K45_DTP_PROP_MODEL_SELECT"), out m_propsDropdown, m_uiHelperHS, new string[0], SetPropModel);
            UIColorField m_propColorPicker = m_uiHelperHS.AddColorPicker(Locale.Get("K45_DTP_PROP_COLOR"), BoardGeneratorHighwayMileage.LoadedMileageMarkerConfig.PropColor, OnChangePropColor);
            KlyteMonoUtils.LimitWidth(m_uiHelperHS.AddCheckboxLocale("K45_DTP_USE_MILES", BoardGeneratorHighwayMileage.LoadedMileageMarkerConfig.UseMiles, OnSetUseMiles).label, m_uiHelperHS.Self.width - 50);


            KlyteMonoUtils.CreateHorizontalScrollPanel(m_uiHelperHS.Self, out scrollTabs, out _, m_uiHelperHS.Self.width - 20, 40, Vector3.zero);

            referenceHelper = m_uiHelperHS;
        }

        protected override void DoInTextCommonTabGroupUI(UIHelperExtension groupTexts)
        {
            base.DoInTextCommonTabGroupUI(groupTexts);
            m_useContrastColorTextCheckbox = groupTexts.AddCheckboxLocale("K45_DTP_USE_CONTRAST_COLOR", false, SetUseContrastColor);
        }

        protected override void PostAwake() { }

        private void OnChangePropColor(Color c)
        {
            BoardGeneratorHighwayMileage.LoadedMileageMarkerConfig.PropColor = c;
            BoardGeneratorHighwayMileage.Instance.SoftReset();
        }
        private void OnSetUseMiles(bool value)
        {
            BoardGeneratorHighwayMileage.LoadedMileageMarkerConfig.UseMiles = value;
            BoardGeneratorHighwayMileage.Instance.SoftReset();
        }
        public void ReloadGroupLib() => ReloadLib<DTPLibMileageMarkerGroup, BoardDescriptorMileageMarkerXml>(m_loadPropGroup);
        public void ReloadTextLib() => ReloadLib<DTPLibTextMeshMileageMarker, BoardTextDescriptorMileageMarkerXml>(m_loadTextDD);

        private static void ReloadLib<LIB, DESC>(UIDropDown loadDD)
            where LIB : BasicLib<LIB, DESC>, new()
            where DESC : ILibable => loadDD.items = BasicLib<LIB, DESC>.Instance.List().ToArray();

        private void DoDeleteGroup()
        {
            BoardGeneratorHighwayMileage.Instance.CleanDescriptor();
            m_propsDropdown.selectedIndex = -1;
            OnChangeTabTexts(-1);

        }

        protected override void OnStart()
        {
            m_propsDropdown.selectedIndex = BoardGeneratorHighwaySigns.Instance.LoadedProps.IndexOf(BoardGeneratorHighwayMileage.LoadedMileageMarkerConfig.m_propName) + 1;
            BoardGeneratorHighwayMileage.Instance.ChangeFont(BoardGeneratorHighwayMileage.LoadedMileageMarkerConfig.FontName);
            DTPUtils.ReloadFontsOf<BoardGeneratorHighwayMileage>(m_fontSelect);
            OnChangePropColor(BoardGeneratorHighwayMileage.LoadedMileageMarkerConfig.PropColor);
            OnChangeTabTexts(-1);
        }

        protected override void SetPropModel(int idx)
        {
            if (!m_isLoading)
            {
                if (idx > 0)
                {
                    BoardGeneratorHighwayMileage.LoadedMileageMarkerConfig.m_propName = BoardGeneratorHighwaySigns.Instance.LoadedProps[idx - 1];
                }
                else
                {
                    BoardGeneratorHighwayMileage.LoadedMileageMarkerConfig.m_propName = null;
                }
                BoardGeneratorHighwayMileage.Instance.SoftReset();
            }
        }

        protected override TextType[] GetAvailableTextTypes() => AVAILABLE_TEXT_TYPES;

        protected override void ReloadTabInfoText()
        {

            m_isLoading = true;
            EnsureTabQuantityTexts(LoadedMileageMarkerConfig.m_textDescriptors?.Length ?? -1);
            ConfigureTabsShownText(LoadedMileageMarkerConfig.m_textDescriptors?.Length ?? 0);
            m_pseudoTabTextsContainer.Self.isVisible = CurrentTabText >= 0;
            if (CurrentTabText >= 0)
            {
                LoadTabTextInfo(LoadedMileageMarkerConfig.m_textDescriptors[CurrentTabText]);
                m_loadPropGroup.items = DTPLibTextMeshStreetPlate.Instance.List().ToArray();
            }
            m_isLoading = false;
            ReloadTextLib();
            ReloadGroupLib();
        }

        protected override void AfterLoadingTabTextInfo(BoardTextDescriptorMileageMarkerXml descriptor)
        {

            m_useContrastColorTextCheckbox.isChecked = descriptor.m_useContrastColor;
        }

        protected override void OnTextTabStripChanged() => OnChangeTabTexts(BoardGeneratorHighwayMileage.LoadedMileageMarkerConfig.m_textDescriptors?.Length ?? 0);
        protected override string GetLocaleNameForContentTypes() => "K45_DTP_OWN_NAME_CONTENT_MM";
        protected override void OnDropdownTextTypeSelectionChanged(int idx) => m_customText.parent.isVisible = BoardGeneratorBuildings.AVAILABLE_TEXT_TYPES[idx] == TextType.Fixed;
        protected override void OnLoadTextLibItem() => ReloadTabInfoText();
        protected override bool isTextEditionAvailable() => true;
        protected override void ReloadTabInfo() { }

        protected override void OnChangeCustomText(BoardTextDescriptorMileageMarkerXml descriptor) => descriptor.GeneratedFixedTextRenderInfo = null;

        #endregion

    }


}
