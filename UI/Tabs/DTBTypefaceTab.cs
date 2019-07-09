using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ICities;
using Klyte.DynamicTextBoards.Utils;
using Klyte.Commons.Extensors;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Klyte.DynamicTextBoards.Overrides;
using Klyte.Commons.Utils;

namespace Klyte.DynamicTextBoards.UI
{

    internal class DTBFontConfigTab : UICustomControl
    {
        public UIComponent mainContainer { get; private set; }

        private UIDropDown m_fontStationBuildings;
        private UIDropDown m_fontHighwayProps;
        private UIDropDown m_fontStreetCorners;

        private UIHelperExtension m_uiHelperDistrict;

        #region Awake
        private void Awake()
        {
            mainContainer = GetComponent<UIComponent>();

            m_uiHelperDistrict = new UIHelperExtension(mainContainer);

            ((UIScrollablePanel)m_uiHelperDistrict.Self).autoLayoutDirection = LayoutDirection.Horizontal;
            ((UIScrollablePanel)m_uiHelperDistrict.Self).wrapLayout = true;

            CreateGroupFileSelect("DTB_FONT_STATIONS", (int idx) => BoardGeneratorBuildings.Instance.ChangeFont(idx == 0 ? null : m_fontStationBuildings.items[idx]), out m_fontStationBuildings);
            CreateGroupFileSelect("DTB_FONT_HIGHWAYS", (int idx) => BoardGeneratorHighwayMileage.Instance.ChangeFont(idx == 0 ? null : m_fontHighwayProps.items[idx]), out m_fontHighwayProps);
            CreateGroupFileSelect("DTB_FONT_ST_CORNERS", (int idx) => BoardGeneratorRoadNodes.Instance.ChangeFont(idx == 0 ? null : m_fontStreetCorners.items[idx]), out m_fontStreetCorners);


            m_uiHelperDistrict.AddSpace(1);
           KlyteMonoUtils.LimitWidth((UIButton)m_uiHelperDistrict.AddButton(Locale.Get("DTB_RELOAD_FONTS"), reloadDropDownsFonts), 380);
           KlyteMonoUtils.LimitWidth((UIButton)m_uiHelperDistrict.AddButton(Locale.Get("DTB_RELOAD_CONFIGS"), BoardGeneratorBuildings.Instance.LoadAllBuildingConfigurations), 380);
            reloadDropDownsFonts();

        }

        private void CreateGroupFileSelect(string i18n, OnDropdownSelectionChanged onChanged, out UIDropDown dropDown)
        {
            dropDown = m_uiHelperDistrict.AddDropdownLocalized(i18n, new String[0], -1, onChanged);
            dropDown.width = 370;
            m_uiHelperDistrict.AddSpace(20);
        }

        #endregion

        private void reloadDropDownsFonts()
        {
            DTBUtils.ReloadFontsOf<BoardGeneratorBuildings>(m_fontStationBuildings);
            DTBUtils.ReloadFontsOf<BoardGeneratorRoadNodes>(m_fontStreetCorners);
            DTBUtils.ReloadFontsOf<BoardGeneratorHighwayMileage>(m_fontHighwayProps);
        }




    }


}
