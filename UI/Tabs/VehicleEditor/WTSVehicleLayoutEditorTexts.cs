using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using FontStashSharp;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Libraries;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSVehicleLayoutEditorTexts : UICustomControl
    {
        public UIPanel MainContainer { get; protected set; }

        private UITabstrip m_tabstrip;
        private UITabContainer m_tabContainer;

        private UIPanel m_tabSettings;
        private UIPanel m_tabSize;
        private UIPanel m_tabAppearence;
        private UIScrollablePanel m_tabIllumination;
        private UIScrollablePanel m_tabFrame;
        private UIPanel m_tabConfig;

        private UITextField m_tabName;


        private UITextField[] m_arrayCoord;
        private UITextField[] m_arrayRotation;
        private UITextField m_textScale;
        private UITextField m_maxWidth;
        private UICheckBox m_applyScaleOnY;
        private UICheckBox m_create180degSimmetricClone;
        private UICheckBox m_invertTextHorizontalAlignClone;


        private UITextField[] m_bgSize;
        private UIColorField m_bgColor;
        private UIDropDown m_dropdownTextAlignHorizontal;
        private UICheckBox m_useContrastColor;
        private UIColorField m_textFixedColor;


        private UIDropDown m_dropdownMaterialType;
        private UISlider m_sliderIllumination;
        private UIDropDown m_dropdownBlinkType;
        private UITextField[] m_arrayCustomBlink;
        private UIComponent m_flagsContainer;
        private Dictionary<Vehicle.Flags, UIMultiStateButton> m_flagsState;

        private UICheckBox m_useFrame;
        private UITextField[] m_frameBackSize;
        private UITextField[] m_frameBackOffset;
        private UITextField[] m_frameDepths;
        private UITextField m_frameFrontBorder;
        private UIColorField m_frameColor;
        private UICheckBox m_frameUseVehicleColor;
        private UISlider m_frameGlassTransparency;
        private UIColorField m_frameGlassColor;
        private UISlider m_frameOuterSpecularLevel;
        private UISlider m_frameGlassSpecularLevel;

        private UIDropDown m_dropdownTextContent;
        private UITextField m_customText;
        private UIDropDown m_overrideFontSelect;
        private UIDropDown m_fontClassSelect;
        private UITextField m_textPrefix;
        private UITextField m_textSuffix;

        private int TabToEdit => WTSVehicleLayoutEditor.Instance.CurrentTab - 1;
        private bool m_isEditing = true;
        private UICheckBox m_allCaps;
        private UICheckBox m_applyAbbreviations;

        private UIButton m_pasteButtonText;

        private string m_clipboard;
        private UITextField m_spriteFilter;

        public void Awake()
        {
            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.padding = new RectOffset(5, 5, 5, 5);
            MainContainer.autoLayoutPadding = new RectOffset(0, 0, 3, 3);

            KlyteMonoUtils.CreateTabsComponent(out m_tabstrip, out m_tabContainer, MainContainer.transform, "TextEditor", new Vector4(0, 0, MainContainer.width, 40), new Vector4(0, 0, MainContainer.width, 315));
            m_tabSettings = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Settings), "K45_WTS_GENERAL_SETTINGS", "TxtSettings");
            m_tabSize = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_MoveCross), "K45_WTS_TEXT_SIZE_ATTRIBUTES", "TxtSize");
            m_tabAppearence = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_AutoColorIcon), "K45_WTS_TEXT_APPEARANCE_ATTRIBUTES", "TxtApp");
            m_tabFrame = TabCommons.CreateScrollableTabLocalized(m_tabstrip, "frame", "K45_WTS_TEXT_CONTAINERFRAME_ATTRIBUTES", "TxtFrm");
            m_tabIllumination = TabCommons.CreateScrollableTabLocalized(m_tabstrip, "SubBarPropsCommonLights", "K45_WTS_TEXT_ILLUMINATION_ATTRIBUTES", "TxtIll");
            m_tabConfig = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_AutoNameIcon), "K45_WTS_TEXT_CONFIGURATION_ATTRIBUTES", "TxtCnf");

            var helperSettings = new UIHelperExtension(m_tabSettings, LayoutDirection.Vertical);
            var helperSize = new UIHelperExtension(m_tabSize, LayoutDirection.Vertical);
            var helperAppearance = new UIHelperExtension(m_tabAppearence, LayoutDirection.Vertical);
            var helperFrame = new UIHelperExtension(m_tabFrame, LayoutDirection.Vertical);
            var helperIllumination = new UIHelperExtension(m_tabIllumination, LayoutDirection.Vertical);
            var helperConfig = new UIHelperExtension(m_tabConfig, LayoutDirection.Vertical);

            AddTextField(Locale.Get("K45_WTS_TEXT_TAB_TITLE"), out m_tabName, helperSettings, OnTabNameChanged);

            AddVector3Field(Locale.Get("K45_WTS_RELATIVE_POS"), out m_arrayCoord, helperSize, OnPositionChange);
            AddVector3Field(Locale.Get("K45_WTS_RELATIVE_ROT"), out m_arrayRotation, helperSize, OnRotationChange);
            AddFloatField(Locale.Get("K45_WTS_TEXT_SCALE"), out m_textScale, helperSize, OnScaleSubmit, false);
            AddFloatField(Locale.Get("K45_WTS_MAX_WIDTH_METERS"), out m_maxWidth, helperSize, OnMaxWidthChange, false);
            AddCheckboxLocale("K45_WTS_RESIZE_Y_TEXT_OVERFLOW", out m_applyScaleOnY, helperSize, OnChangeApplyRescaleOnY);
            AddCheckboxLocale("K45_WTS_CREATE_CLONE_180DEG", out m_create180degSimmetricClone, helperSize, OnChangeCreateSimmetricClone);
            AddCheckboxLocale("K45_WTS_CLONE_180DEG_INVERT_TEXT_HOR_ALIGN", out m_invertTextHorizontalAlignClone, helperSize, OnChangeInvertCloneTextHorizontalAlignment);

            AddVector2Field(Locale.Get("K45_WTS_TEXTBACKGROUNDSIZEGENERATED"), out m_bgSize, helperAppearance, OnBgSizeChanged); m_bgSize.ForEach(x => x.allowNegative = false);
            AddColorField(helperAppearance, Locale.Get("K45_WTS_BG_COLOR"), out m_bgColor, OnBgColorChanged);
            helperAppearance.AddSpace(5);
            AddColorField(helperAppearance, Locale.Get("K45_WTS_TEXT_COLOR"), out m_textFixedColor, OnFixedColorChanged);
            AddCheckboxLocale("K45_WTS_USE_CONTRAST_COLOR", out m_useContrastColor, helperAppearance, OnContrastColorChange);
            helperAppearance.AddSpace(5);
            AddDropdown(Locale.Get("K45_WTS_TEXT_ALIGN_HOR"), out m_dropdownTextAlignHorizontal, helperAppearance, Enum.GetNames(typeof(UIHorizontalAlignment)).Select(x => Locale.Get("K45_ALIGNMENT", x)).ToArray(), OnSetTextAlignmentHorizontal);

            AddCheckboxLocale("K45_WTS_TEXT_USEFRAME", out m_useFrame, helperFrame, OnUseFrameChange);
            AddCheckboxLocale("K45_WTS_TEXT_CONTAINERUSEVEHICLECOLOR", out m_frameUseVehicleColor, helperFrame, OnFrameUseVehicleColorChange);
            AddColorField(helperFrame, Locale.Get("K45_WTS_TEXT_CONTAINEROUTERCOLOR"), out m_frameColor, OnFrameColorChanged);
            AddSlider(Locale.Get("K45_WTS_TEXT_CONTAINEROUTERSPECULARITY"), out m_frameOuterSpecularLevel, helperFrame, OnFrameOuterSpecularLevelChanged, 0, 1, 0.01f, (x) => (x * 100).ToString("F0") + "%");
            AddVector2Field(Locale.Get("K45_WTS_TEXT_CONTAINERBACKSIZE"), out m_frameBackSize, helperFrame, OnFrameBackSizeChanged, true, false, false);
            AddVector2Field(Locale.Get("K45_WTS_TEXT_CONTAINERBACKOFFSET"), out m_frameBackOffset, helperFrame, OnFrameBackOffsetChanged);
            AddVector2Field(Locale.Get("K45_WTS_TEXT_CONTAINERDEPTHFRONTBACK"), out m_frameDepths, helperFrame, OnFrameDepthsChanged, true, false, false);
            AddFloatField(Locale.Get("K45_WTS_TEXT_CONTAINERFRONTBORDERTHICKNESS"), out m_frameFrontBorder, helperFrame, OnFrameBorderThicknessChanged, false);
            AddColorField(helperFrame, Locale.Get("K45_WTS_TEXT_CONTAINERGLASSCOLOR"), out m_frameGlassColor, OnFrameGlassColorChanged);
            AddSlider(Locale.Get("K45_WTS_TEXT_CONTAINERGLASSTRANSPARENCY"), out m_frameGlassTransparency, helperFrame, OnFrameGlassTransparencyChanged, 0, 1, 0.01f, (x) => (x * 100).ToString("F0") + "%");
            AddSlider(Locale.Get("K45_WTS_TEXT_CONTAINERGLASSSPECULARITY"), out m_frameGlassSpecularLevel, helperFrame, OnFrameGlassSpecularLevelChanged, 0, 1, 0.01f, (x) => (x * 100).ToString("F0") + "%");

            AddDropdown(Locale.Get("K45_WTS_TEXT_MATERIALTYPE"), out m_dropdownMaterialType, helperIllumination, Enum.GetNames(typeof(MaterialType)).Select(x => Locale.Get("K45_WTS_TEXTMATERIALTYPE", x.ToString())).ToArray(), OnSetMaterialType);
            AddSlider(Locale.Get("K45_WTS_TEXT_ILLUMINATIONSTRENGTH"), out m_sliderIllumination, helperIllumination, OnChangeIlluminationStrength, 0, 1, 0.025f, (x) => $"{x.ToString("P1")}");
            AddDropdown(Locale.Get("K45_WTS_TEXT_BLINKTYPE"), out m_dropdownBlinkType, helperIllumination, Enum.GetNames(typeof(BlinkType)).Select(x => Locale.Get("K45_WTS_BLINKTYPE", x.ToString())).ToArray(), OnSetBlinkType);
            AddVector4Field(Locale.Get("K45_WTS_TEXT_CUSTOMBLINKPARAMS"), out m_arrayCustomBlink, helperIllumination, OnCustomBlinkChange);



            m_flagsState = new Dictionary<Vehicle.Flags, UIMultiStateButton>();
            var flagsCheck = helperIllumination.AddGroupExtended(Locale.Get("K45_WTS_FLAGSREQUREDFORBIDDEN"));
            m_flagsContainer = flagsCheck.Self.parent;
            ((UIPanel)flagsCheck.Self).autoLayoutDirection = LayoutDirection.Horizontal;
            ((UIPanel)flagsCheck.Self).wrapLayout = true;

            (Enum.GetValues(typeof(Vehicle.Flags)) as Vehicle.Flags[]).ForEach(f =>
            {
                AddMultistateButton(f.ToString(), flagsCheck.Self, out UIMultiStateButton button, out _, out _, flagsCheck.Self.width / 4.1f, new string[] { "AchievementCheckedFalse", "AchievementCheckedTrue", "AchievementCheckedTrueNegative", }, (x, y) => OnSetStateFlag(f, y), new Vector2(20, 20));
                m_flagsState[f] = button;
            });


            AddDropdown(Locale.Get("K45_WTS_TEXT_CONTENT"), out m_dropdownTextContent, helperConfig, WTSDynamicTextRenderingRules.ALLOWED_TYPES_VEHICLE.Select(x => Locale.Get("K45_WTS_BOARD_TEXT_TYPE_DESC_VEHICLE", x.ToString())).ToArray(), OnSetTextOwnNameContent);
            AddTextField(Locale.Get("K45_WTS_CUSTOM_TEXT"), out m_customText, helperConfig, OnSetTextCustom);
            AddFilterableInput(Locale.Get("K45_WTS_SPRITE_NAME"), helperConfig, out m_spriteFilter, out UIListBox popup, OnFilterSprites, OnSpriteNameChanged);
            popup.processMarkup = true;
            popup.height = 210;

            helperConfig.AddSpace(5);
            AddDropdown(Locale.Get("K45_WTS_OVERRIDE_FONT"), out m_overrideFontSelect, helperConfig, new string[0], OnSetOverrideFont);
            AddDropdown(Locale.Get("K45_WTS_CLASS_FONT"), out m_fontClassSelect, helperConfig, (Enum.GetValues(typeof(FontClass)) as FontClass[]).Select(x => Locale.Get("K45_WTS_FONTCLASS", x.ToString())).ToArray(), OnSetFontClass);
            AddTextField(Locale.Get("K45_WTS_PREFIX"), out m_textPrefix, helperConfig, OnSetPrefix);
            AddTextField(Locale.Get("K45_WTS_SUFFIX"), out m_textSuffix, helperConfig, OnSetSuffix);
            AddCheckboxLocale("K45_WTS_TEXT_ALL_CAPS", out m_allCaps, helperConfig, OnSetAllCaps);
            AddCheckboxLocale("K45_WTS_TEXT_APPLYABBREVIATIONS", out m_applyAbbreviations, helperConfig, OnSetApplyAbbreviations);

            WTSUtils.ReloadFontsOf(m_overrideFontSelect, null, true, true);

            WTSVehicleLayoutEditor.Instance.CurrentTabChanged += (newVal) =>
            {
                int targetTab = newVal - 1;
                SafeObtain(OnSetData, targetTab);
            };
            m_isEditing = false;


            AddLibBox<WTSLibVehicleTextItem, BoardTextDescriptorGeneralXml>(helperSettings, out UIButton m_copyButtonText,
                DoCopyText, out m_pasteButtonText,
                DoPasteText, out UIButton m_deleteButtonText,
                DoDeleteText, (loadedItem) => SafeObtain((ref BoardTextDescriptorGeneralXml x) =>
                {
                    string name = x.SaveName;
                    x = XmlUtils.DefaultXmlDeserialize<BoardTextDescriptorGeneralXml>(loadedItem);
                    x.SaveName = name;
                    OnSetData(ref x);
                    x.SaveName = name;
                }),
                () => XmlUtils.DefaultXmlSerialize(WTSVehicleLayoutEditor.Instance.EditingInstance.TextDescriptors[Math.Max(0, TabToEdit)]));



        }


        public void Start() => WriteTheSignsMod.Controller.EventFontsReloadedFromFolder += () => SafeObtain((ref BoardTextDescriptorGeneralXml x) => WTSUtils.ReloadFontsOf(m_overrideFontSelect, x.m_overrideFont, true));




        private void DoDeleteText() => WTSVehicleLayoutEditor.Instance.RemoveTabFromItem(TabToEdit);
        private void DoPasteText() => SafeObtain((ref BoardTextDescriptorGeneralXml x) =>
        {
            if (m_clipboard != null)
            {
                string name = x.SaveName;
                x = XmlUtils.DefaultXmlDeserialize<BoardTextDescriptorGeneralXml>(m_clipboard);
                x.SaveName = name;
                OnSetData(ref x);
            }
        });
        private void DoCopyText() => SafeObtain((ref BoardTextDescriptorGeneralXml x) =>
        {
            m_clipboard = XmlUtils.DefaultXmlSerialize(x);
            m_pasteButtonText.isVisible = true;
        });


        private void OnSetData(ref BoardTextDescriptorGeneralXml x)
        {
            m_tabName.text = x.SaveName ?? "";

            m_arrayCoord[0].text = x.PlacingConfig.Position.X.ToString("F3");
            m_arrayCoord[1].text = x.PlacingConfig.Position.Y.ToString("F3");
            m_arrayCoord[2].text = x.PlacingConfig.Position.Z.ToString("F3");
            m_arrayRotation[0].text = x.PlacingConfig.Rotation.X.ToString("F3");
            m_arrayRotation[1].text = x.PlacingConfig.Rotation.Y.ToString("F3");
            m_arrayRotation[2].text = x.PlacingConfig.Rotation.Z.ToString("F3");
            m_textScale.text = x.m_textScale.ToString("F3");
            m_maxWidth.text = x.m_maxWidthMeters.ToString("F3");
            m_applyScaleOnY.isChecked = x.m_applyOverflowResizingOnY;
            m_invertTextHorizontalAlignClone.isChecked = x.PlacingConfig.m_invertYCloneHorizontalAlign;
            m_create180degSimmetricClone.isChecked = x.PlacingConfig.m_create180degYClone;


            m_bgSize[0].text = x.BackgroundMeshSettings.Size.X.ToString("F3");
            m_bgSize[1].text = x.BackgroundMeshSettings.Size.Y.ToString("F3");
            m_bgColor.selectedColor = x.BackgroundMeshSettings.BackgroundColor;

            m_dropdownTextAlignHorizontal.selectedIndex = (int)x.m_textAlign;
            m_useContrastColor.isChecked = x.ColoringConfig.m_useContrastColor;
            m_dropdownMaterialType.selectedIndex = (int)x.IlluminationConfig.IlluminationType;
            m_sliderIllumination.value = x.IlluminationConfig.IlluminationStrength;
            m_dropdownBlinkType.selectedIndex = (int)x.IlluminationConfig.BlinkType;
            m_textFixedColor.selectedColor = x.ColoringConfig.m_defaultColor;

            m_useFrame.isChecked = x.BackgroundMeshSettings.UseFrame;
            m_frameBackSize[0].text = x.BackgroundMeshSettings.FrameMeshSettings.BackSize.X.ToString("F3");
            m_frameBackSize[1].text = x.BackgroundMeshSettings.FrameMeshSettings.BackSize.Y.ToString("F3");
            m_frameBackOffset[0].text = x.BackgroundMeshSettings.FrameMeshSettings.BackOffset.X.ToString("F3");
            m_frameBackOffset[1].text = x.BackgroundMeshSettings.FrameMeshSettings.BackOffset.Y.ToString("F3");
            m_frameDepths[0].text = x.BackgroundMeshSettings.FrameMeshSettings.FrontDepth.ToString("F3");
            m_frameDepths[1].text = x.BackgroundMeshSettings.FrameMeshSettings.BackDepth.ToString("F3");
            m_frameFrontBorder.text = x.BackgroundMeshSettings.FrameMeshSettings.FrontBorderThickness.ToString("F3");
            m_frameGlassTransparency.value = x.BackgroundMeshSettings.FrameMeshSettings.GlassTransparency;
            m_frameOuterSpecularLevel.value = x.BackgroundMeshSettings.FrameMeshSettings.OuterSpecularLevel;
            m_frameGlassSpecularLevel.value = x.BackgroundMeshSettings.FrameMeshSettings.GlassSpecularLevel;
            m_frameUseVehicleColor.isChecked = x.BackgroundMeshSettings.FrameMeshSettings.InheritColor;
            m_frameColor.selectedColor = x.BackgroundMeshSettings.FrameMeshSettings.OutsideColor;
            m_frameGlassColor.selectedColor = x.BackgroundMeshSettings.FrameMeshSettings.GlassColor;

            m_dropdownTextContent.selectedIndex = Array.IndexOf(WTSDynamicTextRenderingRules.ALLOWED_TYPES_VEHICLE, x.m_textType);
            m_customText.text = x.m_fixedText ?? "";
            m_overrideFontSelect.selectedIndex = x.m_overrideFont == null ? 0 : x.m_overrideFont == WTSController.DEFAULT_FONT_KEY ? 1 : Array.IndexOf(m_overrideFontSelect.items, x.m_overrideFont);
            m_fontClassSelect.selectedIndex = (int)x.m_fontClass;
            m_textPrefix.text = x.m_prefix ?? "";
            m_textSuffix.text = x.m_suffix ?? "";
            m_allCaps.isChecked = x.m_allCaps;
            m_spriteFilter.text = x.m_spriteName ?? "";

            m_arrayCustomBlink[0].text = x.IlluminationConfig.CustomBlink.X.ToString("F3");
            m_arrayCustomBlink[1].text = x.IlluminationConfig.CustomBlink.Y.ToString("F3");
            m_arrayCustomBlink[2].text = x.IlluminationConfig.CustomBlink.Z.ToString("F3");
            m_arrayCustomBlink[3].text = x.IlluminationConfig.CustomBlink.W.ToString("F3");

            foreach (var f in (Enum.GetValues(typeof(Vehicle.Flags)) as Vehicle.Flags[]))
            {
                m_flagsState[f].activeStateIndex = ((x.IlluminationConfig.m_forbiddenFlags & (int)f) != 0) ? 2 : ((x.IlluminationConfig.m_requiredFlags & (int)f) != 0) ? 1 : 0;
            }

            ApplyShowRules(x);
        }

        private void ApplyShowRules(BoardTextDescriptorGeneralXml x)
        {
            m_customText.parent.isVisible = x.m_textType == TextType.Fixed;
            m_textFixedColor.parent.isVisible = !x.ColoringConfig.m_useContrastColor;
            m_invertTextHorizontalAlignClone.isVisible = x.PlacingConfig.m_create180degYClone;
            m_sliderIllumination.parent.isVisible = x.IlluminationConfig.IlluminationType != MaterialType.OPAQUE;
            m_dropdownBlinkType.parent.isVisible = x.IlluminationConfig.IlluminationType != MaterialType.OPAQUE;
            m_arrayCustomBlink[0].parent.isVisible = x.IlluminationConfig.IlluminationType != MaterialType.OPAQUE && m_dropdownBlinkType.selectedIndex == (int)BlinkType.Custom;

            m_textPrefix.parent.isVisible = !x.IsSpriteText();
            m_textSuffix.parent.isVisible = !x.IsSpriteText();
            m_overrideFontSelect.parent.isVisible = !x.IsSpriteText();
            m_fontClassSelect.parent.isVisible = !x.IsSpriteText();
            m_allCaps.isVisible = !x.IsSpriteText();
            m_applyAbbreviations.isVisible = x.m_textType == TextType.StreetSuffix || x.m_textType == TextType.StreetNameComplete || x.m_textType == TextType.StreetPrefix;
            m_spriteFilter.parent.isVisible = x.m_textType == TextType.GameSprite;

            m_flagsContainer.isVisible = x.IlluminationConfig.IlluminationType == MaterialType.FLAGS;

            m_tabFrame.isVisible = ((Vector2)x.BackgroundMeshSettings.Size).magnitude > 0.001f;
            m_frameBackSize[0].parent.isVisible = x.BackgroundMeshSettings.UseFrame;
            m_frameBackOffset[0].parent.isVisible = x.BackgroundMeshSettings.UseFrame;
            m_frameDepths[0].parent.isVisible = x.BackgroundMeshSettings.UseFrame;
            m_frameFrontBorder.parent.isVisible = x.BackgroundMeshSettings.UseFrame;
            m_frameGlassTransparency.parent.isVisible = x.BackgroundMeshSettings.UseFrame;
            m_frameOuterSpecularLevel.parent.isVisible = x.BackgroundMeshSettings.UseFrame;
            m_frameGlassSpecularLevel.parent.isVisible = x.BackgroundMeshSettings.UseFrame;
            m_frameUseVehicleColor.isVisible = x.BackgroundMeshSettings.UseFrame;
            m_frameColor.parent.isVisible = x.BackgroundMeshSettings.UseFrame && !x.BackgroundMeshSettings.FrameMeshSettings.InheritColor;
            m_frameGlassColor.parent.isVisible = x.BackgroundMeshSettings.UseFrame;
        }


        private delegate void SafeObtainMethod(ref BoardTextDescriptorGeneralXml x);

        private void SafeObtain(SafeObtainMethod action, int? targetTab = null)
        {
            if (m_isEditing || WTSVehicleLayoutEditor.Instance.EditingInstance == null)
            {
                return;
            }

            lock (this)
            {
                m_isEditing = true;
                try
                {
                    int effTargetTab = Math.Max(0, targetTab ?? TabToEdit);
                    if (effTargetTab < WTSVehicleLayoutEditor.Instance.EditingInstance.TextDescriptors.Length)
                    {
                        action(ref WTSVehicleLayoutEditor.Instance.EditingInstance.TextDescriptors[effTargetTab]);
                    }
                }
                finally
                {
                    m_isEditing = false;
                }
            }
        }

        private void OnTabNameChanged(string text) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) =>
        {
            if (text.IsNullOrWhiteSpace())
            {
                m_tabName.text = desc.SaveName;
            }
            else
            {
                desc.SaveName = text;
                WTSVehicleLayoutEditor.Instance.SetTabName(TabToEdit, text);
            }
        });
        private void OnSetSuffix(string text) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_suffix = text);
        private void OnSetPrefix(string text) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_prefix = text);
        private void OnSetOverrideFont(int sel) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) =>
        {
            if (sel > 1 && sel < (m_overrideFontSelect?.items?.Length ?? 0))
            {
                desc.m_overrideFont = m_overrideFontSelect.items[sel];
            }
            else if (sel == 1)
            {
                desc.m_overrideFont = WTSController.DEFAULT_FONT_KEY;
            }
            else
            {
                desc.m_overrideFont = null;
            }
        });
        private void OnSetFontClass(int sel) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) =>
        {
            if (sel >= 0)
            {
                desc.m_fontClass = (FontClass)sel;

            }
        });
        private void OnSetTextCustom(string text) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_fixedText = text);

        private string GetCurrentSpriteName()
        {
            string result = null;
            SafeObtain((ref BoardTextDescriptorGeneralXml desc) => result = desc.m_spriteName);
            return result;
        }

        private string OnSpriteNameChanged(string input, int obj, string[] refArray)
        {
            if (obj >= 0)
            {
                var targetValue = refArray[obj].Split(new char[] { '>' }, 2)[1].Trim();
                SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_spriteName = targetValue);
            }
            return GetCurrentSpriteName();
        }

        private string[] OnFilterSprites(string input) => m_spriteFilter.atlas.spriteNames.Where(x => x.ToLower().Contains(input.ToLower())).OrderBy(x => x).Select(x => $"<sprite {x}> {x}").ToArray();

        private void OnSetTextOwnNameContent(int sel)
        {
            SafeObtain((ref BoardTextDescriptorGeneralXml desc) =>
            {
                if (sel >= 0)
                {
                    desc.m_textType = WTSDynamicTextRenderingRules.ALLOWED_TYPES_VEHICLE[sel];
                    ApplyShowRules(desc);
                }
            });
        }

        //private void OnSetShader(int sel) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => { });
        private void OnChangeApplyRescaleOnY(bool isChecked) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_applyOverflowResizingOnY = isChecked);
        private void OnChangeInvertCloneTextHorizontalAlignment(bool isChecked) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.PlacingConfig.m_invertYCloneHorizontalAlign = isChecked);
        private void OnChangeCreateSimmetricClone(bool isChecked)
        {
            SafeObtain((ref BoardTextDescriptorGeneralXml desc) =>
            {
                desc.PlacingConfig.m_create180degYClone = isChecked;
                ApplyShowRules(desc);
            });

        }

        private void OnSetTextAlignmentHorizontal(int sel) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_textAlign = (UIHorizontalAlignment)sel);
        private void OnFixedColorChanged(UIComponent component, Color value) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.ColoringConfig.m_defaultColor = value);
        private void OnMaxWidthChange(float obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_maxWidthMeters = obj);
        private void OnScaleSubmit(float scale) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_textScale = scale);
        private void OnContrastColorChange(bool isChecked)
        {
            SafeObtain((ref BoardTextDescriptorGeneralXml desc) =>
            {
                desc.ColoringConfig.m_useContrastColor = isChecked;
                ApplyShowRules(desc);
            });
        }
        private void OnBgSizeChanged(Vector2 obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) =>
        {
            desc.BackgroundMeshSettings.Size = (Vector2Xml)obj;
            ApplyShowRules(desc);
        });
        private void OnBgColorChanged(UIComponent component, Color value) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.BackgroundMeshSettings.BackgroundColor = value);
        private void OnSetMaterialType(int sel)
        {
            SafeObtain((ref BoardTextDescriptorGeneralXml desc) =>
            {
                desc.IlluminationConfig.IlluminationType = (MaterialType)sel;
                ApplyShowRules(desc);
            });
        }

        private void OnSetBlinkType(int sel) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) =>
        {
            desc.IlluminationConfig.BlinkType = (BlinkType)sel;
            ApplyShowRules(desc);
        });
        private void OnChangeIlluminationStrength(float val) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.IlluminationConfig.IlluminationStrength = val);
        private void OnCustomBlinkChange(Vector4 obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.IlluminationConfig.CustomBlink = (Vector4Xml)obj);
        private void OnSetStateFlag(Vehicle.Flags f, int y) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) =>
        {
            desc.IlluminationConfig.m_forbiddenFlags &= ~(int)f;
            desc.IlluminationConfig.m_requiredFlags &= ~(int)f;
            if (y == 1)
            {
                desc.IlluminationConfig.m_requiredFlags |= (int)f;
            }
            else if (y == 2)
            {
                desc.IlluminationConfig.m_forbiddenFlags |= (int)f;
            }
        });


        private void OnRotationChange(Vector3 obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.PlacingConfig.Rotation = (Vector3Xml)obj);
        private void OnPositionChange(Vector3 obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.PlacingConfig.Position = (Vector3Xml)obj);
        private void OnSetAllCaps(bool isChecked) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_allCaps = isChecked);
        private void OnSetApplyAbbreviations(bool isChecked) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_applyAbbreviations = isChecked);

        #region Frame
        private void OnFrameBorderThicknessChanged(float obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.BackgroundMeshSettings.FrameMeshSettings.FrontBorderThickness = obj);
        private void OnFrameGlassTransparencyChanged(float obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.BackgroundMeshSettings.FrameMeshSettings.GlassTransparency = obj);
        private void OnFrameGlassSpecularLevelChanged(float obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.BackgroundMeshSettings.FrameMeshSettings.GlassSpecularLevel = obj);
        private void OnFrameOuterSpecularLevelChanged(float obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.BackgroundMeshSettings.FrameMeshSettings.OuterSpecularLevel = obj);
        private void OnFrameDepthsChanged(Vector2 obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) =>
        {
            desc.BackgroundMeshSettings.FrameMeshSettings.FrontDepth = obj.x;
            desc.BackgroundMeshSettings.FrameMeshSettings.BackDepth = obj.y;
        });
        private void OnFrameBackOffsetChanged(Vector2 obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.BackgroundMeshSettings.FrameMeshSettings.BackOffset = (Vector2Xml)obj);
        private void OnFrameBackSizeChanged(Vector2 obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.BackgroundMeshSettings.FrameMeshSettings.BackSize = (Vector2Xml)obj);
        private void OnUseFrameChange(bool isChecked) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) =>
        {
            desc.BackgroundMeshSettings.UseFrame = isChecked;
            ApplyShowRules(desc);
        });

        private void OnFrameUseVehicleColorChange(bool isChecked) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) =>
        {
            desc.BackgroundMeshSettings.FrameMeshSettings.InheritColor = isChecked;
            ApplyShowRules(desc);
        });
        private void OnFrameColorChanged(UIComponent component, Color value) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.BackgroundMeshSettings.FrameMeshSettings.OutsideColor = value);
        private void OnFrameGlassColorChanged(UIComponent component, Color value) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.BackgroundMeshSettings.FrameMeshSettings.GlassColor = value);
        #endregion
    }

}
