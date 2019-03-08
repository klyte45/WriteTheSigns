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
                DynamicTextBoardsMod.instance.showVersionInfoPopup();
            }
        }

        private UIPanel mainPanel;
        private UIPanel m_titleLineBuildings;

        private UILabel m_directionLabel;

        private UITabstrip m_StripMain;
        private UITabstrip m_StripDistricts;
        private UITabstrip m_StripBuilings;

        private Dictionary<CategoryTab, UITabstrip> m_StripDistrictsStrips = new Dictionary<CategoryTab, UITabstrip>();
        private Dictionary<CategoryTab, UITabstrip> m_StripBuilingsStrips = new Dictionary<CategoryTab, UITabstrip>();
        
        private Dictionary<string, int> m_cachedDistricts;
        private string m_lastSelectedItem;

        #region Awake
        private void Awake()
        {
            m_instance = this;

            controlContainer = GetComponent<UIPanel>();
            controlContainer.area = new Vector4(0, 0, 0, 0);
            controlContainer.isVisible = false;
            controlContainer.name = "DTBPanel";

            DTBUtils.createUIElement(out mainPanel, controlContainer.transform, "DTBListPanel", new Vector4(0, 0, 875, 550));
            mainPanel.backgroundSprite = "MenuPanel2";

            CreateTitleBar();


            //DTBUtils.createUIElement(out m_StripMain, mainPanel.transform, "DTBTabstrip", new Vector4(5, 40, mainPanel.width - 10, 40));

            //DTBUtils.createUIElement(out UITabContainer tabContainer, mainPanel.transform, "DTBTabContainer", new Vector4(0, 80, mainPanel.width, mainPanel.height - 80));
            //m_StripMain.tabPages = tabContainer;

            //UIButton tabPerBuilding = CreateTabTemplate();
            //tabPerBuilding.normalFgSprite = "ToolbarIconMonuments";
            //tabPerBuilding.tooltip = Locale.Get("DTB_CONFIG_PER_BUILDING_TAB");

            //DTBUtils.createUIElement(out UIPanel contentContainerPerBuilding, null);
            //contentContainerPerBuilding.name = "Container";
            //contentContainerPerBuilding.area = new Vector4(0, 40, mainPanel.width, mainPanel.height - 80);

            //m_StripMain.AddTab("DTBPerBuilding", tabPerBuilding.gameObject, contentContainerPerBuilding.gameObject);
            //CreateTitleRowBuilding(ref m_titleLineBuildings, contentContainerPerBuilding);
            //CreateSsdTabstrip(ref m_StripBuilings, ref m_StripBuilingsStrips, m_titleLineBuildings, contentContainerPerBuilding, true);

            //UIButton tabPerDistrict = CreateTabTemplate();
            //tabPerDistrict.normalFgSprite = "ToolbarIconDistrict";
            //tabPerDistrict.tooltip = Locale.Get("DTB_CONFIG_PER_DISTRICT_TAB");

            //DTBUtils.createUIElement(out UIPanel contentContainerPerDistrict, mainPanel.transform);
            //contentContainerPerDistrict.name = "Container2";
            //contentContainerPerDistrict.area = new Vector4(0, 40, mainPanel.width, mainPanel.height - 80);

            //m_StripMain.AddTab("DTBPerDistrict", tabPerDistrict.gameObject, contentContainerPerDistrict.gameObject);
            //CreateSsdTabstrip(ref m_StripDistricts, ref m_StripDistrictsStrips, null, contentContainerPerDistrict);

            //m_cachedDistricts = DTBUtils.getValidDistricts();



            //m_StripMain.selectedIndex = -1;
            //m_StripBuilings.selectedIndex = -1;
            //m_StripDistricts.selectedIndex = -1;

            //foreach (var strip in m_StripDistrictsStrips.Values)
            //{
            //    strip.selectedIndex = -1;
            //}
            //foreach (var strip in m_StripBuilingsStrips.Values)
            //{
            //    strip.selectedIndex = -1;
            //}

        }


        
        public int getCurrentSelectedDistrictId()
        {
            if (m_lastSelectedItem == null || !m_cachedDistricts.ContainsKey(m_lastSelectedItem))
            {
                return -1;
            }
            return m_cachedDistricts[m_lastSelectedItem];
        }

        private void CreateSsdTabstrip(ref UITabstrip strip, ref Dictionary<CategoryTab, UITabstrip> substrips, UIPanel titleLine, UIComponent parent, bool buildings = false)
        {
            //DTBUtils.createUIElement(out strip, parent.transform, "DTBTabstrip", new Vector4(5, 0, parent.width - 10, 40));

            //var effectiveOffsetY = strip.height + (titleLine?.height ?? 0);

            //DTBUtils.createUIElement(out UITabContainer tabContainer, parent.transform, "DTBTabContainer", new Vector4(0, 40, parent.width, parent.height - 40));
            //strip.tabPages = tabContainer;

            //UIButton tabTemplate = CreateTabTemplate();

            //UIComponent bodyContent = CreateContentTemplate(parent.width - 10, parent.height - effectiveOffsetY - 50);
            //DTBUtils.createUIElement(out UIPanel bodySuper, null);
            //bodySuper.name = "Container";
            //bodySuper.area = new Vector4(0, 40, parent.width, parent.height - 50);

            //Dictionary<CategoryTab, UIComponent> tabsCategories = new Dictionary<CategoryTab, UIComponent>();

            //foreach (var catTab in Enum.GetValues(typeof(CategoryTab)).Cast<CategoryTab>())
            //{
            //    GameObject tabCategory = Instantiate(tabTemplate.gameObject);
            //    GameObject contentCategory = Instantiate(bodySuper.gameObject);
            //    UIButton tabButtonSuper = tabCategory.GetComponent<UIButton>();
            //    tabButtonSuper.tooltip = catTab.getCategoryName();
            //    tabButtonSuper.normalFgSprite = catTab.getCategoryIcon();
            //    tabsCategories[catTab] = strip.AddTab(catTab.ToString(), tabCategory, contentCategory);
            //    tabsCategories[catTab].isVisible = false;
            //    DTBUtils.createUIElement(out UITabstrip subStrip, contentCategory.transform, "DTBTabstripCat" + catTab, new Vector4(5, 0, bodySuper.width - 10, 40));
            //    DTBUtils.createUIElement(out UITabContainer tabSubContainer, contentCategory.transform, "DTBTabContainer" + catTab, new Vector4(5, effectiveOffsetY, bodySuper.width - 10, bodySuper.height - effectiveOffsetY));
            //    subStrip.tabPages = tabSubContainer;
            //    substrips[catTab] = subStrip;
            //}
            //foreach (var kv in ServiceSystemDefinition.sysDefinitions)
            //{
            //    GameObject tab = Instantiate(tabTemplate.gameObject);
            //    GameObject body = Instantiate(bodyContent.gameObject);
            //    var configIdx = kv.Key.toConfigIndex();
            //    String name = kv.Value.Name;
            //    DTBUtils.doLog($"configIdx = {configIdx};kv.Key = {kv.Key}; kv.Value= {kv.Value} ");
            //    String bgIcon = DTBConfigWarehouse.getIconServiceSystem(configIdx);
            //    String fgIcon = DTBConfigWarehouse.getFgIconServiceSystem(configIdx);
            //    UIButton tabButton = tab.GetComponent<UIButton>();
            //    tabButton.tooltip = DTBConfigWarehouse.getNameForServiceSystem(configIdx);
            //    tabButton.normalFgSprite = bgIcon;
            //    if (!string.IsNullOrEmpty(fgIcon))
            //    {
            //        DTBUtils.createUIElement(out UISprite sprite, tabButton.transform, "OverSprite", new Vector4(0, 0, 40, 40));
            //        sprite.spriteName = fgIcon;
            //        sprite.atlas = DTBCommonTextureAtlas.instance.atlas;
            //    }
            //    Type[] components;
            //    Type targetType;
            //    if (buildings)
            //    {
            //        targetType = KlyteUtils.GetImplementationForGenericType(typeof(DTBTabControllerBuildingList<>), kv.Value);
            //        components = new Type[] { targetType };
            //    }
            //    else
            //    {
            //        try
            //        {
            //            targetType = KlyteUtils.GetImplementationForGenericType(typeof(DTBTabControllerDistrictList<>), kv.Value);
            //            components = new Type[] { targetType };
            //        }
            //        catch
            //        {
            //            continue;
            //        }

            //    }
            //    CategoryTab catTab = DTBConfigWarehouse.getCategory(configIdx);
            //    substrips[catTab].AddTab(name, tab, body, components);

            //    body.GetComponent<UIComponent>().eventVisibilityChanged += (x, y) =>
            //    {
            //        if (y)
            //        {
            //            m_directionLabel.isVisible = kv.Key.outsideConnection;
            //        }
            //    };
            //    tabsCategories[catTab].isVisible = true;
            //}
        }



        private static UIButton CreateTabTemplate()
        {
            DTBUtils.createUIElement(out UIButton tabTemplate, null, "DTBTabTemplate");
            DTBUtils.initButton(tabTemplate, false, "GenericTab");
            tabTemplate.autoSize = false;
            tabTemplate.width = 40;
            tabTemplate.height = 40;
            tabTemplate.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            return tabTemplate;
        }

        private void CreateTitleRowBuilding(ref UIPanel titleLine, UIComponent parent)
        {
            DTBUtils.createUIElement(out titleLine, parent.transform, "DTBtitleline", new Vector4(5, 80, parent.width - 10, 40));

            DTBUtils.createUIElement(out UILabel districtNameLabel, titleLine.transform, "districtNameLabel");
            districtNameLabel.autoSize = false;
            districtNameLabel.area = new Vector4(0, 10, 175, 18);
            districtNameLabel.textAlignment = UIHorizontalAlignment.Center;
            districtNameLabel.text = Locale.Get("TUTORIAL_ADVISER_TITLE", "District");

            DTBUtils.createUIElement(out UILabel buildingNameLabel, titleLine.transform, "buildingNameLabel");
            buildingNameLabel.autoSize = false;
            buildingNameLabel.area = new Vector4(200, 10, 198, 18);
            buildingNameLabel.textAlignment = UIHorizontalAlignment.Center;
            buildingNameLabel.text = Locale.Get("DTB_BUILDING_NAME_LABEL");

            DTBUtils.createUIElement(out UILabel vehicleCapacityLabel, titleLine.transform, "vehicleCapacityLabel");
            vehicleCapacityLabel.autoSize = false;
            vehicleCapacityLabel.area = new Vector4(400, 10, 200, 18);
            vehicleCapacityLabel.textAlignment = UIHorizontalAlignment.Center;
            vehicleCapacityLabel.text = Locale.Get("DTB_VEHICLE_CAPACITY_LABEL");

            DTBUtils.createUIElement(out m_directionLabel, titleLine.transform, "directionLabel");
            m_directionLabel.autoSize = false;
            m_directionLabel.area = new Vector4(600, 10, 200, 18);
            m_directionLabel.textAlignment = UIHorizontalAlignment.Center;
            m_directionLabel.text = Locale.Get("DTB_DIRECTION_LABEL");

        }

        private void CreateTitleBar()
        {
            DTBUtils.createUIElement(out UILabel titlebar, mainPanel.transform, "DTBListPanel", new Vector4(75, 10, mainPanel.width - 150, 20));
            titlebar.autoSize = false;
            titlebar.text = DynamicTextBoardsMod.instance.GeneralName;
            titlebar.textAlignment = UIHorizontalAlignment.Center;
            DTBUtils.createDragHandle(titlebar, KlyteModsPanel.instance.mainPanel);

            DTBUtils.createUIElement(out UIButton closeButton, mainPanel.transform, "CloseButton", new Vector4(mainPanel.width - 37, 5, 32, 32));
            DTBUtils.initButton(closeButton, false, "buttonclose", true);
            closeButton.hoveredBgSprite = "buttonclosehover";
            closeButton.eventClick += (x, y) =>
            {
                KlyteCommonsMod.CloseKCPanel();
            };

            DTBUtils.createUIElement(out UISprite logo, mainPanel.transform, "DTBLogo", new Vector4(22, 5f, 32, 32));
            logo.atlas = DTBCommonTextureAtlas.instance.atlas;
            logo.spriteName = "ServiceVehiclesManagerIcon";
            DTBUtils.createDragHandle(logo, KlyteModsPanel.instance.mainPanel);
        }

        private static UIComponent CreateContentTemplate(float width, float height)
        {
            DTBUtils.createUIElement(out UIPanel contentContainer, null);
            contentContainer.name = "Container";
            contentContainer.area = new Vector4(0, 0, width, height);
            DTBUtils.createUIElement(out UIScrollablePanel scrollPanel, contentContainer.transform, "ScrollPanel");
            scrollPanel.width = contentContainer.width - 20f;
            scrollPanel.height = contentContainer.height;
            scrollPanel.autoLayoutDirection = LayoutDirection.Vertical;
            scrollPanel.autoLayoutStart = LayoutStart.TopLeft;
            scrollPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            scrollPanel.autoLayout = true;
            scrollPanel.clipChildren = true;
            scrollPanel.relativePosition = new Vector3(5, 0);

            DTBUtils.createUIElement(out UIPanel trackballPanel, contentContainer.transform, "Trackball");
            trackballPanel.width = 10f;
            trackballPanel.height = scrollPanel.height;
            trackballPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            trackballPanel.autoLayoutStart = LayoutStart.TopLeft;
            trackballPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            trackballPanel.autoLayout = true;
            trackballPanel.relativePosition = new Vector3(contentContainer.width - 15, 0);

            DTBUtils.createUIElement(out UIScrollbar scrollBar, trackballPanel.transform, "Scrollbar");
            scrollBar.width = 10f;
            scrollBar.height = scrollBar.parent.height;
            scrollBar.orientation = UIOrientation.Vertical;
            scrollBar.pivot = UIPivotPoint.BottomLeft;
            scrollBar.AlignTo(trackballPanel, UIAlignAnchor.TopRight);
            scrollBar.minValue = 0f;
            scrollBar.value = 0f;
            scrollBar.incrementAmount = 25f;

            DTBUtils.createUIElement(out UISlicedSprite scrollBg, scrollBar.transform, "ScrollbarBg");
            scrollBg.relativePosition = Vector2.zero;
            scrollBg.autoSize = true;
            scrollBg.size = scrollBg.parent.size;
            scrollBg.fillDirection = UIFillDirection.Vertical;
            scrollBg.spriteName = "ScrollbarTrack";
            scrollBar.trackObject = scrollBg;

            DTBUtils.createUIElement(out UISlicedSprite scrollFg, scrollBg.transform, "ScrollbarFg");
            scrollFg.relativePosition = Vector2.zero;
            scrollFg.fillDirection = UIFillDirection.Vertical;
            scrollFg.autoSize = true;
            scrollFg.width = scrollFg.parent.width - 4f;
            scrollFg.spriteName = "ScrollbarThumb";
            scrollBar.thumbObject = scrollFg;
            scrollPanel.verticalScrollbar = scrollBar;
            scrollPanel.eventMouseWheel += delegate (UIComponent component, UIMouseEventParameter param)
            {
                scrollPanel.scrollPosition += new Vector2(0f, Mathf.Sign(param.wheelDelta) * -1f * scrollBar.incrementAmount);
                param.Use();
            };
            return contentContainer;
        }
        #endregion


        public void SetActiveTab(int idx)
        {
            this.m_StripMain.selectedIndex = idx;
        }

        private void Update()
        {
        }
    }

    public enum CategoryTab
    {
        OutsideConnection,
        PublicTransport,
        EmergencyVehicles,
        SecurityVehicles,
        HealthcareVehicles,
        OtherServices
    }

    public static class CategoryTabExtension
    {
        public static string getCategoryName(this CategoryTab tab)
        {
            switch (tab)
            {
                case CategoryTab.EmergencyVehicles:
                    return Locale.Get("MAIN_TOOL_ND", "FireDepartment");
                case CategoryTab.OutsideConnection:
                    return Locale.Get("AREA_CONNECTIONS");
                case CategoryTab.PublicTransport:
                    return Locale.Get("ASSETIMPORTER_CATEGORY", "PublicTransport");
                case CategoryTab.SecurityVehicles:
                    return Locale.Get("ASSETIMPORTER_CATEGORY", "Police");
                case CategoryTab.HealthcareVehicles:
                    return Locale.Get("ASSETIMPORTER_CATEGORY", "Healthcare");
                case CategoryTab.OtherServices:
                    return Locale.Get("ROUTECHECKBOX6");
                default:
                    throw new Exception($"Not supported: {tab}");
            }

        }
        public static string getCategoryIcon(this CategoryTab tab)
        {
            switch (tab)
            {
                case CategoryTab.EmergencyVehicles:
                    return "SubBarFireDepartmentDisaster";
                case CategoryTab.OutsideConnection:
                    return "IconRightArrow";
                case CategoryTab.PublicTransport:
                    return "ToolbarIconPublicTransport";
                case CategoryTab.SecurityVehicles:
                    return "ToolbarIconPolice";
                case CategoryTab.HealthcareVehicles:
                    return "ToolbarIconHealthcare";
                case CategoryTab.OtherServices:
                    return "ToolbarIconHelp";
                default:
                    throw new Exception($"Not supported: {tab}");
            }

        }
    }

}
