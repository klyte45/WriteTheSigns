using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Utils;
using Klyte.DynamicTextProps.Xml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using static Klyte.DynamicTextProps.UI.DTPEditorUILib;

namespace Klyte.DynamicTextProps.UI
{

    internal class DTPBasicPropInfoEditor : UICustomControl
    {
        public static DTPBasicPropInfoEditor Instance { get; private set; }
        public UIPanel MainContainer { get; protected set; }

        protected UITextField m_propFilter;
        protected UIDropDown m_fontSelect;
        protected UITextField m_name;
        protected UIColorField m_fixedColor;
        private UIListBox m_popup;

        private Dictionary<string, string> m_propsLoaded;

        public event Action<PropInfo> EventPropChanged;
        public event Action<Color32> EventPropColorChanged;

        private PropInfo m_lastSelection;

        private BoardDescriptorGeneralXml EditingInstance => DTPPropTextLayoutEditor.Instance.EditingInstance;

        public Dictionary<string, string> PropsLoaded
        {
            get {
                if (m_propsLoaded == null)
                {
                    m_propsLoaded = PrefabUtils<PropInfo>.AssetsLoaded.ToDictionary(x => GetListName(x), x => x?.name);
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

            AddTextField("Prop", out m_propFilter, helper, null);

            KlyteMonoUtils.UiTextFieldDefaultsForm(m_propFilter);
            var selectorPanel = m_propFilter.parent as UIPanel;
            selectorPanel.autoLayout = true;
            selectorPanel.width = MainContainer.width;
            selectorPanel.autoFitChildrenHorizontally = false;
            selectorPanel.autoFitChildrenVertically = true;
            selectorPanel.width = MainContainer.width;
            selectorPanel.wrapLayout = true;


            AddTextField("Name", out m_name, helper, OnSetName);
            AddColorField(helper, "Fixed Color", out m_fixedColor, (x, y) => EventPropColorChanged?.Invoke(y));

            AddDropdown("Override font", out m_fontSelect, helper, new string[0], OnSetFont);
            DTPUtils.ReloadFontsOf(m_fontSelect, true);

            m_popup = CreatePopup(m_fontSelect, selectorPanel);
            m_popup.isVisible = false;
            m_propFilter.eventGotFocus += (x, t) =>
            {
                m_popup.isVisible = true;
                m_popup.items = GetFilterResult();
                m_popup.selectedIndex = Array.IndexOf(m_popup.items, m_propFilter.text);
                m_popup.EnsureVisible(m_popup.selectedIndex);
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
        }

        private void OnSetName(string text) => EditingInstance.SaveName = text;

        private string[] GetFilterResult() => PropsLoaded
            .ToList()
            .Where((x) => m_propFilter.text.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Value + (PrefabUtils.AuthorList.TryGetValue(x.Value.Split('.')[0], out string author) ? "\n" + author : ""), m_propFilter.text, CompareOptions.IgnoreCase) >= 0)
            .Select(x => x.Key)
            .OrderBy((x) => x)
            .ToArray();
        private static string GetListName(PropInfo x)
        {
            LogUtils.DoWarnLog($"x = {x}");
            return (x?.name?.EndsWith("_Data") ?? false) ? $"{x?.GetLocalizedTitle()}" : x?.name ?? "";
        }

        #region Actions        
        private void OnSetProp(int sel) => EventPropChanged?.Invoke(sel < 0 ? null : m_lastSelection = PrefabUtils<PropInfo>.AssetsLoaded.Where(x => x.name == PropsLoaded[m_popup.items[sel]]).FirstOrDefault());


        protected void OnSetFont(int idx)
        {
            if (idx > 0)
            {
                EditingInstance.FontName = m_fontSelect.items[idx];
            }
            else
            {
                EditingInstance.FontName = null;
            }
        }
        #endregion

    }

}
