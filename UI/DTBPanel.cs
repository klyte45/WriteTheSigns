using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using Klyte.DynamicTextBoards.TextureAtlas;
using UnityEngine;

namespace Klyte.DynamicTextBoards.UI
{

    public class DTBPanel : UICustomControl
    {
        private UIPanel m_controlContainer;

        public static DTBPanel Instance { get; private set; }
        public UIPanel MainPanel { get; private set; }



        public void OnOpenClosePanel(UIComponent component, bool value)
        {
            if (value)
            {
                DynamicTextBoardsMod.Instance.ShowVersionInfoPopup();
            }
        }

        private UIPanel m_mainPanel;

        private UITabstrip m_stripMain;

        #region Awake
        public void Awake()
        {
            Instance = this;

            m_controlContainer = GetComponent<UIPanel>();
            m_controlContainer.area = new Vector4(0, 0, 0, 0);
            m_controlContainer.isVisible = false;
            m_controlContainer.name = "DTBPanel";

            KlyteMonoUtils.CreateUIElement(out m_mainPanel, m_controlContainer.transform, "DTBListPanel", new Vector4(0, 0, 875, 850));
            m_mainPanel.backgroundSprite = "MenuPanel2";

            CreateTitleBar();


            KlyteMonoUtils.CreateUIElement(out m_stripMain, m_mainPanel.transform, "DTBTabstrip", new Vector4(5, 40, m_mainPanel.width - 10, 40));
            m_stripMain.startSelectedIndex = -1;
            m_stripMain.selectedIndex = -1;

            KlyteMonoUtils.CreateUIElement(out UITabContainer tabContainer, m_mainPanel.transform, "DTBTabContainer", new Vector4(0, 80, m_mainPanel.width, m_mainPanel.height - 80));
            m_stripMain.tabPages = tabContainer;


            CreateTab<DTBFontConfigTab>("ToolbarIconZoomOutGlobe", "K45_DTB_FONT_CONFIG_TAB", "DTBFonts");
            CreateTab<DTBPropPlacingTab>("ToolbarIconZoomOutGlobe", "K45_DTB_HIGHWAY_SIGN_CONFIG_TAB", "DTBHighwaySign");
            CreateTab<DTBStreetSignTab>("ToolbarIconZoomOutGlobe", "K45_DTB_STREET_SIGN_CONFIG_TAB", "DTBStreetSign");
        }

        private void CreateTitleBar()
        {
            KlyteMonoUtils.CreateUIElement(out UILabel titlebar, m_mainPanel.transform, "DTBListPanel", new Vector4(75, 10, m_mainPanel.width - 150, 20));
            titlebar.autoSize = false;
            titlebar.text = DynamicTextBoardsMod.Instance.GeneralName;
            titlebar.textAlignment = UIHorizontalAlignment.Center;
            //KlyteMonoUtils.CreateDragHandle(titlebar, KlyteModsPanel.instance.mainPanel);

            KlyteMonoUtils.CreateUIElement(out UIButton closeButton, m_mainPanel.transform, "CloseButton", new Vector4(m_mainPanel.width - 37, 5, 32, 32));
            KlyteMonoUtils.InitButton(closeButton, false, "buttonclose", true);
            closeButton.hoveredBgSprite = "buttonclosehover";
            closeButton.eventClick += (x, y) =>
            {
                //KlyteCommonsMod.CloseKCPanel();
            };

            KlyteMonoUtils.CreateUIElement(out UISprite logo, m_mainPanel.transform, "DTBLogo", new Vector4(22, 5f, 32, 32));
            logo.atlas = DTBCommonTextureAtlas.instance.Atlas;
            logo.spriteName = "ServiceVehiclesManagerIcon";
            //KlyteMonoUtils.CreateDragHandle(logo, KlyteModsPanel.instance.mainPanel);
        }

        private static UIButton CreateTabTemplate()
        {
            KlyteMonoUtils.CreateUIElement(out UIButton tabTemplate, null, "DTBTabTemplate");
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
