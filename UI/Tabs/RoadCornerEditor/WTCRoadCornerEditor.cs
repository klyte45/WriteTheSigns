using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheCity.Data;
using Klyte.WriteTheCity.Utils;
using Klyte.WriteTheCity.Xml;
using System;
using System.Linq;
using UnityEngine;
using static Klyte.WriteTheCity.UI.WTCEditorUILib;

namespace Klyte.WriteTheCity.UI
{

    internal class WTCRoadCornerEditor : UICustomControl
    {
        public static WTCRoadCornerEditor Instance { get; private set; }
        public UIPanel MainContainer { get; protected set; }


        private UIDropDown m_fontSelect;
        private bool m_loadingAbbreviations;

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
            AddDropdown(Locale.Get("K45_WTC_ABBREVIATION_FILE"), out UIDropDown m_abbriviationFile, m_uiHelperHS, new string[0], OnSetAbbreviationFile);
            AddButtonInEditorRow(m_abbriviationFile, CommonsSpriteNames.K45_Reload, () => ReloadAbbreviations(m_abbriviationFile));
            ReloadAbbreviations(m_abbriviationFile);
            AddDropdown(Locale.Get("K45_WTC_CUSTOM_NAME_EXTRACTION_QUALIFIER"), out UIDropDown m_qualifierExtractionDropdown, m_uiHelperHS, Enum.GetNames(typeof(RoadQualifierExtractionMode)).Select(x => Locale.Get($"K45_WTC_RoadQualifierExtractionMode", x)).ToArray(), SetRoadQualifierExtractionMode);


            KlyteMonoUtils.CreateUIElement(out UIPanel secondaryContainer, MainContainer.transform, "SecContainer", new Vector4(0, 0, MainContainer.width, 605));
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
            m_topPanel.autoLayoutPadding = new RectOffset(4, 3, 5, 5);

            KlyteMonoUtils.CreateUIElement(out UILabel m_topPanelTitle, m_topPanel.transform, "topListPanelTitle", new UnityEngine.Vector4(0, 0, m_topPanel.width, 15));
            KlyteMonoUtils.LimitWidthAndBox(m_topPanelTitle, tertiaryContainer.width - 10, true);
            m_topPanelTitle.text = Locale.Get("K45_WTC_ROADCORNER_LISTORDERTITLE");
            m_topPanelTitle.textAlignment = UIHorizontalAlignment.Center;

            var btnSize = 36;
            KlyteMonoUtils.InitCircledButton(m_topPanel, out _, CommonsSpriteNames.K45_New, OnAddItemOnList, "K45_WTC_ROADCORNER_ADDITEMLIST",btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out _, CommonsSpriteNames.K45_Up, OnMoveItemUpOnList, "K45_WTC_ROADCORNER_MOVEITEMUP", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out _, CommonsSpriteNames.K45_Down, OnMoveItemDownOnList, "K45_WTC_ROADCORNER_MOVEITEMDOWN", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out _, CommonsSpriteNames.K45_X, Help_RulesList, "K45_WTC_ROADCORNER_REMOVEITEM", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out _, CommonsSpriteNames.K45_QuestionMark, Help_RulesList, "K45_CMNS_HELP", btnSize);

            KlyteMonoUtils.CreateUIElement(out UIPanel m_listContainer, tertiaryContainer.transform, "previewPanel", new UnityEngine.Vector4(0, 0, tertiaryContainer.width, tertiaryContainer.height - 85));
            KlyteMonoUtils.CreateScrollPanel(m_listContainer, out UIScrollablePanel orderedRulesList, out _, m_listContainer.width - 20, m_listContainer.height);
            orderedRulesList.backgroundSprite = "OptionsScrollbarTrack";
            orderedRulesList.autoLayout = true;
            orderedRulesList.autoLayoutDirection = LayoutDirection.Vertical;

            KlyteMonoUtils.CreateUIElement(out UIPanel editorPanel, secondaryContainer.transform, "EditPanel", new Vector4(0, 0, secondaryContainer.width * 0.75f - 35, secondaryContainer.height));
            editorPanel.gameObject.AddComponent<WTCRoadCornerEditorDetailTabs>();

        }
        private void ReloadAbbreviations(UIDropDown m_abbriviationFile)
        {
            m_loadingAbbreviations = true;
            WriteTheCityMod.Controller.ReloadAbbreviationFiles();
            m_abbriviationFile.items = new string[] { Locale.Get("K45_WTC_NO_ABBREVIATION_FILE_OPTION") }.Union(WriteTheCityMod.Controller.AbbreviationFiles.Keys.OrderBy(x => x)).ToArray();
            m_abbriviationFile.selectedValue = WTCRoadNodesData.Instance.AbbreviationFile;
            m_loadingAbbreviations = false;
        }
        private void SetRoadQualifierExtractionMode(int sel) { }

        private void OnSetAbbreviationFile(int sel) { }

        private void OnMoveItemUpOnList(UIComponent component, UIMouseEventParameter eventParam) { }
        private void OnMoveItemDownOnList(UIComponent component, UIMouseEventParameter eventParam) { }
        private void Help_RulesList(UIComponent component, UIMouseEventParameter eventParam) { }
        private void OnAddItemOnList(UIComponent component, UIMouseEventParameter eventParam) { }

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
