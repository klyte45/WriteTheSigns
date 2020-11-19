using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
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
    internal class WTSOnNetLayoutEditorPropList : UICustomControl
    {
        public UIPanel MainContainer { get; private set; }

        private UIButton m_new;
        private UIButton m_up;
        private UIButton m_down;
        private UIButton m_remove;
        private UIButton m_import;
        private UIButton m_export;
        private UIButton m_help;

        private UIScrollablePanel m_orderedRulesList;
        private UITemplateList<UIButton> m_tabs;
        private int m_selectedIndex;

        public ref OnNetInstanceCacheContainerXml CurrentPropLayout
        {
            get {
                if (m_selectedIndex >= 0 && m_selectedIndex < (CurrentEdited?.BoardsData?.Length ?? 0))
                {
                    return ref CurrentEdited.BoardsData[m_selectedIndex];
                }
                else
                {
                    nullref = null;
                    return ref nullref;
                }
            }
        }

        public int SelectedIndex
        {
            get => m_selectedIndex; private set {
                m_selectedIndex = value;
                if (value >= 0 && value < BoardsData.Length)
                {
                    EventSelectionChanged?.Invoke(ref BoardsData[value]);
                }
                else
                {
                    EventSelectionChanged?.Invoke(ref nullref);
                }
            }
        }

        private OnNetInstanceCacheContainerXml nullref = null;

        public delegate void OnChangeTab(ref OnNetInstanceCacheContainerXml descriptor);

        public event OnChangeTab EventSelectionChanged;

        private OnNetInstanceCacheContainerXml[] BoardsData => CurrentEdited?.BoardsData ?? m_zeroedInstances;
        private readonly OnNetInstanceCacheContainerXml[] m_zeroedInstances = new OnNetInstanceCacheContainerXml[0];

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
            m_topPanelTitle.text = Locale.Get("K45_WTS_SEGMENT_LISTORDERTITLE");
            m_topPanelTitle.textAlignment = UIHorizontalAlignment.Center;

            int btnSize = 36;
            KlyteMonoUtils.CreateUIElement<UILabel>(out UILabel spacing, m_topPanel.transform, "_", new Vector4(0, 0, btnSize / 2, btnSize));
            spacing.textScale = 0;
            spacing.width = btnSize / 4;
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_import, CommonsSpriteNames.K45_Import, OnImportData, "K45_WTS_SEGMENT_IMPORTDATA", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_export, CommonsSpriteNames.K45_Export, (x, y) => OnExportData(), "K45_WTS_SEGMENT_EXPORTDATA", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_help, CommonsSpriteNames.K45_QuestionMark, Help_RulesList, "K45_CMNS_HELP", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_new, CommonsSpriteNames.K45_New, OnAddItemOnList, "K45_WTS_SEGMENT_ADDITEMLIST", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_up, CommonsSpriteNames.K45_Up, OnMoveItemUpOnList, "K45_WTS_SEGMENT_MOVEITEMUP", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_down, CommonsSpriteNames.K45_Down, OnMoveItemDownOnList, "K45_WTS_SEGMENT_MOVEITEMDOWN", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_remove, CommonsSpriteNames.K45_X, OnRemoveItem, "K45_WTS_SEGMENT_REMOVEITEM", btnSize);

            KlyteMonoUtils.CreateUIElement(out UIPanel m_listContainer, MainContainer.transform, "previewPanel", new Vector4(0, 0, MainContainer.width, MainContainer.height - 126));
            KlyteMonoUtils.CreateScrollPanel(m_listContainer, out m_orderedRulesList, out _, m_listContainer.width - 20, m_listContainer.height);
            m_orderedRulesList.backgroundSprite = "OptionsScrollbarTrack";
            m_orderedRulesList.autoLayout = true;
            m_orderedRulesList.autoLayoutDirection = LayoutDirection.Vertical;

            m_tabs = new UITemplateList<UIButton>(m_orderedRulesList, TAB_TEMPLATE_NAME);

            CreateTabTemplate();
        }

        private void OnExportData(string defaultText = null)
        {
            K45DialogControl.ShowModalPromptText(new K45DialogControl.BindProperties
            {
                defaultTextFieldContent = defaultText,
                title = Locale.Get("K45_WTS_SEGMENT_EXPORTRULESTITLE"),
                message = Locale.Get("K45_WTS_SEGMENT_EXPORTRULESMESSAGE"),
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
                         K45DialogControl.UpdateCurrentMessage($"<color #FFFF00>{Locale.Get("K45_WTS_SEGMENT_EXPORTRULESMESSAGE_INVALIDNAME")}</color>\n\n{Locale.Get("K45_WTS_SEGMENT_EXPORTRULESMESSAGE")}");
                         return false;
                     }
                     WTSLibOnNetPropLayoutList.Reload();
                     var currentData = WTSLibOnNetPropLayoutList.Instance.Get(text);
                     if (currentData == null)
                     {
                         AddCurrentListToLibrary(text);
                     }
                     else
                     {
                         K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                         {
                             title = Locale.Get("K45_WTS_SEGMENT_EXPORTRULESTITLE"),
                             message = string.Format(Locale.Get("K45_WTS_SEGMENT_EXPORTRULESMESSAGE_CONFIRMOVERWRITE"), text),
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

        private void AddCurrentListToLibrary(string text)
        {
            WTSLibOnNetPropLayoutList.Reload();
            var newItem = new ExportableBoardInstanceOnNetListXml { Instances = CurrentEdited.BoardsData.Select((x) => XmlUtils.DefaultXmlDeserialize<BoardInstanceOnNetXml>(XmlUtils.DefaultXmlSerialize(x))).ToArray(), Layouts = CurrentEdited.LocalLayouts };
            WTSLibOnNetPropLayoutList.Instance.Add(text, ref newItem);
            K45DialogControl.ShowModal(new K45DialogControl.BindProperties
            {
                title = Locale.Get("K45_WTS_SEGMENT_EXPORTRULESTITLE"),
                message = string.Format(Locale.Get("K45_WTS_SEGMENT_EXPORTRULESMESSAGE_SUCCESSSAVEDATA"), WTSLibOnNetPropLayoutList.Instance.DefaultXmlFileBaseFullPath),
                showButton1 = true,
                textButton1 = Locale.Get("EXCEPTION_OK"),
                showButton2 = true,
                textButton2 = Locale.Get("K45_CMNS_GOTO_FILELOC"),
            }, (x) =>
            {
                if (x == 2)
                {
                    ColossalFramework.Utils.OpenInFileBrowser(WTSLibOnNetPropLayoutList.Instance.DefaultXmlFileBaseFullPath);
                    return false;
                }
                return true;
            });
        }

        private void OnImportData(UIComponent component, UIMouseEventParameter eventParam)
        {
            WTSLibOnNetPropLayoutList.Reload();
            string[] optionList = WTSLibOnNetPropLayoutList.Instance.List().ToArray();
            if (optionList.Length > 0)
            {
                K45DialogControl.ShowModalPromptDropDown(new K45DialogControl.BindProperties
                {
                    title = Locale.Get("K45_WTS_SEGMENT_IMPORTRULESTITLE"),
                    message = Locale.Get("K45_WTS_SEGMENT_IMPORTRULESMESSAGE"),
                    showButton1 = true,
                    textButton1 = Locale.Get("GOALSPANEL_ADD"),
                    showButton2 = true,
                    textButton2 = Locale.Get("CONTENTMANAGER_REPLACE"),
                    showButton3 = true,
                    textButton3 = Locale.Get("CANCEL"),
                }, optionList, 0, (ret, idx, selText) =>
                {
                    if (ret == 1 || ret == 2)
                    {
                        var newConfig = WTSLibOnNetPropLayoutList.Instance.Get(selText);

                        var newEntries = XmlUtils.DefaultXmlDeserialize<OnNetInstanceCacheContainerXml[]>(XmlUtils.DefaultXmlSerialize(newConfig.Instances).Replace(typeof(BoardInstanceOnNetXml).Name, typeof(OnNetInstanceCacheContainerXml).Name));
                        if (ret == 1)
                        {
                            CurrentEdited.BoardsData = CurrentEdited.BoardsData.Union(newEntries).ToArray();
                        }
                        else
                        {
                            CurrentEdited.BoardsData = newEntries;
                        }
                        newConfig.Layouts.ForEach(x =>
                        {
                            if (WTSPropLayoutData.Instance.Get(x.Key) == null)
                            {
                                var value = x.Value;
                                WTSPropLayoutData.Instance.Add(x.Key, ref value);
                            }
                        });
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
                    title = Locale.Get("K45_WTS_SEGMENT_IMPORTRULESTITLE"),
                    message = Locale.Get("K45_WTS_SEGMENT_IMPORTRULESMESSAGE_NOENTRIESFOUND"),
                    showButton1 = true,
                    textButton1 = Locale.Get("EXCEPTION_OK"),
                    showButton2 = true,
                    textButton2 = Locale.Get("K45_CMNS_GOTO_FILELOC"),
                }, (x) =>
                {
                    if (x == 2)
                    {
                        WTSLibOnNetPropLayoutList.Instance.EnsureFileExists();
                        ColossalFramework.Utils.OpenInFileBrowser(WTSLibOnNetPropLayoutList.Instance.DefaultXmlFileBaseFullPath);
                        return false;
                    }
                    return true;
                });
            }

        }

        public void Start() => WTSOnNetLayoutEditor.Instance.EventOnSegmentSelectionChanged += (x) =>
        {
            CurrentEdited = x;
            SelectedIndex = -1;
            Dirty = true;
        };

        private const string TAB_TEMPLATE_NAME = "K45_WTS_TabTemplateSegment";

        private void CreateTabTemplate()
        {
            var go = new GameObject();

            InitTabButton(go, out UIButton button, "AAA", new Vector2(m_orderedRulesList.size.x, 30), null);
            UITemplateUtils.GetTemplateDict()[TAB_TEMPLATE_NAME] = button;
        }

        public void FixTabstrip()
        {
            m_tabs.SetItemCount(BoardsData.Length);
            for (int i = 0; i < BoardsData.Length; i++)
            {
                var but = m_tabs.items[i];
                if (but.stringUserData.IsNullOrWhiteSpace())
                {
                    but.eventClicked += (x, y) =>
                    {
                        SelectedIndex = x.zOrder;
                        FixTabstrip();
                    };
                    but.stringUserData = "A";
                }
                but.text = BoardsData[i]?.SaveName;
            }

            if (SelectedIndex < 1)
            {
                m_up.Disable();
            }
            else
            {
                m_up.Enable();
            }
            if (SelectedIndex <= -1 || SelectedIndex >= BoardsData.Length - 1)
            {
                m_down.Disable();
            }
            else
            {
                m_down.Enable();
            }
            if (SelectedIndex < 0 || SelectedIndex >= BoardsData.Length)
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
            CurrentEdited.BoardsData = CurrentEdited.BoardsData.Where((x, y) => y != SelectedIndex).ToArray();
            SelectedIndex = Math.Min(SelectedIndex, (BoardsData.Length) - 1);
            FixTabstrip();
        }
        private void OnMoveItemUpOnList(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (SelectedIndex > 0 && BoardsData.Length > 1)
            {
                OnNetInstanceCacheContainerXml temp = CurrentEdited.BoardsData[SelectedIndex];
                CurrentEdited.BoardsData[SelectedIndex] = CurrentEdited.BoardsData[SelectedIndex - 1];
                CurrentEdited.BoardsData[SelectedIndex - 1] = temp;
                SelectedIndex--;
                FixTabstrip();
            }
        }
        private void OnMoveItemDownOnList(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (SelectedIndex < BoardsData.Length && BoardsData.Length > 1)
            {
                OnNetInstanceCacheContainerXml temp = CurrentEdited.BoardsData[SelectedIndex];
                CurrentEdited.BoardsData[SelectedIndex] = CurrentEdited.BoardsData[SelectedIndex + 1];
                CurrentEdited.BoardsData[SelectedIndex + 1] = temp;
                SelectedIndex++;
                FixTabstrip();
            }
        }
        private void OnAddItemOnList(UIComponent component, UIMouseEventParameter eventParam)
        {
            CurrentEdited.BoardsData = (CurrentEdited.BoardsData ?? new OnNetInstanceCacheContainerXml[0]).Union(new OnNetInstanceCacheContainerXml[] { new OnNetInstanceCacheContainerXml
            {
                SaveName = "New layout",
            } }).ToArray();
            SelectedIndex = BoardsData.Length - 1;
            FixTabstrip();
        }
        private void Help_RulesList(UIComponent component, UIMouseEventParameter eventParam) => K45DialogControl.ShowModalHelp("SegmentLayouts.General", Locale.Get("K45_WTS_BUILIDINGEDITOR_HELPTITLE"), 0);
        public void Update()
        {

            if (MainContainer.isVisible)
            {
                if (Dirty)
                {
                    FixTabstrip();
                    Dirty = false;
                }
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

        private bool m_dirty;

        private OnNetGroupDescriptorXml CurrentEdited { get; set; }

        public bool Dirty
        {
            get => m_dirty; set {
                if (value && MainContainer.isVisible)
                {
                    FixTabstrip();
                }
                else
                {
                    m_dirty = value;
                }
            }
        }
    }

}
