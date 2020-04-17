using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Overrides;
using Klyte.DynamicTextProps.Utils;
using System;
using System.Linq;
using UnityEngine;
using static Klyte.DynamicTextProps.UI.DTPEditorUILib;


namespace Klyte.DynamicTextProps.UI
{

    internal class DTPTextEditor : UICustomControl
    {
        public static DTPTextEditor Instance { get; private set; }
        public UIPanel MainContainer { get; protected set; }

        private UITabstrip m_tabstrip;
        private UITabContainer m_tabContainer;

        private UIPanel m_tabSize;
        private UIPanel m_tabAppearence;
        private UIPanel m_tabConfig;

        public void Awake()
        {
            Instance = this;

            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.padding = new RectOffset(5, 5, 5, 5);
            MainContainer.autoLayoutPadding = new RectOffset(0, 0, 3, 3);

            KlyteMonoUtils.CreateTabsComponent(out m_tabstrip, out m_tabContainer, MainContainer.transform, "TextEditor", new Vector4(0, 0, MainContainer.width, 40), new Vector4(0, 0, MainContainer.width, MainContainer.height - 40));
            m_tabSize = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_MoveCross), Locale.Get("K45_DTP_TEXT_SIZE_ATTRIBUTES"), "TxtSize");
            m_tabAppearence = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_AutoColorIcon), Locale.Get("K45_DTP_TEXT_APPEARENCE_ATTRIBUTES"), "TxtApp");
            m_tabConfig = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_AutoNameIcon), Locale.Get("K45_DTP_TEXT_CONFIGURATION_ATTRIBUTES"), "TxtCnf");

            var helperSize = new UIHelperExtension(m_tabSize, LayoutDirection.Vertical);
            var helperAppearance = new UIHelperExtension(m_tabAppearence, LayoutDirection.Vertical);
            var helperConfig = new UIHelperExtension(m_tabConfig, LayoutDirection.Vertical);

            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_POS"), out UITextField[] arrayCoord, helperSize, OnPositionChange);
            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_ROT"), out UITextField[] arrayRotation, helperSize, OnRotationChange);
            AddFloatField(Locale.Get("K45_DTP_TEXT_SCALE"), out UITextField textScale, helperSize, OnScaleSubmit, false);
            AddFloatField(Locale.Get("K45_DTP_MAX_WIDTH_METERS"), out UITextField maxWidth, helperSize, OnMaxWidthChange, false);
            UICheckBox applyScaleOnY = helperSize.AddCheckboxLocale("K45_DTP_RESIZE_Y_TEXT_OVERFLOW", false, OnChangeApplyRescaleOnY);


            AddDropdown(Locale.Get("K45_DTP_TEXT_ALIGN_HOR"), out UIDropDown m_dropdownTextAlignHorizontal, helperAppearance, Enum.GetNames(typeof(UIHorizontalAlignment)).Select(x => Locale.Get("K45_ALIGNMENT", x)).ToArray(), OnSetTextAlignmentHorizontal);
            AddDropdown(Locale.Get("K45_DTP_TEXT_ALIGN_VER"), out UIDropDown m_dropdownTextAlignVertical, helperAppearance, Enum.GetNames(typeof(UIVerticalAlignment)).Select(x => Locale.Get("K45_VERT_ALIGNMENT", x)).ToArray(), OnSetTextAlignmentVertical);
            helperAppearance.AddSpace(5);
            helperAppearance.AddCheckboxLocale("K45_DTP_USE_CONTRAST_COLOR", false, OnContrastColorChange);
            AddColorField(helperAppearance, Locale.Get("K45_DTP_TEXT_COLOR"), out UIColorField textFixedColor, OnFixedColorChanged);
            helperAppearance.AddSpace(5);
            AddDropdown(Locale.Get("K45_DTP_OVERRIDE_TEXT_SHADER"), out UIDropDown shaderSelector, helperAppearance, new string[] { Locale.Get("K45_DTP_DEFAULT_SHADER_OPTION") }.Union(Shader.FindObjectsOfType<Shader>().Select(x => x.name).OrderBy(x => x)).ToArray(), OnSetShader);


            AddDropdown(Locale.Get("K45_DTP_TEXT_CONTENT"), out UIDropDown m_dropdownTextContent, helperConfig, Enum.GetNames(typeof(TextType)).Select(x => Locale.Get("K45_DTP_BOARD_TEXT_TYPE_DESC", x.ToString())).ToArray(), OnSetTextOwnNameContent);
            AddTextField(Locale.Get("K45_DTP_CUSTOM_TEXT"), out UITextField m_customText, helperConfig, OnSetTextCustom);
            helperConfig.AddSpace(5);
            AddDropdown(Locale.Get("K45_DTP_OVERRIDE_FONT"), out UIDropDown overrideFontSelect, helperConfig, new string[0], OnSetOverrideFont);
            AddTextField(Locale.Get("K45_DTP_PREFIX"), out UITextField m_textPrefix, helperConfig, OnSetPrefix);
            AddTextField(Locale.Get("K45_DTP_SUFFIX"), out UITextField m_textSuffix, helperConfig, OnSetSuffix);

            DTPUtils.ReloadFontsOf(overrideFontSelect, true);


        }

        private void OnSetSuffix(string text) { }
        private void OnSetPrefix(string text) { }
        private void OnSetOverrideFont(int sel) { }
        private void OnSetTextCustom(string text) { }
        private void OnSetTextOwnNameContent(int sel) { }
        private void OnSetShader(int sel) { }
        private void OnChangeApplyRescaleOnY(bool isChecked) { }
        private void OnSetTextAlignmentHorizontal(int sel) { }
        private void OnSetTextAlignmentVertical(int sel) { }
        private void OnFixedColorChanged(UIComponent component, Color value) { }
        private void OnMaxWidthChange(float obj) { }
        private void OnScaleSubmit(float scale) { }
        private void OnContrastColorChange(bool isChecked) { }
        private void OnRotationChange(Vector3 obj) { }
        private void OnPositionChange(Vector3 obj) { }
    }

}
