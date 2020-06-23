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
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSPropLayoutEditorTexts : UICustomControl
    {
        public UIPanel MainContainer { get; protected set; }

        private UITabstrip m_tabstrip;
        private UITabContainer m_tabContainer;

        private UIPanel m_tabSettings;
        private UIPanel m_tabSize;
        private UIPanel m_tabAppearence;
        private UIPanel m_tabConfig;

        private UITextField m_tabName;


        private UITextField[] m_arrayCoord;
        private UITextField[] m_arrayRotation;
        private UITextField m_textScale;
        private UITextField m_maxWidth;
        private UICheckBox m_applyScaleOnY;
        private UICheckBox m_create180degSimmetricClone;
        private UICheckBox m_invertTextHorizontalAlignClone;


        private UIDropDown m_dropdownTextAlignHorizontal;
        private UICheckBox m_useContrastColor;
        private UIColorField m_textFixedColor;
        private UIDropDown m_dropdownMaterialType;
        private UISlider m_sliderIllumination;
        private UIDropDown m_dropdownBlinkType;
        private UITextField[] m_arrayCustomBlink;


        private UIDropDown m_dropdownTextContent;
        private UITextField m_customText;
        private UIDropDown m_destinationRef;
        private UIDropDown m_referenceNode;
        private UIDropDown m_fontClassSelect;
        private UIDropDown m_overrideFontSelect;
        private UITextField m_textPrefix;
        private UITextField m_textSuffix;
        private UITextField[] m_arrayRowColumnsCount;
        private UITextField[] m_arrayRowColumnsSpacing;
        private UICheckBox m_checkboxVerticalFirst;

        private int TabToEdit => WTSPropLayoutEditor.Instance.CurrentTab - 1;
        private bool m_isEditing = true;
        private UICheckBox m_allCaps;
        private UICheckBox m_applyAbbreviations;

        private UIButton m_pasteButtonText;

        private string m_clipboard;
        private UITextField m_spriteFilter;
        private UIListBox m_popup;
        private UIDropDown m_verticalAlignDD;

        public void Awake()
        {
            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.padding = new RectOffset(5, 5, 5, 5);
            MainContainer.autoLayoutPadding = new RectOffset(0, 0, 3, 3);

            KlyteMonoUtils.CreateTabsComponent(out m_tabstrip, out m_tabContainer, MainContainer.transform, "TextEditor", new Vector4(0, 0, MainContainer.width, 40), new Vector4(0, 0, MainContainer.width, MainContainer.height - 40));
            m_tabSettings = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Settings), "K45_WTS_GENERAL_SETTINGS", "TxtSettings");
            m_tabSize = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_MoveCross), "K45_WTS_TEXT_SIZE_ATTRIBUTES", "TxtSize");
            m_tabAppearence = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_AutoColorIcon), "K45_WTS_TEXT_APPEARANCE_ATTRIBUTES", "TxtApp");
            m_tabConfig = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_AutoNameIcon), "K45_WTS_TEXT_CONFIGURATION_ATTRIBUTES", "TxtCnf");

            var helperSettings = new UIHelperExtension(m_tabSettings, LayoutDirection.Vertical);
            var helperSize = new UIHelperExtension(m_tabSize, LayoutDirection.Vertical);
            var helperAppearance = new UIHelperExtension(m_tabAppearence, LayoutDirection.Vertical);
            var helperConfig = new UIHelperExtension(m_tabConfig, LayoutDirection.Vertical);

            AddTextField(Locale.Get("K45_WTS_TEXT_TAB_TITLE"), out m_tabName, helperSettings, OnTabNameChanged);

            AddVector3Field(Locale.Get("K45_WTS_RELATIVE_POS"), out m_arrayCoord, helperSize, OnPositionChange);
            AddVector3Field(Locale.Get("K45_WTS_RELATIVE_ROT"), out m_arrayRotation, helperSize, OnRotationChange);
            AddFloatField(Locale.Get("K45_WTS_TEXT_SCALE"), out m_textScale, helperSize, OnScaleSubmit, false);
            AddFloatField(Locale.Get("K45_WTS_MAX_WIDTH_METERS"), out m_maxWidth, helperSize, OnMaxWidthChange, false);
            AddCheckboxLocale("K45_WTS_RESIZE_Y_TEXT_OVERFLOW", out m_applyScaleOnY, helperSize, OnChangeApplyRescaleOnY);
            AddCheckboxLocale("K45_WTS_CREATE_CLONE_180DEG", out m_create180degSimmetricClone, helperSize, OnChangeCreateSimmetricClone);
            AddCheckboxLocale("K45_WTS_CLONE_180DEG_INVERT_TEXT_HOR_ALIGN", out m_invertTextHorizontalAlignClone, helperSize, OnChangeInvertCloneTextHorizontalAlignment);

            AddDropdown(Locale.Get("K45_WTS_TEXT_ALIGN_HOR"), out m_dropdownTextAlignHorizontal, helperAppearance, Enum.GetNames(typeof(UIHorizontalAlignment)).Select(x => Locale.Get("K45_ALIGNMENT", x)).ToArray(), OnSetTextAlignmentHorizontal);
            helperAppearance.AddSpace(5);
            AddDropdown(Locale.Get("K45_WTS_TEXT_MATERIALTYPE"), out m_dropdownMaterialType, helperAppearance, Enum.GetNames(typeof(MaterialType)).Select(x => Locale.Get("K45_WTS_TEXTMATERIALTYPE", x.ToString())).ToArray(), OnSetMaterialType);
            AddSlider(Locale.Get("K45_WTS_TEXT_ILLUMINATIONSTRENGTH"), out m_sliderIllumination, helperAppearance, OnChangeIlluminationStrength, 0, 1, 0.025f, (x) => $"{x.ToString("P1")}");
            AddDropdown(Locale.Get("K45_WTS_TEXT_BLINKTYPE"), out m_dropdownBlinkType, helperAppearance, Enum.GetNames(typeof(BlinkType)).Select(x => Locale.Get("K45_WTS_BLINKTYPE", x.ToString())).ToArray(), OnSetBlinkType);
            AddVector4Field(Locale.Get("K45_WTS_TEXT_CUSTOMBLINKPARAMS"), out m_arrayCustomBlink, helperAppearance, OnCustomBlinkChange);
            AddCheckboxLocale("K45_WTS_USE_CONTRAST_COLOR", out m_useContrastColor, helperAppearance, OnContrastColorChange);
            AddColorField(helperAppearance, Locale.Get("K45_WTS_TEXT_COLOR"), out m_textFixedColor, OnFixedColorChanged);


            AddDropdown(Locale.Get("K45_WTS_TEXT_CONTENT"), out m_dropdownTextContent, helperConfig, Enum.GetNames(typeof(TextType)).Select(x => Locale.Get("K45_WTS_BOARD_TEXT_TYPE_DESC", x.ToString())).ToArray(), OnSetTextOwnNameContent);
            AddTextField(Locale.Get("K45_WTS_CUSTOM_TEXT"), out m_customText, helperConfig, OnSetTextCustom);
            AddFilterableInput(Locale.Get("K45_WTS_SPRITE_NAME"), helperConfig, out m_spriteFilter, out UIListBox popup, OnFilterSprites, GetCurrentSpriteName, OnSpriteNameChanged);
            popup.processMarkup = true;

            AddDropdown(Locale.Get("K45_WTS_PROPLAYOUT_DESTINATIONREFERENCE"), out m_destinationRef, helperConfig, (Enum.GetValues(typeof(DestinationReference)) as DestinationReference[]).OrderBy(x => (int)x).Select(x => Locale.Get("K45_WTS_BOARD_TEXT_TYPE_DESC", x.ToString())).ToArray(), OnChangeDestinationRef);
            AddDropdown(Locale.Get("K45_WTS_TEXT_TARGETSEGMENTROTATION"), out m_referenceNode, helperConfig, (Enum.GetValues(typeof(ReferenceNode)) as ReferenceNode[]).OrderBy(x => (int)x).Select(x => Locale.Get("K45_WTS_TEXT_REFERENCENODE_OPT", x.ToString())).ToArray(), OnReferenceNodeChange);
            helperConfig.AddSpace(5);
            AddDropdown(Locale.Get("K45_WTS_CLASS_FONT"), out m_fontClassSelect, helperConfig, (Enum.GetValues(typeof(FontClass)) as FontClass[]).Select(x => Locale.Get("K45_WTS_FONTCLASS", x.ToString())).ToArray(), OnSetFontClass);
            AddDropdown(Locale.Get("K45_WTS_OVERRIDE_FONT"), out m_overrideFontSelect, helperConfig, new string[0], OnSetOverrideFont);
            AddTextField(Locale.Get("K45_WTS_PREFIX"), out m_textPrefix, helperConfig, OnSetPrefix);
            AddTextField(Locale.Get("K45_WTS_SUFFIX"), out m_textSuffix, helperConfig, OnSetSuffix);
            AddCheckboxLocale("K45_WTS_TEXT_ALL_CAPS", out m_allCaps, helperConfig, OnSetAllCaps);
            AddCheckboxLocale("K45_WTS_TEXT_APPLYABBREVIATIONS", out m_applyAbbreviations, helperConfig, OnSetApplyAbbreviations);
            AddVector2Field(Locale.Get("K45_WTS_TEXT_ROW_COLUMNS"), out m_arrayRowColumnsCount, helperConfig, OnRowColumnCountChanged, true, true);
            m_arrayRowColumnsCount.ForEach(x => x.allowNegative = false);
            AddVector2Field(Locale.Get("K45_WTS_TEXT_ROW_COLUMNS_SPACING"), out m_arrayRowColumnsSpacing, helperConfig, OnRowColumnSpacingChanged);
            AddDropdown(Locale.Get("K45_WTS_VERTICAL_ALIGNMENT"), out m_verticalAlignDD, helperConfig, Enum.GetNames(typeof(UIVerticalAlignment)).Select(x => Locale.Get("K45_VERT_ALIGNMENT", x)).ToArray(), OnSetVerticalAlign);
            AddCheckboxLocale("K45_WTS_TEXT_FILLCOLUMNSFIRST", out m_checkboxVerticalFirst, helperConfig, OnColumnsFirstChanged);

            WTSUtils.ReloadFontsOf(m_overrideFontSelect, null, true, true);

            WTSPropLayoutEditor.Instance.CurrentTabChanged += (newVal) =>
            {
                int targetTab = newVal - 1;
                SafeObtain(OnSetData, targetTab);
            };
            m_isEditing = false;


            AddLibBox<WTSLibPropTextItem, BoardTextDescriptorGeneralXml>(helperSettings, out UIButton m_copyButtonText,
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
                () => XmlUtils.DefaultXmlSerialize(WTSPropLayoutEditor.Instance.EditingInstance.TextDescriptors[Math.Max(0, TabToEdit)]));



        }

        public void Start() => WriteTheSignsMod.Controller.EventFontsReloadedFromFolder += () => SafeObtain((ref BoardTextDescriptorGeneralXml x) => WTSUtils.ReloadFontsOf(m_overrideFontSelect, x.m_overrideFont, true));


        private enum ReferenceNode
        {
            E6,
            E5,
            E4,
            E3,
            E2,
            E1,
            E0,
            C,
            D0,
            D1,
            D2,
            D3,
            D4,
            D5,
            D6
        }

        private void OnReferenceNodeChange(int selIdx) => SafeObtain((ref BoardTextDescriptorGeneralXml x) => x.m_targetNodeRelative = selIdx - 7);
        private void OnChangeDestinationRef(int selIdx) => SafeObtain((ref BoardTextDescriptorGeneralXml x) => x.m_destinationRelative = (DestinationReference)selIdx - 1);


        private void DoDeleteText() => WTSPropLayoutEditor.Instance.RemoveTabFromItem(TabToEdit);
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
            m_tabName.text = x.SaveName;

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

            m_dropdownTextAlignHorizontal.selectedIndex = (int)x.m_textAlign;
            m_useContrastColor.isChecked = x.ColoringConfig.m_useContrastColor;
            m_dropdownMaterialType.selectedIndex = (int)x.IlluminationConfig.IlluminationType;
            m_sliderIllumination.value = x.IlluminationConfig.IlluminationStrength;
            m_dropdownBlinkType.selectedIndex = (int)x.IlluminationConfig.BlinkType;
            m_textFixedColor.selectedColor = x.ColoringConfig.m_defaultColor;

            m_dropdownTextContent.items = WTSPropRenderingRules.ALLOWED_TYPES_PER_RENDERING_CLASS[WTSPropLayoutEditor.Instance.EditingInstance.m_allowedRenderClass].Select(x => Locale.Get("K45_WTS_BOARD_TEXT_TYPE_DESC", x.ToString())).ToArray();
            m_dropdownTextContent.selectedIndex = Array.IndexOf(WTSPropRenderingRules.ALLOWED_TYPES_PER_RENDERING_CLASS[WTSPropLayoutEditor.Instance.EditingInstance.m_allowedRenderClass], x.m_textType);
            m_customText.text = x.m_fixedText ?? "";
            m_destinationRef.selectedIndex = (int)(x.m_destinationRelative + 1);
            m_referenceNode.selectedIndex = x.m_targetNodeRelative + 7;
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
            m_arrayRowColumnsCount[1].text = x.MultiItemSettings.SubItemsPerRow.ToString();
            m_arrayRowColumnsCount[0].text = x.MultiItemSettings.SubItemsPerColumn.ToString();
            m_arrayRowColumnsSpacing[0].text = x.MultiItemSettings.SubItemSpacing.X.ToString("F3");
            m_arrayRowColumnsSpacing[1].text = x.MultiItemSettings.SubItemSpacing.Y.ToString("F3");

            ApplyShowRules(x);
        }

        private void ApplyShowRules(BoardTextDescriptorGeneralXml x)
        {
            m_customText.parent.isVisible = x.m_textType == TextType.Fixed;
            m_destinationRef.parent.isVisible = WTSPropLayoutEditor.Instance.EditingInstance.m_allowedRenderClass == TextRenderingClass.RoadNodes && x.IsTextRelativeToSegment();
            m_referenceNode.parent.isVisible = WTSPropLayoutEditor.Instance.EditingInstance.m_allowedRenderClass == TextRenderingClass.RoadNodes && x.IsTextRelativeToSegment();
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
            m_arrayRowColumnsCount[0].parent.isVisible = x.IsMultiItemText();
            m_arrayRowColumnsSpacing[0].parent.isVisible = x.IsMultiItemText();
            m_checkboxVerticalFirst.isVisible = x.IsMultiItemText();
        }


        private delegate void SafeObtainMethod(ref BoardTextDescriptorGeneralXml x);

        private void SafeObtain(SafeObtainMethod action, int? targetTab = null)
        {
            if (m_isEditing || WTSPropLayoutEditor.Instance.EditingInstance == null)
            {
                return;
            }

            lock (this)
            {
                m_isEditing = true;
                try
                {
                    int effTargetTab = Math.Max(0, targetTab ?? TabToEdit);
                    if (effTargetTab < WTSPropLayoutEditor.Instance.EditingInstance.TextDescriptors.Length)
                    {
                        action(ref WTSPropLayoutEditor.Instance.EditingInstance.TextDescriptors[effTargetTab]);
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
                WTSPropLayoutEditor.Instance.SetCurrentTabName(text);
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

        private string OnSpriteNameChanged(int obj, string[] refArray)
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
                    desc.m_textType = WTSPropRenderingRules.ALLOWED_TYPES_PER_RENDERING_CLASS[WTSPropLayoutEditor.Instance.EditingInstance.m_allowedRenderClass][sel];
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
        private void OnRotationChange(Vector3 obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.PlacingConfig.Rotation = (Vector3Xml)obj);
        private void OnPositionChange(Vector3 obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.PlacingConfig.Position = (Vector3Xml)obj);
        private void OnSetAllCaps(bool isChecked) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_allCaps = isChecked);
        private void OnSetApplyAbbreviations(bool isChecked) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.m_applyAbbreviations = isChecked);
        private void OnSetVerticalAlign(int sel) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.MultiItemSettings.VerticalAlign = (UIVerticalAlignment)sel);



        private void OnRowColumnSpacingChanged(Vector2 obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.MultiItemSettings.SubItemSpacing = (Vector2Xml)obj);
        private void OnRowColumnCountChanged(Vector2 obj) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) =>
        {
            desc.MultiItemSettings.SubItemsPerRow = Mathf.RoundToInt(obj.y);
            desc.MultiItemSettings.SubItemsPerColumn = Mathf.RoundToInt(obj.x);

            m_arrayRowColumnsCount[1].text = desc.MultiItemSettings.SubItemsPerRow.ToString();
            m_arrayRowColumnsCount[0].text = desc.MultiItemSettings.SubItemsPerColumn.ToString();
        });

        private void OnColumnsFirstChanged(bool isChecked) => SafeObtain((ref BoardTextDescriptorGeneralXml desc) => desc.MultiItemSettings.VerticalFirst = isChecked);
    }

}
