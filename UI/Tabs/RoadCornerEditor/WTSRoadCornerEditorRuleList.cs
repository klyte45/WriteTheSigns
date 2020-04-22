using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Linq;
using UnityEngine;
using static Klyte.WriteTheSigns.UI.WTSEditorUILib;

namespace Klyte.WriteTheSigns.UI
{
    internal class WTSRoadCornerEditorRuleList : UICustomControl
    {
        private UIPanel MainContainer { get; set; }

        private UIButton m_new;
        private UIButton m_up;
        private UIButton m_down;
        private UIButton m_remove;
        private UIButton m_help;

        private UIScrollablePanel m_orderedRulesList;
        private int m_selectedIndex;

        public int SelectedIndex
        {
            get => m_selectedIndex; private set {
                m_selectedIndex = value;
                EventSelectionChanged?.Invoke(value);
            }
        }

        public event Action<int> EventSelectionChanged;


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

            int btnSize = 36;
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_new, CommonsSpriteNames.K45_New, OnAddItemOnList, "K45_WTS_ROADCORNER_ADDITEMLIST", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_up, CommonsSpriteNames.K45_Up, OnMoveItemUpOnList, "K45_WTS_ROADCORNER_MOVEITEMUP", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_down, CommonsSpriteNames.K45_Down, OnMoveItemDownOnList, "K45_WTS_ROADCORNER_MOVEITEMDOWN", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_remove, CommonsSpriteNames.K45_X, OnRemoveItem, "K45_WTS_ROADCORNER_REMOVEITEM", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_help, CommonsSpriteNames.K45_QuestionMark, Help_RulesList, "K45_CMNS_HELP", btnSize);

            KlyteMonoUtils.CreateUIElement(out UIPanel m_listContainer, MainContainer.transform, "previewPanel", new Vector4(0, 0, MainContainer.width, MainContainer.height - 85));
            KlyteMonoUtils.CreateScrollPanel(m_listContainer, out m_orderedRulesList, out _, m_listContainer.width - 20, m_listContainer.height);
            m_orderedRulesList.backgroundSprite = "OptionsScrollbarTrack";
            m_orderedRulesList.autoLayout = true;
            m_orderedRulesList.autoLayoutDirection = LayoutDirection.Vertical;

        }


        public void Start() => FixTabstrip();


        private UIButton AddTabButton(string tabName)
        {
            InitTabButton(m_orderedRulesList, out UIButton button, tabName, new Vector2(m_orderedRulesList.size.x, 30), null);
            button.text = tabName;
            return button;
        }

        public void FixTabstrip()
        {

            while (m_orderedRulesList.components.Count > WTSRoadNodesData.Instance.CurrentDescriptorOrder.Length)
            {
                Destroy(m_orderedRulesList.components[WTSRoadNodesData.Instance.CurrentDescriptorOrder.Length]);
                m_orderedRulesList.RemoveUIComponent(m_orderedRulesList.components[m_orderedRulesList.components.Count - 1]);
            }
            while (m_orderedRulesList.components.Count < WTSRoadNodesData.Instance.CurrentDescriptorOrder.Length)
            {
                AddTabButton("!!!").eventClicked += (x, y) =>
                {
                    SelectedIndex = x.zOrder;
                    FixTabstrip();
                };
            }
            for (int i = 0; i < WTSRoadNodesData.Instance.CurrentDescriptorOrder.Length; i++)
            {
                (m_orderedRulesList.components[i] as UIButton).text = WTSRoadNodesData.Instance.CurrentDescriptorOrder[i].SaveName;
            }
            WTSRoadNodesData.Instance.ResetBoards();
            if (SelectedIndex < 1)
            {
                m_up.Disable();
            }
            else
            {
                m_up.Enable();
            }
            if (SelectedIndex <= -1 || SelectedIndex >= WTSRoadNodesData.Instance.CurrentDescriptorOrder.Length - 1)
            {
                m_down.Disable();
            }
            else
            {
                m_down.Enable();
            }
            if (SelectedIndex < 0 || SelectedIndex >= WTSRoadNodesData.Instance.CurrentDescriptorOrder.Length)
            {
                m_remove.Disable();
            }
            else
            {
                m_remove.Enable();
            }
        }

        private void OnRemoveItem(UIComponent component, UIMouseEventParameter eventParam)
        {
            WTSRoadNodesData.Instance.CurrentDescriptorOrder = WTSRoadNodesData.Instance.CurrentDescriptorOrder.Where((x, y) => y != SelectedIndex).ToArray();
            SelectedIndex = Math.Min(SelectedIndex, WTSRoadNodesData.Instance.CurrentDescriptorOrder.Length - 1);
            FixTabstrip();
        }
        private void OnMoveItemUpOnList(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (SelectedIndex > 0 && WTSRoadNodesData.Instance.CurrentDescriptorOrder.Length > 1)
            {
                BoardInstanceRoadNodeXml temp = WTSRoadNodesData.Instance.CurrentDescriptorOrder[SelectedIndex];
                WTSRoadNodesData.Instance.CurrentDescriptorOrder[SelectedIndex] = WTSRoadNodesData.Instance.CurrentDescriptorOrder[SelectedIndex - 1];
                WTSRoadNodesData.Instance.CurrentDescriptorOrder[SelectedIndex - 1] = temp;
                SelectedIndex--;
                FixTabstrip();
            }
        }
        private void OnMoveItemDownOnList(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (SelectedIndex < WTSRoadNodesData.Instance.CurrentDescriptorOrder.Length && WTSRoadNodesData.Instance.CurrentDescriptorOrder.Length > 1)
            {
                BoardInstanceRoadNodeXml temp = WTSRoadNodesData.Instance.CurrentDescriptorOrder[SelectedIndex];
                WTSRoadNodesData.Instance.CurrentDescriptorOrder[SelectedIndex] = WTSRoadNodesData.Instance.CurrentDescriptorOrder[SelectedIndex + 1];
                WTSRoadNodesData.Instance.CurrentDescriptorOrder[SelectedIndex + 1] = temp;
                SelectedIndex++;
                FixTabstrip();
            }
        }
        private void OnAddItemOnList(UIComponent component, UIMouseEventParameter eventParam)
        {
            WTSRoadNodesData.Instance.CurrentDescriptorOrder = WTSRoadNodesData.Instance.CurrentDescriptorOrder.Union(new BoardInstanceRoadNodeXml[] { new BoardInstanceRoadNodeXml
            {
                SaveName = "New rule",
            } }).ToArray();
            SelectedIndex = WTSRoadNodesData.Instance.CurrentDescriptorOrder.Length - 1;
            FixTabstrip();
        }
        private void Help_RulesList(UIComponent component, UIMouseEventParameter eventParam) { }
        public void Update()
        {

            if (MainContainer.isVisible)
            {
                foreach (UIButton btn in m_orderedRulesList.GetComponentsInChildren<UIButton>())
                {
                    if (btn.zOrder == SelectedIndex)
                    {
                        btn.state = UIButton.ButtonState.Focused;
                    }
                    else
                    {
                        btn.state = UIButton.ButtonState.Normal;
                    }
                }
            }
        }
    }

}
