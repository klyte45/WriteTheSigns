using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheCity.Libraries;
using Klyte.WriteTheCity.Xml;
using UnityEngine;
using static ItemClass;
using static Klyte.WriteTheCity.UI.WTCEditorUILib;

namespace Klyte.WriteTheCity.UI
{

    internal class WTCRoadCornerEditorDetailTabs : UICustomControl
    {
        public UIPanel MainContainer { get; protected set; }


        public void Awake()
        {
            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.clipChildren = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.autoLayoutPadding = new RectOffset(0, 0, 4, 4);

            KlyteMonoUtils.CreateTabsComponent(out UITabstrip m_tabstrip, out UITabContainer m_tabContainer, MainContainer.transform, "TextEditor", new Vector4(0, 0, MainContainer.width, 40), new Vector4(0, 0, MainContainer.width, MainContainer.height - 40));
            UIPanel m_tabSettings = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Settings), "K45_WTC_ROADCORNER_BASIC_SETTINGS", "RcSettings");
            UIPanel m_tabSpawning = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Reload), "K45_WTC_ROADCORNER_SPAWNING_SETTINGS", "RcSpawning");
            UIPanel m_tabAppearence = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_AutoColorIcon), "K45_WTC_ROADCORNER_APPEARANCE_SETTINGS", "RcAppearence");
            UIPanel m_tabDistricts = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, "ToolbarIconDistrict", "K45_WTC_ROADCORNER_DISTRICT_SETTINGS", "RcDistricts");

            var helperSettings = new UIHelperExtension(m_tabSettings, LayoutDirection.Vertical);
            var helperSpawning = new UIHelperExtension(m_tabSpawning, LayoutDirection.Vertical);
            var helperAppearence = new UIHelperExtension(m_tabAppearence, LayoutDirection.Vertical);
            var helperDistricts = new UIHelperExtension(m_tabDistricts, LayoutDirection.Vertical);


            AddTextField(Locale.Get("K45_WTC_ROADCORNER_NAME"), out UITextField name, helperSettings, OnSetName);

            helperSettings.AddSpace(5);

            AddDropdown(Locale.Get("K45_WTC_ROADCORNER_PROPLAYOUT"), out UIDropDown m_propLayoutSelect, helperSettings, new string[0], OnPropLayoutChange);
            AddVector3Field(Locale.Get("K45_WTC_ROADCORNER_POSITION"), out UITextField[] m_position, helperSettings, OnPositionChanged);
            AddVector3Field(Locale.Get("K45_WTC_ROADCORNER_ROTATION"), out UITextField[] m_rotation, helperSettings, OnRotationChanged);
            AddVector3Field(Locale.Get("K45_WTC_ROADCORNER_SCALE"), out UITextField[] m_scale, helperSettings, OnScaleChanged);
            AddLibBox<WTCLibRoadCornerRule, BoardInstanceRoadNodeXml>(helperSettings, out UIButton m_copySettings, OnCopyRule, out UIButton m_pasteSettings, OnPasteRule, out _, null, OnLoadRule, GetRuleSerialized);

            UIHelperExtension helperAllowedTypes = helperSpawning.AddGroupExtended(Locale.Get("K45_WTC_ROADCORNER_ALLOWTITLE"), out UILabel roadTypesTitle, out _);
            AddCheckboxLocale("K45_WTC_ROADCORNER_ALLOW_DIRTROADS", out UICheckBox m_allowDirty, helperAllowedTypes, (x) => ToggleAllow(Level.Level1, x));
            AddCheckboxLocale("K45_WTC_ROADCORNER_ALLOW_ALLEYS", out UICheckBox m_allowAlleys, helperAllowedTypes, (x) => ToggleAllow((Level)5, x));
            AddCheckboxLocale("K45_WTC_ROADCORNER_ALLOW_SMALLROADS", out UICheckBox m_allowSmallRoads, helperAllowedTypes, (x) => ToggleAllow(Level.Level2, x));
            AddCheckboxLocale("K45_WTC_ROADCORNER_ALLOW_MEDIUMROADS", out UICheckBox m_allowMediumRoads, helperAllowedTypes, (x) => ToggleAllow(Level.Level3, x));
            AddCheckboxLocale("K45_WTC_ROADCORNER_ALLOW_LARGEROADS", out UICheckBox m_allowLargeRoads, helperAllowedTypes, (x) => ToggleAllow(Level.Level4, x));
            AddCheckboxLocale("K45_WTC_ROADCORNER_ALLOW_HIGHWAYS", out UICheckBox m_allowHighways, helperAllowedTypes, (x) => ToggleAllow(Level.Level5, x));

            AddSlider(Locale.Get("K45_WTC_ROADCORNER_SPAWN_CHANCE"), out UISlider m_spawnChance, helperSpawning, OnChangeSpawnChance, 0, 255, 1);
            AddCheckboxLocale("K45_WTC_ROADCORNER_PLACEALSOONDISTRICTBORDER", out UICheckBox m_placeDistrictBorder, helperSpawning, OnChangeSpawnOnDistrictBorder);
            AddCheckboxLocale("K45_WTC_ROADCORNER_SPAWNONSEGMENT", out UICheckBox m_spawnOnSegment, helperSpawning, OnChangeSpawnOnSegment);
            AddCheckboxLocale("K45_WTC_ROADCORNER_ONLYIFOUTBOUNDTRAFFICFROMNODE", out UICheckBox m_onlyIfOutbound, helperSpawning, OnChangeOnlyIfOutboundTrafficFromNode);

            AddCheckboxLocale("K45_WTC_ROADCORNER_USEDISTRICTCOLOR", out UICheckBox m_useDistrictColor, helperAppearence, OnChangeUseDistrictColor);
            AddCheckboxLocale("K45_WTC_ROADCORNER_APPLYABBREVIATIONS_FULLNAME", out UICheckBox m_applyAbbreviations_full, helperAppearence, OnChangeApplyAbbreviationsFullName);
            AddCheckboxLocale("K45_WTC_ROADCORNER_APPLYABBREVIATIONS_SUFFIX", out UICheckBox m_applyAbbreviations_suffix, helperAppearence, OnChangeApplyAbbreviationsSuffix);

            AddCheckboxLocale("K45_WTC_ROADCORNER_DISTRICTSELECTIONASWHITELIST", out UICheckBox m_districtWhiteList, helperDistricts, OnSetDistrictsAsWhitelist);
            AddCheckboxLocale("K45_WTC_ROADCORNER_DISTRICTSELECTIONASBLACKLIST", out UICheckBox m_districtBlackList, helperDistricts, OnSetDistrictsAsBlacklist);
            KlyteMonoUtils.CreateUIElement(out UIPanel m_listContainer, helperDistricts.Self.transform, "previewPanel", new UnityEngine.Vector4(0, 0, helperDistricts.Self.width, helperDistricts.Self.height - 90));
            KlyteMonoUtils.CreateScrollPanel(m_listContainer, out UIScrollablePanel orderedRulesList, out _, m_listContainer.width - 20, m_listContainer.height);
            orderedRulesList.backgroundSprite = "OptionsScrollbarTrack";
            orderedRulesList.autoLayout = true;
            orderedRulesList.autoLayoutDirection = LayoutDirection.Vertical;
        }

        private BoardInstanceRoadNodeXml GetRuleSerialized() => null;
        private void OnLoadRule(string obj) { }
        private void OnPasteRule() { }
        private void OnCopyRule() { }
        private void OnSetDistrictsAsBlacklist(bool isChecked) { }
        private void OnSetDistrictsAsWhitelist(bool isChecked) { }

        private void OnChangeSpawnOnDistrictBorder(bool isChecked) { }
        private void OnChangeOnlyIfOutboundTrafficFromNode(bool isChecked) { }
        private void OnChangeSpawnOnSegment(bool isChecked) { }

        private void ToggleAllow(Level level1, bool x) { }
        private void OnRotationChanged(Vector3 obj) { }
        private void OnScaleChanged(Vector3 obj) { }
        private void OnPositionChanged(Vector3 obj) { }
        private void OnPropLayoutChange(int sel) { }
        private void OnChangeUseDistrictColor(bool isChecked) { }
        private void OnChangeSpawnChance(float val) { }
        private void OnChangeApplyAbbreviationsSuffix(bool isChecked) { }
        private void OnChangeApplyAbbreviationsFullName(bool isChecked) { }
        private void OnSetName(string text) { }


        private void OnMouseMove(UIComponent component, UIMouseEventParameter eventParam) { }
        private void ChangeViewZoom(UIComponent component, UIMouseEventParameter eventParam) { }

    }

}
