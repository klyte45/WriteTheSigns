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

            ((UIScrollablePanel)m_uiHelperDistrict.self).autoLayoutDirection = LayoutDirection.Horizontal;
            ((UIScrollablePanel)m_uiHelperDistrict.self).wrapLayout = true;

            CreateGroupFileSelect("DTB_FONT_STATIONS", (int idx) => Redirector<BoardGeneratorBuildings>.instance.ChangeFont(idx == 0 ? null : m_fontStationBuildings.items[idx]), out m_fontStationBuildings);
            CreateGroupFileSelect("DTB_FONT_HIGHWAYS", (int idx) => Redirector<BoardGeneratorHighwayMileage>.instance.ChangeFont(idx == 0 ? null : m_fontHighwayProps.items[idx]), out m_fontHighwayProps);
            CreateGroupFileSelect("DTB_FONT_ST_CORNERS", (int idx) => Redirector<BoardGeneratorRoadNodes>.instance.ChangeFont(idx == 0 ? null : m_fontStreetCorners.items[idx]), out m_fontStreetCorners);


            m_uiHelperDistrict.AddSpace(1);
            DTBUtils.LimitWidth((UIButton)m_uiHelperDistrict.AddButton(Locale.Get("DTB_RELOAD_FONTS"), reloadDropDownsFonts), 380);
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
            reloadFontOf<BoardGeneratorBuildings>(m_fontStationBuildings);
            reloadFontOf<BoardGeneratorRoadNodes>(m_fontStreetCorners);
            reloadFontOf<BoardGeneratorHighwayMileage>(m_fontHighwayProps);
        }

        private void reloadFontOf<BG>(UIDropDown target) where BG : BoardGeneratorParent<BG>
        {
            DTBUtils.doLog($"{Redirector<BG>.instance}");
            List<string> items = Font.GetOSInstalledFontNames().ToList();
            items.Insert(0, Locale.Get("DTB_DEFAULT_FONT_LABEL"));
            target.items = items.ToArray();
            string filename = Redirector<BG>.instance.DrawFont.baseFont.fontNames[0];
            if (items.Contains(filename))
            {
                target.selectedValue = filename;
            }
            else
            {
                target.selectedIndex = 0;
                Redirector<BG>.instance.ChangeFont(null);
            }

        }


    }


}
