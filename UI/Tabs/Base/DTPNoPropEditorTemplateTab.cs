using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Data;
using Klyte.DynamicTextProps.Libraries;
using Klyte.DynamicTextProps.Overrides;
using Klyte.DynamicTextProps.Utils;
using System.Linq;
using UnityEngine;

namespace Klyte.DynamicTextProps.UI
{

    internal abstract class DTPNoPropEditorTemplateTab<BG, BBC, D, BD, BTD, LIBTXT, LIBPROP> : DTPXmlEditorParentTab<BG, BBC, D, BD, BTD, LIBTXT>
        where BG : BoardGeneratorParent<BG, BBC, D, BD, BTD>
        where BBC : IBoardBunchContainer
        where BD : BoardDescriptorParentXml<BD, BTD>, ILibable, IFontConfigContainer, IPropParamsContainer
        where BTD : BoardTextDescriptorParentXml<BTD>, ILibable
        where D : DTPBaseData<D, BBC>, new()
        where LIBTXT : BasicLib<LIBTXT, BTD>, new()
        where LIBPROP : BasicLib<LIBPROP, BD>, new()
    {

        protected UICheckBox m_useContrastColorTextCheckbox;

        protected abstract BD CurrentConfig { get; set; }

        protected override BTD[] CurrentSelectedDescriptorArray
        {
            get => CurrentConfig.m_textDescriptors;
            set => CurrentConfig.m_textDescriptors = value;
        }

        #region Awake

        protected abstract void GenerateDefaultModels();
        protected abstract string GetLibLocaleEntry();
        protected abstract void BeforeCreatingTextScrollPanel();
        protected abstract void CleanDescriptor();

        protected virtual void ExtraActionsOnStart() { }

        protected override void AwakePropEditor(out UIScrollablePanel scrollTabs, out UIHelperExtension referenceHelper)
        {
            GenerateDefaultModels();

            m_loadPropGroup = AddLibBox<LIBPROP, BD>(Locale.Get(GetLibLocaleEntry()), m_uiHelperHS,
                        out _, null,
                        out _, null,
                        out _, null,
                        (x) =>
                        {
                            CurrentConfig = XmlUtils.DefaultXmlDeserialize<BD>(XmlUtils.DefaultXmlSerialize(x));
                            Start();
                        },
                () => CurrentConfig);

            var buttonErase = (UIButton)m_uiHelperHS.AddButton(Locale.Get("K45_DTP_ERASE_CURRENT_CONFIG"), DoDeleteGroup);
            KlyteMonoUtils.LimitWidth(buttonErase, m_uiHelperHS.Self.width - 20, true);
            buttonErase.color = Color.red;

            AddDropdown(Locale.Get("K45_DTP_PROP_MODEL_SELECT"), out m_propsDropdown, m_uiHelperHS, new string[0], SetPropModel);
            BeforeCreatingTextScrollPanel();

            KlyteMonoUtils.CreateHorizontalScrollPanel(m_uiHelperHS.Self, out scrollTabs, out _, m_uiHelperHS.Self.width - 20, 40, Vector3.zero);
            referenceHelper = m_uiHelperHS;

        }

        protected override void OnTextTabStripChanged() => OnChangeTabTexts(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors?.Length ?? 0);
        protected override void DoInTextCommonTabGroupUI(UIHelperExtension groupTexts) => m_useContrastColorTextCheckbox = groupTexts.AddCheckboxLocale("K45_DTP_USE_CONTRAST_COLOR", false, SetUseContrastColor);

        protected void OnChangePropColor(Color c)
        {
            BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.PropColor = c;
            BoardGeneratorRoadNodes.Instance.SoftReset();
        }

        public void ReloadGroupLib() => ReloadLib<LIBPROP, BD>(m_loadPropGroup);
        public void ReloadTextLib() => ReloadLib<LIBTXT, BTD>(m_loadTextDD);

        private static void ReloadLib<LIB, DESC>(UIDropDown loadDD)
            where LIB : BasicLib<LIB, DESC>, new()
            where DESC : ILibable => loadDD.items = BasicLib<LIB, DESC>.Instance.List().ToArray();

        private void DoDeleteGroup()
        {
            CleanDescriptor();
            m_propsDropdown.selectedIndex = -1;
            OnChangeTabTexts(-1);

        }

        protected override void OnStart()
        {
            m_propsDropdown.selectedIndex = BoardGeneratorHighwaySigns.Instance.LoadedProps.IndexOf(CurrentConfig.PropName ?? "") + 1;
            BoardGeneratorParent<BG, BBC, D, BD, BTD>.Instance.ChangeFont(CurrentConfig.FontName);
            DTPUtils.ReloadFontsOf<BG>(m_fontSelect);
            OnChangePropColor(CurrentConfig.PropColor);
            OnChangeTabTexts(-1);
            ExtraActionsOnStart();
        }

        protected override void SetPropModel(int idx)
        {
            if (!m_isLoading && idx >= 0)
            {
                CurrentConfig.PropName = idx == 0 ? null : BoardGeneratorHighwaySigns.Instance.LoadedProps[idx - 1];
                BoardGeneratorParent<BG>.Instance.Reset();
            }
        }


        protected override void ReloadTabInfoText()
        {

            m_isLoading = true;
            EnsureTabQuantityTexts(CurrentSelectedDescriptorArray?.Length ?? -1);
            ConfigureTabsShownText(CurrentSelectedDescriptorArray?.Length ?? 0);
            m_pseudoTabTextsContainer.Self.isVisible = CurrentTabText >= 0;
            if (CurrentTabText >= 0)
            {
                LoadTabTextInfo(CurrentSelectedDescriptorArray[CurrentTabText]);
                m_loadTextDD.items = BasicLib<LIBTXT, BTD>.Instance.List().ToArray();
            }
            m_isLoading = false;
            ReloadTextLib();
            ReloadGroupLib();
        }

        protected override void AfterLoadingTabTextInfo(BTD descriptor) => m_useContrastColorTextCheckbox.isChecked = descriptor?.m_useContrastColor ?? false;
        protected override void OnDropdownTextTypeSelectionChanged(int idx) { }
        protected override void OnLoadTextLibItem() => ReloadTabInfoText();
        protected override void PostAwake() { }
        protected override bool IsTextEditionAvailable() => true;
        protected override void ReloadTabInfo() { }

        #endregion

    }


}
