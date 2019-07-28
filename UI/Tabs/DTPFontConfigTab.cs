using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ICities;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Overrides;
using Klyte.DynamicTextProps.Utils;

namespace Klyte.DynamicTextProps.UI
{

    internal class DTPFontConfigTab : UICustomControl
    {
        public UIComponent MainContainer { get; private set; }

        private UIDropDown m_fontStationBuildings;
        private UIDropDown m_fontHighwayProps;

        private UIHelperExtension m_uiHelperDistrict;

        #region Awake
        public void Awake()
        {
            LogUtils.DoLog("Awake");
            MainContainer = GetComponent<UIComponent>();
            m_uiHelperDistrict = new UIHelperExtension(MainContainer);
            ((UIScrollablePanel) m_uiHelperDistrict.Self).autoLayoutDirection = LayoutDirection.Horizontal;
            ((UIScrollablePanel) m_uiHelperDistrict.Self).wrapLayout = true;
            CreateGroupFileSelect("K45_DTP_FONT_STATIONS", (int idx) => BoardGeneratorBuildings.Instance.ChangeFont(idx == 0 ? null : m_fontStationBuildings.items[idx]), out m_fontStationBuildings);
            CreateGroupFileSelect("K45_DTP_FONT_HIGHWAYS", (int idx) => BoardGeneratorHighwayMileage.Instance.ChangeFont(idx == 0 ? null : m_fontHighwayProps.items[idx]), out m_fontHighwayProps);

            m_uiHelperDistrict.AddSpace(1);
            KlyteMonoUtils.LimitWidth((UIButton) m_uiHelperDistrict.AddButton(Locale.Get("K45_DTP_RELOAD_FONTS"), ReloadDropDownsFonts), 380);
            KlyteMonoUtils.LimitWidth((UIButton) m_uiHelperDistrict.AddButton(Locale.Get("K45_DTP_RELOAD_CONFIGS"), LoadAllBuildingConfigurations), 380);
            ReloadDropDownsFonts();

        }

        private void LoadAllBuildingConfigurations() => BoardGeneratorBuildings.Instance.LoadAllBuildingConfigurations();

        private void CreateGroupFileSelect(string i18n, OnDropdownSelectionChanged onChanged, out UIDropDown dropDown)
        {
            dropDown = m_uiHelperDistrict.AddDropdownLocalized(i18n, new string[0], -1, onChanged);
            dropDown.width = 370;
            m_uiHelperDistrict.AddSpace(20);
        }

        #endregion

        private void ReloadDropDownsFonts()
        {
            DTPUtils.ReloadFontsOf<BoardGeneratorBuildings>(m_fontStationBuildings);
            DTPUtils.ReloadFontsOf<BoardGeneratorHighwayMileage>(m_fontHighwayProps);
        }




    }


}
