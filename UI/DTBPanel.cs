using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI;
using Klyte.Commons.Utils;
using Klyte.DynamicTextBoards.TextureAtlas;
using Klyte.DynamicTextBoards.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.DynamicTextBoards.UI
{

    public class DTBPanel : UICustomControl
    {
        private static DTBPanel m_instance;
        private UIPanel controlContainer;

        public static DTBPanel instance => m_instance;
        public UIPanel m_mainPanel { get; private set; }



        private void OnOpenClosePanel(UIComponent component, bool value)
        {
            if (value)
            {
                DynamicTextBoardsMod.Instance.ShowVersionInfoPopup();
            }
        }

        private UIPanel mainPanel;

        private UITabstrip m_StripMain;

        #region Awake
        private void Awake()
        {
            m_instance = this;

            controlContainer = GetComponent<UIPanel>();
            controlContainer.area = new Vector4(0, 0, 0, 0);
            controlContainer.isVisible = false;
            controlContainer.name = "DTBPanel";

            KlyteMonoUtils.CreateUIElement(out mainPanel, controlContainer.transform, "DTBListPanel", new Vector4(0, 0, 875, 850));
            mainPanel.backgroundSprite = "MenuPanel2";

            CreateTitleBar();


            KlyteMonoUtils.CreateUIElement(out m_StripMain, mainPanel.transform, "DTBTabstrip", new Vector4(5, 40, mainPanel.width - 10, 40));
            m_StripMain.startSelectedIndex = -1;
            m_StripMain.selectedIndex = -1;

            KlyteMonoUtils.CreateUIElement(out UITabContainer tabContainer, mainPanel.transform, "DTBTabContainer", new Vector4(0, 80, mainPanel.width, mainPanel.height - 80));
            m_StripMain.tabPages = tabContainer;


            CreateTab<DTBFontConfigTab>("ToolbarIconZoomOutGlobe", "K45_DTB_FONT_CONFIG_TAB", "DTBFonts");
            CreateTab<DTBHighwaySignTab>("ToolbarIconZoomOutGlobe", "K45_DTB_HIGHWAY_SIGN_CONFIG_TAB", "DTBHighwaySign");
        }

        private void CreateTitleBar()
        {
            KlyteMonoUtils.CreateUIElement(out UILabel titlebar, mainPanel.transform, "DTBListPanel", new Vector4(75, 10, mainPanel.width - 150, 20));
            titlebar.autoSize = false;
            titlebar.text = DynamicTextBoardsMod.Instance.GeneralName;
            titlebar.textAlignment = UIHorizontalAlignment.Center;
            //KlyteMonoUtils.CreateDragHandle(titlebar, KlyteModsPanel.instance.mainPanel);

            KlyteMonoUtils.CreateUIElement(out UIButton closeButton, mainPanel.transform, "CloseButton", new Vector4(mainPanel.width - 37, 5, 32, 32));
            KlyteMonoUtils.InitButton(closeButton, false, "buttonclose", true);
            closeButton.hoveredBgSprite = "buttonclosehover";
            closeButton.eventClick += (x, y) =>
            {
                //KlyteCommonsMod.CloseKCPanel();
            };

            KlyteMonoUtils.CreateUIElement(out UISprite logo, mainPanel.transform, "DTBLogo", new Vector4(22, 5f, 32, 32));
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
            contentContainer.area = new Vector4(15, 0, mainPanel.width - 30, mainPanel.height - 70);
            m_StripMain.AddTab(objectName, tab.gameObject, contentContainer.gameObject);

            KlyteMonoUtils.CreateScrollPanel(contentContainer, out UIScrollablePanel scrollablePanel, out UIScrollbar scrollbar, contentContainer.width - 20, contentContainer.height - 5, new Vector3()).Self.gameObject.AddComponent<T>();
            scrollablePanel.scrollPadding = new RectOffset(10, 10, 10, 10);
        }
        #endregion
    }

}
