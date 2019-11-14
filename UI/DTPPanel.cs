using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.UI.Images;
using Klyte.DynamicTextProps.Utils;
using UnityEngine;

namespace Klyte.DynamicTextProps.UI
{

    public class DTPPanel : UICustomControl
    {
        private UIPanel m_controlContainer;

        public static DTPPanel Instance { get; private set; }
        public UIPanel MainPanel { get; private set; }

        private UIPanel m_mainPanel;

        private UITabstrip m_stripMain;

        #region Awake
        public void Awake()
        {
            Instance = this;

            m_controlContainer = GetComponent<UIPanel>();
            m_controlContainer.area = new Vector4(0, 0, 0, 0);
            m_controlContainer.isVisible = false;
            m_controlContainer.name = "DTPPanel";

            KlyteMonoUtils.CreateUIElement(out m_mainPanel, m_controlContainer.transform, "DTPListPanel", new Vector4(0, 0, 875, 850));
            m_mainPanel.backgroundSprite = "MenuPanel2";

            CreateTitleBar();


            KlyteMonoUtils.CreateUIElement(out m_stripMain, m_mainPanel.transform, "DTPTabstrip", new Vector4(5, 40, m_mainPanel.width - 10, 40));
            m_stripMain.startSelectedIndex = -1;
            m_stripMain.selectedIndex = -1;

            KlyteMonoUtils.CreateUIElement(out UITabContainer tabContainer, m_mainPanel.transform, "DTPTabContainer", new Vector4(0, 80, m_mainPanel.width, m_mainPanel.height - 80));
            m_stripMain.tabPages = tabContainer;

            CreateTab<DTPPropPlacingTab>("InfoIconEscapeRoutes", "K45_DTP_HIGHWAY_SIGN_CONFIG_TAB", "DTPHighwaySign");
            CreateTab<DTPStreetSignTab>("InfoIconTrafficRoutes", "K45_DTP_STREET_SIGN_CONFIG_TAB", "DTPStreetSign");
            CreateTab<DTPMileageMarkerTab>("LocationMarkerNormal", "K45_DTP_MILEAGE_MARKERS_CONFIG_TAB", "DTPMileageMarkerTab");
            CreateTab<DTPBuildingEditorTab2>("IconAssetBuilding", "K45_DTP_BUILDING_CONFIG_TAB", "DTPBuildingEditorTab");
        }

        private void CreateTitleBar()
        {
            KlyteMonoUtils.CreateUIElement(out UILabel titlebar, m_mainPanel.transform, "DTPListPanel", new Vector4(75, 10, m_mainPanel.width - 150, 20));
            titlebar.autoSize = false;
            titlebar.text = DynamicTextPropsMod.Instance.GeneralName;
            titlebar.textAlignment = UIHorizontalAlignment.Center;
            //KlyteMonoUtils.CreateDragHandle(titlebar, KlyteModsPanel.instance.mainPanel);

            KlyteMonoUtils.CreateUIElement(out UIButton closeButton, m_mainPanel.transform, "CloseButton", new Vector4(m_mainPanel.width - 37, 5, 32, 32));
            KlyteMonoUtils.InitButton(closeButton, false, "buttonclose", true);
            closeButton.hoveredBgSprite = "buttonclosehover";
            closeButton.eventClick += (x, y) => DynamicTextPropsMod.Instance.ClosePanel();

            KlyteMonoUtils.CreateUIElement(out UISprite logo, m_mainPanel.transform, "DTPLogo", new Vector4(22, 5f, 32, 32));
            logo.spriteName = DTPResourceLoader.instance.GetDefaultSpriteNameFor(CommonSpriteNames.KDTPIcon);
            //KlyteMonoUtils.CreateDragHandle(logo, KlyteModsPanel.instance.mainPanel);
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
            contentContainer.area = new Vector4(15, 0, m_mainPanel.width - 30, m_mainPanel.height - 70);
            m_stripMain.AddTab(objectName, tab.gameObject, contentContainer.gameObject);


            KlyteMonoUtils.CreateScrollPanel(contentContainer, out UIScrollablePanel scrollablePanel, out _, contentContainer.width - 20, contentContainer.height - 5, new Vector3()).Self.gameObject.AddComponent<T>();
            scrollablePanel.scrollPadding = new RectOffset(10, 10, 10, 10);
        }
        #endregion
    }

}
