using ColossalFramework.Globalization;
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

            CreateTab<DTPPropPlacingTab2>("InfoIconEscapeRoutes", "K45_DTP_HIGHWAY_SIGN_CONFIG_TAB", "DTPHighwaySign");
            CreateTab<DTPStreetSignTab3>("InfoIconTrafficRoutes", "K45_DTP_STREET_SIGN_CONFIG_TAB", "DTPStreetSign");
            CreateTab<DTPMileageMarkerTab3>("LocationMarkerNormal", "K45_DTP_MILEAGE_MARKERS_CONFIG_TAB", "DTPMileageMarkerTab");
            CreateTab<DTPBuildingEditorTab2>("IconAssetBuilding", "K45_DTP_BUILDING_CONFIG_TAB", "DTPBuildingEditorTab");
        }
      

        private static UIButton CreateTabTemplate()
        {
            KlyteMonoUtils.CreateUIElement(out UIButton tabTemplate, null, "DTPTabTemplate");
            KlyteMonoUtils.InitButton(tabTemplate, false, "GenericTab");
            tabTemplate.autoSize = false;
            tabTemplate.width = 40;
            tabTemplate.height = 40;
            tabTemplate.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            return tabTemplate;
        }

        private void CreateTab<T>(string sprite, string localeKey, string objectName) where T : UICustomControl
        {
            UIButton tab = CreateTabTemplate();
            tab.normalFgSprite = sprite;
            tab.tooltip = Locale.Get(localeKey);

            KlyteMonoUtils.CreateUIElement(out UIPanel contentContainer, null);
            contentContainer.name = "Container";
            contentContainer.area = new Vector4(15, 0, MainPanel.width - 30, MainPanel.height - 70);
            m_stripMain.AddTab(objectName, tab.gameObject, contentContainer.gameObject);


            KlyteMonoUtils.CreateScrollPanel(contentContainer, out UIScrollablePanel scrollablePanel, out _, contentContainer.width - 20, contentContainer.height - 5, new Vector3()).Self.gameObject.AddComponent<T>();
            scrollablePanel.scrollPadding = new RectOffset(10, 10, 10, 10);
        }
        #endregion
    }

}
