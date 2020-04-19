using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheCity.Xml;
using System;
using System.Linq;
using UnityEngine;

namespace Klyte.WriteTheCity.UI
{

    internal class WTCPropTextLayoutEditor : UICustomControl
    {
        public static WTCPropTextLayoutEditor Instance { get; private set; }
        public UIPanel MainContainer { get; protected set; }

        private UIDropDown m_configList;
        private UIButton m_newButton;
        private UIButton m_deleteButton;
        private UIButton m_refreshListButton;

        private BoardDescriptorGeneralXml m_editingInstance = new BoardDescriptorGeneralXml();
        internal ref BoardDescriptorGeneralXml EditingInstance => ref m_editingInstance;
        internal PropInfo CurrentInfo
        {
            get => m_currentInfo;
            private set {
                m_currentInfo = value;
                EditingInstance.m_propName = value.name;
            }
        }


        private UIPanel m_topBar;
        private UIPanel m_middleBar;
        private UIScrollablePanel m_editTabstrip;
        private UIPanel m_editArea;
        private WTCBasicPropInfoEditor m_propInfoEditor;

        private UIButton m_plusButton;

        internal int CurrentTab { get; private set; }

        private UIPanel m_basicInfoEditor;
        private UIPanel m_textInfoEditor;
        private PropInfo m_currentInfo;

        internal Color? CurrentSelectedColor => EditingInstance.FixedColor;

        internal event Action<int> CurrentTabChanged;

        public void Awake()
        {
            Instance = this;

            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.autoLayoutPadding = new RectOffset(5, 5, 5, 5);


            KlyteMonoUtils.CreateUIElement(out m_topBar, MainContainer.transform, "topBar", new UnityEngine.Vector4(0, 0, MainContainer.width - MainContainer.padding.horizontal, 50));
            m_topBar.autoLayout = true;
            m_topBar.autoLayoutDirection = LayoutDirection.Horizontal;
            m_topBar.padding = new RectOffset(5, 5, 5, 5);

            KlyteMonoUtils.InitCircledButton(m_topBar, out m_refreshListButton, CommonsSpriteNames.K45_Reload, RefreshConfigList, "K45_WTC_REFRESH_CONFIG_LIST");

            m_configList = UIHelperExtension.CloneBasicDropDownNoLabel(new string[0], OnConfigSelectionChange, m_topBar);
            m_configList.width = 735;

            KlyteMonoUtils.InitCircledButton(m_topBar, out m_newButton, CommonsSpriteNames.K45_New, OnNewConfig, "K45_WTC_CREATE_NEW_CONFIG");
            KlyteMonoUtils.InitCircledButton(m_topBar, out m_deleteButton, CommonsSpriteNames.K45_Delete, OnDeleteConfig, "K45_WTC_DELETE_SELECTED_CONFIG");

            KlyteMonoUtils.CreateUIElement(out m_middleBar, MainContainer.transform, "previewBar", new UnityEngine.Vector4(0, 0, MainContainer.width - MainContainer.padding.horizontal, 300));
            m_middleBar.autoLayout = true;
            m_middleBar.autoLayoutDirection = LayoutDirection.Horizontal;


            KlyteMonoUtils.CreateUIElement(out UIPanel previewContainer, m_middleBar.transform, "previewContainer", new UnityEngine.Vector4(0, 0, m_middleBar.width * .6f, m_middleBar.height - m_middleBar.padding.vertical));
            WTCEditorPropPreview propPreview = previewContainer.gameObject.AddComponent<WTCEditorPropPreview>();


            KlyteMonoUtils.CreateScrollPanel(m_middleBar, out m_editTabstrip, out _, m_middleBar.width - previewContainer.width - m_middleBar.padding.horizontal - (m_middleBar.autoLayoutPadding.horizontal * 2) - 20, 300);
            m_editTabstrip.autoLayout = true;
            m_editTabstrip.autoLayoutDirection = LayoutDirection.Vertical;

            InitTabButton(m_editTabstrip, out _, Locale.Get("K45_WTC_BASIC_INFO_TAB_TITLE"), new Vector2(m_editTabstrip.size.x, 30));
            InitTabButton(m_editTabstrip, out m_plusButton, Locale.Get("K45_WTC_ADD_NEW_TEXT_ENTRY"), new Vector2(m_editTabstrip.size.x, 30), false);
            m_plusButton.eventClicked += AddTabToItem;

            KlyteMonoUtils.CreateUIElement(out m_editArea, MainContainer.transform, "editArea", new UnityEngine.Vector4(0, 0, MainContainer.width - MainContainer.padding.horizontal, MainContainer.height - m_middleBar.height - m_topBar.height - MainContainer.padding.vertical - (MainContainer.autoLayoutPadding.vertical * 2) - 5));
            m_editArea.padding = new RectOffset(5, 5, 5, 5);


            KlyteMonoUtils.CreateUIElement(out m_basicInfoEditor, m_editArea.transform, "basicTab", new UnityEngine.Vector4(0, 0, m_editArea.width - m_editArea.padding.horizontal, m_editArea.height - m_editArea.padding.vertical));
            m_propInfoEditor = m_basicInfoEditor.gameObject.AddComponent<WTCBasicPropInfoEditor>();
            KlyteMonoUtils.CreateUIElement(out m_textInfoEditor, m_editArea.transform, "textTab", new UnityEngine.Vector4(0, 0, m_editArea.width - m_editArea.padding.horizontal, m_editArea.height - m_editArea.padding.vertical));
            m_textInfoEditor.gameObject.AddComponent<WTCTextEditor>();

            OnTabChange(0);

            m_propInfoEditor.EventPropChanged += (x) =>
            {
                if (x != null)
                {
                    CurrentInfo = x;
                    propPreview.ResetCamera();
                }
            };

            m_propInfoEditor.EventPropColorChanged += (x) => EditingInstance.FixedColor = (x == default(Color) ? (Color?)null : x);
        }

        private void InitTabButton(UIComponent parent, out UIButton tabTemplate, string text, Vector2 size, bool useDefaultAction = true)
        {
            KlyteMonoUtils.CreateUIElement(out tabTemplate, parent.transform, name, new UnityEngine.Vector4(0, 0, 40, 40));
            KlyteMonoUtils.InitButton(tabTemplate, false, "GenericTab");
            tabTemplate.autoSize = false;
            tabTemplate.size = size;
            tabTemplate.text = text;
            tabTemplate.group = parent;
            if (useDefaultAction)
            {
                tabTemplate.eventClicked += (x, y) => OnTabChange(x.zOrder);
            }
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
            InitTabButton(m_editTabstrip, out UIButton button, "", new Vector2(m_editTabstrip.size.x, 30));
            EditingInstance.m_textDescriptors = EditingInstance.m_textDescriptors.Union(new BoardTextDescriptorGeneralXml[] { new BoardTextDescriptorGeneralXml{
                SaveName = $"Tab {button.zOrder - 1}"
            }
            }).ToArray();
            button.text = $"Tab {button.zOrder - 1}";
            m_plusButton.zOrder = 9999999;
        }

        public void SetCurrentTabName(string name)
        {
            if (CurrentTab > 0 && CurrentTab < m_editTabstrip.components.Count - 1)
            {
                (m_editTabstrip.components[CurrentTab] as UIButton).text = name;
            }
        }

        private void OnDeleteConfig(UIComponent component, UIMouseEventParameter eventParam)
        {
            K45DialogControl.ShowModal(
                   new K45DialogControl.BindProperties
                   {
                       title = Locale.Get("K45_WTC_PROPEDIT_CONFIGDELETE_TITLE"),
                       message = string.Format(Locale.Get("K45_WTC_PROPEDIT_CONFIGDELETE_MESSAGE"), m_configList.selectedValue),
                       showButton1 = true,
                       textButton1 = Locale.Get("YES"),
                       showButton2 = true,
                       textButton2 = Locale.Get("NO"),
                   }
                , (x) =>
                {
                    if (x == 1)
                    {
                        //do delete
                    }
                    return true;
                }); ;
        }
        private void OnNewConfig(UIComponent component, UIMouseEventParameter eventParam) => ShowNewConfigModal();

        private void ShowNewConfigModal(string lastError = null)
        {
            K45DialogControl.ShowModalPromptText(
                  new K45DialogControl.BindProperties
                  {
                      title = Locale.Get("K45_WTC_PROPEDIT_CONFIGNEW_TITLE"),
                      message = (lastError.IsNullOrWhiteSpace() ? "" : $"{ Locale.Get("K45_WTC_PROPEDIT_CONFIGNEW_ANERROROCURRED")} {lastError}\n\n") + Locale.Get("K45_WTC_PROPEDIT_CONFIGNEW_MESSAGE"),
                      showButton1 = true,
                      textButton1 = Locale.Get("EXCEPTION_OK"),
                      showButton2 = true,
                      textButton2 = Locale.Get("MODS_DECLINE")
                  }, (x, y) =>
                  {
                      if (x == 1)
                      {
                          string error = null;
                          if (y.IsNullOrWhiteSpace())
                          {
                              error = $"{ Locale.Get("K45_WTC_PROPEDIT_CONFIGNEW_INVALIDNAME")}";
                          }
                          else if (Array.IndexOf(m_configList.items, y) >= 0)
                          {
                              error = $"{ Locale.Get("K45_WTC_PROPEDIT_CONFIGNEW_ALREADY_EXISTS")}";
                          }

                          if (error.IsNullOrWhiteSpace())
                          {
                              //do creation
                          }
                          else
                          {
                              ShowNewConfigModal(error);
                          }
                      }
                      return true;
                  }
         );
        }

        private void RefreshConfigList(UIComponent component, UIMouseEventParameter eventParam) { }

        private void OnConfigSelectionChange(int sel) { }

        #region Preview Area

        #endregion

        public void Update()
        {


            foreach (UIButton btn in m_editTabstrip.GetComponentsInChildren<UIButton>())
            {
                if (btn != m_plusButton)
                {
                    if (btn.zOrder == CurrentTab)
                    {
                        btn.state |= UIButton.ButtonState.Focused;
                    }
                    else
                    {
                        btn.state &= ~UIButton.ButtonState.Focused;
                    }
                }
            }
        }



    }

}
