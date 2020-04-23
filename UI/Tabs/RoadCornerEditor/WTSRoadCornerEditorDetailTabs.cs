using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Libraries;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Linq;
using UnityEngine;
using static ItemClass;
using static Klyte.WriteTheSigns.UI.WTSEditorUILib;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSRoadCornerEditorDetailTabs : UICustomControl
    {
        public UIPanel MainContainer { get; protected set; }
        private int m_currentIdx = -1;
        private bool m_isEditing;

        private UITabstrip m_tabstrip;

        private UITextField m_name;
        private UIDropDown m_propLayoutSelect;
        private UITextField[] m_position;
        private UITextField[] m_rotation;
        private UITextField[] m_scale;

        private UIButton m_copySettings;
        private UIButton m_pasteSettings;

        private UILabel m_roadTypesTitle;
        private UICheckBox m_allowDirty;
        private UICheckBox m_allowAlleys;
        private UICheckBox m_allowSmallRoads;
        private UICheckBox m_allowMediumRoads;
        private UICheckBox m_allowLargeRoads;
        private UICheckBox m_allowHighways;
        private UISlider m_spawnChance;
        private UICheckBox m_placeDistrictBorder;
        private UICheckBox m_spawnOnSegment;

        private UIPanel m_spawnInSegmentOptions;
        private UICheckBox m_ensureRoadTypeAllowed;
        private UICheckBox m_allowAnotherRuleForCorner;

        private UICheckBox m_useDistrictColor;
        private UICheckBox m_applyAbbreviations_full;
        private UICheckBox m_applyAbbreviations_suffix;

        private UICheckBox m_districtWhiteList;
        private UICheckBox m_districtBlackList;
        private UIPanel m_listContainer;
        private UIScrollablePanel m_districtList;
        public void Awake()
        {
            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.clipChildren = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.autoLayoutPadding = new RectOffset(0, 0, 4, 4);

            KlyteMonoUtils.CreateTabsComponent(out m_tabstrip, out UITabContainer m_tabContainer, MainContainer.transform, "TextEditor", new Vector4(0, 0, MainContainer.width, 40), new Vector4(0, 0, MainContainer.width, MainContainer.height - 40));
            UIPanel m_tabSettings = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Settings), "K45_WTS_ROADCORNER_BASIC_SETTINGS", "RcSettings");
            UIPanel m_tabSpawning = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Reload), "K45_WTS_ROADCORNER_SPAWNING_SETTINGS", "RcSpawning");
            UIPanel m_tabAppearence = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_AutoColorIcon), "K45_WTS_ROADCORNER_APPEARANCE_SETTINGS", "RcAppearence");
            UIPanel m_tabDistricts = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, "ToolbarIconDistrict", "K45_WTS_ROADCORNER_DISTRICT_SETTINGS", "RcDistricts");

            var helperSettings = new UIHelperExtension(m_tabSettings, LayoutDirection.Vertical);
            var helperSpawning = new UIHelperExtension(m_tabSpawning, LayoutDirection.Vertical);
            var helperAppearence = new UIHelperExtension(m_tabAppearence, LayoutDirection.Vertical);
            var helperDistricts = new UIHelperExtension(m_tabDistricts, LayoutDirection.Vertical);


            AddTextField(Locale.Get("K45_WTS_ROADCORNER_NAME"), out m_name, helperSettings, OnSetName);

            helperSettings.AddSpace(5);

            AddDropdown(Locale.Get("K45_WTS_ROADCORNER_PROPLAYOUT"), out m_propLayoutSelect, helperSettings, new string[0], OnPropLayoutChange);
            AddButtonInEditorRow(m_propLayoutSelect, CommonsSpriteNames.K45_Reload, LoadAvailableLayouts);
            AddVector3Field(Locale.Get("K45_WTS_ROADCORNER_POSITION"), out m_position, helperSettings, OnPositionChanged);
            AddVector3Field(Locale.Get("K45_WTS_ROADCORNER_ROTATION"), out m_rotation, helperSettings, OnRotationChanged);
            AddVector3Field(Locale.Get("K45_WTS_ROADCORNER_SCALE"), out m_scale, helperSettings, OnScaleChanged);
            AddLibBox<WTSLibRoadCornerRule, BoardInstanceRoadNodeXml>(helperSettings, out m_copySettings, OnCopyRule, out m_pasteSettings, OnPasteRule, out _, null, OnLoadRule, GetRuleSerialized);

            UIHelperExtension helperAllowedTypes = helperSpawning.AddGroupExtended(Locale.Get("K45_WTS_ROADCORNER_ALLOWTITLE"), out m_roadTypesTitle, out _);
            AddCheckboxLocale("K45_WTS_ROADCORNER_ALLOW_DIRTROADS", out m_allowDirty, helperAllowedTypes, (x) => ToggleAllow(Level.Level1, x));
            AddCheckboxLocale("K45_WTS_ROADCORNER_ALLOW_ALLEYS", out m_allowAlleys, helperAllowedTypes, (x) => ToggleAllow((Level)5, x));
            AddCheckboxLocale("K45_WTS_ROADCORNER_ALLOW_SMALLROADS", out m_allowSmallRoads, helperAllowedTypes, (x) => ToggleAllow(Level.Level2, x));
            AddCheckboxLocale("K45_WTS_ROADCORNER_ALLOW_MEDIUMROADS", out m_allowMediumRoads, helperAllowedTypes, (x) => ToggleAllow(Level.Level3, x));
            AddCheckboxLocale("K45_WTS_ROADCORNER_ALLOW_LARGEROADS", out m_allowLargeRoads, helperAllowedTypes, (x) => ToggleAllow(Level.Level4, x));
            AddCheckboxLocale("K45_WTS_ROADCORNER_ALLOW_HIGHWAYS", out m_allowHighways, helperAllowedTypes, (x) => ToggleAllow(Level.Level5, x));

            AddSlider(Locale.Get("K45_WTS_ROADCORNER_SPAWN_CHANCE"), out m_spawnChance, helperSpawning, OnChangeSpawnChance, 0, 255, 1, (x) => (x / 255).ToString("P0"));
            AddCheckboxLocale("K45_WTS_ROADCORNER_PLACEALSOONDISTRICTBORDER", out m_placeDistrictBorder, helperSpawning, OnChangeSpawnOnDistrictBorder);
            AddCheckboxLocale("K45_WTS_ROADCORNER_SPAWNONSEGMENT", out m_spawnOnSegment, helperSpawning, OnChangeSpawnOnSegment);
            UIHelperExtension helperSpawningSegment = helperSpawning.AddGroupExtended(Locale.Get("K45_WTS_ROADCORNER_SPAWNONSEGMENT_OPTIONS"));
            m_spawnInSegmentOptions = helperSpawningSegment.Self as UIPanel;
            m_spawnInSegmentOptions.width = 620;
            AddDropdown(Locale.Get("K45_WTS_ROADCORNER_FLOWREQUIREMENT"), out m_flowRequirement, helperSpawningSegment, Enum.GetNames(typeof(TrafficDirectionRequired)).Select(x => Locale.Get("K45_WTS_TRAFFICDIRECTIONREQUIRED", x)).ToArray(), OnChangeFlowRequirement);
            AddVector2Field(Locale.Get("K45_WTS_ROADCORNER_MINMAXLANESREQUIRED"), out m_minMaxLaneRequired, helperSpawningSegment, OnLanesRequiredChange, false);
            helperSpawningSegment.AddSpace(10);
            AddCheckboxLocale("K45_WTS_ROADCORNER_ENSUREROADTYPEALLOWED", out m_ensureRoadTypeAllowed, helperSpawningSegment, OnChangeEnsureRoadTypeAllowed);
            AddCheckboxLocale("K45_WTS_ROADCORNER_ALLOWANOTHERRULEFORCORNER", out m_allowAnotherRuleForCorner, helperSpawningSegment, OnChangeAllowAnotherRuleForCorner);

            AddCheckboxLocale("K45_WTS_ROADCORNER_USEDISTRICTCOLOR", out m_useDistrictColor, helperAppearence, OnChangeUseDistrictColor);
            AddCheckboxLocale("K45_WTS_ROADCORNER_APPLYABBREVIATIONS_FULLNAME", out m_applyAbbreviations_full, helperAppearence, OnChangeApplyAbbreviationsFullName);
            AddCheckboxLocale("K45_WTS_ROADCORNER_APPLYABBREVIATIONS_SUFFIX", out m_applyAbbreviations_suffix, helperAppearence, OnChangeApplyAbbreviationsSuffix);

            AddCheckboxLocale("K45_WTS_ROADCORNER_DISTRICTSELECTIONASWHITELIST", out m_districtWhiteList, helperDistricts, OnSetDistrictsAsWhitelist);
            AddCheckboxLocale("K45_WTS_ROADCORNER_DISTRICTSELECTIONASBLACKLIST", out m_districtBlackList, helperDistricts, OnSetDistrictsAsBlacklist);
            KlyteMonoUtils.CreateUIElement(out m_listContainer, helperDistricts.Self.transform, "previewPanel", new UnityEngine.Vector4(0, 0, helperDistricts.Self.width, helperDistricts.Self.height - 90));
            KlyteMonoUtils.CreateScrollPanel(m_listContainer, out m_districtList, out _, m_listContainer.width - 20, m_listContainer.height);
            m_districtList.backgroundSprite = "OptionsScrollbarTrack";
            m_districtList.autoLayout = true;
            m_districtList.autoLayoutDirection = LayoutDirection.Vertical;

            WTSRoadCornerEditor.Instance.RuleList.EventSelectionChanged += OnChangeTab;
            LoadAvailableLayouts();
            MainContainer.isVisible = false;
            m_pasteSettings.isVisible = false;
        }


        private void LoadAvailableLayouts()
        {
            m_propLayoutSelect.items = WTSPropLayoutData.Instance.ListWhere(x => x.m_allowedRenderClass == TextRenderingClass.RoadNodes).ToArray();
            SafeObtain((ref BoardInstanceRoadNodeXml x) => m_propLayoutSelect.selectedIndex = Math.Max(0, Array.IndexOf(m_propLayoutSelect.items, x.PropLayoutName)));
        }


        private delegate void SafeObtainMethod(ref BoardInstanceRoadNodeXml x);
        private void SafeObtain(SafeObtainMethod action, int? targetTab = null)
        {
            int effTargetTab = Math.Max(-1, targetTab ?? m_currentIdx);
            if (m_isEditing || effTargetTab < 0)
            {
                return;
            }

            lock (this)
            {
                m_isEditing = true;
                try
                {
                    if (effTargetTab < WTSRoadNodesData.Instance.DescriptorRulesOrder.Length)
                    {
                        action(ref WTSRoadNodesData.Instance.DescriptorRulesOrder[effTargetTab]);
                        WTSRoadNodesData.Instance.ResetBoards();
                    }
                }
                finally
                {
                    m_isEditing = false;
                }
            }
        }
        private void OnChangeTab(int obj)
        {
            LogUtils.DoWarnLog($"SEL IDX: {obj}");
            MainContainer.isVisible = obj >= 0;
            m_currentIdx = obj;
            SafeObtain((ref BoardInstanceRoadNodeXml x) =>
            {
                m_name.text = x.SaveName;
                m_propLayoutSelect.selectedValue = x.PropLayoutName;
                m_position[0].text = x.PropPositionX.ToString("F3");
                m_position[1].text = x.PropPositionY.ToString("F3");
                m_position[2].text = x.PropPositionZ.ToString("F3");
                m_rotation[0].text = x.PropRotationX.ToString("F3");
                m_rotation[1].text = x.PropRotationY.ToString("F3");
                m_rotation[2].text = x.PropRotationZ.ToString("F3");
                m_scale[0].text = x.PropScale.x.ToString("F3");
                m_scale[1].text = x.PropScale.y.ToString("F3");
                m_scale[2].text = x.PropScale.z.ToString("F3");

                m_allowDirty.isChecked = x.AllowedLevels.Contains(Level.Level1);
                m_allowAlleys.isChecked = x.AllowedLevels.Contains((Level)5);
                m_allowSmallRoads.isChecked = x.AllowedLevels.Contains(Level.Level2);
                m_allowMediumRoads.isChecked = x.AllowedLevels.Contains(Level.Level3);
                m_allowLargeRoads.isChecked = x.AllowedLevels.Contains(Level.Level4);
                m_allowHighways.isChecked = x.AllowedLevels.Contains(Level.Level5);
                m_spawnChance.value = x.SpawnChance;
                m_placeDistrictBorder.isChecked = x.PlaceOnDistrictBorder;
                m_spawnOnSegment.isChecked = x.PlaceOnSegmentInsteadOfCorner;
                m_flowRequirement.selectedIndex = (int)x.TrafficDirectionRequired;
                m_minMaxLaneRequired[0].text = x.MinIncomeOutcomeLanes.ToString("D0");
                m_minMaxLaneRequired[1].text = x.MaxIncomeOutcomeLanes.ToString("D0");
                m_ensureRoadTypeAllowed.isChecked = x.EnsureSegmentTypeInAllowedTypes;
                m_allowAnotherRuleForCorner.isChecked = x.AllowAnotherRuleForCorner;

                m_useDistrictColor.isChecked = x.UseDistrictColor;
                m_applyAbbreviations_full.isChecked = x.ApplyAbreviationsOnFullName;
                m_applyAbbreviations_suffix.isChecked = x.ApplyAbreviationsOnSuffix;

                m_districtWhiteList.isChecked = !x.SelectedDistrictsIsBlacklist;
                m_districtBlackList.isChecked = x.SelectedDistrictsIsBlacklist;

                m_minMaxLaneRequired[0].parent.isVisible = x.TrafficDirectionRequired != TrafficDirectionRequired.NONE;
                //m_districtList.isChecked = x.;


                m_spawnInSegmentOptions.isVisible = x.PlaceOnSegmentInsteadOfCorner;
            });
        }

        private string m_clipboard;

        private BoardInstanceRoadNodeXml nullValue = null;
        private UITextField[] m_minMaxLaneRequired;
        private UIDropDown m_flowRequirement;

        private ref BoardInstanceRoadNodeXml GetRuleSerialized()
        {
            int effTargetTab = Math.Max(-1, m_currentIdx);
            if (effTargetTab >= 0 && effTargetTab < WTSRoadNodesData.Instance.DescriptorRulesOrder.Length)
            {
                return ref WTSRoadNodesData.Instance.DescriptorRulesOrder[effTargetTab];
            }
            else
            {
                nullValue = null;
                return ref nullValue;
            }
        }

        private void OnLoadRule(string obj) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x = XmlUtils.DefaultXmlDeserialize<BoardInstanceRoadNodeXml>(obj));
        private void OnPasteRule() => OnLoadRule(m_clipboard);
        private void OnCopyRule() => SafeObtain((ref BoardInstanceRoadNodeXml x) =>
        {
            m_clipboard = XmlUtils.DefaultXmlSerialize(x);
            m_pasteSettings.isVisible = true;
        });
        private void OnSetDistrictsAsBlacklist(bool isChecked) => SafeObtain((ref BoardInstanceRoadNodeXml x) => { x.SelectedDistrictsIsBlacklist = isChecked; m_districtWhiteList.isChecked = !isChecked; });
        private void OnSetDistrictsAsWhitelist(bool isChecked) => SafeObtain((ref BoardInstanceRoadNodeXml x) => { x.SelectedDistrictsIsBlacklist = !isChecked; m_districtBlackList.isChecked = !isChecked; });

        private void OnChangeSpawnOnDistrictBorder(bool isChecked) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.PlaceOnDistrictBorder = isChecked);


        private void OnChangeFlowRequirement(int sel) => SafeObtain((ref BoardInstanceRoadNodeXml x) =>
        {
            if (sel >= 0)
            {
                x.TrafficDirectionRequired = (TrafficDirectionRequired)sel;
                m_minMaxLaneRequired[0].parent.isVisible = x.TrafficDirectionRequired != TrafficDirectionRequired.NONE;
            }
        });
        private void OnLanesRequiredChange(Vector2 obj) => SafeObtain((ref BoardInstanceRoadNodeXml x) =>
        {
            x.MinIncomeOutcomeLanes = Mathf.RoundToInt(Mathf.Max(1, Mathf.Min(obj.x, obj.y, 99)));
            x.MaxIncomeOutcomeLanes = Mathf.RoundToInt(Mathf.Min(99, Mathf.Max(obj.x, obj.y, 1)));

            m_minMaxLaneRequired[0].text = x.MinIncomeOutcomeLanes.ToString("D0");
            m_minMaxLaneRequired[1].text = x.MaxIncomeOutcomeLanes.ToString("D0");

        });
        private void OnChangeSpawnOnSegment(bool isChecked) => SafeObtain((ref BoardInstanceRoadNodeXml x) =>
        {
            x.PlaceOnSegmentInsteadOfCorner = isChecked;
            m_spawnInSegmentOptions.isVisible = isChecked;
        });

        private void OnChangeAllowAnotherRuleForCorner(bool isChecked) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.AllowAnotherRuleForCorner = isChecked);
        private void OnChangeEnsureRoadTypeAllowed(bool isChecked) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.EnsureSegmentTypeInAllowedTypes = isChecked);
        private void ToggleAllow(Level targetLevel, bool value) => SafeObtain((ref BoardInstanceRoadNodeXml x) =>
        {
            if (value)
            {
                x.AllowedLevels.Add(targetLevel);
            }
            else
            {
                x.AllowedLevels.Remove(targetLevel);
            }
        });
        private void OnRotationChanged(Vector3 obj) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.m_propRotation = obj);
        private void OnScaleChanged(Vector3 obj) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.PropScale = obj);
        private void OnPositionChanged(Vector3 obj) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.m_propPosition = obj);
        private void OnPropLayoutChange(int sel) => SafeObtain((ref BoardInstanceRoadNodeXml x) =>
        {
            if (sel >= 0)
            {
                x.PropLayoutName = m_propLayoutSelect.items[sel];
            }
            else
            {
                x.PropLayoutName = null;
            }
        });
        private void OnChangeUseDistrictColor(bool isChecked) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.UseDistrictColor = isChecked);
        private void OnChangeSpawnChance(float val) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.SpawnChance = (byte)val);
        private void OnChangeApplyAbbreviationsSuffix(bool isChecked) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.ApplyAbreviationsOnSuffix = isChecked);
        private void OnChangeApplyAbbreviationsFullName(bool isChecked) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.ApplyAbreviationsOnFullName = isChecked);
        private void OnSetName(string text) => SafeObtain((ref BoardInstanceRoadNodeXml x) =>
        {
            if (!text.IsNullOrWhiteSpace())
            {
                x.SaveName = text;
                WTSRoadCornerEditor.Instance.RuleList.FixTabstrip();
            }
            else
            {
                m_name.text = x.SaveName;
            }
        });
    }

}
