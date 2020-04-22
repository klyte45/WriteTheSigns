using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Linq;
using UnityEngine;
using static Klyte.WriteTheSigns.UI.WTSEditorUILib;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSRoadCornerEditor : UICustomControl
    {
        private static WTSRoadCornerEditor m_instance;
        public static WTSRoadCornerEditor Instance
        {
            get {
                if (m_instance == null)
                {
                    m_instance = FindObjectOfType<WTSRoadCornerEditor>();
                }
                return m_instance;
            }
        }
        public UIPanel MainContainer { get; protected set; }

        private UIDropDown m_fontSelect;
        private UIDropDown m_abbriviationFile;
        private UIDropDown m_qualifierExtractionDropdown;

        public void Awake()
        {

            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.autoLayoutPadding = new RectOffset(5, 5, 5, 5);

            var m_uiHelperHS = new UIHelperExtension(MainContainer);

            AddDropdown(Locale.Get("K45_WTS_FONT_ST_CORNERS"), out m_fontSelect, m_uiHelperHS, new string[0], OnSetFont);
            AddButtonInEditorRow(m_fontSelect, CommonsSpriteNames.K45_Reload, ReloadFonts);
            ReloadFonts();

            AddDropdown(Locale.Get("K45_WTS_ABBREVIATION_FILE"), out m_abbriviationFile, m_uiHelperHS, new string[0], OnSetAbbreviationFile);
            AddButtonInEditorRow(m_abbriviationFile, CommonsSpriteNames.K45_Reload, () => ReloadAbbreviations());
            ReloadAbbreviations();

            AddDropdown(Locale.Get("K45_WTS_CUSTOM_NAME_EXTRACTION_QUALIFIER"), out m_qualifierExtractionDropdown, m_uiHelperHS, Enum.GetNames(typeof(RoadQualifierExtractionMode)).Select(x => Locale.Get($"K45_WTS_RoadQualifierExtractionMode", x)).ToArray(), SetRoadQualifierExtractionMode);
            m_qualifierExtractionDropdown.selectedIndex = (int)WTSRoadNodesData.Instance.RoadQualifierExtraction;

            KlyteMonoUtils.CreateUIElement(out UIPanel secondaryContainer, MainContainer.transform, "SecContainer", new Vector4(0, 0, MainContainer.width, 605));
            secondaryContainer.autoLayout = true;
            secondaryContainer.autoLayoutDirection = LayoutDirection.Horizontal;
            secondaryContainer.autoLayoutPadding = new RectOffset(0, 10, 0, 0);

            KlyteMonoUtils.CreateUIElement(out UIPanel tertiaryContainer, secondaryContainer.transform, "TrcContainer", new Vector4(0, 0, secondaryContainer.width * 0.25f, secondaryContainer.height));
            tertiaryContainer.gameObject.AddComponent<WTSRoadCornerEditorRuleList>();

            KlyteMonoUtils.CreateUIElement(out UIPanel editorPanel, secondaryContainer.transform, "EditPanel", new Vector4(0, 0, secondaryContainer.width * 0.75f - 35, secondaryContainer.height));
            editorPanel.gameObject.AddComponent<WTSRoadCornerEditorDetailTabs>();

        }

        private void ReloadFonts()
        {
            WTSUtils.ReloadFontsOf(m_fontSelect, WTSRoadNodesData.Instance.DefaultFont, false, true);
        }

        private void ReloadAbbreviations()
        {
            WriteTheSignsMod.Controller.ReloadAbbreviationFiles();
            m_abbriviationFile.items = new string[] { Locale.Get("K45_WTS_NO_ABBREVIATION_FILE_OPTION") }.Union(WriteTheSignsMod.Controller.AbbreviationFiles.Keys.OrderBy(x => x)).ToArray();
            m_abbriviationFile.selectedIndex = WTSRoadNodesData.Instance.AbbreviationFile.IsNullOrWhiteSpace() ? 0 : Array.IndexOf(m_abbriviationFile.items, WTSRoadNodesData.Instance.AbbreviationFile);
        }
        private void SetRoadQualifierExtractionMode(int sel) => WTSRoadNodesData.Instance.RoadQualifierExtraction = (RoadQualifierExtractionMode)m_qualifierExtractionDropdown.selectedIndex;

        private void OnSetAbbreviationFile(int sel)
        {
            if (sel > 0)
            {
                WTSRoadNodesData.Instance.DefaultFont = m_abbriviationFile.selectedValue;
            }
            else if (sel == 0)
            {
                WTSRoadNodesData.Instance.DefaultFont = null;
            }
        }


        private void OnSetFont(int sel)
        {
            if (sel > 0)
            {
                WTSRoadNodesData.Instance.DefaultFont = m_fontSelect.selectedValue;
            }
            else if (sel == 0)
            {
                WTSRoadNodesData.Instance.DefaultFont = null;
            }
        }
    }

}
