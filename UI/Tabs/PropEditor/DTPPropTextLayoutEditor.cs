﻿using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using System;
using UnityEngine;

namespace Klyte.DynamicTextProps.UI
{

    internal class DTPPropTextLayoutEditor : UICustomControl
    {
        public static DTPPropTextLayoutEditor Instance { get; private set; }
        public UIPanel MainContainer { get; protected set; }

        private UIDropDown m_configList;
        private UIButton m_newButton;
        private UIButton m_deleteButton;
        private UIButton m_refreshListButton;


        internal PropInfo CurrentInfo { get; private set; }


        private UIPanel m_topBar;
        private UIPanel m_middleBar;
        private UIScrollablePanel m_editTabstrip;
        private UIPanel m_editArea;
        private DTPBasicPropInfoEditor m_propInfoEditor;

        private UIButton m_plusButton;

        private int m_currentTab;

        private UIPanel m_basicInfoEditor;
        private UIPanel m_textInfoEditor;
        internal Color32 CurrentSelectedColor { get; private set; }

        public void Awake()
        {
            Instance = this;

            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.autoLayoutPadding = new RectOffset(5, 5, 5, 5);


            KlyteMonoUtils.CreateUIElement(out m_topBar, MainContainer.transform, "topBar", new UnityEngine.Vector4(0, 0, MainContainer.width, 50));
            m_topBar.autoLayout = true;
            m_topBar.autoLayoutDirection = LayoutDirection.Horizontal;
            m_topBar.padding = new RectOffset(5, 5, 5, 5);

            KlyteMonoUtils.InitCircledButton(m_topBar, out m_refreshListButton, CommonsSpriteNames.K45_Reload, RefreshConfigList, "K45_DTP_REFRESH_CONFIG_LIST");

            m_configList = UIHelperExtension.CloneBasicDropDownNoLabel(new string[0], OnConfigSelectionChange, m_topBar);
            m_configList.width = 745;

            KlyteMonoUtils.InitCircledButton(m_topBar, out m_newButton, CommonsSpriteNames.K45_New, OnNewConfig, "K45_DTP_CREATE_NEW_CONFIG");
            KlyteMonoUtils.InitCircledButton(m_topBar, out m_deleteButton, CommonsSpriteNames.K45_Delete, OnDeleteConfig, "K45_DTP_DELETE_SELECTED_CONFIG");

            KlyteMonoUtils.CreateUIElement(out m_middleBar, MainContainer.transform, "previewBar", new UnityEngine.Vector4(0, 0, MainContainer.width, 300));
            m_middleBar.autoLayout = true;
            m_middleBar.autoLayoutDirection = LayoutDirection.Horizontal;


            KlyteMonoUtils.CreateUIElement(out UIPanel previewContainer, m_middleBar.transform, "previewContainer", new UnityEngine.Vector4(0, 0, m_middleBar.width * .6f, m_middleBar.height - m_middleBar.padding.vertical));
            DTPEditorPropPreview propPreview = previewContainer.gameObject.AddComponent<DTPEditorPropPreview>();


            KlyteMonoUtils.CreateScrollPanel(m_middleBar, out m_editTabstrip, out _, m_middleBar.width - previewContainer.width - m_middleBar.padding.horizontal - (m_middleBar.autoLayoutPadding.horizontal * 2) - 20, 300);
            m_editTabstrip.autoLayout = true;
            m_editTabstrip.autoLayoutDirection = LayoutDirection.Vertical;

            InitTabButton(m_editTabstrip, out _, Locale.Get("K45_DTP_BASIC_INFO_TAB_TITLE"), new Vector2(m_editTabstrip.size.x, 30));
            InitTabButton(m_editTabstrip, out m_plusButton, Locale.Get("K45_DTP_ADD_NEW_TEXT_ENTRY"), new Vector2(m_editTabstrip.size.x, 30), false);
            m_plusButton.eventClicked += AddTabToItem;

            KlyteMonoUtils.CreateUIElement(out m_editArea, MainContainer.transform, "editArea", new UnityEngine.Vector4(0, 0, MainContainer.width - MainContainer.padding.horizontal, MainContainer.height - m_middleBar.height - m_topBar.height - MainContainer.padding.vertical - (MainContainer.autoLayoutPadding.vertical * 2) - 5));
            m_editArea.padding = new RectOffset(5, 5, 5, 5);


            KlyteMonoUtils.CreateUIElement(out m_basicInfoEditor, m_editArea.transform, "basicTab", new UnityEngine.Vector4(0, 0, m_editArea.width - m_editArea.padding.horizontal, m_editArea.height - m_editArea.padding.vertical));
            m_propInfoEditor = m_basicInfoEditor.gameObject.AddComponent<DTPBasicPropInfoEditor>();
            KlyteMonoUtils.CreateUIElement(out m_textInfoEditor, m_editArea.transform, "textTab", new UnityEngine.Vector4(0, 0, m_editArea.width - m_editArea.padding.horizontal, m_editArea.height - m_editArea.padding.vertical));
            m_textInfoEditor.gameObject.AddComponent<DTPTextEditor>();

            OnTabChange(0);

            m_propInfoEditor.EventPropChanged += (x) =>
            {
                if (x != null)
                {
                    CurrentInfo = x;
                    propPreview.ResetCamera();
                }
            };

            m_propInfoEditor.EventPropColorChanged += (x) => CurrentSelectedColor = x;
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
            m_currentTab = idx;
            m_basicInfoEditor.isVisible = m_currentTab == 0;
            m_textInfoEditor.isVisible = m_currentTab != 0;
        }

        private void AddTabToItem(UIComponent x, UIMouseEventParameter y)
        {
            InitTabButton(m_editTabstrip, out UIButton button, "", new Vector2(m_editTabstrip.size.x, 30));
            button.text = $"Tab {button.zOrder - 1}";
            m_plusButton.zOrder = 9999999;
        }

        private void OnDeleteConfig(UIComponent component, UIMouseEventParameter eventParam)
        {
            K45DialogControl.ShowModal(
                   new K45DialogControl.BindProperties
                   {
                       title = Locale.Get("K45_DTP_PROPEDIT_CONFIGDELETE_TITLE"),
                       message = string.Format(Locale.Get("K45_DTP_PROPEDIT_CONFIGDELETE_MESSAGE"), m_configList.selectedValue),
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
                      title = Locale.Get("K45_DTP_PROPEDIT_CONFIGNEW_TITLE"),
                      message = (lastError.IsNullOrWhiteSpace() ? "" : $"{ Locale.Get("K45_DTP_PROPEDIT_CONFIGNEW_ANERROROCURRED")} {lastError}\n\n") + Locale.Get("K45_DTP_PROPEDIT_CONFIGNEW_MESSAGE"),
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
                              error = $"{ Locale.Get("K45_DTP_PROPEDIT_CONFIGNEW_INVALIDNAME")}";
                          }
                          else if (Array.IndexOf(m_configList.items, y) >= 0)
                          {
                              error = $"{ Locale.Get("K45_DTP_PROPEDIT_CONFIGNEW_ALREADY_EXISTS")}";
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
                    if (btn.zOrder == m_currentTab)
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