using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensions;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSPropLayoutEditor : UICustomControl
    {
        public static WTSPropLayoutEditor Instance { get; private set; }
        public UIPanel MainContainer { get; protected set; }

        public WTSPropLayoutEditorBasics m_basicsTab;


        #region Panel areas
        private UIPanel m_middleBar;
        private UIPanel m_editArea;
        #endregion

        #region Top bar controls
        private UITextField m_configList;
        private UIButton m_newButton;
        private UIButton m_deleteButton;
        private UIButton m_importButton;
        private UIButton m_helpButton;
        private UILabel m_scopeInfo;
        #endregion
        #region Mid bar controls
        private UIScrollablePanel m_editTabstrip;
        private UIButton m_plusButton;
        #endregion
        #region Bottom bar panels
        private UIPanel m_basicInfoEditor;
        private UIPanel m_textInfoEditor;
        #endregion

        internal int CurrentTab { get; private set; }
        private PropInfo m_currentInfo;
        private BoardDescriptorGeneralXml m_editingInstance;

        internal ref BoardDescriptorGeneralXml EditingInstance => ref m_editingInstance;
        internal PropInfo CurrentPropInfo
        {
            get => m_currentInfo;
            set
            {
                m_currentInfo = value;
                EditingInstance.PropName = value?.name;
                PropPreview.ResetCamera();
            }
        }

        internal Color? CurrentSelectedColor => EditingInstance.FixedColor;

        internal WTSPropLayoutEditorPreview PropPreview { get; private set; }

        internal event Action<int> CurrentTabChanged;

        public void Awake()
        {
            Instance = this;

            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.autoLayoutPadding = new RectOffset(5, 5, 5, 5);
            MainContainer.width = MainContainer.parent.width;

            var mainContainerHelper = new UIHelperExtension(MainContainer);
            AddFilterableInput("AAAA", mainContainerHelper, out m_configList, out _, OnFilterLayouts, OnConfigSelectionChange);
            m_configList.width += m_configList.parent.GetComponentInChildren<UILabel>().width;
            GameObject.Destroy(m_configList.parent.GetComponentInChildren<UILabel>());
            m_configList.tooltipLocaleID = "K45_WTS_TOOLTIP_LAYOUTSELECTOR";
            AddLabel("", mainContainerHelper, out m_scopeInfo, out _);
            m_scopeInfo.processMarkup = true;

            m_helpButton = AddButtonInEditorRow(m_configList, CommonsSpriteNames.K45_QuestionMark, OnHelp_General, "K45_CMNS_HELP", true, 30);
            m_deleteButton = AddButtonInEditorRow(m_configList, CommonsSpriteNames.K45_Delete, OnDeleteConfig, "K45_WTS_DELETE_SELECTED_CONFIG", true, 30);
            m_importButton = AddButtonInEditorRow(m_configList, CommonsSpriteNames.K45_Import, OnImportIntoCity, "K45_WTS_IMPORTINTOCITY_CONFIG", false, 30);
            m_newButton = AddButtonInEditorRow(m_configList, CommonsSpriteNames.K45_New, ShowNewConfigModal, "K45_WTS_CREATE_NEW_CONFIG", true, 30);
            AddButtonInEditorRow(m_configList, CommonsSpriteNames.K45_Reload, OnRefresh, "K45_WTS_RELOADFILES", true, 30);

            m_importButton.color = Color.green;
            m_deleteButton.color = Color.red;

            KlyteMonoUtils.CreateUIElement(out m_middleBar, MainContainer.transform, "previewBar", new Vector4(0, 0, MainContainer.width - MainContainer.padding.horizontal, 300));
            m_middleBar.autoLayout = true;
            m_middleBar.autoLayoutDirection = LayoutDirection.Horizontal;


            KlyteMonoUtils.CreateUIElement(out UIPanel previewContainer, m_middleBar.transform, "previewContainer", new UnityEngine.Vector4(0, 0, m_middleBar.width * .6f, m_middleBar.height - m_middleBar.padding.vertical));
            PropPreview = previewContainer.gameObject.AddComponent<WTSPropLayoutEditorPreview>();


            KlyteMonoUtils.CreateScrollPanel(m_middleBar, out m_editTabstrip, out _, m_middleBar.width - previewContainer.width - m_middleBar.padding.horizontal - (m_middleBar.autoLayoutPadding.horizontal * 2) - 20, 300);
            m_editTabstrip.autoLayout = true;
            m_editTabstrip.autoLayoutDirection = LayoutDirection.Vertical;

            InitTabButton(m_editTabstrip, out _, Locale.Get("K45_WTS_BASIC_INFO_TAB_TITLE"), new Vector2(m_editTabstrip.size.x, 30), (x, y) => OnTabChange(x.zOrder));
            InitTabButton(m_editTabstrip, out m_plusButton, Locale.Get("K45_WTS_ADD_NEW_TEXT_ENTRY"), new Vector2(m_editTabstrip.size.x, 30), null);
            m_plusButton.eventClicked += AddTabToItem;

            KlyteMonoUtils.CreateUIElement(out m_editArea, MainContainer.transform, "editArea", new UnityEngine.Vector4(0, 0, MainContainer.width - MainContainer.padding.horizontal, MainContainer.height - m_middleBar.height - 80 - MainContainer.padding.vertical - (MainContainer.autoLayoutPadding.vertical * 2) - 5));
            m_editArea.padding = new RectOffset(5, 5, 5, 5);


            KlyteMonoUtils.CreateUIElement(out m_basicInfoEditor, m_editArea.transform, "basicTab", new UnityEngine.Vector4(0, 0, m_editArea.width - m_editArea.padding.horizontal, m_editArea.height - m_editArea.padding.vertical));
            m_basicsTab = m_basicInfoEditor.gameObject.AddComponent<WTSPropLayoutEditorBasics>();
            KlyteMonoUtils.CreateUIElement(out m_textInfoEditor, m_editArea.transform, "textTab", new UnityEngine.Vector4(0, 0, m_editArea.width - m_editArea.padding.horizontal, m_editArea.height - m_editArea.padding.vertical));
            m_textInfoEditor.gameObject.AddComponent<WTSPropLayoutEditorTexts>();

            OnTabChange(0);
            OnConfigSelectionChange("", -1, new string[0]);
        }

        private void OnRefresh()
        {
            WTSPropLayoutData.Instance.ReloadAllPropsConfigurations();
            m_configList.text = ExecuteItemChange(m_configList.text, WTSPropLayoutData.Instance.Get(m_configList.text) != null) ?? "";
        }

        private void OnHelp_General() => K45DialogControl.ShowModalHelp("PropLayouts.General", Locale.Get("K45_WTS_PROPEDITOR_HELPTITLE"), 0);

        public void SetCurrentSelectionNewName(string newName)
        {
            WTSPropLayoutData.Instance.Add(newName,  EditingInstance);
            m_configList.text = newName;
        }


        private void OnTabChange(int idx)
        {
            CurrentTab = idx;
            m_basicInfoEditor.isVisible = CurrentTab == 0;
            m_textInfoEditor.isVisible = CurrentTab != 0;
            CurrentTabChanged?.Invoke(idx);
        }

        private void AddTabToItem(UIComponent x, UIMouseEventParameter y)
        {
            UIButton button = AddTabButton($"Tab {m_plusButton.zOrder - 1}");
            var newItem = new BoardTextDescriptorGeneralXml
            {
                SaveName = $"Tab {button.zOrder}"
            };
            EditingInstance.TextDescriptors = EditingInstance.TextDescriptors.Concat(new BoardTextDescriptorGeneralXml[] {
                newItem
            }).ToArray();
            button.text = newItem.SaveName;
            OnTabChange(EditingInstance.TextDescriptors.Length);
        }

        private UIButton AddTabButton(string tabName)
        {
            InitTabButton(m_editTabstrip, out UIButton button, "", new Vector2(m_editTabstrip.size.x, 30), (x, y) => OnTabChange(x.zOrder));
            button.text = tabName;
            m_plusButton.zOrder = 9999999;
            return button;
        }

        internal void RemoveTabFromItem(int tabToEdit)
        {
            Destroy(m_editTabstrip.components[tabToEdit + 1]);
            EditingInstance.TextDescriptors = EditingInstance.TextDescriptors.Where((x, y) => y != tabToEdit).ToArray();
            OnTabChange(Mathf.Min(CurrentTab, EditingInstance.TextDescriptors.Length));
        }

        public void SetCurrentTabName(string name)
        {
            if (CurrentTab > 0 && CurrentTab < m_editTabstrip.components.Count - 1)
            {
                (m_editTabstrip.components[CurrentTab] as UIButton).text = name;
            }
        }

        private void OnImportIntoCity()
        {
            EditingInstance.m_configurationSource = ConfigurationSource.CITY;
            ExecuteItemChange(EditingInstance.SaveName, true);
        }
        private void OnDeleteConfig()
        {
            K45DialogControl.ShowModal(
                   new K45DialogControl.BindProperties
                   {
                       title = Locale.Get("K45_WTS_PROPEDIT_CONFIGDELETE_TITLE"),
                       message = string.Format(Locale.Get("K45_WTS_PROPEDIT_CONFIGDELETE_MESSAGE"), m_configList.text),
                       showButton1 = true,
                       textButton1 = Locale.Get("YES"),
                       showButton2 = true,
                       textButton2 = Locale.Get("NO"),
                   }
                , (x) =>
                {
                    if (x == 1)
                    {
                        WTSPropLayoutData.Instance.Remove(m_configList.text);
                        OnRefresh();
                    }
                    return true;
                }); ;
        }


        private void ShowNewConfigModal() => ShowNewConfigModal(null);
        private void ShowNewConfigModal(string lastError) => K45DialogControl.ShowModalPromptText(
                  new K45DialogControl.BindProperties
                  {
                      title = Locale.Get("K45_WTS_PROPEDIT_CONFIGNEW_TITLE"),
                      message = (lastError.IsNullOrWhiteSpace() ? "" : $"{ Locale.Get("K45_WTS_PROPEDIT_CONFIGNEW_ANERROROCURRED")} {lastError}\n\n") + Locale.Get("K45_WTS_PROPEDIT_CONFIGNEW_MESSAGE"),
                      showButton1 = true,
                      textButton1 = Locale.Get("EXCEPTION_OK"),
                      showButton2 = true,
                      textButton2 = Locale.Get("CANCEL")
                  }, (x, text) =>
                  {
                      if (x == 1)
                      {
                          string error = null;
                          if (text.IsNullOrWhiteSpace())
                          {
                              error = $"{ Locale.Get("K45_WTS_PROPEDIT_CONFIGNEW_INVALIDNAME")}";
                          }
                          else if (WTSPropLayoutData.Instance.List().Contains(text))
                          {
                              error = $"{ Locale.Get("K45_WTS_PROPEDIT_CONFIGNEW_ALREADY_EXISTS")}";
                          }

                          if (error.IsNullOrWhiteSpace())
                          {
                              var newModel = new BoardDescriptorGeneralXml
                              {
                                  m_configurationSource = ConfigurationSource.CITY
                              };
                              WTSPropLayoutData.Instance.Add(text,  newModel);
                              m_configList.text = ExecuteItemChange(text, true);
                          }
                          else
                          {
                              ShowNewConfigModal(error);
                          }
                      }
                      return true;
                  }
         );

        internal void ReplaceItem(string key, string data)
        {
            BoardDescriptorGeneralXml newItem = XmlUtils.DefaultXmlDeserialize<BoardDescriptorGeneralXml>(data);
            newItem.m_configurationSource = ConfigurationSource.CITY;
            WTSPropLayoutData.Instance.Add(key,  newItem);
            OnTabChange(0);
        }

        private IEnumerator OnFilterLayouts(string input, Wrapper<string[]> result)
        {
            yield return WTSPropLayoutData.Instance.FilterBy(input, null, result);
        }

        private string OnConfigSelectionChange(string typed, int sel, string[] items)
        {
            if (sel == -1)
            {
                sel = Array.IndexOf(items, typed?.Trim());
            }
            bool isValidSelection = sel >= 0 && sel < items.Length;
            string targetValue = isValidSelection ? items[sel] : "";

            return ExecuteItemChange(targetValue, isValidSelection);
        }

        private string ExecuteItemChange(string targetValue, bool isValidSelection)
        {
            m_editArea.isVisible = isValidSelection;
            if (isValidSelection)
            {
                m_middleBar.Enable();
                EditingInstance = WTSPropLayoutData.Instance.Get(targetValue);
                OnTabChange(0);
                while (m_editTabstrip.components.Count > EditingInstance.TextDescriptors.Length + 2)
                {
                    Destroy(m_editTabstrip.components[m_editTabstrip.components.Count - 2]);
                    m_editTabstrip.RemoveUIComponent(m_editTabstrip.components[m_editTabstrip.components.Count - 2]);
                }
                while (m_editTabstrip.components.Count < EditingInstance.TextDescriptors.Length + 2)
                {
                    AddTabButton("!!!");
                }
                for (int i = 1; i <= EditingInstance.TextDescriptors.Length; i++)
                {
                    (m_editTabstrip.components[i] as UIButton).text = EditingInstance.TextDescriptors[i - 1].SaveName;
                }
                switch (EditingInstance.m_configurationSource)
                {
                    case ConfigurationSource.ASSET:
                        m_deleteButton.isVisible = false;
                        m_importButton.isVisible = true;
                        m_plusButton.isVisible = false;
                        m_scopeInfo.localeID = "K45_WTS_CURRENTSOURCEPROP_ASSET";
                        break;
                    case ConfigurationSource.GLOBAL:
                        m_deleteButton.isVisible = false;
                        m_importButton.isVisible = true;
                        m_plusButton.isVisible = false;
                        m_scopeInfo.localeID = "K45_WTS_CURRENTSOURCEPROP_GLOBAL";
                        break;
                    case ConfigurationSource.CITY:
                        m_deleteButton.Enable();
                        m_deleteButton.isVisible = true;
                        m_importButton.isVisible = false;
                        m_plusButton.isVisible = true;
                        m_scopeInfo.localeID = "K45_WTS_CURRENTSOURCEPROP_CITY";
                        break;
                }

            }
            else
            {
                while (m_editTabstrip.components.Count > 2)
                {
                    Destroy(m_editTabstrip.components[m_editTabstrip.components.Count - 2]);
                    m_editTabstrip.RemoveUIComponent(m_editTabstrip.components[m_editTabstrip.components.Count - 2]);
                }
                m_middleBar.Disable();
                m_scopeInfo.localeID = "K45_WTS_PROPLAYOUT_SELECTAITEM";
                m_deleteButton.Disable();
                m_importButton.isVisible = false;
            }
            return isValidSelection ? targetValue : null;
        }




        public void Update()
        {
            if (MainContainer.isVisible)
            {
                foreach (UIButton btn in m_editTabstrip.GetComponentsInChildren<UIButton>())
                {
                    if (btn != m_plusButton)
                    {
                        if (btn.zOrder == CurrentTab)
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

}
