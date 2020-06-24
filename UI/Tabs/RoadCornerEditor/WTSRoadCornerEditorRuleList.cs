using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Interfaces;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Libraries;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.WriteTheSigns.UI
{
    internal class WTSRoadCornerEditorRuleList : UICustomControl
    {
        private UIPanel MainContainer { get; set; }

        private UIButton m_new;
        private UIButton m_up;
        private UIButton m_down;
        private UIButton m_remove;
        private UIButton m_import;
        private UIButton m_export;
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


            KlyteMonoUtils.CreateUIElement(out UIPanel m_topPanel, MainContainer.transform, "topListPanel", new UnityEngine.Vector4(0, 0, MainContainer.width, 111));
            m_topPanel.autoLayout = true;
            m_topPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            m_topPanel.wrapLayout = true;
            m_topPanel.autoLayoutPadding = new RectOffset(8, 8, 5, 5);

            KlyteMonoUtils.CreateUIElement(out UILabel m_topPanelTitle, m_topPanel.transform, "topListPanelTitle", new UnityEngine.Vector4(0, 0, m_topPanel.width - 16, 15));
            KlyteMonoUtils.LimitWidthAndBox(m_topPanelTitle, m_topPanel.width - 16, true);
            m_topPanelTitle.text = Locale.Get("K45_WTS_ROADCORNER_LISTORDERTITLE");
            m_topPanelTitle.textAlignment = UIHorizontalAlignment.Center;

            int btnSize = 36;
            KlyteMonoUtils.CreateUIElement<UILabel>(out UILabel spacing, m_topPanel.transform, "_", new Vector4(0, 0, btnSize / 2, btnSize));
            spacing.textScale = 0;
            spacing.width = btnSize / 4;
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_import, CommonsSpriteNames.K45_Import, OnImportData, "K45_WTS_ROADCORNER_IMPORTDATA", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_export, CommonsSpriteNames.K45_Export, (x, y) => OnExportData(), "K45_WTS_ROADCORNER_EXPORTDATA", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_help, CommonsSpriteNames.K45_QuestionMark, Help_RulesList, "K45_CMNS_HELP", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_new, CommonsSpriteNames.K45_New, OnAddItemOnList, "K45_WTS_ROADCORNER_ADDITEMLIST", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_up, CommonsSpriteNames.K45_Up, OnMoveItemUpOnList, "K45_WTS_ROADCORNER_MOVEITEMUP", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_down, CommonsSpriteNames.K45_Down, OnMoveItemDownOnList, "K45_WTS_ROADCORNER_MOVEITEMDOWN", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_remove, CommonsSpriteNames.K45_X, OnRemoveItem, "K45_WTS_ROADCORNER_REMOVEITEM", btnSize);

            KlyteMonoUtils.CreateUIElement(out UIPanel m_listContainer, MainContainer.transform, "previewPanel", new Vector4(0, 0, MainContainer.width, MainContainer.height - 126));
            KlyteMonoUtils.CreateScrollPanel(m_listContainer, out m_orderedRulesList, out _, m_listContainer.width - 20, m_listContainer.height);
            m_orderedRulesList.backgroundSprite = "OptionsScrollbarTrack";
            m_orderedRulesList.autoLayout = true;
            m_orderedRulesList.autoLayoutDirection = LayoutDirection.Vertical;

        }

        private void OnExportData(string defaultText = null)
        {
            K45DialogControl.ShowModalPromptText(new K45DialogControl.BindProperties
            {
                defaultTextFieldContent = defaultText,
                title = Locale.Get("K45_WTS_ROADCORNER_EXPORTRULESTITLE"),
                message = Locale.Get("K45_WTS_ROADCORNER_EXPORTRULESMESSAGE"),
                showButton1 = true,
                textButton1 = Locale.Get("SAVE"),
                showButton2 = true,
                textButton2 = Locale.Get("CANCEL"),
            }, (ret, text) =>
             {
                 if (ret == 1)
                 {
                     if (text.IsNullOrWhiteSpace())
                     {
                         K45DialogControl.UpdateCurrentMessage($"<color #FFFF00>{Locale.Get("K45_WTS_ROADCORNER_EXPORTRULESMESSAGE_INVALIDNAME")}</color>\n\n{Locale.Get("K45_WTS_ROADCORNER_EXPORTRULESMESSAGE")}");
                         return false;
                     }
                     WTSLibRoadCornerRuleList.Reload();
                     var currentData = WTSLibRoadCornerRuleList.Instance.Get(text);
                     if (currentData == null)
                     {
                         AddCurrentListToLibrary(text);
                     }
                     else
                     {
                         K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                         {
                             title = Locale.Get("K45_WTS_ROADCORNER_EXPORTRULESTITLE"),
                             message = string.Format(Locale.Get("K45_WTS_ROADCORNER_EXPORTRULESMESSAGE_CONFIRMOVERWRITE"), text),
                             showButton1 = true,
                             textButton1 = Locale.Get("YES"),
                             showButton2 = true,
                             textButton2 = Locale.Get("NO"),
                         }, (x) =>
                         {
                             if (x == 1)
                             {
                                 AddCurrentListToLibrary(text);
                             }
                             else
                             {
                                 OnExportData(text);
                             }
                             return true;
                         });
                     }
                 }
                 return true;
             });

        }

        private static void AddCurrentListToLibrary(string text)
        {
            WTSLibRoadCornerRuleList.Reload();
            var newItem = new ILibableAsContainer<BoardInstanceRoadNodeXml>
            {
                Data = WTSRoadNodesData.Instance.DescriptorRulesOrderXml
            };
            WTSLibRoadCornerRuleList.Instance.Add(text, ref newItem);
            K45DialogControl.ShowModal(new K45DialogControl.BindProperties
            {
                title = Locale.Get("K45_WTS_ROADCORNER_EXPORTRULESTITLE"),
                message = string.Format(Locale.Get("K45_WTS_ROADCORNER_EXPORTRULESMESSAGE_SUCCESSSAVEDATA"), WTSLibRoadCornerRuleList.Instance.DefaultXmlFileBaseFullPath),
                showButton1 = true,
                textButton1 = Locale.Get("EXCEPTION_OK"),
                showButton2 = true,
                textButton2 = Locale.Get("K45_CMNS_GOTO_FILELOC"),
            }, (x) =>
            {
                if (x == 2)
                {
                    ColossalFramework.Utils.OpenInFileBrowser(WTSLibRoadCornerRuleList.Instance.DefaultXmlFileBaseFullPath);
                    return false;
                }
                return true;
            });
        }

        private void OnImportData(UIComponent component, UIMouseEventParameter eventParam)
        {
            WTSLibRoadCornerRuleList.Reload();
            string[] optionList = WTSLibRoadCornerRuleList.Instance.List().ToArray();
            if (optionList.Length > 0)
            {
                K45DialogControl.ShowModalPromptDropDown(new K45DialogControl.BindProperties
                {
                    title = Locale.Get("K45_WTS_ROADCORNER_IMPORTRULESTITLE"),
                    message = Locale.Get("K45_WTS_ROADCORNER_IMPORTRULESMESSAGE"),
                    showButton1 = true,
                    textButton1 = Locale.Get("LOAD"),
                    showButton2 = true,
                    textButton2 = Locale.Get("CANCEL"),
                }, optionList, 0, (ret, idx, selText) =>
                {
                    if (ret == 1)
                    {
                        var newConfig = WTSLibRoadCornerRuleList.Instance.Get(selText);
                        WTSRoadNodesData.Instance.DescriptorRulesOrderXml = newConfig.Data;
                        FixTabstrip();
                        SelectedIndex = -1;
                    }
                    return true;
                });
            }
            else
            {
                K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                {
                    title = Locale.Get("K45_WTS_ROADCORNER_IMPORTRULESTITLE"),
                    message = Locale.Get("K45_WTS_ROADCORNER_IMPORTRULESMESSAGE_NOENTRIESFOUND"),
                    showButton1 = true,
                    textButton1 = Locale.Get("EXCEPTION_OK"),
                    showButton2 = true,
                    textButton2 = Locale.Get("K45_CMNS_GOTO_FILELOC"),
                }, (x) =>
                {
                    if (x == 2)
                    {
                        WTSLibRoadCornerRuleList.Instance.EnsureFileExists();
                        ColossalFramework.Utils.OpenInFileBrowser(WTSLibRoadCornerRuleList.Instance.DefaultXmlFileBaseFullPath);
                        return false;
                    }
                    return true;
                });
            }

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

            while (m_orderedRulesList.components.Count > WTSRoadNodesData.Instance.DescriptorRulesOrder.Length)
            {
                Destroy(m_orderedRulesList.components[WTSRoadNodesData.Instance.DescriptorRulesOrder.Length]);
                m_orderedRulesList.RemoveUIComponent(m_orderedRulesList.components[m_orderedRulesList.components.Count - 1]);
            }
            while (m_orderedRulesList.components.Count < WTSRoadNodesData.Instance.DescriptorRulesOrder.Length)
            {
                AddTabButton("!!!").eventClicked += (x, y) =>
                {
                    SelectedIndex = x.zOrder;
                    FixTabstrip();
                };
            }
            for (int i = 0; i < WTSRoadNodesData.Instance.DescriptorRulesOrder.Length; i++)
            {
                (m_orderedRulesList.components[i] as UIButton).text = WTSRoadNodesData.Instance.DescriptorRulesOrder[i].SaveName;
            }
            WTSRoadNodesData.Instance.ResetCacheDescriptors();
            if (SelectedIndex < 1)
            {
                m_up.Disable();
            }
            else
            {
                m_up.Enable();
            }
            if (SelectedIndex <= -1 || SelectedIndex >= WTSRoadNodesData.Instance.DescriptorRulesOrder.Length - 1)
            {
                m_down.Disable();
            }
            else
            {
                m_down.Enable();
            }
            if (SelectedIndex < 0 || SelectedIndex >= WTSRoadNodesData.Instance.DescriptorRulesOrder.Length)
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
            WTSRoadNodesData.Instance.DescriptorRulesOrder = WTSRoadNodesData.Instance.DescriptorRulesOrder.Where((x, y) => y != SelectedIndex).ToArray();
            SelectedIndex = Math.Min(SelectedIndex, WTSRoadNodesData.Instance.DescriptorRulesOrder.Length - 1);
            FixTabstrip();
        }
        private void OnMoveItemUpOnList(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (SelectedIndex > 0 && WTSRoadNodesData.Instance.DescriptorRulesOrder.Length > 1)
            {
                BoardInstanceRoadNodeXml temp = WTSRoadNodesData.Instance.DescriptorRulesOrder[SelectedIndex];
                WTSRoadNodesData.Instance.DescriptorRulesOrder[SelectedIndex] = WTSRoadNodesData.Instance.DescriptorRulesOrder[SelectedIndex - 1];
                WTSRoadNodesData.Instance.DescriptorRulesOrder[SelectedIndex - 1] = temp;
                SelectedIndex--;
                FixTabstrip();
            }
        }
        private void OnMoveItemDownOnList(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (SelectedIndex < WTSRoadNodesData.Instance.DescriptorRulesOrder.Length && WTSRoadNodesData.Instance.DescriptorRulesOrder.Length > 1)
            {
                BoardInstanceRoadNodeXml temp = WTSRoadNodesData.Instance.DescriptorRulesOrder[SelectedIndex];
                WTSRoadNodesData.Instance.DescriptorRulesOrder[SelectedIndex] = WTSRoadNodesData.Instance.DescriptorRulesOrder[SelectedIndex + 1];
                WTSRoadNodesData.Instance.DescriptorRulesOrder[SelectedIndex + 1] = temp;
                SelectedIndex++;
                FixTabstrip();
            }
        }
        private void OnAddItemOnList(UIComponent component, UIMouseEventParameter eventParam)
        {
            WTSRoadNodesData.Instance.DescriptorRulesOrder = WTSRoadNodesData.Instance.DescriptorRulesOrder.Union(new BoardInstanceRoadNodeXml[] { new BoardInstanceRoadNodeXml
            {
                SaveName = "New rule",
            } }).ToArray();
            SelectedIndex = WTSRoadNodesData.Instance.DescriptorRulesOrder.Length - 1;
            FixTabstrip();
        }
        private void Help_RulesList(UIComponent component, UIMouseEventParameter eventParam) => K45DialogControl.ShowModalHelp("NodeRulesEditor.General", Locale.Get("K45_WTS_NODERULESEDITOR_HELPTITLE"), 0);
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
