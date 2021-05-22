using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons;
using Klyte.Commons.Extensions;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Libraries;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using System;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSVehicleLayoutEditorBasics : UICustomControl
    {
        public UIPanel MainContainer { get; protected set; }

        private UITabstrip m_tabstrip;
        private UITabContainer m_tabContainer;

        private UIPanel m_tabSettings;
        private UIPanel m_tabLib;

        protected UIDropDown m_fontSelect;

        private UIButton m_pasteButton;

        private LayoutDescriptorVehicleXml EditingInstance => WTSVehicleLayoutEditor.Instance.EditingInstance;

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

            WTSVehicleLayoutEditor.Instance.CurrentTabChanged += (x) =>
            {
                if (x == 0 && EditingInstance != null)
                {
                    m_fontSelect.selectedIndex = EditingInstance.FontName == null ? 0 : EditingInstance.FontName == WTSController.DEFAULT_FONT_KEY ? 1 : Array.IndexOf(m_fontSelect.items, EditingInstance.FontName);
                }
                MainContainer.isVisible = x == 0 && EditingInstance != null;
            };

            AddLibBox<WTSLibVehicleLayout, LayoutDescriptorVehicleXml>(helperLib, out UIButton m_copyButtonText,
                DoCopyText, out m_pasteButton,
                DoPasteText, out _,
                null, LoadIntoCurrentConfig,
                () => XmlUtils.DefaultXmlSerialize(WTSVehicleLayoutEditor.Instance.EditingInstance));
            m_pasteButton.isVisible = m_clipboard != null;

        }

        public void Start()
        {
            WriteTheSignsMod.Controller.EventFontsReloadedFromFolder += () => WTSUtils.ReloadFontsOf(m_fontSelect, EditingInstance?.FontName, true);
            WTSUtils.ReloadFontsOf(m_fontSelect, null, true);
        }

        private void LoadIntoCurrentConfig(string loadedItem)
        {
            var data = XmlUtils.DefaultXmlDeserialize<LayoutDescriptorVehicleXml>(loadedItem);
            if (!data.IsValid())
            {
                if (CommonProperties.DebugMode)
                {
                    K45DialogControl.ShowModalError("The vehicle layout failed to be loaded! See data below.", loadedItem);
                }
                return;
            }
            WTSVehicleData.Instance.CityDescriptors[WTSVehicleLayoutEditor.Instance.CurrentVehicleInfo.name] = data;
            WTSVehicleLayoutEditor.Instance.ReloadVehicle();
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
                EditingInstance.FontName
                    = idx > 1 ? m_fontSelect.items[idx]
                    : idx == 1 ? WTSController.DEFAULT_FONT_KEY
                    : null;
            }
        }
    }

}
