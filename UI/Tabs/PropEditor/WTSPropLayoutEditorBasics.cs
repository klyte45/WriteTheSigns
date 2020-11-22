using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Libraries;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSPropLayoutEditorBasics : UICustomControl
    {
        public UIPanel MainContainer { get; protected set; }

        private UITabstrip m_tabstrip;
        private UITabContainer m_tabContainer;

        private UIPanel m_tabSettings;
        private UIPanel m_tabLib;

        protected UITextField m_propFilter;
        protected UIDropDown m_fontSelect;
        protected UITextField m_name;
        protected UIColorField m_fixedColor;
        private UIDropDown m_dropdownTextContent;

        private UIButton m_pasteButton;


        private PropInfo m_lastSelection;

        private BoardDescriptorGeneralXml EditingInstance => WTSPropLayoutEditor.Instance.EditingInstance;

        public void Awake()
        {
            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.padding = new RectOffset(5, 5, 5, 5);
            MainContainer.autoLayoutPadding = new RectOffset(0, 0, 3, 3);
            

            KlyteMonoUtils.CreateTabsComponent(out m_tabstrip, out m_tabContainer, MainContainer.transform, "TextEditor", new Vector4(0, 0, MainContainer.width, 40), new Vector4(0, 0, MainContainer.width, MainContainer.height - 40));
            m_tabSettings = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Settings), "K45_WTS_GENERAL_SETTINGS", "PrpSettings");
            m_tabLib = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Load), "K45_WTS_PROP_ITEM_LIB_TITLE", "PrpLib");

            m_tabSettings.clipChildren = true;
            m_tabLib.clipChildren = true;

            var helperSettings = new UIHelperExtension(m_tabSettings, LayoutDirection.Vertical);
            var helperLib = new UIHelperExtension(m_tabLib, LayoutDirection.Vertical);



            AddFilterableInput(Locale.Get("K45_WTS_PROP_MODEL_SELECT"), helperSettings, out m_propFilter, out _, PrefabIndexes<PropInfo>.instance.BasicInputFiltering, OnSetProp);


            AddTextField(Locale.Get("K45_WTS_PROP_TAB_TITLE"), out m_name, helperSettings, OnSetName);
            AddColorField(helperSettings, Locale.Get("K45_WTS_PROP_COLOR"), out m_fixedColor, OnSetPropColor);

            AddDropdown(Locale.Get("K45_WTS_OVERRIDE_FONT"), out m_fontSelect, helperSettings, new string[0], OnSetFont);

            AddDropdown(Locale.Get("K45_WTS_TEXT_AVAILABILITY"), out m_dropdownTextContent, helperSettings, Enum.GetNames(typeof(TextRenderingClass)).Select(x => Locale.Get("K45_WTS_BOARD_TEXT_AVAILABILITY_DESC", x.ToString())).ToArray(), OnSetTextOwnNameContent);



            WTSPropLayoutEditor.Instance.CurrentTabChanged += (x) =>
            {
                if (x == 0 && EditingInstance != null)
                {
                    m_name.text = EditingInstance.SaveName;
                    m_fixedColor.selectedColor = EditingInstance.FixedColor ?? default;
                    m_fontSelect.selectedIndex = EditingInstance.FontName == null ? 0 : EditingInstance.FontName == WTSController.DEFAULT_FONT_KEY ? 1 : Array.IndexOf(m_fontSelect.items, EditingInstance.FontName);
                    m_dropdownTextContent.selectedIndex = (int)EditingInstance.m_allowedRenderClass;

                    var currentKV = PrefabIndexes<PropInfo>.instance.PrefabsLoaded.Where(x => x.Value?.name == EditingInstance.m_propName).FirstOrDefault();
                    m_lastSelection = currentKV.Value;
                    WTSPropLayoutEditor.Instance.CurrentPropInfo = m_lastSelection;
                    m_propFilter.text = currentKV.Key ?? "";
                }
            };




            AddLibBox<WTSLibPropSettings, BoardDescriptorGeneralXml>(helperLib, out UIButton m_copyButtonText,
                DoCopyText, out m_pasteButton,
                DoPasteText, out _,
                null, LoadIntoCurrentConfig,
                () => XmlUtils.DefaultXmlSerialize(WTSPropLayoutEditor.Instance.EditingInstance));
            m_pasteButton.isVisible = m_clipboard != null;

        }

        public void Start()
        {
            WriteTheSignsMod.Controller.EventFontsReloadedFromFolder += () => WTSUtils.ReloadFontsOf(m_fontSelect, EditingInstance?.FontName, true);
            WTSUtils.ReloadFontsOf(m_fontSelect, null, true);
        }

        private void LoadIntoCurrentConfig(string loadedItem) => WTSPropLayoutEditor.Instance.ReplaceItem(EditingInstance.SaveName, loadedItem);


        private string m_clipboard;

        private void DoPasteText() => LoadIntoCurrentConfig(m_clipboard);
        private void DoCopyText()
        {
            m_clipboard = XmlUtils.DefaultXmlSerialize(EditingInstance);
            m_pasteButton.isVisible = true;
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
                    WTSPropLayoutEditor.Instance.SetCurrentSelectionNewName(text);
                }
            }
        }

        private void ShowChangeNameConfigModal(string lastError)
        {
            K45DialogControl.ShowModalPromptText(
                  new K45DialogControl.BindProperties
                  {
                      title = Locale.Get("K45_WTS_PROPEDIT_NAMECHANGE_TITLE"),
                      message = (lastError.IsNullOrWhiteSpace() ? "" : $"{ Locale.Get("K45_WTS_PROPEDIT_NAMECHANGE_ANERROROCURRED")} {lastError}\n\n") + Locale.Get("K45_WTS_PROPEDIT_NAMECHANGE_MESSAGE"),
                      showButton1 = true,
                      textButton1 = Locale.Get("EXCEPTION_OK"),
                      showButton2 = true,
                      textButton2 = Locale.Get("CANCEL")
                  }, (x, text) =>
                  {
                      if (x == 1)
                      {
                          string error = ValidateConfigName(text);

                          if (error.IsNullOrWhiteSpace())
                          {
                              WTSPropLayoutEditor.Instance.SetCurrentSelectionNewName(text);
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
                error = $"{Locale.Get("K45_WTS_PROPEDIT_CONFIGNEW_INVALIDNAME")}";
            }
            else if (text != EditingInstance.SaveName && WTSPropLayoutData.Instance.Get(text) != null)
            {
                error = $"{Locale.Get("K45_WTS_PROPEDIT_CONFIGNEW_ALREADY_EXISTS")}";
            }

            return error;
        }

        #region Actions        
        private void OnSetPropColor(UIComponent component, Color value) => EditingInstance.FixedColor = (value == default ? (Color?)null : value);

        private string OnSetProp(string typed, int sel, string[] items)
        {
            if (sel >= 0)
            {
                PrefabIndexes<PropInfo>.instance.PrefabsLoaded.TryGetValue(items[sel], out PropInfo targetProp);
                WTSPropLayoutEditor.Instance.CurrentPropInfo = targetProp;
                return items[sel];
            }
            else
            {
                WTSPropLayoutEditor.Instance.CurrentPropInfo = null;
                return null;
            }
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
                    EditingInstance.FontName = WTSController.DEFAULT_FONT_KEY;
                }
                else
                {
                    EditingInstance.FontName = null;
                }
            }
        }
        private void OnSetTextOwnNameContent(int sel) => EditingInstance.m_allowedRenderClass = (TextRenderingClass)sel;

        #endregion

    }

}
