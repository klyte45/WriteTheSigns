using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using UnityEngine;

namespace Klyte.WriteTheSigns.UI
{
    internal class WTSRoadCornerEditorRuleList : UICustomControl
    {
        public UIPanel MainContainer { get; protected set; }

        public void Awake()
        {

            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.autoLayoutPadding = new RectOffset(0, 0, 4, 4);


            KlyteMonoUtils.CreateUIElement(out UIPanel m_topPanel, MainContainer.transform, "topListPanel", new UnityEngine.Vector4(0, 0, MainContainer.width, 70));
            m_topPanel.autoLayout = true;
            m_topPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            m_topPanel.wrapLayout = true;
            m_topPanel.autoLayoutPadding = new RectOffset(4, 3, 5, 5);

            KlyteMonoUtils.CreateUIElement(out UILabel m_topPanelTitle, m_topPanel.transform, "topListPanelTitle", new UnityEngine.Vector4(0, 0, m_topPanel.width, 15));
            KlyteMonoUtils.LimitWidthAndBox(m_topPanelTitle, MainContainer.width - 10, true);
            m_topPanelTitle.text = Locale.Get("K45_WTS_ROADCORNER_LISTORDERTITLE");
            m_topPanelTitle.textAlignment = UIHorizontalAlignment.Center;

            var btnSize = 36;
            KlyteMonoUtils.InitCircledButton(m_topPanel, out _, CommonsSpriteNames.K45_New, OnAddItemOnList, "K45_WTS_ROADCORNER_ADDITEMLIST", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out _, CommonsSpriteNames.K45_Up, OnMoveItemUpOnList, "K45_WTS_ROADCORNER_MOVEITEMUP", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out _, CommonsSpriteNames.K45_Down, OnMoveItemDownOnList, "K45_WTS_ROADCORNER_MOVEITEMDOWN", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out _, CommonsSpriteNames.K45_X, Help_RulesList, "K45_WTS_ROADCORNER_REMOVEITEM", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out _, CommonsSpriteNames.K45_QuestionMark, Help_RulesList, "K45_CMNS_HELP", btnSize);

            KlyteMonoUtils.CreateUIElement(out UIPanel m_listContainer, MainContainer.transform, "previewPanel", new Vector4(0, 0, MainContainer.width, MainContainer.height - 85));
            KlyteMonoUtils.CreateScrollPanel(m_listContainer, out UIScrollablePanel orderedRulesList, out _, m_listContainer.width - 20, m_listContainer.height);
            orderedRulesList.backgroundSprite = "OptionsScrollbarTrack";
            orderedRulesList.autoLayout = true;
            orderedRulesList.autoLayoutDirection = LayoutDirection.Vertical;
            
        }


        private void OnMoveItemUpOnList(UIComponent component, UIMouseEventParameter eventParam) { }
        private void OnMoveItemDownOnList(UIComponent component, UIMouseEventParameter eventParam) { }
        private void Help_RulesList(UIComponent component, UIMouseEventParameter eventParam) { }
        private void OnAddItemOnList(UIComponent component, UIMouseEventParameter eventParam) { }
    }

}
