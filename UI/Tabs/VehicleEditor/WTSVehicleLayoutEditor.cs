using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Packaging;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Singleton;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSVehicleLayoutEditor : UICustomControl
    {
        public static WTSVehicleLayoutEditor Instance { get; private set; }
        public UIPanel MainContainer { get; protected set; }


        private Dictionary<string, VehicleInfo> m_loadedVehicles;

        #region Panel areas
        private UIPanel m_topBar;
        private UIPanel m_middleBar;
        private UILabel m_cantEditText;
        private UIPanel m_editArea;
        #endregion

        #region Top bar controls
        private UITextField m_vehicleSearch;


        private UILabel m_labelSelectionDescription;
        private UIPanel m_containerSelectionDescription;
        private UIButton m_btnNew;
        private UIButton m_btnCopy;
        private UIButton m_btnDelete;
        private UIButton m_btnLoad;
        private UIButton m_btnExport;
        private UIButton m_btnSteam;
        private UIButton m_btnReload;
        #endregion
        #region Mid bar controls
        private UIScrollablePanel m_editTabstrip;
        private UIButton m_plusButton;
        private WTSVehicleLayoutEditorPreview m_preview;
        private UIPanel m_orderedRulesList;
        #endregion
        #region Bottom bar panels
        private UIPanel m_basicInfoEditor;
        private UIPanel m_textInfoEditor;
        #endregion

        internal int CurrentTab { get; private set; }

        private LayoutDescriptorVehicleXml m_editingInstance;
        private UITemplateList<UIButton> m_tabs;

        private event Action<LayoutDescriptorVehicleXml, ConfigurationSource> EventOnVehicleSelectionChanged;

        internal ref LayoutDescriptorVehicleXml EditingInstance => ref m_editingInstance;
        internal VehicleInfo CurrentVehicleInfo { get; set; }

        public bool LockSelection { get; private set; }
        public ConfigurationSource CurrentConfigurationSource { get; private set; }

        internal event Action<int> CurrentTabChanged;

        public void Awake()
        {
            Instance = this;

            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.autoLayoutPadding = new RectOffset(5, 5, 5, 5);
            MainContainer.clipChildren = true;


            KlyteMonoUtils.CreateUIElement(out m_topBar, MainContainer.transform, "topBar", new UnityEngine.Vector4(0, 0, MainContainer.width - MainContainer.padding.horizontal, 50));
            m_topBar.autoLayout = true;
            m_topBar.autoLayoutDirection = LayoutDirection.Vertical;
            m_topBar.padding = new RectOffset(5, 5, 5, 5);
            m_topBar.autoFitChildrenVertically = true;
            var m_topHelper = new UIHelperExtension(m_topBar);

            AddFilterableInput(Locale.Get("K45_WTS_VEHICLEEDITOR_SELECTMODEL"), m_topHelper, out m_vehicleSearch, out _, OnFilterVehicleTyped, GetCurrentSelection, OnVehicleNameSelected);

            AddLabel("", m_topHelper, out m_labelSelectionDescription, out m_containerSelectionDescription);
            KlyteMonoUtils.LimitWidthAndBox(m_labelSelectionDescription, (m_topHelper.Self.width / 2), true);
            m_labelSelectionDescription.prefix = Locale.Get("K45_WTS_CURRENTSELECTION") + ": ";
            m_btnNew = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_New, OnCreateNewCity, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_NEWINCITY", false);
            m_btnCopy = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Copy, OnCopyToCity, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_COPYTOCITY", false);
            m_btnDelete = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Delete, OnDeleteFromCity, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_DELETEFROMCITY", false);
            m_btnLoad = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Load, OnOpenGlobalFolder, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_OPENGLOBALSFOLDER", false);
            m_btnExport = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Export, OnExportAsGlobal, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_EXPORTASGLOBAL", false);
            m_btnSteam = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Steam, OnExportAsAsset, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_EXPORTTOASSETFOLDER", false);
            m_btnReload = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Reload, OnReloadDescriptors, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_RELOADDESCRIPTORS", false);


            KlyteMonoUtils.CreateUIElement(out m_middleBar, MainContainer.transform, "previewBar", new Vector4(0, 0, MainContainer.width - MainContainer.padding.horizontal, 300));
            m_middleBar.autoLayout = true;
            m_middleBar.autoLayoutDirection = LayoutDirection.Horizontal;


            KlyteMonoUtils.CreateUIElement(out UIPanel previewContainer, m_middleBar.transform, "previewContainer", new UnityEngine.Vector4(0, 0, m_middleBar.width * .6f, m_middleBar.height - m_middleBar.padding.vertical));
            m_preview = previewContainer.gameObject.AddComponent<WTSVehicleLayoutEditorPreview>();


            KlyteMonoUtils.CreateScrollPanel(m_middleBar, out m_editTabstrip, out _, m_middleBar.width - previewContainer.width - m_middleBar.padding.horizontal - (m_middleBar.autoLayoutPadding.horizontal * 2) - 20, 300);
            m_editTabstrip.autoLayout = true;
            m_editTabstrip.autoLayoutDirection = LayoutDirection.Vertical;

            InitTabButton(m_editTabstrip, out _, Locale.Get("K45_WTS_BASIC_INFO_TAB_TITLE"), new Vector2(m_editTabstrip.size.x, 30), (x, y) => OnTabChange(x.zOrder));
            KlyteMonoUtils.CreateUIElement(out m_orderedRulesList, m_editTabstrip.transform, "GenTabs", new Vector4(0, 0, m_editTabstrip.width, 0));
            m_orderedRulesList.autoFitChildrenVertically = true;
            m_orderedRulesList.autoLayout = true;
            m_orderedRulesList.autoLayoutDirection = LayoutDirection.Vertical;
            InitTabButton(m_editTabstrip, out m_plusButton, Locale.Get("K45_WTS_ADD_NEW_TEXT_ENTRY"), new Vector2(m_editTabstrip.size.x, 30), null);
            m_plusButton.eventClicked += AddTabToItem;

            m_tabs = new UITemplateList<UIButton>(m_orderedRulesList, TAB_TEMPLATE_NAME);
            KlyteMonoUtils.CreateUIElement(out m_cantEditText, MainContainer.transform, "text", new UnityEngine.Vector4(0, 0, MainContainer.width - MainContainer.padding.horizontal, MainContainer.height - m_middleBar.height - m_topBar.height - MainContainer.padding.vertical - (MainContainer.autoLayoutPadding.vertical * 2) - 5));
            m_cantEditText.text = Locale.Get("K45_WTS_VEHICLEEDITOR_CANTEDITTEXT");
            m_cantEditText.textAlignment = UIHorizontalAlignment.Center;

            KlyteMonoUtils.CreateUIElement(out m_editArea, MainContainer.transform, "editArea", new UnityEngine.Vector4(0, 0, MainContainer.width - MainContainer.padding.horizontal, MainContainer.height - m_middleBar.height - m_topBar.height - MainContainer.padding.vertical - (MainContainer.autoLayoutPadding.vertical * 2) - 5));
            m_editArea.padding = new RectOffset(5, 5, 5, 5);


            KlyteMonoUtils.CreateUIElement(out m_basicInfoEditor, m_editArea.transform, "basicTab", new UnityEngine.Vector4(0, 0, m_editArea.width - m_editArea.padding.horizontal, m_editArea.height - m_editArea.padding.vertical));
            m_basicInfoEditor.gameObject.AddComponent<WTSVehicleLayoutEditorBasics>();
            //KlyteMonoUtils.CreateUIElement(out m_textInfoEditor, m_editArea.transform, "textTab", new UnityEngine.Vector4(0, 0, m_editArea.width - m_editArea.padding.horizontal, m_editArea.height - m_editArea.padding.vertical));
            //m_textInfoEditor.gameObject.AddComponent<WTSVehicleLayoutEditorTexts>();
            CreateTabTemplate();

            ReloadVehicle();
            OnTabChange(0);

        }


        private void OnReloadDescriptors()
        {
            WriteTheSignsMod.Controller?.VehicleTextsSingleton?.LoadAllVehiclesConfigurations();
            ReloadVehicle();
        }
        private void OnExportAsAsset() => ExportTo(Path.Combine(Path.GetDirectoryName(PackageManager.FindAssetByName(CurrentVehicleInfo.name)?.package?.packagePath), $"{WTSController.m_defaultFileNameVehiclesXml}.xml"));
        private void ExportTo(string output)
        {
            if (CurrentVehicleInfo != null)
            {
                var assetId = CurrentVehicleInfo.name.Split('.')[0] + ".";
                var descriptorsToExport = new List<LayoutDescriptorVehicleXml>();
                foreach (string assetName in m_loadedVehicles
                .Where((x) => x.Value.name.StartsWith(assetId))
                .Select(x => x.Value.name))
                {
                    WTSVehicleTextsSingleton.GetTargetDescriptor(assetName, out _, out LayoutDescriptorVehicleXml target);
                    if (target != null)
                    {
                        target.VehicleAssetName = assetName;
                        descriptorsToExport.Add(target);
                    }
                }
                if (descriptorsToExport.Count > 0)
                {
                    var exportableLayouts = new ExportableLayoutDescriptorVehicleXml
                    {
                        Descriptors = descriptorsToExport.ToArray()
                    };
                    File.WriteAllText(output, XmlUtils.DefaultXmlSerialize(exportableLayouts));

                    WriteTheSignsMod.Controller?.VehicleTextsSingleton?.LoadAllVehiclesConfigurations();
                }
            }
        }

        private void OnExportAsGlobal() => ExportTo(Path.Combine(WTSController.DefaultVehiclesConfigurationFolder, $"{WTSController.m_defaultFileNameVehiclesXml}_{PackageManager.FindAssetByName(CurrentVehicleInfo.name)?.package.packageMainAsset}.xml"));

        private void OnOpenGlobalFolder() => ColossalFramework.Utils.OpenInFileBrowser(WTSController.DefaultVehiclesConfigurationFolder);
        private void OnDeleteFromCity()
        {
            K45DialogControl.ShowModal(new K45DialogControl.BindProperties
            {
                message = Locale.Get("K45_WTS_PROMPTDELETEBUILDINGLAYOUT"),
                showButton1 = true,
                showButton2 = true,
                textButton1 = Locale.Get("YES"),
                textButton2 = Locale.Get("NO"),
            }, (x) =>
            {
                if (x == 1)
                {
                    WTSVehicleData.Instance.CityDescriptors.Remove(CurrentVehicleInfo.name);
                    ReloadVehicle();
                }

                return true;
            });
        }
        private void OnCopyToCity()
        {
            WTSVehicleData.Instance.CityDescriptors[CurrentVehicleInfo.name] = XmlUtils.DefaultXmlDeserialize<LayoutDescriptorVehicleXml>(XmlUtils.DefaultXmlSerialize(EditingInstance));
            ReloadVehicle();
        }
        private void OnCreateNewCity()
        {
            WTSVehicleData.Instance.CityDescriptors[CurrentVehicleInfo.name] = new LayoutDescriptorVehicleXml
            {
                VehicleAssetName = CurrentVehicleInfo.name,
            };
            ReloadVehicle();
        }

        internal void ReloadVehicle()
        {

            m_middleBar.isVisible = CurrentVehicleInfo != null;
            m_containerSelectionDescription.isVisible = CurrentVehicleInfo != null;
            if (CurrentVehicleInfo != null)
            {
                WTSVehicleTextsSingleton.GetTargetDescriptor(CurrentVehicleInfo.name, out ConfigurationSource source, out LayoutDescriptorVehicleXml target);
                m_labelSelectionDescription.text = (CurrentVehicleInfo.name?.EndsWith("_Data") ?? false) ? Locale.Get("VEHICLE_TITLE", CurrentVehicleInfo.name) + "\n" : $"{CurrentVehicleInfo.name}\n";
                m_labelSelectionDescription.suffix = $"{Locale.Get("K45_WTS_CURRENTLY_USING")}: {Locale.Get("K45_WTS_CONFIGURATIONSOURCE", source.ToString())}";
                EditingInstance = target;
                CurrentConfigurationSource = source;
                EventOnVehicleSelectionChanged?.Invoke(EditingInstance, CurrentConfigurationSource);

                m_btnNew.isVisible = CurrentConfigurationSource != ConfigurationSource.CITY;
                m_btnCopy.isVisible = CurrentConfigurationSource != ConfigurationSource.CITY && CurrentConfigurationSource != ConfigurationSource.NONE;
                m_btnDelete.isVisible = CurrentConfigurationSource == ConfigurationSource.CITY;
                m_btnExport.isVisible = CurrentConfigurationSource == ConfigurationSource.CITY;
                m_btnSteam.isVisible = CurrentConfigurationSource == ConfigurationSource.CITY && CurrentVehicleInfo.name.EndsWith("_Data");
                OnTabChange(0);
            }
            m_editArea.isVisible = CurrentVehicleInfo != null && CurrentConfigurationSource == ConfigurationSource.CITY;
            m_cantEditText.isVisible = CurrentConfigurationSource == ConfigurationSource.ASSET || CurrentConfigurationSource == ConfigurationSource.GLOBAL;
        }

        private string OnVehicleNameSelected(int arg1, string[] arg2)
        {
            if (arg1 < 0)
            {
                CurrentVehicleInfo = null;
                ReloadVehicle();
                return "";
            }
            var result = arg2[arg1];
            CurrentVehicleInfo = m_loadedVehicles.TryGetValue(result, out VehicleInfo info) ? info : null;
            ReloadVehicle();
            return result;
        }
        private string GetCurrentSelection() => CurrentVehicleInfo?.name ?? "";
        private string[] OnFilterVehicleTyped(string arg)
        {
            if (m_loadedVehicles == null)
            {
                m_loadedVehicles = PrefabUtils.instance.GetInfos<VehicleInfo>().ToDictionary(x => x.name.EndsWith("_Data") ? Locale.Get("VEHICLE_TITLE", x.name) : x.name, x => x);
            }
            return m_loadedVehicles
            .Where((x) => arg.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Key + (PrefabUtils.instance.AuthorList.TryGetValue(x.Value.name.Split('.')[0], out string author) ? "\n" + author : ""), arg, CompareOptions.IgnoreCase) >= 0)
            .Select(x => x.Key)
            .OrderBy((x) => x)
            .ToArray();
        }

        private void OnTabChange(int idx)
        {
            CurrentTab = idx;
            //m_basicInfoEditor.isVisible = CurrentTab == 0;
            //m_textInfoEditor.isVisible = CurrentTab != 0;
            CurrentTabChanged?.Invoke(idx);
            FixTabstrip();
            m_preview.ResetCamera();

        }

        private void AddTabToItem(UIComponent x, UIMouseEventParameter y)
        {
            var newItem = new BoardTextDescriptorGeneralXml
            {
                SaveName = $"New text"
            };
            EditingInstance.TextDescriptors = EditingInstance.TextDescriptors.Union(new BoardTextDescriptorGeneralXml[] { newItem
            }).ToArray();
            FixTabstrip();
        }

        private const string TAB_TEMPLATE_NAME = "K45_WTS_TabTemplateVehicle";

        private void CreateTabTemplate()
        {
            var go = new GameObject();

            InitTabButton(go, out UIButton button, "AAA", new Vector2(m_editTabstrip.size.x, 30), null);
            UITemplateUtils.GetTemplateDict()[TAB_TEMPLATE_NAME] = button;
        }

        public void FixTabstrip()
        {
            m_tabs.SetItemCount(EditingInstance?.TextDescriptors?.Length ?? 0);
            for (int i = 0; i < (EditingInstance?.TextDescriptors?.Length ?? 0); i++)
            {
                var but = m_tabs.items[i];
                if (but.stringUserData.IsNullOrWhiteSpace())
                {
                    but.eventClicked += (x, y) => OnTabChange(x.zOrder + 1);
                    but.stringUserData = "A";
                }
                but.text = EditingInstance.TextDescriptors[i]?.SaveName ?? "<EMPTY NAME>";
            }
        }





        public void Update()
        {
            if (MainContainer.isVisible)
            {
                foreach (UIButton btn in m_editTabstrip.GetComponentsInChildren<UIButton>())
                {
                    if (btn != m_plusButton)
                    {
                        if (btn.zOrder == CurrentTab)
                        {
                            btn.state = UIButton.ButtonState.Focused;
                        }
                        else
                        {
                            btn.state = UIButton.ButtonState.Normal;
                        }
                    }
                }
                foreach (UIButton btn in m_orderedRulesList.GetComponentsInChildren<UIButton>())
                {
                    if (btn.zOrder == CurrentTab - 1)
                    {
                        btn.state = UIButton.ButtonState.Focused;
                    }
                    else
                    {
                        btn.state = UIButton.ButtonState.Normal;
                    }
                }
            }
        }

    }

}
