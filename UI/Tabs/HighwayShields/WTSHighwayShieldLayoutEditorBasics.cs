using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensions;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Libraries;
using Klyte.WriteTheSigns.Sprites;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSHighwayShieldLayoutEditorBasics : UICustomControl
    {
        public UIPanel MainContainer { get; protected set; }

        private UITabstrip m_tabstrip;
        private UITabContainer m_tabContainer;

        private UIPanel m_tabSettings;
        private UIPanel m_tabLib;

        protected UIDropDown m_fontSelect;
        protected UIColorField m_bgColor;
        protected UICheckBox m_useHwColor;
        protected UITextField m_spriteFilter;

        private UIButton m_pasteButton;

        private HighwayShieldDescriptor EditingInstance => WTSHighwayShieldEditor.Instance.EditingInstance;
        private WTSHighwayShieldLayoutEditorPreview Preview => WTSHighwayShieldEditor.Instance.Preview;
        public void Awake()
        {
            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.padding = new RectOffset(5, 5, 5, 5);
            MainContainer.autoLayoutPadding = new RectOffset(0, 0, 3, 3);

            KlyteMonoUtils.CreateTabsComponent(out m_tabstrip, out m_tabContainer, MainContainer.transform, "TextEditor", new Vector4(0, 0, MainContainer.width, 40), new Vector4(0, 0, MainContainer.width, 315));
            m_tabSettings = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Settings), "K45_WTS_GENERAL_SETTINGS", "PrpSettings");
            m_tabLib = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Load), "K45_WTS_PROP_ITEM_LIB_TITLE", "PrpLib");

            m_tabSettings.clipChildren = true;
            m_tabLib.clipChildren = true;

            var helperSettings = new UIHelperExtension(m_tabSettings, LayoutDirection.Vertical);
            var helperLib = new UIHelperExtension(m_tabLib, LayoutDirection.Vertical);

            AddDropdown(Locale.Get("K45_WTS_OVERRIDE_FONT"), out m_fontSelect, helperSettings, new string[0], OnSetFont);
            AddColorField(helperSettings, Locale.Get("K45_WTS_BACKGROUND_COLOR"), out m_bgColor, OnBgColorChanged);
            AddCheckboxLocale("K45_WTS_USEHWCOLOR", out m_useHwColor, helperSettings, OnUseHwColorChanged);

            IEnumerator OnFilter(string x, Wrapper<string[]> result)
            {
                yield return result.Value = OnFilterParamImages(WTSHighwayShieldEditor.Instance.Preview.OverrideSprite, x);
            }


            AddFilterableInput(Locale.Get("K45_WTS_BGSPRITE_NAME"), helperSettings, out m_spriteFilter, out UIListBox lb2, OnFilter, OnBgSpriteNameChanged);
            lb2.size = new Vector2(MainContainer.width - 20, 220);
            lb2.processMarkup = true;
            m_spriteFilter.eventGotFocus += (x, y) =>
            {
                var text = ((UITextField)x).text;
                if (text.StartsWith(WTSAtlasesLibrary.PROTOCOL_IMAGE))
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


            WTSHighwayShieldEditor.Instance.CurrentTabChanged += OnTabChanged;

            AddLibBox<WTSLibHighwayShieldLayout, HighwayShieldDescriptor>(helperLib, out UIButton m_copyButtonText,
                DoCopyText, out m_pasteButton,
                DoPasteText, out _,
                null, LoadIntoCurrentConfig,
                () => XmlUtils.DefaultXmlSerialize(WTSHighwayShieldEditor.Instance.EditingInstance));
            m_pasteButton.isVisible = m_clipboard != null;

        }

        private void OnTabChanged(int x)
        {
            if (x == 0 && EditingInstance != null)
            {
                m_fontSelect.selectedIndex = EditingInstance.FontName == null ? 0 : EditingInstance.FontName == WTSController.DEFAULT_FONT_KEY ? 1 : Array.IndexOf(m_fontSelect.items, EditingInstance.FontName);
                m_bgColor.selectedColor = EditingInstance.BackgroundColor;
                m_useHwColor.isChecked = EditingInstance.BackgroundColorIsFromHighway;
                m_spriteFilter.text = EditingInstance.BackgroundImage ?? "";
            }
            MainContainer.isVisible = x == 0 && EditingInstance != null;
        }

        private string OnBgSpriteNameChanged(string input, int selIdx, string[] refArray)
        {
            if (selIdx >= 0 && lastProtocol_searchedParam == WTSAtlasesLibrary.PROTOCOL_IMAGE && refArray[selIdx].EndsWith("/"))
            {
                StartCoroutine(RefocusParamIn2Frames(m_spriteFilter));
                return lastProtocol_searchedParam + refArray[selIdx].Trim();
            }
            else
            {
                string result = EditingInstance.BackgroundImage = (selIdx >= 0 && lastProtocol_searchedParam != null) ? lastProtocol_searchedParam + refArray[selIdx].Trim() : input;
                lastProtocol_searchedParam = null;
                Preview.ReloadData();
                WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
                return result;
            }
        }
        private string lastProtocol_searchedParam;
        private string[] OnFilterParamImages(UISprite sprite, string arg) => WriteTheSignsMod.Controller.AtlasesLibrary.OnFilterParamImagesByText(sprite, arg, null, out lastProtocol_searchedParam);
        private void OnUseHwColorChanged(bool isChecked) => EditingInstance.BackgroundColorIsFromHighway = isChecked;
        private void OnBgColorChanged(Color val)
        {
            EditingInstance.BackgroundColor = val;
            Preview.ReloadData();
            WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
        }

        public void Start()
        {
            WriteTheSignsMod.Controller.EventFontsReloadedFromFolder += () => WTSUtils.ReloadFontsOf(m_fontSelect, EditingInstance?.FontName, true);
            WTSUtils.ReloadFontsOf(m_fontSelect, null, true);
        }

        private void LoadIntoCurrentConfig(string loadedItem)
        {
            WTSHighwayShieldsData.Instance.CityDescriptors[WTSHighwayShieldEditor.Instance.CurrentSelection] = XmlUtils.DefaultXmlDeserialize<HighwayShieldDescriptor>(loadedItem);
            WTSHighwayShieldEditor.Instance.ReloadShield();
        }

        private string m_clipboard;

        private void DoPasteText() => LoadIntoCurrentConfig(m_clipboard);
        private void DoCopyText()
        {
            m_clipboard = XmlUtils.DefaultXmlSerialize(EditingInstance);
            m_pasteButton.isVisible = true;
        }



        protected void OnSetFont(int idx)
        {
            if (EditingInstance != null)
            {
                EditingInstance.FontName = idx > 1 ? m_fontSelect.items[idx] : idx == 1 ? WTSController.DEFAULT_FONT_KEY : null;

                Preview.ReloadData();
                WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
            }
        }

        private IEnumerator RefocusParamIn2Frames(UITextField target)
        {
            yield return new WaitForEndOfFrame();
            target.Focus();
        }

    }

}
