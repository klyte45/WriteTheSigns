using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheCity.Data;
using Klyte.WriteTheCity.Utils;
using UnityEngine;
using static Klyte.WriteTheCity.UI.WTCEditorUILib;

namespace Klyte.WriteTheCity.UI
{

    internal class WTCRoadCornerEditor : UICustomControl
    {
        public static WTCRoadCornerEditor Instance { get; private set; }
        public UIPanel MainContainer { get; protected set; }


        private UIDropDown m_fontSelect;

        public void Awake()
        {
            Instance = this;

            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.autoLayoutPadding = new RectOffset(5, 5, 5, 5);

            var m_uiHelperHS = new UIHelperExtension(MainContainer);

            AddDropdown(Locale.Get("K45_WTC_FONT_ST_CORNERS"), out m_fontSelect, m_uiHelperHS, new string[0], OnSetFont);

            AddButtonInEditorRow(m_fontSelect, CommonsSpriteNames.K45_Reload, () => WTCUtils.ReloadFontsOf(m_fontSelect));
            WTCUtils.ReloadFontsOf(m_fontSelect);

            KlyteMonoUtils.CreateUIElement(out UIPanel secondaryContainer, MainContainer.transform, "SecContainer", new Vector4(0, 0, MainContainer.width, 705));
            secondaryContainer.autoLayout = true;
            secondaryContainer.autoLayoutDirection = LayoutDirection.Horizontal;
            secondaryContainer.autoLayoutPadding = new RectOffset(0, 10, 0, 0);

            KlyteMonoUtils.CreateUIElement(out UIPanel tertiaryContainer, secondaryContainer.transform, "TrcContainer", new Vector4(0, 0, secondaryContainer.width * 0.25f, secondaryContainer.height));
            tertiaryContainer.autoLayout = true;
            tertiaryContainer.autoLayoutDirection = LayoutDirection.Vertical;
            tertiaryContainer.autoLayoutPadding = new RectOffset(0, 0, 4, 4);


            KlyteMonoUtils.CreateUIElement(out UIPanel m_topPanel, tertiaryContainer.transform, "topListPanel", new UnityEngine.Vector4(0, 0, tertiaryContainer.width, 70));
            m_topPanel.autoLayout = true;
            m_topPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            m_topPanel.wrapLayout = true;
            m_topPanel.autoLayoutPadding = new RectOffset(7, 7, 5, 5);

            KlyteMonoUtils.CreateUIElement(out UILabel m_topPanelTitle, m_topPanel.transform, "topListPanelTitle", new UnityEngine.Vector4(0, 0, m_topPanel.width, 15));
            KlyteMonoUtils.LimitWidthAndBox(m_topPanelTitle, tertiaryContainer.width - 10, true);
            m_topPanelTitle.text = Locale.Get("K45_WTC_ROADCORNER_LISTORDERTITLE");
            m_topPanelTitle.textAlignment = UIHorizontalAlignment.Center;

            KlyteMonoUtils.InitCircledButton(m_topPanel, out _, CommonsSpriteNames.K45_New, OnAddItemOnList, "K45_WTC_ROADCORNER_ADDITEMLIST");
            KlyteMonoUtils.InitCircledButton(m_topPanel, out _, CommonsSpriteNames.K45_Up, OnMoveItemUpOnList, "K45_WTC_ROADCORNER_MOVEITEMUP");
            KlyteMonoUtils.InitCircledButton(m_topPanel, out _, CommonsSpriteNames.K45_Down, OnMoveItemDownOnList, "K45_WTC_ROADCORNER_MOVEITEMDOWN");
            KlyteMonoUtils.InitCircledButton(m_topPanel, out _, CommonsSpriteNames.K45_X, OnRemoveItemOnList, "K45_WTC_ROADCORNER_REMOVEITEM");

            KlyteMonoUtils.CreateUIElement(out UIPanel m_listContainer, tertiaryContainer.transform, "previewPanel", new UnityEngine.Vector4(0, 0, tertiaryContainer.width, tertiaryContainer.height - 320));
            KlyteMonoUtils.CreateScrollPanel(m_listContainer, out UIScrollablePanel orderedRulesList, out _, m_listContainer.width - 20, m_listContainer.height);
            orderedRulesList.backgroundSprite = "OptionsScrollbarTrack";
            orderedRulesList.autoLayout = true;
            orderedRulesList.autoLayoutDirection = LayoutDirection.Vertical;

            KlyteMonoUtils.CreateUIElement(out UIPanel m_previewPanel, tertiaryContainer.transform, "previewPanel", new UnityEngine.Vector4(0, 0, tertiaryContainer.width - 10, 220));
            m_previewPanel.backgroundSprite = "GenericPanel";
            m_previewPanel.autoLayout = true;

            KlyteMonoUtils.CreateUIElement(out UITextureSprite m_preview, m_previewPanel.transform, "preview", new UnityEngine.Vector4(0, 0, m_previewPanel.width, m_previewPanel.height));
            m_preview.eventMouseWheel += ChangeViewZoom;
            m_preview.eventMouseMove += OnMouseMove;

            KlyteMonoUtils.CreateUIElement(out UIPanel editorPanel, secondaryContainer.transform, "EditPanel", new Vector4(0, 0, secondaryContainer.width * 0.75f - 10, secondaryContainer.height));
            editorPanel.autoLayout = true;
            editorPanel.autoLayoutDirection = LayoutDirection.Vertical;
            editorPanel.autoLayoutPadding = new RectOffset(0, 0, 4, 4);

            var m_editorHelper = new UIHelperExtension(editorPanel);

            AddTextField(Locale.Get("K45_WTC_ROADCORNER_NAME"), out UITextField name, m_editorHelper, OnSetName);

            m_editorHelper.AddSpace(5);
            m_editorHelper.AddCheckbox(Locale.Get("K45_WTC_ROADCORNER_USEDISTRICTCOLOR"), false, OnChangeUseDistrictColor);
            m_editorHelper.AddCheckbox(Locale.Get("K45_WTC_ROADCORNER_APPLYABBREVIATIONS_FULLNAME"), false, OnChangeApplyAbbreviationsFullName);
            m_editorHelper.AddCheckbox(Locale.Get("K45_WTC_ROADCORNER_APPLYABBREVIATIONS_SUFFIX"), false, OnChangeApplyAbbreviationsSuffix);
            AddSlider(Locale.Get("K45_WTC_ROADCORNER_SPAWN_CHANCE"), out UISlider m_spawnChance, m_editorHelper, OnChangeSpawnChance, 0, 255, 1);

            m_editorHelper.AddSpace(5);
            m_editorHelper.AddCheckbox(Locale.Get("K45_WTC_ROADCORNER_USEASWHITELIST"), false, OnSetWhiteList);
            m_editorHelper.AddCheckbox(Locale.Get("K45_WTC_ROADCORNER_USEASBLACKLIST"), false, OnSetBlackList);
            AddDropdown(Locale.Get("K45_WTC_ROADCORNER_CLASSESDD"), out UIDropDown m_classesDD, m_editorHelper, new string[0], (x) => { });
            AddButtonInEditorRow(m_classesDD, CommonsSpriteNames.K45_Plus, AddToSelectionList);
            KlyteMonoUtils.CreateUIElement(out UIPanel m_listClassesContainer, m_editorHelper.Self.transform, "previewPanel", new UnityEngine.Vector4(0, 0, m_editorHelper.Self.width - 10, 200));
            KlyteMonoUtils.CreateScrollPanel(m_listClassesContainer, out UIScrollablePanel m_selectedClassesList, out _, m_listClassesContainer.width - 25, m_listClassesContainer.height);
            m_selectedClassesList.backgroundSprite = "OptionsScrollbarTrack";
            m_selectedClassesList.autoLayout = true;
            m_selectedClassesList.autoLayoutDirection = LayoutDirection.Horizontal;
            m_selectedClassesList.autoLayoutPadding = new RectOffset(5, 5, 5, 5);
            m_editorHelper.AddSpace(5);

            AddDropdown(Locale.Get("K45_WTC_ROADCORNER_PROPLAYOUT"), out UIDropDown m_propLayoutSelect, m_editorHelper, new string[0], OnPropLayoutChange);
            AddVector3Field(Locale.Get("K45_WTC_ROADCORNER_POSITION"), out UITextField[] m_position, m_editorHelper, OnPositionChanged);
            AddVector3Field(Locale.Get("K45_WTC_ROADCORNER_ROTATION"), out UITextField[] m_rotation, m_editorHelper, OnRotationChanged);
            AddVector3Field(Locale.Get("K45_WTC_ROADCORNER_SCALE"), out UITextField[] m_scale, m_editorHelper, OnScaleChanged);

        }

        private void OnMoveItemUpOnList(UIComponent component, UIMouseEventParameter eventParam) { }
        private void OnMoveItemDownOnList(UIComponent component, UIMouseEventParameter eventParam) { }
        private void OnRemoveItemOnList(UIComponent component, UIMouseEventParameter eventParam) { }
        private void OnAddItemOnList(UIComponent component, UIMouseEventParameter eventParam) { }
        private void OnMouseMove(UIComponent component, UIMouseEventParameter eventParam) { }
        private void ChangeViewZoom(UIComponent component, UIMouseEventParameter eventParam) { }

        private void AddToSelectionList() { }
        private void OnSetBlackList(bool isChecked) { }
        private void OnSetWhiteList(bool isChecked) { }
        private void OnRotationChanged(Vector3 obj) { }
        private void OnScaleChanged(Vector3 obj) { }
        private void OnPositionChanged(Vector3 obj) { }
        private void OnPropLayoutChange(int sel) { }
        private void OnChangeUseDistrictColor(bool isChecked) { }
        private void OnChangeSpawnChance(float val) { }
        private void OnChangeApplyAbbreviationsSuffix(bool isChecked) { }
        private void OnChangeApplyAbbreviationsFullName(bool isChecked) { }
        private void OnSetName(string text) { }

        private void OnSetFont(int sel)
        {
            if (sel > 0)
            {
                WTCRoadNodesData.Instance.DefaultFont = m_fontSelect.selectedValue;
            }
            else if (sel == 0)
            {
                WTCRoadNodesData.Instance.DefaultFont = null;
            }
        }



    }

}
