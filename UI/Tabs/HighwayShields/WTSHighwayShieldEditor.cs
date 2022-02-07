using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensions;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Singleton;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;
namespace Klyte.WriteTheSigns.UI
{
    internal class WTSHighwayShieldEditor : UICustomControl
    {

        public static WTSHighwayShieldEditor Instance { get; private set; }
        public UIPanel MainContainer { get; protected set; }


        #region Panel areas
        private UIPanel m_topBar;
        private UIPanel m_middleBar;
        private UILabel m_cantEditText;
        private UIPanel m_editArea;
        #endregion

        #region Top bar controls
        private UITextField m_hwTypeSearch;


        private UILabel m_labelSelectionDescription;
        private UIPanel m_containerSelectionDescription;
        private UIButton m_btnNew;
        private UIButton m_btnCopyToCity;
        private UIButton m_btnDelete;
        private UIButton m_btnLoad;
        private UIButton m_btnExport;
        private UIButton m_btnReload;
        private UIButton m_btnCopy;
        private UIButton m_btnPaste;
        #endregion
        #region Mid bar controls
        private UIScrollablePanel m_editTabstrip;
        private UIButton m_plusButton;
        private UIPanel m_orderedRulesList;
        #endregion
        #region Bottom bar panels
        private UIPanel m_basicInfoEditor;
        private UIPanel m_textInfoEditor;
        #endregion

        internal int CurrentTab { get; private set; }

        private HighwayShieldDescriptor m_editingInstance;
        private UITemplateList<UIButton> m_tabs;

        private event Action<HighwayShieldDescriptor, ConfigurationSource> EventOnHwTypeSelectionChanged;

        internal ref HighwayShieldDescriptor EditingInstance => ref m_editingInstance;

        public bool LockSelection { get; private set; }
        public ConfigurationSource CurrentConfigurationSource { get; private set; }
        internal WTSHighwayShieldLayoutEditorPreview Preview { get; private set; }

        internal event Action<int> CurrentTabChanged;

        private string m_clipboard;

        internal string CurrentSelection { get; private set; }

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


            AddFilterableInput(Locale.Get("K45_WTS_HIGHWAYTYPE_SELECT"), m_topHelper, out m_hwTypeSearch, out _, OnFilter, OnHwTypeSelected);
            //AddButtonInEditorRow(m_hwTypeSearch, Commons.UI.SpriteNames.CommonsSpriteNames.K45_QuestionMark, Help_HighwayType, null, true, 30);

            AddLabel("", m_topHelper, out m_labelSelectionDescription, out m_containerSelectionDescription);
            KlyteMonoUtils.LimitWidthAndBox(m_labelSelectionDescription, (m_topHelper.Self.width / 2), out UIPanel containerBoxDescription, true);
            m_labelSelectionDescription.prefix = Locale.Get("K45_WTS_CURRENTSELECTION") + ": ";
            m_btnReload = AddButtonInEditorRow(containerBoxDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Reload, OnReloadDescriptors, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_RELOADDESCRIPTORS", false);
            m_btnExport = AddButtonInEditorRow(containerBoxDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Export, OnExportAsGlobal, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_EXPORTASGLOBAL", false);
            m_btnLoad = AddButtonInEditorRow(containerBoxDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Load, OnOpenGlobalFolder, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_OPENGLOBALSFOLDER", false);
            m_btnDelete = AddButtonInEditorRow(containerBoxDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Delete, OnDeleteFromCity, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_DELETEFROMCITY", false);
            m_btnCopyToCity = AddButtonInEditorRow(containerBoxDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Import, OnCopyToCity, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_COPYTOCITY", false);
            m_btnNew = AddButtonInEditorRow(containerBoxDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_New, OnCreateNewCity, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_NEWINCITY", false);
            m_btnCopy = AddButtonInEditorRow(containerBoxDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Copy, OnCopyToClipboard, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_COPYTOCLIPBOARD", false);
            m_btnPaste = AddButtonInEditorRow(containerBoxDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Paste, OnPasteFromClipboard, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_PASTEFROMCLIPBOARD", false);
            m_btnPaste.isVisible = false;


            KlyteMonoUtils.CreateUIElement(out m_middleBar, MainContainer.transform, "previewBar", new Vector4(0, 0, MainContainer.width - MainContainer.padding.horizontal, 300));
            m_middleBar.autoLayout = true;
            m_middleBar.autoLayoutDirection = LayoutDirection.Horizontal;


            KlyteMonoUtils.CreateUIElement(out UIPanel previewContainer, m_middleBar.transform, "previewContainer", new UnityEngine.Vector4(0, 0, m_middleBar.width * .6f, m_middleBar.height - m_middleBar.padding.vertical));
            Preview = previewContainer.gameObject.AddComponent<WTSHighwayShieldLayoutEditorPreview>();


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
            KlyteMonoUtils.CreateUIElement(out m_cantEditText, MainContainer.transform, "text", new UnityEngine.Vector4(0, 0, MainContainer.width - MainContainer.padding.horizontal, 315));
            m_cantEditText.text = Locale.Get("K45_WTS_HWTYPEEDITOR_CANTEDITTEXT");
            m_cantEditText.textAlignment = UIHorizontalAlignment.Center;
            m_cantEditText.wordWrap = true;

            KlyteMonoUtils.CreateUIElement(out m_editArea, MainContainer.transform, "editArea", new UnityEngine.Vector4(0, 0, MainContainer.width - MainContainer.padding.horizontal, MainContainer.height - m_middleBar.height - m_topBar.height - MainContainer.padding.vertical - (MainContainer.autoLayoutPadding.vertical * 2) - 5));
            m_editArea.padding = new RectOffset(5, 5, 5, 5);


            KlyteMonoUtils.CreateUIElement(out m_basicInfoEditor, m_editArea.transform, "basicTab", new UnityEngine.Vector4(0, 0, m_editArea.width - m_editArea.padding.horizontal, m_editArea.height - m_editArea.padding.vertical));
            m_basicInfoEditor.gameObject.AddComponent<WTSHighwayShieldLayoutEditorBasics>();
            KlyteMonoUtils.CreateUIElement(out m_textInfoEditor, m_editArea.transform, "textTab", new UnityEngine.Vector4(0, 0, m_editArea.width - m_editArea.padding.horizontal, m_editArea.height - m_editArea.padding.vertical));
            m_textInfoEditor.gameObject.AddComponent<WTSHighwayShieldLayoutEditorTexts>();
            CreateTabTemplate();

            ReloadShield();
            OnTabChange(0);

        }

        private IEnumerator OnFilter(string x, Wrapper<string[]> result)
        {
            yield return WriteTheSignsMod.Controller.ConnectorADR.ListAllAvailableHighwayTypes(x, result);
        }

        private void OnReloadDescriptors()
        {
            WriteTheSignsMod.Controller.HighwayShieldsSingleton.LoadAllShieldsConfigurations();
            ReloadShield();
        }

        private void ExportTo(string output)
        {
            if (EditingInstance != null)
            {
                File.WriteAllText(output, XmlUtils.DefaultXmlSerialize(EditingInstance));
            }
        }

        private void OnExportAsGlobal() => ExportTo(Path.Combine(WTSController.DefaultHwShieldsConfigurationFolder, $"{WTSController.m_defaultFileNameShieldXml}_{CurrentSelection}.xml"));

        private void OnOpenGlobalFolder() => ColossalFramework.Utils.OpenInFileBrowser(WTSController.DefaultHwShieldsConfigurationFolder);
        private void OnDeleteFromCity() => K45DialogControl.ShowModal(new K45DialogControl.BindProperties
        {
            message = Locale.Get("K45_WTS_PROMPTDELETEHWSHIELDLAYOUT"),
            showButton1 = true,
            showButton2 = true,
            textButton1 = Locale.Get("YES"),
            textButton2 = Locale.Get("NO"),
        }, (x) =>
        {
            if (x == 1)
            {
                WTSHighwayShieldsData.Instance.CityDescriptors.Remove(CurrentSelection);
                ReloadShield();
            }

            return true;
        });

        internal void RemoveTabFromItem(int tabToEdit)
        {
            EditingInstance.TextDescriptors = EditingInstance.TextDescriptors.Where((x, y) => y != tabToEdit).ToList();
            OnTabChange(Mathf.Min(CurrentTab, EditingInstance.TextDescriptors.Count));
        }

        private void OnCopyToCity()
        {
            if (EditingInstance != null)
            {
                WTSHighwayShieldsData.Instance.CityDescriptors[CurrentSelection] = XmlUtils.DefaultXmlDeserialize<HighwayShieldDescriptor>(XmlUtils.DefaultXmlSerialize(EditingInstance));
                ReloadShield();
            }

        }
        private void OnCopyToClipboard()
        {
            m_clipboard = XmlUtils.DefaultXmlSerialize(EditingInstance);
            m_btnPaste.isVisible = CurrentConfigurationSource == ConfigurationSource.CITY;
        }

        private void OnPasteFromClipboard()
        {
            var temp = XmlUtils.DefaultXmlDeserialize<HighwayShieldDescriptor>(m_clipboard);
            temp.SaveName = CurrentSelection;
            EditingInstance = temp;
        }

        private void OnCreateNewCity()
        {
            WTSHighwayShieldsData.Instance.CityDescriptors[CurrentSelection] = new HighwayShieldDescriptor
            {
                SaveName = CurrentSelection,
            };
            ReloadShield();
        }

        internal void ReloadShield()
        {

            m_middleBar.isVisible = CurrentSelection != null;
            m_containerSelectionDescription.isVisible = CurrentSelection != null;
            if (CurrentSelection != null)
            {
                WTSHighwayShieldsSingleton.GetTargetDescriptor(CurrentSelection, out ConfigurationSource source, out HighwayShieldDescriptor target);
                m_labelSelectionDescription.text = $"{CurrentSelection}\n";
                m_labelSelectionDescription.suffix = $"{Locale.Get("K45_WTS_CURRENTLY_USING")}: {Locale.Get("K45_WTS_CONFIGURATIONSOURCE", source.ToString())}";
                EditingInstance = target;
                CurrentConfigurationSource = source;
                EventOnHwTypeSelectionChanged?.Invoke(EditingInstance, CurrentConfigurationSource);

                m_btnNew.isVisible = CurrentConfigurationSource != ConfigurationSource.CITY;
                m_btnCopyToCity.isVisible = CurrentConfigurationSource != ConfigurationSource.CITY && CurrentConfigurationSource != ConfigurationSource.NONE;
                m_btnDelete.isVisible = CurrentConfigurationSource == ConfigurationSource.CITY;
                m_btnExport.isVisible = CurrentConfigurationSource == ConfigurationSource.CITY;
                OnTabChange(0);
            }
            m_editArea.isVisible = CurrentSelection != null && CurrentConfigurationSource == ConfigurationSource.CITY;
            m_cantEditText.isVisible = CurrentConfigurationSource == ConfigurationSource.GLOBAL;
            m_plusButton.isVisible = CurrentSelection != null && CurrentConfigurationSource == ConfigurationSource.CITY;
            m_editTabstrip.isVisible = CurrentSelection != null && CurrentConfigurationSource != ConfigurationSource.NONE;
            m_btnCopy.isVisible = CurrentSelection != null && CurrentConfigurationSource != ConfigurationSource.NONE;
            m_btnPaste.isVisible = m_clipboard != null && CurrentConfigurationSource == ConfigurationSource.CITY;
        }

        private string OnHwTypeSelected(string input, int arg1, string[] arg2)
        {
            string result = arg1 < 0 ? input : arg2[arg1];
            if (arg2?.Contains(result) ?? false)
            {
                CurrentSelection = result;
                ReloadShield();
            }
            else
            {
                CurrentSelection = null;
            }
            return CurrentSelection is null ? "" : result;
        }

        private void OnTabChange(int idx)
        {
            CurrentTab = idx;
            m_basicInfoEditor.isVisible = CurrentTab == 0;
            m_textInfoEditor.isVisible = CurrentTab != 0;
            CurrentTabChanged?.Invoke(idx);
            FixTabstrip();
            Preview.ReloadData();

        }

        private void AddTabToItem(UIComponent x, UIMouseEventParameter y)
        {
            var newItem = new ImageLayerTextDescriptorXml
            {
                SaveName = $"New text"
            };
            EditingInstance.TextDescriptors.Add(newItem);
            FixTabstrip();
        }

        private const string TAB_TEMPLATE_NAME = "K45_WTS_TabTemplateHighwayShield";

        private void CreateTabTemplate()
        {
            var go = new GameObject();

            InitTabButton(go, out UIButton button, "AAA", new Vector2(m_editTabstrip.size.x, 30), null);
            UITemplateUtils.GetTemplateDict()[TAB_TEMPLATE_NAME] = button;
        }

        public void FixTabstrip()
        {
            m_tabs.SetItemCount(EditingInstance?.TextDescriptors?.Count ?? 0);
            for (int i = 0; i < (EditingInstance?.TextDescriptors?.Count ?? 0); i++)
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

        internal void SetTabName(int tabToEdit, string text)
        {
            EditingInstance.TextDescriptors[tabToEdit].SaveName = text;
            FixTabstrip();
        }

        public void Update()
        {
            if (MainContainer.isVisible)
            {
                foreach (UIButton btn in m_editTabstrip.GetComponentsInChildren<UIButton>())
                {
                    if (btn != m_plusButton)
                    {
                        btn.state = btn.zOrder == CurrentTab ? UIButton.ButtonState.Focused : UIButton.ButtonState.Normal;
                    }
                }
                foreach (UIButton btn in m_orderedRulesList.GetComponentsInChildren<UIButton>())
                {
                    btn.state = btn.zOrder == CurrentTab - 1 ? UIButton.ButtonState.Focused : UIButton.ButtonState.Normal;
                }
            }
        }
    }

}
