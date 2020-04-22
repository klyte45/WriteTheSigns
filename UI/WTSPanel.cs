﻿using ColossalFramework.UI;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using UnityEngine;

namespace Klyte.WriteTheSigns.UI
{

    public class WTSPanel : BasicKPanel<WriteTheSignsMod, WTSController, WTSPanel>
    {

        private UITabstrip m_stripMain;
        public override float PanelWidth => 875;

        public override float PanelHeight => 850;

        #region Awake
        protected override void AwakeActions()
        {
            KlyteMonoUtils.CreateUIElement(out m_stripMain, MainPanel.transform, "WTSTabstrip", new Vector4(5, 40, MainPanel.width - 10, 40));
            m_stripMain.startSelectedIndex = -1;
            m_stripMain.selectedIndex = -1;

            KlyteMonoUtils.CreateUIElement(out UITabContainer tabContainer, MainPanel.transform, "WTSTabContainer", new Vector4(0, 80, MainPanel.width, MainPanel.height - 80));
            m_stripMain.tabPages = tabContainer;

            //m_stripMain.CreateTabLocalized<WTSPropPlacingTab2>("InfoIconEscapeRoutes", "K45_WTS_HIGHWAY_SIGN_CONFIG_TAB", "WTSHighwaySign");
            //m_stripMain.CreateTabLocalized<WTSMileageMarkerTab3>("LocationMarkerNormal", "K45_WTS_MILEAGE_MARKERS_CONFIG_TAB", "WTSMileageMarkerTab");
            //m_stripMain.CreateTabLocalized<WTSBuildingEditorTab2>("IconAssetBuilding", "K45_WTS_BUILDING_CONFIG_TAB", "WTSBuildingEditorTab");
            m_stripMain.CreateTabLocalized<WTSPropLayoutEditor>("IconAssetProp", "K45_WTS_PROP_LIBRARY_EDITOR_TAB", "WTSBuildingEditorTab", false);
            m_stripMain.CreateTabLocalized<WTSRoadCornerEditor>("InfoIconTrafficRoutes", "K45_WTS_STREET_SIGN_CONFIG_TAB", "WTSStreetSign",false);
        }

        #endregion
    }

}