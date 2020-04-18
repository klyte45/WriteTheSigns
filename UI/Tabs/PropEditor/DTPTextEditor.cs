using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Utils;
using Klyte.DynamicTextProps.Xml;
using System;
using System.Linq;
using UnityEngine;
using static Klyte.DynamicTextProps.UI.DTPEditorUILib;


namespace Klyte.DynamicTextProps.UI
{

    internal class DTPTextEditor : UICustomControl
    {
        public UIPanel MainContainer { get; protected set; }

        private UITabstrip m_tabstrip;
        private UITabContainer m_tabContainer;

        private UIPanel m_tabSize;
        private UIPanel m_tabAppearence;
        private UIPanel m_tabConfig;

        private UITextField[] m_arrayCoord;
        private UITextField[] m_arrayRotation;
        private UITextField m_textScale;
        private UITextField m_maxWidth;
        private UICheckBox m_applyScaleOnY;


        private UIDropDown m_dropdownTextAlignHorizontal;
        private UIDropDown m_dropdownTextAlignVertical;
        private UICheckBox m_useContrastColor;
        private UIColorField m_textFixedColor;


        private UIDropDown m_dropdownTextContent;
        private UITextField m_customText;
        private UIDropDown m_overrideFontSelect;
        private UITextField m_textPrefix;
        private UITextField m_textSuffix;

        private int TabToEdit => DTPPropTextLayoutEditor.Instance.CurrentTab - 1;
        private bool m_isEditing = true;
        private UICheckBox m_allCaps;

        public void Awake()
        {
            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.padding = new RectOffset(5, 5, 5, 5);
            MainContainer.autoLayoutPadding = new RectOffset(0, 0, 3, 3);

            KlyteMonoUtils.CreateTabsComponent(out m_tabstrip, out m_tabContainer, MainContainer.transform, "TextEditor", new Vector4(0, 0, MainContainer.width, 40), new Vector4(0, 0, MainContainer.width, MainContainer.height - 40));
            m_tabSize = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_MoveCross), "K45_DTP_TEXT_SIZE_ATTRIBUTES", "TxtSize");
            m_tabAppearence = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_AutoColorIcon), "K45_DTP_TEXT_APPEARENCE_ATTRIBUTES", "TxtApp");
            m_tabConfig = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_AutoNameIcon), "K45_DTP_TEXT_CONFIGURATION_ATTRIBUTES", "TxtCnf");

            var helperSize = new UIHelperExtension(m_tabSize, LayoutDirection.Vertical);
            var helperAppearance = new UIHelperExtension(m_tabAppearence, LayoutDirection.Vertical);
            var helperConfig = new UIHelperExtension(m_tabConfig, LayoutDirection.Vertical);

            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_POS"), out m_arrayCoord, helperSize, OnPositionChange);
            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_ROT"), out m_arrayRotation, helperSize, OnRotationChange);
            AddFloatField(Locale.Get("K45_DTP_TEXT_SCALE"), out m_textScale, helperSize, OnScaleSubmit, false);
            AddFloatField(Locale.Get("K45_DTP_MAX_WIDTH_METERS"), out m_maxWidth, helperSize, OnMaxWidthChange, false);
            m_applyScaleOnY = helperSize.AddCheckboxLocale("K45_DTP_RESIZE_Y_TEXT_OVERFLOW", false, OnChangeApplyRescaleOnY);


            AddDropdown(Locale.Get("K45_DTP_TEXT_ALIGN_HOR"), out m_dropdownTextAlignHorizontal, helperAppearance, Enum.GetNames(typeof(UIHorizontalAlignment)).Select(x => Locale.Get("K45_ALIGNMENT", x)).ToArray(), OnSetTextAlignmentHorizontal);
            AddDropdown(Locale.Get("K45_DTP_TEXT_ALIGN_VER"), out m_dropdownTextAlignVertical, helperAppearance, Enum.GetNames(typeof(UIVerticalAlignment)).Select(x => Locale.Get("K45_VERT_ALIGNMENT", x)).ToArray(), OnSetTextAlignmentVertical);
            helperAppearance.AddSpace(5);
            m_useContrastColor = helperAppearance.AddCheckboxLocale("K45_DTP_USE_CONTRAST_COLOR", false, OnContrastColorChange);
            AddColorField(helperAppearance, Locale.Get("K45_DTP_TEXT_COLOR"), out m_textFixedColor, OnFixedColorChanged);
            //helperAppearance.AddSpace(5);
            //AddDropdown(Locale.Get("K45_DTP_OVERRIDE_TEXT_SHADER"), out UIDropDown shaderSelector, helperAppearance, new string[] { Locale.Get("K45_DTP_DEFAULT_SHADER_OPTION") }.Union(Shader.FindObjectsOfType<Shader>().Select(x => x.name).OrderBy(x => x)).ToArray(), OnSetShader);


            AddDropdown(Locale.Get("K45_DTP_TEXT_CONTENT"), out m_dropdownTextContent, helperConfig, Enum.GetNames(typeof(TextType)).Select(x => Locale.Get("K45_DTP_BOARD_TEXT_TYPE_DESC", x.ToString())).ToArray(), OnSetTextOwnNameContent);
            AddTextField(Locale.Get("K45_DTP_CUSTOM_TEXT"), out m_customText, helperConfig, OnSetTextCustom);
            helperConfig.AddSpace(5);
            AddDropdown(Locale.Get("K45_DTP_OVERRIDE_FONT"), out m_overrideFontSelect, helperConfig, new string[0], OnSetOverrideFont);
            AddTextField(Locale.Get("K45_DTP_PREFIX"), out m_textPrefix, helperConfig, OnSetPrefix);
            AddTextField(Locale.Get("K45_DTP_SUFFIX"), out m_textSuffix, helperConfig, OnSetSuffix);
            m_allCaps = helperConfig.AddCheckboxLocale("K45_DTP_TEXT_ALL_CAPS", false, OnSetAllCaps);

            DTPUtils.ReloadFontsOf(m_overrideFontSelect, true);

            DTPPropTextLayoutEditor.Instance.CurrentTabChanged += (newVal) =>
            {
                int targetTab = newVal - 1;
                SafeObtain((ref BoardTextDescriptorGeneralXml x) =>
                {
                    m_arrayCoord[0].text = x.m_textRelativePosition.x.ToString("F3");
                    m_arrayCoord[1].text = x.m_textRelativePosition.y.ToString("F3");
                    m_arrayCoord[2].text = x.m_textRelativePosition.z.ToString("F3");
                    m_arrayRotation[0].text = x.m_textRelativeRotation.x.ToString("F3");
                    m_arrayRotation[1].text = x.m_textRelativeRotation.y.ToString("F3");
                    m_arrayRotation[2].text = x.m_textRelativeRotation.z.ToString("F3");
                    m_textScale.text = x.m_textScale.ToString("F3");
                    m_maxWidth.text = x.m_maxWidthMeters.ToString("F3");
                    m_applyScaleOnY.isChecked = x.m_applyOverflowResizingOnY;

                    m_dropdownTextAlignHorizontal.selectedIndex = (int)x.m_textAlign;
                    m_dropdownTextAlignVertical.selectedIndex = (int)x.m_verticalAlign;
                    m_useContrastColor.isChecked = x.m_useContrastColor;
                    m_textFixedColor.selectedColor = x.m_defaultColor;

                    m_dropdownTextContent.selectedIndex = (int)x.m_textType;
                    m_customText.text = x.m_fixedText ?? "";
                    m_overrideFontSelect.selectedIndex = x.m_overrideFont == null ? 0 : Array.IndexOf(m_overrideFontSelect.items, x.m_overrideFont);
                    m_textPrefix.text = x.m_prefix ?? "";
                    m_textSuffix.text = x.m_suffix ?? "";
                    m_allCaps.isChecked = x.m_allCaps;


                    m_customText.parent.isVisible = x.m_textType == TextType.Fixed;
                    m_textFixedColor.parent.isVisible = !x.m_useContrastColor;
                }, targetTab);
            };
            m_isEditing = false;
        }


        private delegate void SafeObtainMethod(ref BoardTextDescriptorGeneralXml x);

        private void SafeObtain(SafeObtainMethod action, int? targetTab = null)
        {
            if (m_isEditing)
            {
                return;
            }

            lock (this)
            {
                m_isEditing = true;
                try
                {
                    int effTargetTab = Math.Max(0, targetTab ?? TabToEdit);
                    if (effTargetTab < DTPPropTextLayoutEditor.Instance.EditingInstance.m_textDescriptors.Length)
                    {
                        action(ref DTPPropTextLayoutEditor.Instance.EditingInstance.m_textDescriptors[effTargetTab]);
                    }
                }
                finally
                {
                    m_isEditing = false;
                }
            }
        }

        private void OnSetSuffix(string text) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_suffix = text);
        private void OnSetPrefix(string text) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_prefix = text);
        private void OnSetOverrideFont(int sel) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) =>
        {
            if (sel > 0 && sel < (m_overrideFontSelect?.items?.Length ?? 0))
            {
                desc.m_overrideFont = m_overrideFontSelect.items[sel];
            }
            else
            {
                desc.m_overrideFont = null;
            }
        });
        private void OnSetTextCustom(string text) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_fixedText = text);
        private void OnSetTextOwnNameContent(int sel)
        {
            SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_textType = (TextType)sel);
            m_customText.parent.isVisible = (TextType)sel == TextType.Fixed;
        }

        //private void OnSetShader(int sel) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => { });
        private void OnChangeApplyRescaleOnY(bool isChecked) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_applyOverflowResizingOnY = isChecked);
        private void OnSetTextAlignmentHorizontal(int sel) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_textAlign = (UIHorizontalAlignment)sel);
        private void OnSetTextAlignmentVertical(int sel) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_verticalAlign = (UIVerticalAlignment)sel);
        private void OnFixedColorChanged(UIComponent component, Color value) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_defaultColor = value);
        private void OnMaxWidthChange(float obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_maxWidthMeters = obj);
        private void OnScaleSubmit(float scale) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_textScale = scale);
        private void OnContrastColorChange(bool isChecked)
        {
            SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_useContrastColor = isChecked);
            m_textFixedColor.parent.isVisible = !isChecked;
        }

        private void OnRotationChange(Vector3 obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_textRelativeRotation = obj);
        private void OnPositionChange(Vector3 obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_textRelativePosition = obj);
        private void OnSetAllCaps(bool isChecked) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_allCaps = isChecked);
    }

}
