using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.WriteTheCity.Libraries;
using Klyte.WriteTheCity.Rendering;
using Klyte.WriteTheCity.Utils;
using Klyte.WriteTheCity.Xml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using static Klyte.WriteTheCity.UI.WTCEditorUILib;

namespace Klyte.WriteTheCity.UI
{

    internal class WTCBasicPropInfoEditor : UICustomControl
    {
        public static WTCBasicPropInfoEditor Instance { get; private set; }
        public UIPanel MainContainer { get; protected set; }

        protected UITextField m_propFilter;
        protected UIDropDown m_fontSelect;
        protected UITextField m_name;
        protected UIColorField m_fixedColor;
        private UIDropDown m_dropdownTextContent;
        private UIListBox m_popup;

        private Dictionary<string, string> m_propsLoaded;

        private PropInfo m_lastSelection;

        private BoardDescriptorGeneralXml EditingInstance => WTCPropTextLayoutEditor.Instance.EditingInstance;

        public Dictionary<string, string> PropsLoaded
        {
            get {
                if (m_propsLoaded == null)
                {
                    m_propsLoaded = PrefabUtils<PropInfo>.AssetsLoaded.Where(x => x != null).ToDictionary(x => GetListName(x), x => x?.name);
                }
                return m_propsLoaded;
            }
        }

        public void Awake()
        {
            Instance = this;

            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.padding = new RectOffset(5, 5, 5, 5);
            MainContainer.autoLayoutPadding = new RectOffset(0, 0, 3, 3);

            var helper = new UIHelperExtension(MainContainer);

            AddTextField(Locale.Get("K45_WTC_PROP_MODEL_SELECT"), out m_propFilter, helper, null);

            KlyteMonoUtils.UiTextFieldDefaultsForm(m_propFilter);
            var selectorPanel = m_propFilter.parent as UIPanel;
            selectorPanel.autoLayout = true;
            selectorPanel.width = MainContainer.width;
            selectorPanel.autoFitChildrenHorizontally = false;
            selectorPanel.autoFitChildrenVertically = true;
            selectorPanel.width = MainContainer.width;
            selectorPanel.wrapLayout = true;


            AddTextField(Locale.Get("K45_WTC_PROP_TAB_TITLE"), out m_name, helper, OnSetName);
            AddColorField(helper, Locale.Get("K45_WTC_PROP_COLOR"), out m_fixedColor, OnSetPropColor);

            AddDropdown(Locale.Get("K45_WTC_OVERRIDE_FONT"), out m_fontSelect, helper, new string[0], OnSetFont);
            WTCUtils.ReloadFontsOf(m_fontSelect, true);

            AddDropdown(Locale.Get("K45_WTC_TEXT_AVAILABILITY"), out m_dropdownTextContent, helper, Enum.GetNames(typeof(TextRenderingClass)).Select(x => Locale.Get("K45_WTC_BOARD_TEXT_AVAILABILITY_DESC", x.ToString())).ToArray(), OnSetTextOwnNameContent);

            WTCPropTextLayoutEditor.Instance.CurrentTabChanged += (x) =>
            {
                if (x == 0 && EditingInstance != null)
                {
                    m_name.text = EditingInstance.SaveName;
                    m_fixedColor.selectedColor = EditingInstance.FixedColor ?? default;
                    m_fontSelect.selectedIndex = EditingInstance.FontName == null ? 0 : EditingInstance.FontName == WTCController.DEFAULT_FONT_KEY ? 1 : Array.IndexOf(m_fontSelect.items, EditingInstance.FontName);
                    m_dropdownTextContent.selectedIndex = (int)EditingInstance.m_allowedRenderClass;
                    if (PrefabUtils<PropInfo>.AssetsLoaded == null)
                    {

                    }
                    else
                    {
                        m_lastSelection = PrefabUtils<PropInfo>.AssetsLoaded.Where(x => x?.name == EditingInstance.m_propName).FirstOrDefault();
                        WTCPropTextLayoutEditor.Instance.CurrentPropInfo = m_lastSelection;
                        m_propFilter.text = (m_lastSelection != null) ? GetListName(m_lastSelection) : "";
                    }

                }
            };

            WTCController.EventFontsReloadedFromFolder += () => WTCUtils.ReloadFontsOf(m_fontSelect, true);

            m_popup = ConfigurePropSelectionPopup(selectorPanel);

        }

        private UIListBox ConfigurePropSelectionPopup(UIPanel selectorPanel)
        {
            UIListBox m_popup = CreatePopup(m_fontSelect, selectorPanel);
            m_popup.isVisible = false;
            m_propFilter.eventGotFocus += (x, t) =>
            {
                m_popup.isVisible = true;
                m_popup.items = GetFilterResult();
                m_popup.selectedIndex = Array.IndexOf(m_popup.items, m_propFilter.text);
                m_popup.EnsureVisible(m_popup.selectedIndex);
                m_propFilter.SelectAll();
            };
            m_propFilter.eventLostFocus += (x, t) =>
            {
                if (m_popup.selectedIndex >= 0)
                {
                    m_propFilter.text = m_popup.items[m_popup.selectedIndex];
                    OnSetProp(m_popup.selectedIndex);
                }
                else
                {
                    m_propFilter.text = GetListName(m_lastSelection);
                }
                m_popup.isVisible = false;
            };
            m_propFilter.eventKeyUp += (x, y) =>
            {
                if (m_propFilter.hasFocus)
                {
                    m_popup.items = GetFilterResult();
                    m_popup.Invalidate();
                }
            };
            m_popup.eventSelectedIndexChanged += (x, y) =>
            {
                if (!m_propFilter.hasFocus)
                {
                    if (m_popup.selectedIndex >= 0)
                    {
                        m_propFilter.text = m_popup.items[m_popup.selectedIndex];
                        OnSetProp(m_popup.selectedIndex);
                    }
                    else
                    {
                        m_propFilter.text = "";
                    }
                }
            };
            return m_popup;
        }

        private void OnSetName(string text)
        {
            if (EditingInstance != null && EditingInstance.SaveName != text)
            {
                string validationErrors = ValidateConfigName(text);
                if (!validationErrors.IsNullOrWhiteSpace())
                {
                    ShowChangeNameConfigModal(validationErrors);
                }
                else
                {
                    EditingInstance.SaveName = text;
                    WTCPropTextLayoutEditor.Instance.MarkDirty();
                    WTCPropTextLayoutEditor.Instance.SetCurrentSelectionNewName(text);
                }
            }
        }

        private void ShowChangeNameConfigModal(string lastError)
        {
            K45DialogControl.ShowModalPromptText(
                  new K45DialogControl.BindProperties
                  {
                      title = Locale.Get("K45_WTC_PROPEDIT_NAMECHANGE_TITLE"),
                      message = (lastError.IsNullOrWhiteSpace() ? "" : $"{ Locale.Get("K45_WTC_PROPEDIT_NAMECHANGE_ANERROROCURRED")} {lastError}\n\n") + Locale.Get("K45_WTC_PROPEDIT_NAMECHANGE_MESSAGE"),
                      showButton1 = true,
                      textButton1 = Locale.Get("OK"),
                      showButton2 = true,
                      textButton2 = Locale.Get("CANCEL")
                  }, (x, text) =>
                  {
                      if (x == 1)
                      {
                          string error = ValidateConfigName(text);

                          if (error.IsNullOrWhiteSpace())
                          {
                              EditingInstance.SaveName = text;
                              WTCPropTextLayoutEditor.Instance.MarkDirty();
                              WTCPropTextLayoutEditor.Instance.SetCurrentSelectionNewName(text);
                          }
                          else
                          {
                              ShowChangeNameConfigModal(error);
                          }
                      }
                      else
                      {
                          m_name.text = EditingInstance.SaveName;
                      }
                      return true;
                  }
         );
        }

        private string ValidateConfigName(string text)
        {
            string error = null;
            if (text.IsNullOrWhiteSpace())
            {
                error = $"{ Locale.Get("K45_WTC_PROPEDIT_CONFIGNEW_INVALIDNAME")}";
            }
            else if (text != EditingInstance.SaveName && WTCLibPropSettings.Instance.Get(text) != null)
            {
                error = $"{ Locale.Get("K45_WTC_PROPEDIT_CONFIGNEW_ALREADY_EXISTS")}";
            }

            return error;
        }

        private string[] GetFilterResult() => PropsLoaded
            .ToList()
            .Where((x) => m_propFilter.text.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Value + (PrefabUtils.AuthorList.TryGetValue(x.Value.Split('.')[0], out string author) ? "\n" + author : ""), m_propFilter.text, CompareOptions.IgnoreCase) >= 0)
            .Select(x => x.Key)
            .OrderBy((x) => x)
            .ToArray();
        private static string GetListName(PropInfo x) => (x?.name?.EndsWith("_Data") ?? false) ? $"{x?.GetLocalizedTitle()}" : x?.name ?? "";

        #region Actions        
        private void OnSetPropColor(UIComponent component, Color value)
        {
            EditingInstance.FixedColor = (value == default ? (Color?)null : value);
            WTCPropTextLayoutEditor.Instance.MarkDirty();
        }

        private void OnSetProp(int sel)
        {
            PropInfo targetProp = (sel < 0 ? null : m_lastSelection = PrefabUtils<PropInfo>.AssetsLoaded.Where(x => x.name == PropsLoaded[m_popup.items[sel]]).FirstOrDefault());
            WTCPropTextLayoutEditor.Instance.CurrentPropInfo = targetProp;
            WTCPropTextLayoutEditor.Instance.MarkDirty();
        }

        protected void OnSetFont(int idx)
        {
            if (EditingInstance != null)
            {
                if (idx > 1)
                {
                    EditingInstance.FontName = m_fontSelect.items[idx];
                }
                else if (idx == 1)
                {
                    EditingInstance.FontName = WTCController.DEFAULT_FONT_KEY;
                }
                else
                {
                    EditingInstance.FontName = null;
                }
                WTCPropTextLayoutEditor.Instance.MarkDirty();
            }
        }
        private void OnSetTextOwnNameContent(int sel)
        {
            EditingInstance.m_allowedRenderClass = (TextRenderingClass)sel;
            WTCPropTextLayoutEditor.Instance.MarkDirty();
        }
        #endregion

    }

}
