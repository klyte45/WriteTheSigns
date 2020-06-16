using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ICities;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Utils;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSFontsSettings : UICustomControl
    {

        public UIPanel MainContainer { get; protected set; }

        private UIDropDown m_fontSelectStreetSigns;
        private UIDropDown m_fontSelectBuildings;
        private UIDropDown m_fontSelectLineSymbols;
        private UIDropDown m_fontSelectVehicles;

        public void Awake()
        {

            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.autoLayoutPadding = new RectOffset(5, 5, 5, 5);

            var m_uiHelperHS = new UIHelperExtension(MainContainer);

            AddFontDD("K45_WTS_FONT_ST_CORNERS", m_uiHelperHS, out m_fontSelectStreetSigns, OnSetFontStreet);
            AddFontDD("K45_WTS_FONT_STATIONS", m_uiHelperHS, out m_fontSelectBuildings, OnSetFontBuildings);
            AddFontDD("K45_WTS_FONT_TRANSPORTLINES", m_uiHelperHS, out m_fontSelectLineSymbols, OnSetFontPublicTransportLineSymbol);
            AddFontDD("K45_WTS_FONT_VEHICLES", m_uiHelperHS, out m_fontSelectVehicles, OnSetFontVehicles);

            ReloadFonts();
        }

        private void AddFontDD(string locale, UIHelperExtension m_uiHelperHS, out UIDropDown targetDD, OnDropdownSelectionChanged callback)
        {
            AddDropdown(Locale.Get(locale), out targetDD, m_uiHelperHS, new string[0], callback);
            AddButtonInEditorRow(targetDD, CommonsSpriteNames.K45_Reload, ReloadFonts);
        }

        private void ReloadFonts()
        {
            WTSUtils.ReloadFontsOf(m_fontSelectStreetSigns, WTSRoadNodesData.Instance.DefaultFont, false, true);
            WTSUtils.ReloadFontsOf(m_fontSelectBuildings, WTSBuildingsData.Instance.DefaultFont, false, true);
            WTSUtils.ReloadFontsOf(m_fontSelectLineSymbols, WTSEtcData.Instance.FontSettings.PublicTransportLineSymbolFont, false, true);
            WTSUtils.ReloadFontsOf(m_fontSelectVehicles, WTSVehicleData.Instance.DefaultFont, false, true);
        }
        private void OnSetFontStreet(int sel)
        {
            if (sel > 0)
            {
                WTSRoadNodesData.Instance.DefaultFont = m_fontSelectStreetSigns.selectedValue;
            }
            else if (sel == 0)
            {
                WTSRoadNodesData.Instance.DefaultFont = null;
            }
        }
        private void OnSetFontBuildings(int sel)
        {
            if (sel > 0)
            {
                WTSBuildingsData.Instance.DefaultFont = m_fontSelectBuildings.selectedValue;
            }
            else if (sel == 0)
            {
                WTSBuildingsData.Instance.DefaultFont = null;
            }
        }
        private void OnSetFontPublicTransportLineSymbol(int sel)
        {
            if (sel > 0)
            {
                WTSEtcData.Instance.FontSettings.PublicTransportLineSymbolFont = m_fontSelectLineSymbols.selectedValue;
            }
            else if (sel == 0)
            {
                WTSEtcData.Instance.FontSettings.PublicTransportLineSymbolFont = null;
            }
        }
        private void OnSetFontVehicles(int sel)
        {
            if (sel > 0)
            {
                WTSVehicleData.Instance.DefaultFont = m_fontSelectVehicles.selectedValue;
            }
            else if (sel == 0)
            {
                WTSVehicleData.Instance.DefaultFont = null;
            }
        }
    }
}


