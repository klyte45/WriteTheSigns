using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Libraries;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Linq;
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
        private UIPanel m_submeshTitlePanel;
        private UIPanel m_submeshesContainer;
        private UIScrollablePanel m_submeshesScroll;
        private UITemplateList<UICheckBox> m_submeshList;

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
            AddLabel(Locale.Get("K45_WTS_SUBMESHESTOTURNBLACK"), helperSettings, out _, out m_submeshTitlePanel);

            KlyteMonoUtils.CreateUIElement(out m_submeshesContainer, helperSettings.Self.transform, "submeshes panel", new Vector4(0, 0, helperSettings.Self.width, helperSettings.Self.height - 100));
            KlyteMonoUtils.CreateScrollPanel(m_submeshesContainer, out m_submeshesScroll, out _, m_submeshesContainer.width - 30, m_submeshesContainer.height);

            m_submeshList = new UITemplateList<UICheckBox>(m_submeshesScroll, UIHelperExtension.kCheckBoxTemplate);

            WTSVehicleLayoutEditor.Instance.CurrentTabChanged += (x) =>
            {
                if (x == 0 && EditingInstance != null)
                {
                    m_fontSelect.selectedIndex = EditingInstance.FontName == null ? 0 : EditingInstance.FontName == WTSController.DEFAULT_FONT_KEY ? 1 : Array.IndexOf(m_fontSelect.items, EditingInstance.FontName);
                }
                FixSubmeshList();
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
            WTSVehicleData.Instance.AssetsDescriptors[EditingInstance.VehicleAssetName] = XmlUtils.DefaultXmlDeserialize<LayoutDescriptorVehicleXml>(loadedItem);
            WTSVehicleLayoutEditor.Instance.ReloadVehicle();
        }

        private string m_clipboard;

        private void DoPasteText() => LoadIntoCurrentConfig(m_clipboard);
        private void DoCopyText()
        {
            m_clipboard = XmlUtils.DefaultXmlSerialize(EditingInstance);
            m_pasteButton.isVisible = true;
        }

        public void FixSubmeshList()
        {
            var newLen = WTSVehicleLayoutEditor.Instance?.CurrentVehicleInfo?.m_subMeshes?.Length ?? 0;
            m_submeshList.SetItemCount(newLen);
            for (int i = 0; i < newLen; i++)
            {
                var but = m_submeshList.items[i];
                if (but.stringUserData.IsNullOrWhiteSpace())
                {
                    but.eventCheckChanged += (x, y) => OnSetSubmeshBlack(x.zOrder, y);
                    but.stringUserData = "A";
                }
                but.text = WTSVehicleLayoutEditor.Instance.CurrentVehicleInfo.m_subMeshes[i]?.m_subInfo?.name ?? $"<UNNAMED SUBMESH {i}>";
                but.isChecked = EditingInstance?.BlackSubmeshes?.Contains(i) ?? false;
            }
            m_submeshTitlePanel.isVisible = newLen > 0;
            m_submeshesContainer.isVisible = newLen > 0;
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


        protected void OnSetSubmeshBlack(int idx, bool value)
        {
            if (EditingInstance != null)
            {
                if (EditingInstance.BlackSubmeshes.Contains(idx) != value)
                {
                    if (value)
                    {
                        EditingInstance.BlackSubmeshes = EditingInstance.BlackSubmeshes.Union(new int[] { idx }).ToArray();
                    }
                    else
                    {
                        EditingInstance.BlackSubmeshes = EditingInstance.BlackSubmeshes.Where(x => x != idx).ToArray();
                    }
                }
            }
        }


    }

}
