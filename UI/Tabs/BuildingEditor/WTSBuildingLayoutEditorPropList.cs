﻿using ColossalFramework;
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
    internal class WTSBuildingLayoutEditorPropList : UICustomControl
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

        public ref BoardInstanceBuildingXml CurrentPropLayout
        {
            get {
                if (m_selectedIndex >= 0 && m_selectedIndex < (CurrentEdited?.PropInstances?.Length ?? 0))
                {
                    return ref CurrentEdited.PropInstances[m_selectedIndex];
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
                if (value >= 0 && value < PropInstances.Length)
                {
                    EventSelectionChanged?.Invoke(CurrentEdited?.BuildingName, ref PropInstances[value], Source);
                }
                else
                {
                    EventSelectionChanged?.Invoke(CurrentEdited?.BuildingName, ref nullref, Source);
                }
            }
        }

        private BoardInstanceBuildingXml nullref = null;

        public delegate void OnChangeTab(string building, ref BoardInstanceBuildingXml descriptor, ConfigurationSource source);

        public event OnChangeTab EventSelectionChanged;

        private BoardInstanceBuildingXml[] PropInstances => CurrentEdited?.PropInstances ?? m_zeroedInstances;
        private readonly BoardInstanceBuildingXml[] m_zeroedInstances = new BoardInstanceBuildingXml[0];

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
            m_topPanelTitle.text = Locale.Get("K45_WTS_BUILDING_LISTORDERTITLE");
            m_topPanelTitle.textAlignment = UIHorizontalAlignment.Center;

            int btnSize = 36;
            KlyteMonoUtils.CreateUIElement<UILabel>(out UILabel spacing, m_topPanel.transform, "_", new Vector4(0, 0, btnSize / 2, btnSize));
            spacing.textScale = 0;
            spacing.width = btnSize / 4;
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_import, CommonsSpriteNames.K45_Import, OnImportData, "K45_WTS_BUILDING_IMPORTDATA", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_export, CommonsSpriteNames.K45_Export, (x, y) => OnExportData(), "K45_WTS_BUILDING_EXPORTDATA", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_help, CommonsSpriteNames.K45_QuestionMark, Help_RulesList, "K45_CMNS_HELP", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_new, CommonsSpriteNames.K45_New, OnAddItemOnList, "K45_WTS_BUILDING_ADDITEMLIST", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_up, CommonsSpriteNames.K45_Up, OnMoveItemUpOnList, "K45_WTS_BUILDING_MOVEITEMUP", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_down, CommonsSpriteNames.K45_Down, OnMoveItemDownOnList, "K45_WTS_BUILDING_MOVEITEMDOWN", btnSize);
            KlyteMonoUtils.InitCircledButton(m_topPanel, out m_remove, CommonsSpriteNames.K45_X, OnRemoveItem, "K45_WTS_BUILDING_REMOVEITEM", btnSize);

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
                title = Locale.Get("K45_WTS_BUILDING_EXPORTRULESTITLE"),
                message = Locale.Get("K45_WTS_BUILDING_EXPORTRULESMESSAGE"),
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
                         K45DialogControl.UpdateCurrentMessage($"<color #FFFF00>{Locale.Get("K45_WTS_BUILDING_EXPORTRULESMESSAGE_INVALIDNAME")}</color>\n\n{Locale.Get("K45_WTS_BUILDING_EXPORTRULESMESSAGE")}");
                         return false;
                     }
                     WTSLibBuildingPropLayoutList.Reload();
                     var currentData = WTSLibBuildingPropLayoutList.Instance.Get(text);
                     if (currentData == null)
                     {
                         AddCurrentListToLibrary(text);
                     }
                     else
                     {
                         K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                         {
                             title = Locale.Get("K45_WTS_BUILDING_EXPORTRULESTITLE"),
                             message = string.Format(Locale.Get("K45_WTS_BUILDING_EXPORTRULESMESSAGE_CONFIRMOVERWRITE"), text),
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
            WTSLibBuildingPropLayoutList.Reload();
            var newItem = new ExportableBoardInstanceBuildingListXml { Instances = CurrentEdited.PropInstances, Layouts = CurrentEdited.CaculateLocalLayouts() };
            WTSLibBuildingPropLayoutList.Instance.Add(text,  newItem);
            K45DialogControl.ShowModal(new K45DialogControl.BindProperties
            {
                title = Locale.Get("K45_WTS_BUILDING_EXPORTRULESTITLE"),
                message = string.Format(Locale.Get("K45_WTS_BUILDING_EXPORTRULESMESSAGE_SUCCESSSAVEDATA"), WTSLibBuildingPropLayoutList.Instance.DefaultXmlFileBaseFullPath),
                showButton1 = true,
                textButton1 = Locale.Get("EXCEPTION_OK"),
                showButton2 = true,
                textButton2 = Locale.Get("K45_CMNS_GOTO_FILELOC"),
            }, (x) =>
            {
                if (x == 2)
                {
                    ColossalFramework.Utils.OpenInFileBrowser(WTSLibBuildingPropLayoutList.Instance.DefaultXmlFileBaseFullPath);
                    return false;
                }
                return true;
            });
        }

        private void OnImportData(UIComponent component, UIMouseEventParameter eventParam)
        {
            WTSLibBuildingPropLayoutList.Reload();
            string[] optionList = WTSLibBuildingPropLayoutList.Instance.List().ToArray();
            if (optionList.Length > 0)
            {
                K45DialogControl.ShowModalPromptDropDown(new K45DialogControl.BindProperties
                {
                    title = Locale.Get("K45_WTS_BUILDING_IMPORTRULESTITLE"),
                    message = Locale.Get("K45_WTS_BUILDING_IMPORTRULESMESSAGE"),
                    showButton1 = true,
                    textButton1 = Locale.Get("LOAD"),
                    showButton2 = true,
                    textButton2 = Locale.Get("CANCEL"),
                }, optionList, 0, (ret, idx, selText) =>
                {
                    if (ret == 1)
                    {
                        var newConfig = WTSLibBuildingPropLayoutList.Instance.Get(selText);
                        CurrentEdited.PropInstances = newConfig.Instances;
                        newConfig.Layouts.ForEach(x =>
                        {
                            if (WTSPropLayoutData.Instance.Get(x.Key) == null)
                            {
                                var value = x.Value;
                                WTSPropLayoutData.Instance.Add(x.Key,  value);
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
                    title = Locale.Get("K45_WTS_BUILDING_IMPORTRULESTITLE"),
                    message = Locale.Get("K45_WTS_BUILDING_IMPORTRULESMESSAGE_NOENTRIESFOUND"),
                    showButton1 = true,
                    textButton1 = Locale.Get("EXCEPTION_OK"),
                    showButton2 = true,
                    textButton2 = Locale.Get("K45_CMNS_GOTO_FILELOC"),
                }, (x) =>
                {
                    if (x == 2)
                    {
                        WTSLibBuildingPropLayoutList.Instance.EnsureFileExists();
                        ColossalFramework.Utils.OpenInFileBrowser(WTSLibBuildingPropLayoutList.Instance.DefaultXmlFileBaseFullPath);
                        return false;
                    }
                    return true;
                });
            }

        }

        public void Start() => WTSBuildingLayoutEditor.Instance.EventOnBuildingSelectionChanged += (x, y) =>
        {
            CurrentEdited = x;
            Source = y;

            SelectedIndex = -1;
            Dirty = true;
        };

        private const string TAB_TEMPLATE_NAME = "K45_WTS_TabTemplateBuilding";

        private void CreateTabTemplate()
        {
            var go = new GameObject();

            InitTabButton(go, out UIButton button, "AAA", new Vector2(m_orderedRulesList.size.x, 30), null);
            UITemplateUtils.GetTemplateDict()[TAB_TEMPLATE_NAME] = button;
        }

        public void FixTabstrip()
        {
            m_tabs.SetItemCount(PropInstances.Length);
            for (int i = 0; i < PropInstances.Length; i++)
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
                but.text = PropInstances[i]?.SaveName;
            }

            if (SelectedIndex < 1 || Source != ConfigurationSource.CITY)
            {
                m_up.Disable();
            }
            else
            {
                m_up.Enable();
            }
            if (SelectedIndex <= -1 || SelectedIndex >= PropInstances.Length - 1 || Source != ConfigurationSource.CITY)
            {
                m_down.Disable();
            }
            else
            {
                m_down.Enable();
            }
            if (SelectedIndex < 0 || SelectedIndex >= PropInstances.Length || Source != ConfigurationSource.CITY)
            {
                m_remove.Disable();
            }
            else
            {
                m_remove.Enable();
            }

            if (Source != ConfigurationSource.CITY)
            {
                m_new.Disable();
                m_import.Disable();
            }
            else
            {
                m_new.Enable();
                m_import.Enable();
            }
        }

        private void OnRemoveItem(UIComponent component, UIMouseEventParameter eventParam)
        {
            CurrentEdited.PropInstances = CurrentEdited.PropInstances.Where((x, y) => y != SelectedIndex).ToArray();
            SelectedIndex = Math.Min(SelectedIndex, (PropInstances.Length) - 1);
            FixTabstrip();
        }
        private void OnMoveItemUpOnList(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (SelectedIndex > 0 && PropInstances.Length > 1)
            {
                BoardInstanceBuildingXml temp = CurrentEdited.PropInstances[SelectedIndex];
                CurrentEdited.PropInstances[SelectedIndex] = CurrentEdited.PropInstances[SelectedIndex - 1];
                CurrentEdited.PropInstances[SelectedIndex - 1] = temp;
                SelectedIndex--;
                FixTabstrip();
            }
        }
        private void OnMoveItemDownOnList(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (SelectedIndex < PropInstances.Length && PropInstances.Length > 1)
            {
                BoardInstanceBuildingXml temp = CurrentEdited.PropInstances[SelectedIndex];
                CurrentEdited.PropInstances[SelectedIndex] = CurrentEdited.PropInstances[SelectedIndex + 1];
                CurrentEdited.PropInstances[SelectedIndex + 1] = temp;
                SelectedIndex++;
                FixTabstrip();
            }
        }
        private void OnAddItemOnList(UIComponent component, UIMouseEventParameter eventParam)
        {
            CurrentEdited.PropInstances = CurrentEdited.PropInstances.Concat(new BoardInstanceBuildingXml[] { new BoardInstanceBuildingXml
            {
                SaveName = "New layout",
            } }).ToArray();
            SelectedIndex = PropInstances.Length - 1;
            FixTabstrip();
        }
        private void Help_RulesList(UIComponent component, UIMouseEventParameter eventParam) => K45DialogControl.ShowModalHelp("BuildingLayouts.General", Locale.Get("K45_WTS_BUILIDINGEDITOR_HELPTITLE"), 0);
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

        private BuildingGroupDescriptorXml CurrentEdited { get; set; }
        private ConfigurationSource Source { get; set; }

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
