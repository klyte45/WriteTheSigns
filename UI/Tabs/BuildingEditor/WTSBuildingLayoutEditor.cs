using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Singleton;
using Klyte.WriteTheSigns.Xml;
using System;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.WriteTheSigns.UI
{
    internal enum ConfigurationSource
    {
        NONE,
        ASSET,
        GLOBAL,
        CITY
    }

    internal class WTSBuildingLayoutEditor : UICustomControl
    {
        private static WTSBuildingLayoutEditor m_instance;
        public static WTSBuildingLayoutEditor Instance
        {
            get {
                if (m_instance == null)
                {
                    m_instance = FindObjectOfType<WTSBuildingLayoutEditor>();
                }
                return m_instance;
            }
        }

        private string m_currentBuildingName;
        public string CurrentBuildingName
        {
            get => m_currentBuildingName; private set {
                m_currentBuildingName = value;
                ReloadBuilding();
            }
        }

        private BuildingGroupDescriptorXml CurrentEditingInstance { get; set; }
        private ConfigurationSource CurrentConfigurationSource { get; set; }



        internal event Action<BuildingGroupDescriptorXml, ConfigurationSource> EventOnBuildingSelectionChanged;




        public UIPanel MainContainer { get; protected set; }

        private UIButton m_buttonTool;
        private UIPanel m_secondaryContainer;
        private UILabel m_labelSelectionDescription;
        private UIPanel m_containerSelectionDescription;
        private UIButton m_btnNew;
        private UIButton m_btnCopy;
        private UIButton m_btnDelete;
        private UIButton m_btnLoad;
        private UIButton m_btnExport;
        private UIButton m_btnSteam;
        private UIButton m_btnReload;

        public WTSBuildingLayoutEditorPropList LayoutList { get; private set; }

        public void Awake()
        {

            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.autoLayoutPadding = new RectOffset(5, 5, 5, 5);

            var m_uiHelperHS = new UIHelperExtension(MainContainer);

            m_buttonTool = (UIButton)m_uiHelperHS.AddButton(Locale.Get("K45_WTS_PICK_A_BUILDING"), EnablePickTool);
            KlyteMonoUtils.LimitWidth(m_buttonTool, (m_uiHelperHS.Self.width - 20), true);


            AddLabel("", m_uiHelperHS, out m_labelSelectionDescription, out m_containerSelectionDescription);
            KlyteMonoUtils.LimitWidthAndBox(m_labelSelectionDescription, (m_uiHelperHS.Self.width / 2), true);
            m_labelSelectionDescription.prefix = Locale.Get("K45_WTS_CURRENTSELECTION") + ": ";
            m_btnNew = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_New, OnCreateNewCity, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_NEWINCITY", false);
            m_btnCopy = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Copy, OnCopyToCity, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_COPYTOCITY", false);
            m_btnDelete = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Delete, OnDeleteFromCity, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_DELETEFROMCITY", false);
            m_btnLoad = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Load, OnOpenGlobalFolder, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_OPENGLOBALSFOLDER", false);
            m_btnExport = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Export, OnExportAsGlobal, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_EXPORTASGLOBAL", false);
            m_btnSteam = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Steam, OnExportAsAsset, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_EXPORTTOASSETFOLDER", false);
            m_btnReload = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Reload, OnReloadDescriptors, "K45_WTS_BUILDINGEDITOR_BUTTONROWACTION_RELOADDESCRIPTORS", false);

            KlyteMonoUtils.CreateUIElement(out m_secondaryContainer, MainContainer.transform, "SecContainer", new Vector4(0, 0, MainContainer.width, 655));
            m_secondaryContainer.autoLayout = true;
            m_secondaryContainer.autoLayoutDirection = LayoutDirection.Horizontal;
            m_secondaryContainer.autoLayoutPadding = new RectOffset(0, 10, 0, 0);

            KlyteMonoUtils.CreateUIElement(out UIPanel tertiaryContainer, m_secondaryContainer.transform, "TrcContainer", new Vector4(0, 0, m_secondaryContainer.width * 0.25f, m_secondaryContainer.height));
            LayoutList = tertiaryContainer.gameObject.AddComponent<WTSBuildingLayoutEditorPropList>();

            KlyteMonoUtils.CreateUIElement(out UIPanel editorPanel, m_secondaryContainer.transform, "EditPanel", new Vector4(0, 0, m_secondaryContainer.width * 0.75f - 35, m_secondaryContainer.height));
            editorPanel.gameObject.AddComponent<WTSBuildingLayoutEditorPropDetail>();

            OnBuildingSet(null);
        }

        internal float GetCurrentMappingThresold() => CurrentEditingInstance?.StopMappingThresold ?? 1f;
        internal bool IsEditing(string refName) => CurrentBuildingName == refName;

        private void OnReloadDescriptors() { }
        private void OnExportAsAsset() { }
        private void OnExportAsGlobal() { }
        private void OnOpenGlobalFolder() { }
        private void OnDeleteFromCity() { }
        private void OnCopyToCity() { }
        private void OnCreateNewCity()
        {
            WTSBuildingsData.Instance.CityDescriptors[m_currentBuildingName] = new BuildingGroupDescriptorXml
            {
                BuildingName = m_currentBuildingName,
            };
            ReloadBuilding();
        }

        private void EnablePickTool()
        {
            OnBuildingSet(null);
            WriteTheSignsMod.Controller.BuildingEditorToolInstance.OnBuildingSelect += OnBuildingSet;
            WriteTheSignsMod.Controller.BuildingEditorToolInstance.enabled = true;
        }

        private void OnBuildingSet(ushort id) => OnBuildingSet(WTSBuildingPropsSingleton.GetReferenceModelName(ref BuildingManager.instance.m_buildings.m_buffer[id]));
        private void OnBuildingSet(string buildingId)
        {
            CurrentBuildingName = buildingId;
            ReloadBuilding();
        }


        private void ReloadBuilding()
        {
            m_secondaryContainer.isVisible = m_currentBuildingName != null;
            m_containerSelectionDescription.isVisible = m_currentBuildingName != null;
            WTSBuildingPropsSingleton.GetTargetDescriptor(m_currentBuildingName, out ConfigurationSource source, out BuildingGroupDescriptorXml target);
            m_labelSelectionDescription.text = Locale.Get("BUILDING_TITLE", m_currentBuildingName) + "\n";
            m_labelSelectionDescription.suffix = $"{Locale.Get("K45_WTS_CURRENTLY_USING")}: {Locale.Get("K45_WTS_CONFIGURATIONSOURCE", source.ToString())}";
            CurrentEditingInstance = target;
            CurrentConfigurationSource = source;
            EventOnBuildingSelectionChanged?.Invoke(CurrentEditingInstance, CurrentConfigurationSource);

            m_btnNew.isVisible = CurrentConfigurationSource != ConfigurationSource.CITY;
        }

    }

}
