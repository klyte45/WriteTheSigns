using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensions;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Libraries;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Sprites;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;
using static Klyte.WriteTheSigns.Xml.BoardTextDescriptorGeneralXml.ColoringSettings;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSHighwayShieldLayoutEditorTexts : UICustomControl
    {
        public UIPanel MainContainer { get; protected set; }

        private UITabstrip m_tabstrip;


        private UIPanel m_tabSettings;
        private UIPanel m_tabSize;
        private UIPanel m_tabAppearence;
        private UIPanel m_tabConfig;

        private UITextField m_tabName;

        private UITextField[] m_arrayCoord;
        private UITextField[] m_arrayPivot;
        private UITextField m_textScale;
        private UITextField m_charSpacingFactor;
        private UITextField m_maxWidth;
        private UITextField m_fixedHeight;
        private UICheckBox m_applyScaleOnY;

        private UIDropDown m_colorSource;
        private UIColorField m_textFixedColor;

        private UIDropDown m_dropdownTextContent;
        private UITextField m_customText;
        private UIDropDown m_overrideFontSelect;
        private UIDropDown m_fontClassSelect;
        private UITextField m_textPrefix;
        private UITextField m_textSuffix;

        private int TabToEdit => WTSHighwayShieldEditor.Instance.CurrentTab - 1;
        private bool m_isEditing = true;

        private UIButton m_pasteButtonText;

        private string m_clipboard;
        private UITextField m_spriteFilter;
        private string lastProtocol_searchedParam;

        private WTSHighwayShieldLayoutEditorPreview Preview => WTSHighwayShieldEditor.Instance.Preview;

        public void Awake()
        {
            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.padding = new RectOffset(5, 5, 5, 5);
            MainContainer.autoLayoutPadding = new RectOffset(0, 0, 3, 3);

            KlyteMonoUtils.CreateTabsComponent(out m_tabstrip, out UITabContainer m_tabContainer, MainContainer.transform, "TextEditor", new Vector4(0, 0, MainContainer.width, 40), new Vector4(0, 0, MainContainer.width, 315));
            m_tabSettings = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Settings), "K45_WTS_GENERAL_SETTINGS", "TxtSettings");
            m_tabSize = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_MoveCross), "K45_WTS_TEXT_SIZE_ATTRIBUTES", "TxtSize");
            m_tabAppearence = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_AutoColorIcon), "K45_WTS_TEXT_APPEARANCE_ATTRIBUTES", "TxtApp");
            m_tabConfig = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_AutoNameIcon), "K45_WTS_TEXT_CONFIGURATION_ATTRIBUTES", "TxtCnf");

            var helperSettings = new UIHelperExtension(m_tabSettings, LayoutDirection.Vertical);
            var helperSize = new UIHelperExtension(m_tabSize, LayoutDirection.Vertical);
            var helperAppearance = new UIHelperExtension(m_tabAppearence, LayoutDirection.Vertical);
            var helperConfig = new UIHelperExtension(m_tabConfig, LayoutDirection.Vertical);

            AddTextField(Locale.Get("K45_WTS_TEXT_TAB_TITLE"), out m_tabName, helperSettings, OnTabNameChanged);

            AddVector2Field(Locale.Get("K45_WTS_RELATIVE_OFFSET"), out m_arrayCoord, helperSize, OnPositionChange);
            AddVector2Field(Locale.Get("K45_WTS_PIVOT_OFFSET"), out m_arrayPivot, helperSize, OnPivotChange);
            AddFloatField(Locale.Get("K45_WTS_TEXT_SCALE"), out m_textScale, helperSize, OnScaleSubmit, false);
            AddFloatField(Locale.Get("K45_WTS_CHARACTER_SPACING"), out m_charSpacingFactor, helperSize, OnSpacingFactorChanged, false);
            AddIntField(Locale.Get("K45_WTS_MAX_WIDTH_PIXELS"), out m_maxWidth, helperSize, OnMaxWidthChange, false);
            AddIntField(Locale.Get("K45_WTS_FIXED_HEIGHT_PIXELS"), out m_fixedHeight, helperSize, OnFixedHeightChange, false);
            AddCheckboxLocale("K45_WTS_RESIZE_Y_TEXT_OVERFLOW", out m_applyScaleOnY, helperSize, OnChangeApplyRescaleOnY);

            helperAppearance.AddSpace(5);
            AddColorField(helperAppearance, Locale.Get("K45_WTS_TEXT_COLOR"), out m_textFixedColor, OnFixedColorChanged);
            AddDropdown(Locale.Get("K45_WTS_COLOR_SOURCE"), out m_colorSource, helperAppearance, (Enum.GetValues(typeof(ColoringSource)) as ColoringSource[]).Select(x => Locale.Get("K45_WTS_COLORSRC", x.ToString())).ToArray(), OnSetColorSource);
            helperAppearance.AddSpace(5);

            AddDropdown(Locale.Get("K45_WTS_TEXT_CONTENT"), out m_dropdownTextContent, helperConfig, WTSDynamicTextRenderingRules.ALLOWED_TYPES_HIGHWAY_SHIELDS.Select(x => Locale.Get("K45_WTS_BOARD_TEXT_TYPE_DESC_HWSHIELD", x.ToString())).ToArray(), OnSetTextOwnNameContent);
            AddTextField(Locale.Get("K45_WTS_CUSTOM_TEXT"), out m_customText, helperConfig, OnSetTextCustom);
            IEnumerator OnFilter(string x, Wrapper<string[]> result)
            {
                yield return result.Value = OnFilterSprites(WTSHighwayShieldEditor.Instance.Preview.OverrideSprite, x);
            }

            AddFilterableInput(Locale.Get("K45_WTS_SPRITE_NAME"), helperConfig, out m_spriteFilter, out UIListBox lb2, OnFilter, OnSpriteNameChanged);
            lb2.size = new Vector2(MainContainer.width - 20, 220);
            lb2.processMarkup = true;
            m_spriteFilter.eventGotFocus += (x, y) =>
            {
                var text = ((UITextField)x).text;
                if (text.StartsWith(WTSAtlasesLibrary.PROTOCOL_IMAGE_ASSET) || text.StartsWith(WTSAtlasesLibrary.PROTOCOL_IMAGE))
                {
                    WTSHighwayShieldEditor.Instance.Preview.OverrideSprite.spriteName = ((UITextField)x).text.Split('/').Last().Trim();
                }
            };
            lb2.eventItemMouseHover += (x, y) =>
            {
                if (y >= 0 && y < lb2.items.Length)
                {
                    WTSHighwayShieldEditor.Instance.Preview.OverrideSprite.spriteName = lb2.items[y].Split('/').Last().Trim();
                }
            };
            lb2.eventVisibilityChanged += (x, y) => WTSHighwayShieldEditor.Instance.Preview.OverrideSprite.parent.isVisible = y;
            WTSHighwayShieldEditor.Instance.Preview.OverrideSprite.parent.isVisible = false;

            helperConfig.AddSpace(5);
            AddDropdown(Locale.Get("K45_WTS_OVERRIDE_FONT"), out m_overrideFontSelect, helperConfig, new string[0], OnSetOverrideFont);
            AddDropdown(Locale.Get("K45_WTS_CLASS_FONT"), out m_fontClassSelect, helperConfig, (Enum.GetValues(typeof(FontClass)) as FontClass[]).Select(x => Locale.Get("K45_WTS_FONTCLASS", x.ToString())).ToArray(), OnSetFontClass);
            AddTextField(Locale.Get("K45_WTS_PREFIX"), out m_textPrefix, helperConfig, OnSetPrefix);
            AddTextField(Locale.Get("K45_WTS_SUFFIX"), out m_textSuffix, helperConfig, OnSetSuffix);

            WTSUtils.ReloadFontsOf(m_overrideFontSelect, null, true, true);

            WTSHighwayShieldEditor.Instance.CurrentTabChanged += (newVal) =>
            {
                int targetTab = newVal - 1;
                SafeObtain(OnSetData, targetTab);
            };
            m_isEditing = false;

            AddLibBox<WTSLibHighwayShieldTextLayer, ImageLayerTextDescriptorXml>(helperSettings, out UIButton m_copyButtonText,
                 DoCopyText, out m_pasteButtonText,
                 DoPasteText, out UIButton m_deleteButtonText,
                 DoDeleteText, (loadedItem) => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
                 {
                     string name = x.SaveName;
                     x = XmlUtils.DefaultXmlDeserialize<ImageLayerTextDescriptorXml>(loadedItem);
                     x.SaveName = name;
                     OnSetData(ref x);
                     x.SaveName = name;
                 }),
                 () => XmlUtils.DefaultXmlSerialize(WTSHighwayShieldEditor.Instance.EditingInstance.TextDescriptors[Math.Max(0, TabToEdit)]));
            m_pasteButtonText.isVisible = false;
        }


        public void Start() => WriteTheSignsMod.Controller.EventFontsReloadedFromFolder += () => SafeObtain((ref ImageLayerTextDescriptorXml x) => WTSUtils.ReloadFontsOf(m_overrideFontSelect, x.m_overrideFont, true));

        private void DoDeleteText() => WTSHighwayShieldEditor.Instance.RemoveTabFromItem(TabToEdit);
        private void DoPasteText() => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
        {
            if (m_clipboard != null)
            {
                string name = x.SaveName;
                x = XmlUtils.DefaultXmlDeserialize<ImageLayerTextDescriptorXml>(m_clipboard);
                x.SaveName = name;
                OnSetData(ref x);
            }
        });
        private void DoCopyText() => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
        {
            m_clipboard = XmlUtils.DefaultXmlSerialize(x);
            m_pasteButtonText.isVisible = true;
        });


        private void OnSetData(ref ImageLayerTextDescriptorXml x)
        {
            m_tabName.text = x.SaveName ?? "";

            m_arrayCoord[0].text = x.OffsetUV.x.ToString("F3");
            m_arrayCoord[1].text = x.OffsetUV.y.ToString("F3");
            m_arrayPivot[0].text = x.PivotUV.x.ToString("F3");
            m_arrayPivot[1].text = x.PivotUV.y.ToString("F3");
            m_charSpacingFactor.text = x.m_charSpacingFactor.ToString("F3");
            m_textScale.text = x.m_textScale.ToString("F3");
            m_maxWidth.text = x.m_maxWidthPixels.ToString("0");
            m_applyScaleOnY.isChecked = x.m_applyOverflowResizingOnY;
            m_fixedHeight.text = x.m_fixedHeightPixels.ToString("0");


            m_colorSource.selectedIndex = (int)x.ColoringConfig.ColorSource;
            m_textFixedColor.selectedColor = x.ColoringConfig.m_cachedColor;

            m_dropdownTextContent.selectedIndex = Array.IndexOf(WTSDynamicTextRenderingRules.ALLOWED_TYPES_HIGHWAY_SHIELDS, x.m_textType);
            m_customText.text = x.m_fixedText ?? "";
            m_overrideFontSelect.selectedIndex = x.m_overrideFont == null ? 0 : x.m_overrideFont == WTSController.DEFAULT_FONT_KEY ? 1 : Array.IndexOf(m_overrideFontSelect.items, x.m_overrideFont);
            m_fontClassSelect.selectedIndex = (int)x.m_fontClass;
            m_textPrefix.text = x.m_prefix ?? "";
            m_textSuffix.text = x.m_suffix ?? "";
            m_spriteFilter.text = x.m_spriteParam?.ToString() ?? "";

            ApplyShowRules(x);
        }

        private void ApplyShowRules(ImageLayerTextDescriptorXml x)
        {
            m_customText.parent.isVisible = x.m_textType == TextType.Fixed;
            m_textFixedColor.parent.isVisible = x.ColoringConfig.ColorSource == ColoringSource.Fixed;

            m_textPrefix.parent.isVisible = !x.IsSpriteText();
            m_textSuffix.parent.isVisible = !x.IsSpriteText();
            m_overrideFontSelect.parent.isVisible = !x.IsSpriteText();
            m_fontClassSelect.parent.isVisible = !x.IsSpriteText();
            m_spriteFilter.parent.isVisible = x.m_textType == TextType.GameSprite;
        }


        private delegate void SafeObtainMethod(ref ImageLayerTextDescriptorXml x);

        private void SafeObtain(SafeObtainMethod action, int? targetTab = null)
        {
            if (m_isEditing || WTSHighwayShieldEditor.Instance.EditingInstance == null)
            {
                return;
            }

            lock (this)
            {
                m_isEditing = true;
                try
                {
                    int effTargetTab = Math.Max(0, targetTab ?? TabToEdit);
                    if (effTargetTab < WTSHighwayShieldEditor.Instance.EditingInstance.TextDescriptors.Count)
                    {
                        var x = WTSHighwayShieldEditor.Instance.EditingInstance.TextDescriptors[effTargetTab];
                        action(ref x);
                        WTSHighwayShieldEditor.Instance.EditingInstance.TextDescriptors[effTargetTab] = x;
                        Preview.ReloadData();
                    }
                }
                finally
                {
                    m_isEditing = false;
                }
            }
        }

        private void OnTabNameChanged(string text) => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
        {
            if (text.IsNullOrWhiteSpace())
            {
                m_tabName.text = x.SaveName;
            }
            else
            {
                x.SaveName = text;
                WTSHighwayShieldEditor.Instance.SetTabName(TabToEdit, text);
            }
        });
        private void OnSetSuffix(string text) => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
        {
            x.m_suffix = text;

            WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
        });
        private void OnSetPrefix(string text) => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
        {
            x.m_prefix = text;

            WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
        });

        private void OnSetOverrideFont(int sel) => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
        {
            x.m_overrideFont = sel > 1 && sel < (m_overrideFontSelect?.items?.Length ?? 0) ? m_overrideFontSelect.items[sel]
                    : sel == 1 ? WTSController.DEFAULT_FONT_KEY
                    : null;

            WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
        });
        private void OnSetFontClass(int sel) => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
        {
            if (sel >= 0)
            {
                x.m_fontClass = (FontClass)sel;

                WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
            }
        });
        private void OnSetTextCustom(string text) => SafeObtain((ref ImageLayerTextDescriptorXml x) => x.m_fixedText = text);


        private string OnSpriteNameChanged(string input, int selIdx, string[] refArray)
        {
            if (selIdx >= 0 && lastProtocol_searchedParam == WTSAtlasesLibrary.PROTOCOL_IMAGE && refArray[selIdx].EndsWith("/"))
            {
                StartCoroutine(RefocusParamIn2Frames(m_spriteFilter));
                return lastProtocol_searchedParam + refArray[selIdx].Trim();
            }
            else
            {
                string result = "";
                SafeObtain((ref ImageLayerTextDescriptorXml x) => result = (x.SpriteParam = (selIdx >= 0 && lastProtocol_searchedParam != null) ? lastProtocol_searchedParam + refArray[selIdx].Trim() : input) ?? "");
                lastProtocol_searchedParam = null;
                WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
                return result;
            }
        }
        private IEnumerator RefocusParamIn2Frames(UITextField target)
        {
            yield return new WaitForEndOfFrame();
            target.Focus();
        }

        private string[] OnFilterSprites(UISprite sprite, string arg) => WriteTheSignsMod.Controller.AtlasesLibrary.OnFilterParamImagesByText(sprite, arg, null, out lastProtocol_searchedParam);
        private void OnSetTextOwnNameContent(int sel) => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
                                                       {
                                                           if (sel >= 0)
                                                           {
                                                               x.m_textType = WTSDynamicTextRenderingRules.ALLOWED_TYPES_HIGHWAY_SHIELDS[sel];
                                                               ApplyShowRules(x);
                                                               WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
                                                           }
                                                       });

        private void OnChangeApplyRescaleOnY(bool isChecked) => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
        {
            x.m_applyOverflowResizingOnY = isChecked;
            WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
        });
        private void OnFixedColorChanged(Color value) => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
                                                       {
                                                           x.ColoringConfig.m_cachedColor = value;

                                                           WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
                                                       });

        private void OnMaxWidthChange(int obj) => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
        {
            x.m_maxWidthPixels = obj;
            WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
        });
        private void OnSetColorSource(int obj) => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
        {
            if (obj >= 0)
            {
                x.ColoringConfig.ColorSource = (ColoringSource)obj;
                ApplyShowRules(x);
                WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
            }
        });
        private void OnFixedHeightChange(int obj) => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
        {
            x.m_fixedHeightPixels = obj;

            WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
        });
        private void OnScaleSubmit(float scale) => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
        {
            x.m_textScale = scale;
            WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
        });

        private void OnPivotChange(Vector2 obj) => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
        {
            x.PivotUV = Vector2.Min(Vector2.one, Vector2.Max(Vector2.zero, obj));
            m_arrayPivot[0].text = x.PivotUV.x.ToString("F3");
            m_arrayPivot[1].text = x.PivotUV.y.ToString("F3");
            WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
        });
        private void OnPositionChange(Vector2 obj) => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
        {
            x.OffsetUV = Vector2.Min(Vector2.one, Vector2.Max(Vector2.zero, obj));
            m_arrayCoord[0].text = x.OffsetUV.x.ToString("F3");
            m_arrayCoord[1].text = x.OffsetUV.y.ToString("F3");
            WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
        });
        private void OnSpacingFactorChanged(float obj) => SafeObtain((ref ImageLayerTextDescriptorXml x) =>
        {
            x.m_charSpacingFactor = Math.Max(0, obj);
            m_charSpacingFactor.text = x.m_charSpacingFactor.ToString("F3");
            WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
        });
    }
}
