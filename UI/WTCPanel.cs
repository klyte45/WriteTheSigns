using ColossalFramework.UI;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using UnityEngine;

namespace Klyte.WriteTheCity.UI
{

    public class WTCPanel : BasicKPanel<WriteTheCityMod, WTCController, WTCPanel>
    {

        private UITabstrip m_stripMain;
        public override float PanelWidth => 875;

        public override float PanelHeight => 850;

        #region Awake
        protected override void AwakeActions()
        {
            KlyteMonoUtils.CreateUIElement(out m_stripMain, MainPanel.transform, "WTCTabstrip", new Vector4(5, 40, MainPanel.width - 10, 40));
            m_stripMain.startSelectedIndex = -1;
            m_stripMain.selectedIndex = -1;

            KlyteMonoUtils.CreateUIElement(out UITabContainer tabContainer, MainPanel.transform, "WTCTabContainer", new Vector4(0, 80, MainPanel.width, MainPanel.height - 80));
            m_stripMain.tabPages = tabContainer;

            //m_stripMain.CreateTabLocalized<WTCPropPlacingTab2>("InfoIconEscapeRoutes", "K45_WTC_HIGHWAY_SIGN_CONFIG_TAB", "WTCHighwaySign");
            //m_stripMain.CreateTabLocalized<WTCStreetSignTab3>("InfoIconTrafficRoutes", "K45_WTC_STREET_SIGN_CONFIG_TAB", "WTCStreetSign");
            //m_stripMain.CreateTabLocalized<WTCMileageMarkerTab3>("LocationMarkerNormal", "K45_WTC_MILEAGE_MARKERS_CONFIG_TAB", "WTCMileageMarkerTab");
            //m_stripMain.CreateTabLocalized<WTCBuildingEditorTab2>("IconAssetBuilding", "K45_WTC_BUILDING_CONFIG_TAB", "WTCBuildingEditorTab");
            m_stripMain.CreateTabLocalized<WTCPropTextLayoutEditor>("IconAssetBuilding2", "K45_WTC_PROP_LIBRARY_EDITOR_TAB", "WTCBuildingEditorTab", false);
        }

        #endregion
    }

}
