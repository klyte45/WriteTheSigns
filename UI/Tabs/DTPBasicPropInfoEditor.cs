using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ICities;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

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

        private Dictionary<string, string> m_propsLoaded;

        public event Action<PropInfo> EventPropChanged;
        public event Action<Color32> EventPropColorChanged;

        private PropInfo m_lastSelection;

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
            m_propsLoaded = PrefabUtils<PropInfo>.AssetsLoaded.ToDictionary(x => GetListName(x), x => x?.name);
            //m_propSelect.items = m_propsLoaded.Keys.OrderBy((x) => x).ToArray();
            //m_propSelect.builtinKeyNavigation = true;

            KlyteMonoUtils.UiTextFieldDefaultsForm(m_propFilter);
            var selectorPanel = m_propFilter.parent as UIPanel;
            selectorPanel.autoLayout = true;
            selectorPanel.width = MainContainer.width;
            selectorPanel.autoFitChildrenHorizontally = false;
            selectorPanel.autoFitChildrenVertically = true;
            selectorPanel.width = MainContainer.width;
            selectorPanel.wrapLayout = true;


            AddTextField("Name", out m_name, helper, (x) => { });
            AddColorField(helper, "Fixed Color", out m_fixedColor);
            m_fixedColor.eventSelectedColorChanged += (x, y) => EventPropColorChanged?.Invoke(y);

            AddDropdown("Override font", out m_fontSelect, helper, new string[0], OnSetFont);
            DTPUtils.ReloadFontsOf(m_fontSelect, true);

            CreatePopup(m_fontSelect, selectorPanel);
            m_Popup.isVisible = false;
            m_propFilter.eventGotFocus += (x, t) =>
            {
                m_Popup.isVisible = true;
                m_Popup.items = GetFilterResult();
                m_Popup.selectedIndex = Array.IndexOf(m_Popup.items, m_propFilter.text);
                m_Popup.EnsureVisible(m_Popup.selectedIndex);
            };
            m_propFilter.eventLostFocus += (x, t) =>
            {
                if (m_Popup.selectedIndex >= 0)
                {
                    m_propFilter.text = m_Popup.items[m_Popup.selectedIndex];
                    OnSetProp(m_Popup.selectedIndex);
                }
                else
                {
                    m_propFilter.text = GetListName(m_lastSelection);
                }
                m_Popup.isVisible = false;
            };
            m_propFilter.eventKeyUp += (x, y) =>
            {
                if (m_propFilter.hasFocus)
                {
                    m_Popup.items = GetFilterResult();
                    m_Popup.Invalidate();
                }
            };
            m_Popup.eventSelectedIndexChanged += (x, y) =>
            {
                if (!m_propFilter.hasFocus)
                {
                    if (m_Popup.selectedIndex >= 0)
                    {
                        m_propFilter.text = m_Popup.items[m_Popup.selectedIndex];
                        OnSetProp(m_Popup.selectedIndex);
                    }
                    else
                    {
                        m_propFilter.text = "";
                    }
                }
            };
        }

        private string[] GetFilterResult() => m_propsLoaded
            .ToList()
            .Where((x) => m_propFilter.text.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Value + (PrefabUtils.AuthorList.TryGetValue(x.Value.Split('.')[0], out string author) ? "\n" + author : ""), m_propFilter.text, CompareOptions.IgnoreCase) >= 0)
            .Select(x => x.Key)
            .OrderBy((x) => x)
            .ToArray();
        private static string GetListName(PropInfo x) => (x?.name?.EndsWith("_Data") ?? true) ? $"{x?.GetLocalizedTitle()}" : x?.name;

        // Token: 0x040005C0 RID: 1472
        private UIListBox m_Popup;

        // Token: 0x040005C1 RID: 1473
        private UIScrollbar m_ActiveScrollbar;


        private MethodInfo PixelsToUnits = typeof(UIDropDown).GetMethod("PixelsToUnits", RedirectorUtils.allFlags);

        private void CreatePopup(UIDropDown refDD, UIPanel rootContainer)
        {
            Vector2 size2 = CalculatePopupSize(rootContainer, 6, refDD.itemHeight, refDD.itemPadding.vertical);
            m_Popup = rootContainer.AddUIComponent<UIListBox>();
            m_Popup.builtinKeyNavigation = refDD.builtinKeyNavigation;
            m_Popup.name = " - List";
            m_Popup.gameObject.hideFlags = HideFlags.DontSave;
            m_Popup.atlas = refDD.atlas;
            m_Popup.anchor = UIAnchorStyle.None;
            m_Popup.font = refDD.font;
            m_Popup.pivot = UIPivotPoint.TopLeft;
            m_Popup.size = size2;
            m_Popup.itemHeight = refDD.itemHeight;
            m_Popup.itemHighlight = refDD.itemHighlight;
            m_Popup.itemHover = refDD.itemHover;
            m_Popup.itemPadding = refDD.itemPadding;
            m_Popup.color = refDD.popupColor;
            m_Popup.itemTextColor = refDD.popupTextColor;
            m_Popup.textScale = refDD.textScale;
            //m_Popup.items = refDD.items;
            //m_Popup.filteredItems = (int[])m_FilteredItems.GetValue(refDD);
            m_Popup.listPadding = refDD.listPadding;
            m_Popup.normalBgSprite = refDD.listBackground;
            m_Popup.useDropShadow = refDD.useDropShadow;
            m_Popup.dropShadowColor = refDD.dropShadowColor;
            m_Popup.dropShadowOffset = refDD.dropShadowOffset;
            m_Popup.useGradient = refDD.useGradient;
            m_Popup.bottomColor = refDD.bottomColor;
            m_Popup.useOutline = refDD.useOutline;
            m_Popup.outlineColor = refDD.outlineColor;
            m_Popup.outlineSize = refDD.outlineSize;
            m_Popup.zOrder = int.MaxValue;
            if (size2.y >= refDD.listHeight && refDD.listScrollbar != null)
            {
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(refDD.listScrollbar.gameObject);
                m_ActiveScrollbar = gameObject.GetComponent<UIScrollbar>();
                float d = (float)PixelsToUnits.Invoke(refDD, new object[0]);
                Vector3 a = m_Popup.transform.TransformDirection(Vector3.right);
                Vector3 position = m_Popup.transform.position + a * (size2.x - m_ActiveScrollbar.width) * d;
                m_ActiveScrollbar.transform.parent = m_Popup.transform;
                m_ActiveScrollbar.transform.position = position;
                m_ActiveScrollbar.anchor = (UIAnchorStyle.Top | UIAnchorStyle.Bottom);
                m_ActiveScrollbar.height = m_Popup.height;
                m_Popup.width -= m_ActiveScrollbar.width;
                m_Popup.scrollbar = m_ActiveScrollbar;
                m_Popup.eventSizeChanged += delegate (UIComponent component, Vector2 size)
                {
                    m_ActiveScrollbar.height = component.height;
                };
            }
            m_Popup.EnsureVisible(m_Popup.selectedIndex);
        }



        private PropertyChangedEventHandler<int> PopupItemClicked(UIDropDown targetDD)
        {
            return (x, y) =>
            {
                targetDD.selectedIndex = y;
                targetDD.ClosePopup();
            };
        }

        private Vector2 CalculatePopupSize(UIPanel root, int itemCount, float itemHeight, float listPaddingVertical)
        {
            float num = root.size.x - root.padding.horizontal;
            float b = itemCount * itemHeight + listPaddingVertical;
            if (itemCount == 0)
            {
                b = itemHeight / 2 + listPaddingVertical;
            }
            return new Vector2(num, b);
        }



        private void OnSetProp(int sel) => EventPropChanged?.Invoke(sel < 0 ? null : m_lastSelection = PrefabUtils<PropInfo>.AssetsLoaded.Where(x => x.name == m_propsLoaded[m_Popup.items[sel]]).FirstOrDefault());


        #region Actions        
        protected void OnSetFont(int idx)
        {
            if (idx >= 0)
            {

            }
        }
        #endregion



        #region UI Utils
        private static void AddColorField(UIHelperExtension helper, string text, out UIColorField m_colorEditor)
        {
            m_colorEditor = helper.AddColorPicker(text, Color.white, (x) => { });
            KlyteMonoUtils.LimitWidthAndBox(m_colorEditor.parent.GetComponentInChildren<UILabel>(), helper.Self.width / 2, true);
        }


        protected static void SetIcon(UIButton copyButton, CommonsSpriteNames spriteName, Color color)
        {
            UISprite icon = copyButton.AddUIComponent<UISprite>();
            icon.relativePosition = new Vector3(2, 2);
            icon.width = 36;
            icon.height = 36;
            icon.spriteName = KlyteResourceLoader.GetDefaultSpriteNameFor(spriteName);
            icon.color = color;
        }

        protected static UIButton ConfigureActionButton(UIComponent parent)
        {
            KlyteMonoUtils.CreateUIElement(out UIButton actionButton, parent.transform, "DTPBtn");
            KlyteMonoUtils.InitButton(actionButton, false, "ButtonMenu");
            actionButton.focusedBgSprite = "";
            actionButton.autoSize = false;
            actionButton.width = 40;
            actionButton.height = 40;
            actionButton.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            return actionButton;
        }

        protected void AddSlider(string label, out UISlider slider, UIHelperExtension parentHelper, OnValueChanged onChange, float min, float max, float step)
        {
            slider = (UISlider)parentHelper.AddSlider(label, min, max, step, min, onChange);
            slider.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            slider.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            KlyteMonoUtils.LimitWidthAndBox(slider.parent.GetComponentInChildren<UILabel>(), (parentHelper.Self.width / 2) - 10, true);
        }

        protected void AddVector3Field(string label, out UITextField[] fieldArray, UIHelperExtension parentHelper, Action<Vector3> onChange)
        {
            fieldArray = parentHelper.AddVector3Field(label, Vector3.zero, onChange);
            KlyteMonoUtils.LimitWidthAndBox(fieldArray[0].parent.GetComponentInChildren<UILabel>(), (parentHelper.Self.width / 2) - 10, true);
            fieldArray[0].zOrder = 1;
            fieldArray[1].zOrder = 2;
            fieldArray[2].zOrder = 3;
        }

        protected void AddFloatField(string label, out UITextField field, UIHelperExtension parentHelper, Action<float> onChange, bool acceptNegative)
        {
            field = parentHelper.AddFloatField(label, 0, onChange, acceptNegative);
            KlyteMonoUtils.LimitWidthAndBox(field.parent.GetComponentInChildren<UILabel>(), (parentHelper.Self.width / 2) - 10, true);
        }
        protected void AddIntField(string label, out UITextField field, UIHelperExtension parentHelper, Action<int> onChange, bool acceptNegative)
        {
            field = parentHelper.AddIntField(label, 0, onChange, acceptNegative);
            KlyteMonoUtils.LimitWidthAndBox(field.parent.GetComponentInChildren<UILabel>(), (parentHelper.Self.width / 2) - 10, true);
        }
        protected void AddDropdown(string title, out UIDropDown dropdown, UIHelperExtension parentHelper, string[] options, OnDropdownSelectionChanged onChange) => AddDropdown(title, out dropdown, out UILabel label, parentHelper, options, onChange);
        protected void AddDropdown(string title, out UIDropDown dropdown, out UILabel label, UIHelperExtension parentHelper, string[] options, OnDropdownSelectionChanged onChange)
        {
            dropdown = (UIDropDown)parentHelper.AddDropdown(title, options, 0, onChange);
            dropdown.width = (parentHelper.Self.width / 2) - 10;
            dropdown.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            dropdown.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            label = dropdown.parent.GetComponentInChildren<UILabel>();
            KlyteMonoUtils.LimitWidthAndBox(label, (parentHelper.Self.width / 2) - 10);
            label.padding.top = 10;
        }


        protected void AddTextField(string title, out UITextField textField, UIHelperExtension parentHelper, OnTextSubmitted onSubmit, OnTextChanged onChanged = null) => AddTextField(title, out textField, out UILabel label, parentHelper, onSubmit, onChanged);

        protected void AddTextField(string title, out UITextField textField, out UILabel label, UIHelperExtension parentHelper, OnTextSubmitted onSubmit, OnTextChanged onChanged = null)
        {
            textField = parentHelper.AddTextField(title, onChanged, "", onSubmit);
            textField.width = (parentHelper.Self.width / 2) - 10;
            textField.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            textField.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            label = textField.parent.GetComponentInChildren<UILabel>();
            KlyteMonoUtils.LimitWidthAndBox(label, (parentHelper.Self.width / 2) - 10, true);
        }


        #endregion
    }

}
