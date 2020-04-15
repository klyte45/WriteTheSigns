using ColossalFramework.UI;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using UnityEngine;

namespace Klyte.DynamicTextProps.UI
{

    public class DTPPanel : BasicKPanel<DynamicTextPropsMod, DTPController, DTPPanel>
    {

        private UITabstrip m_stripMain;
        public override float PanelWidth => 875;

        public override float PanelHeight => 850;

        #region Awake
        protected override void AwakeActions()
        {
            KlyteMonoUtils.CreateUIElement(out m_stripMain, MainPanel.transform, "DTPTabstrip", new Vector4(5, 40, MainPanel.width - 10, 40));
            m_stripMain.startSelectedIndex = -1;
            m_stripMain.selectedIndex = -1;

            KlyteMonoUtils.CreateUIElement(out UITabContainer tabContainer, MainPanel.transform, "DTPTabContainer", new Vector4(0, 80, MainPanel.width, MainPanel.height - 80));
            m_stripMain.tabPages = tabContainer;

            //m_stripMain.CreateTabLocalized<DTPPropPlacingTab2>("InfoIconEscapeRoutes", "K45_DTP_HIGHWAY_SIGN_CONFIG_TAB", "DTPHighwaySign");
            //m_stripMain.CreateTabLocalized<DTPStreetSignTab3>("InfoIconTrafficRoutes", "K45_DTP_STREET_SIGN_CONFIG_TAB", "DTPStreetSign");
            //m_stripMain.CreateTabLocalized<DTPMileageMarkerTab3>("LocationMarkerNormal", "K45_DTP_MILEAGE_MARKERS_CONFIG_TAB", "DTPMileageMarkerTab");
            //m_stripMain.CreateTabLocalized<DTPBuildingEditorTab2>("IconAssetBuilding", "K45_DTP_BUILDING_CONFIG_TAB", "DTPBuildingEditorTab");
            m_stripMain.CreateTabLocalized<DTPPropTextLayoutEditor>("IconAssetBuilding2", "K45_DTP_BUILDING_CONFIG_TAB2", "DTPBuildingEditorTab", false);
        }

        #endregion
    }

}
