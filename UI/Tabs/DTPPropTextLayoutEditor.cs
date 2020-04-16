using ColossalFramework;
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

        private DTPPropPreviewRenderer m_previewRenderer;
        private UIPanel m_previewPanel;
        private UITextureSprite m_preview;
        private UIPanel m_previewControls;
        private UIButton m_lockToSelection;

        private PropInfo m_currentInfo;

        private float m_targetZoom = 3;
        private float m_targetZoomSqrt = 9;
        private float m_targetRotation = 0;
        private Vector2 m_targetCameraPosition = default;
        private Vector2 m_cameraPosition = default;

        private UIPanel m_topBar;
        private UIPanel m_previewBar;
        private UIScrollablePanel m_editTabstrip;
        private UIPanel m_editArea;
        private DTPBasicPropInfoEditor m_propInfoEditor;

        private UIButton m_plusButton;

        private bool m_viewLocked;
        private int m_currentTab;

        private UIPanel m_basicInfoEditor;
        private UIPanel m_textInfoEditor;
        private Color32 CurrentSelectedColor { get; set; }
        public float TargetZoom
        {
            get => m_targetZoom;
            set {
                m_targetZoom = value;
                m_targetZoomSqrt = Mathf.Sqrt(value);
            }
        }

        private float m_maxZoom = 2f;
        //private float m_minZoom = -10f;

        public void Awake()
        {
            Instance = this;

            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;


            KlyteMonoUtils.CreateUIElement(out m_topBar, MainContainer.transform, "topBar", new UnityEngine.Vector4(0, 0, MainContainer.width, 50));
            m_topBar.autoLayout = true;
            m_topBar.autoLayoutDirection = LayoutDirection.Horizontal;
            m_topBar.padding = new RectOffset(5, 5, 5, 5);

            InitCircledButton(m_topBar, out m_refreshListButton, CommonsSpriteNames.K45_Reload, RefreshConfigList, "K45_DTP_REFRESH_CONFIG_LIST");

            m_configList = UIHelperExtension.CloneBasicDropDownNoLabel(new string[0], OnConfigSelectionChange, m_topBar);
            m_configList.width = 745;

            InitCircledButton(m_topBar, out m_newButton, CommonsSpriteNames.K45_New, OnNewConfig, "K45_DTP_CREATE_NEW_CONFIG");
            InitCircledButton(m_topBar, out m_deleteButton, CommonsSpriteNames.K45_Delete, OnDeleteConfig, "K45_DTP_DELETE_SELECTED_CONFIG");

            KlyteMonoUtils.CreateUIElement(out m_previewBar, MainContainer.transform, "previewBar", new UnityEngine.Vector4(0, 0, MainContainer.width, 300));
            m_previewBar.autoLayout = true;
            m_previewBar.autoLayoutDirection = LayoutDirection.Horizontal;


            KlyteMonoUtils.CreateUIElement(out m_previewPanel, m_previewBar.transform, "previewPanel", new UnityEngine.Vector4(0, 0, m_previewBar.width * .6f, 300));
            m_previewPanel.padding = new RectOffset(16, 16, 16, 16);
            m_previewPanel.backgroundSprite = "MenuPanel3";
            m_previewPanel.autoLayout = true;

            KlyteMonoUtils.CreateUIElement(out m_preview, m_previewPanel.transform, "preview", new UnityEngine.Vector4(0, 0, m_previewPanel.width - 32, m_previewPanel.height - 32));
            KlyteMonoUtils.CreateElement(out m_previewRenderer, MainContainer.transform);
            m_previewRenderer.Size = m_preview.size * 2f;
            m_preview.texture = m_previewRenderer.Texture;
            m_preview.eventMouseWheel += ChangeViewZoom;
            m_preview.eventMouseMove += OnMouseMove;
            m_previewRenderer.Zoom = TargetZoom;

            KlyteMonoUtils.CreateUIElement(out m_previewControls, m_previewBar.transform, "previewPanel", new UnityEngine.Vector4(0, 0, 50, 300));
            m_previewControls.padding = new RectOffset(5, 5, 5, 5);
            m_previewControls.autoLayout = true;
            m_previewControls.autoLayoutDirection = LayoutDirection.Vertical;


            InitCircledButton(m_previewControls, out m_lockToSelection, CommonsSpriteNames.K45_Unlock, (x, y) => ToggleLock(), "K45_DTP_LOCK_UNLOCK_TO_CURRENT_ITEM");
            m_lockToSelection.focusedBgSprite = null;
            m_viewLocked = true;
            ToggleLock();

            InitCircledButton(m_previewControls, out UIButton resetView, CommonsSpriteNames.K45_Reload, (x, y) => ResetCamera(), "K45_DTP_RESET_VIEW");

            KlyteMonoUtils.CreateScrollPanel(m_previewBar, out m_editTabstrip, out _, m_previewBar.width - m_previewPanel.width - m_previewControls.width - m_previewBar.padding.horizontal - (m_previewBar.autoLayoutPadding.horizontal * 2) - 20, 300);
            m_editTabstrip.autoLayout = true;
            m_editTabstrip.autoLayoutDirection = LayoutDirection.Vertical;

            InitTabButton(m_editTabstrip, out _, Locale.Get("K45_DTP_BASIC_INFO_TAB_TITLE"), new Vector2(m_editTabstrip.size.x, 30));
            InitTabButton(m_editTabstrip, out m_plusButton, Locale.Get("K45_DTP_ADD_NEW_TEXT_ENTRY"), new Vector2(m_editTabstrip.size.x, 30), false);
            m_plusButton.eventClicked += AddTabToItem;

            KlyteMonoUtils.CreateUIElement(out m_editArea, MainContainer.transform, "editArea", new UnityEngine.Vector4(0, 0, MainContainer.width - MainContainer.padding.horizontal, MainContainer.height - m_previewBar.height - m_topBar.height - MainContainer.padding.vertical - (MainContainer.autoLayoutPadding.vertical * 2) - 5));
            m_editArea.padding = new RectOffset(5, 5, 5, 5);


            KlyteMonoUtils.CreateUIElement(out m_basicInfoEditor, m_editArea.transform, "basicTab", new UnityEngine.Vector4(0, 0, m_editArea.width - m_editArea.padding.horizontal, m_editArea.height - m_editArea.padding.vertical));
            m_propInfoEditor = m_basicInfoEditor.gameObject.AddComponent<DTPBasicPropInfoEditor>();
            KlyteMonoUtils.CreateUIElement(out m_textInfoEditor, m_editArea.transform, "textTab", new UnityEngine.Vector4(0, 0, m_editArea.width - m_editArea.padding.horizontal, m_editArea.height - m_editArea.padding.vertical));

            OnTabChange(0);

            m_propInfoEditor.EventPropChanged += (x) =>
            {
                if (x != null)
                {
                    m_currentInfo = x;
                    ResetCamera();
                }
            };

            m_propInfoEditor.EventPropColorChanged += (x) => CurrentSelectedColor = x;
        }

        private void ToggleLock()
        {
            m_viewLocked = !m_viewLocked;
            if (m_viewLocked)
            {
                m_lockToSelection.normalFgSprite = KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Lock);
                m_lockToSelection.hoveredFgSprite = KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Unlock);
                m_lockToSelection.color = Color.red;
                m_lockToSelection.focusedColor = Color.red;
                m_lockToSelection.textColor = Color.yellow;
                m_lockToSelection.hoveredColor = Color.green;
                ResetCamera();
            }
            else
            {
                m_lockToSelection.normalFgSprite = KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Unlock);
                m_lockToSelection.hoveredFgSprite = KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Lock);
                m_lockToSelection.color = Color.white;
                m_lockToSelection.focusedColor = Color.white;
                m_lockToSelection.textColor = Color.white;
                m_lockToSelection.hoveredColor = Color.red;
            }
        }

        private void InitCircledButton(UIComponent parent, out UIButton m_newButton, CommonsSpriteNames sprite, MouseEventHandler onClicked, string tooltipLocale)
        {
            string name = Locale.Get(tooltipLocale);
            KlyteMonoUtils.CreateUIElement(out m_newButton, parent.transform, name, new UnityEngine.Vector4(0, 0, 40, 40));
            KlyteMonoUtils.InitButtonFull(m_newButton, false, "OptionBase");
            m_newButton.focusedBgSprite = "";
            m_newButton.normalFgSprite = KlyteResourceLoader.GetDefaultSpriteNameFor(sprite);
            m_newButton.scaleFactor = 0.6f;
            m_newButton.eventClicked += onClicked;
            m_newButton.tooltip = name;
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

        private void ResetCamera()
        {
            m_maxZoom = Mathf.Max(Mathf.Pow(m_currentInfo.m_mesh.bounds.extents.y / m_currentInfo.m_mesh.bounds.extents.x, 1 / 3f) * 3f, 3f);
            TargetZoom = m_maxZoom;
            m_targetRotation = 0;
            Vector3 target = m_currentInfo.m_mesh.bounds.center;
            target.y *= -1;
            m_targetCameraPosition = target;
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

        private void OnMouseMove(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (m_currentInfo != default && !m_viewLocked)
            {
                if ((eventParam.buttons & UIMouseButton.Left) != 0)
                {
                    Vector3 min = m_currentInfo.m_mesh.bounds.min;
                    Vector3 max = m_currentInfo.m_mesh.bounds.max;
                    min.y = -max.y;
                    max.y = -m_currentInfo.m_mesh.bounds.min.y;
                    m_targetCameraPosition = Vector2.Max(min, Vector2.Min(max, new Vector2(-eventParam.moveDelta.x / component.width, eventParam.moveDelta.y / component.height) + m_targetCameraPosition));
                }
                else if ((eventParam.buttons & UIMouseButton.Right) != 0)
                {
                    m_targetRotation += eventParam.moveDelta.x;
                }
            }
        }
        private void ChangeViewZoom(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (!m_viewLocked)
            {
                TargetZoom = Mathf.Max(Mathf.Min(TargetZoom + eventParam.wheelDelta * 0.125f, m_maxZoom), 1f / m_currentInfo.m_mesh.bounds.extents.magnitude / m_currentInfo.m_mesh.bounds.extents.magnitude);
            }
        }

        public void Update()
        {
            if (m_currentInfo != default && MainContainer.isVisible)
            {
                if (Mathf.Abs(m_previewRenderer.Zoom - TargetZoom) > 0.01f)
                {
                    m_previewRenderer.Zoom = Mathf.Lerp(m_previewRenderer.Zoom, TargetZoom, 0.25f);
                }
                if (Mathf.Abs(m_previewRenderer.CameraRotation - m_targetRotation) > 0.01f)
                {
                    m_previewRenderer.CameraRotation = Mathf.Lerp(m_previewRenderer.CameraRotation, m_targetRotation, 0.25f);
                }
                if (Mathf.Abs(m_cameraPosition.sqrMagnitude - m_targetCameraPosition.sqrMagnitude) > 0.0001f)
                {
                    m_cameraPosition = Vector2.Lerp(m_cameraPosition, m_targetCameraPosition, 0.25f);
                }

                RedrawModel();
            }


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

        private void RedrawModel()
        {
            if (m_currentInfo == default)
            {
                return;
            }
            m_preview.isVisible = true;
            m_previewRenderer.RenderProp(m_currentInfo, m_cameraPosition, CurrentSelectedColor);
        }


    }

}
