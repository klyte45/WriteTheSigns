using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ICities;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using System;
using System.Reflection;
using UnityEngine;


namespace Klyte.WriteTheCity.UI
{

    internal static class WTCEditorUILib
    {
        #region UI Utils
        public static void AddColorField(UIHelperExtension helper, string text, out UIColorField m_colorEditor, PropertyChangedEventHandler<Color> onSelectedColorChanged)
        {
            m_colorEditor = helper.AddColorPicker(text, Color.white, (x) => { });
            KlyteMonoUtils.LimitWidthAndBox(m_colorEditor.parent.GetComponentInChildren<UILabel>(), helper.Self.width / 2, true);
            m_colorEditor.eventSelectedColorChanged += onSelectedColorChanged;
        }
        public static void AddIntField(string label, out UITextField field, UIHelperExtension parentHelper, Action<int> onChange, bool acceptNegative)
        {
            field = parentHelper.AddIntField(label, 0, onChange, acceptNegative);
            KlyteMonoUtils.LimitWidthAndBox(field.parent.GetComponentInChildren<UILabel>(), (parentHelper.Self.width / 2) - 10, true);
        }

        public static UIButton ConfigureActionButton(UIComponent parent)
        {
            KlyteMonoUtils.CreateUIElement(out UIButton actionButton, parent.transform, "WTCBtn");
            KlyteMonoUtils.InitButton(actionButton, false, "ButtonMenu");
            actionButton.focusedBgSprite = "";
            actionButton.autoSize = false;
            actionButton.width = 40;
            actionButton.height = 40;
            actionButton.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            return actionButton;
        }

        public static void AddSlider(string label, out UISlider slider, UIHelperExtension parentHelper, OnValueChanged onChange, float min, float max, float step)
        {
            slider = (UISlider)parentHelper.AddSlider(label, min, max, step, min, onChange);
            slider.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            slider.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            KlyteMonoUtils.LimitWidthAndBox(slider.parent.GetComponentInChildren<UILabel>(), (parentHelper.Self.width / 2) - 10, true);
        }
        public static void AddVector3Field(string label, out UITextField[] fieldArray, UIHelperExtension parentHelper, Action<Vector3> onChange)
        {
            fieldArray = parentHelper.AddVector3Field(label, Vector3.zero, onChange);
            KlyteMonoUtils.LimitWidthAndBox(fieldArray[0].parent.GetComponentInChildren<UILabel>(), (parentHelper.Self.width / 2) - 10, true);
            fieldArray.ForEach(x =>
            {
                x.eventMouseWheel += RollFloat;
                x.tooltip = Locale.Get("K45_CMNS_FLOAT_EDITOR_TOOLTIP_HELP");
            });
            fieldArray[0].zOrder = 1;
            fieldArray[1].zOrder = 2;
            fieldArray[2].zOrder = 3;
        }

        private static readonly MethodInfo m_submitField = typeof(UITextField).GetMethod("OnSubmit", RedirectorUtils.allFlags);
        public static void RollFloat(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is UITextField tf && tf.numericalOnly && float.TryParse(tf.text, out float currentValue))
            {
                bool ctrlPressed = Event.current.control;
                bool shiftPressed = Event.current.shift;
                bool altPressed = Event.current.alt;
                tf.text = Mathf.Max(tf.allowNegative ? float.MinValue : 0, currentValue + 0.0003f + (eventParam.wheelDelta * (altPressed && ctrlPressed ? 0.001f : ctrlPressed ? 0.1f : altPressed ? 0.01f : shiftPressed ? 10 : 1))).ToString("F3");
                m_submitField.Invoke(tf, new object[0]);
            }
        }

        public static void AddFloatField(string label, out UITextField field, UIHelperExtension parentHelper, Action<float> onChange, bool acceptNegative)
        {
            field = parentHelper.AddFloatField(label, 0, onChange, acceptNegative);
            field.width = 90;
            KlyteMonoUtils.LimitWidthAndBox(field.parent.GetComponentInChildren<UILabel>(), (parentHelper.Self.width / 2) - 10, true);
            field.eventMouseWheel += RollFloat;
        }
        public static void AddDropdown(string title, out UIDropDown dropdown, UIHelperExtension parentHelper, string[] options, OnDropdownSelectionChanged onChange) => AddDropdown(title, out dropdown, out UILabel label, parentHelper, options, onChange);
        public static void AddDropdown(string title, out UIDropDown dropdown, out UILabel label, UIHelperExtension parentHelper, string[] options, OnDropdownSelectionChanged onChange)
        {
            dropdown = (UIDropDown)parentHelper.AddDropdown(title, options, 0, onChange);
            dropdown.width = (parentHelper.Self.width / 2) - 10;
            dropdown.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            dropdown.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            label = dropdown.parent.GetComponentInChildren<UILabel>();
            KlyteMonoUtils.LimitWidthAndBox(label, (parentHelper.Self.width / 2) - 10);
            label.padding.top = 10;
        }
        public static void AddTextField(string title, out UITextField textField, UIHelperExtension parentHelper, OnTextSubmitted onSubmit, OnTextChanged onChanged = null) => AddTextField(title, out textField, out UILabel label, parentHelper, onSubmit, onChanged);
        public static void AddTextField(string title, out UITextField textField, out UILabel label, UIHelperExtension parentHelper, OnTextSubmitted onSubmit, OnTextChanged onChanged = null)
        {
            textField = parentHelper.AddTextField(title, onChanged, "", onSubmit);
            textField.width = (parentHelper.Self.width / 2) - 10;
            textField.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            textField.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            label = textField.parent.GetComponentInChildren<UILabel>();
            KlyteMonoUtils.LimitWidthAndBox(label, (parentHelper.Self.width / 2) - 10, true);
        }
        public static UIListBox CreatePopup(UIDropDown refDD, UIPanel rootContainer)
        {
            UIListBox popup;
            Vector2 size2 = CalculatePopupSize(rootContainer, 6, refDD.itemHeight, refDD.itemPadding.vertical);
            popup = rootContainer.AddUIComponent<UIListBox>();
            popup.builtinKeyNavigation = refDD.builtinKeyNavigation;
            popup.name = " - List";
            popup.gameObject.hideFlags = HideFlags.DontSave;
            popup.atlas = refDD.atlas;
            popup.anchor = UIAnchorStyle.None;
            popup.font = refDD.font;
            popup.pivot = UIPivotPoint.TopLeft;
            popup.size = size2;
            popup.itemHeight = refDD.itemHeight;
            popup.itemHighlight = refDD.itemHighlight;
            popup.itemHover = refDD.itemHover;
            popup.itemPadding = refDD.itemPadding;
            popup.color = refDD.popupColor;
            popup.itemTextColor = refDD.popupTextColor;
            popup.textScale = refDD.textScale;
            //m_Popup.items = refDD.items;
            //m_Popup.filteredItems = (int[])m_FilteredItems.GetValue(refDD);
            popup.listPadding = refDD.listPadding;
            popup.normalBgSprite = refDD.listBackground;
            popup.useDropShadow = refDD.useDropShadow;
            popup.dropShadowColor = refDD.dropShadowColor;
            popup.dropShadowOffset = refDD.dropShadowOffset;
            popup.useGradient = refDD.useGradient;
            popup.bottomColor = refDD.bottomColor;
            popup.useOutline = refDD.useOutline;
            popup.outlineColor = refDD.outlineColor;
            popup.outlineSize = refDD.outlineSize;
            popup.zOrder = int.MaxValue;
            popup.EnsureVisible(popup.selectedIndex);

            return popup;
        }

        private static Vector2 CalculatePopupSize(UIPanel root, int itemCount, float itemHeight, float listPaddingVertical)
        {
            float num = root.size.x - root.padding.horizontal;
            float b = itemCount * itemHeight + listPaddingVertical;
            if (itemCount == 0)
            {
                b = itemHeight / 2 + listPaddingVertical;
            }
            return new Vector2(num, b);
        }

        #endregion
    }

}
