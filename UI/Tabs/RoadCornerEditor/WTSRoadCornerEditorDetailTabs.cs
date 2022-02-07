using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensions;
using Klyte.Commons.UI;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Libraries;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using static ItemClass;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSRoadCornerEditorDetailTabs : UICustomControl
    {
        public UIPanel MainContainer { get; protected set; }

        private const string DISTRICT_SELECTOR_TEMPLATE = "K45_WTS_DistrictSelectorTemplate";
        private int m_currentIdx = -1;

        private UITabstrip m_tabstrip;

        private UITextField m_name;
        private UITextField m_propLayoutSelect;
        private UITextField[] m_position;
        private UITextField[] m_rotation;
        private UITextField[] m_scale;

        private UIButton m_copySettings;
        private UIButton m_pasteSettings;

        private UICheckBox m_allowDirty;
        private UICheckBox m_allowAlleys;
        private UICheckBox m_allowSmallRoads;
        private UICheckBox m_allowMediumRoads;
        private UICheckBox m_allowLargeRoads;
        private UICheckBox m_allowHighways;

        private UISlider m_spawnChance;
        private UITextField[] m_minMaxHalfWidth;
        private UICheckBox m_ignoreEmpty;

        private UIPanel m_spawnInCornerOptions;
        private UICheckBox m_placeDistrictBorder;
        private UICheckBox m_placeRoadTransition;

        private UICheckBox m_useDistrictColor;

        private UICheckBox m_districtWhiteList;
        private UICheckBox m_districtBlackList;
        private UIDropDown m_districtResolutionOrder;
        private UIPanel m_listContainer;
        private UIScrollablePanel m_districtList;
        private UITemplateList<UIPanel> m_checkboxTemplateList;



        public void Awake()
        {
            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.clipChildren = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.autoLayoutPadding = new RectOffset(0, 0, 4, 4);

            KlyteMonoUtils.CreateTabsComponent(out m_tabstrip, out UITabContainer m_tabContainer, MainContainer.transform, "TextEditor", new Vector4(0, 0, MainContainer.width, 40), new Vector4(0, 0, MainContainer.width, MainContainer.height - 40));
            UIPanel m_tabSettings = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Settings), "K45_WTS_ROADCORNER_BASIC_SETTINGS", "RcSettings");
            UIPanel m_tabRoads = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, "ToolbarIconRoads", "K45_WTS_ROADCORNER_ALLOWTITLE", "RcRoad");
            UIPanel m_tabSpawning = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Reload), "K45_WTS_ROADCORNER_SPAWNING_SETTINGS", "RcSpawning");
            UIPanel m_tabAppearence = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_AutoColorIcon), "K45_WTS_ROADCORNER_APPEARANCE_SETTINGS", "RcAppearence");
            UIPanel m_tabDistricts = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, "ToolbarIconDistrict", "K45_WTS_ROADCORNER_DISTRICT_SETTINGS", "RcDistricts");

            var helperSettings = new UIHelperExtension(m_tabSettings, LayoutDirection.Vertical);
            var helperSpawning = new UIHelperExtension(m_tabSpawning, LayoutDirection.Vertical);
            var helperAppearence = new UIHelperExtension(m_tabAppearence, LayoutDirection.Vertical);
            var helperDistricts = new UIHelperExtension(m_tabDistricts, LayoutDirection.Vertical);

            var helperRoads = new UIHelperExtension(m_tabRoads, LayoutDirection.Vertical);


            AddTextField(Locale.Get("K45_WTS_ROADCORNER_NAME"), out m_name, helperSettings, OnSetName);

            helperSettings.AddSpace(5);

            AddFilterableInput(Locale.Get("K45_WTS_ROADCORNER_PROPLAYOUT"), helperSettings, out m_propLayoutSelect, out _, OnFilterLayouts, OnPropLayoutChange);

            AddVector3Field(Locale.Get("K45_WTS_ROADCORNER_POSITION"), out m_position, helperSettings, OnPositionChanged);
            AddVector3Field(Locale.Get("K45_WTS_ROADCORNER_ROTATION"), out m_rotation, helperSettings, OnRotationChanged);
            AddVector3Field(Locale.Get("K45_WTS_ROADCORNER_SCALE"), out m_scale, helperSettings, OnScaleChanged);
            AddLibBox<WTSLibRoadCornerRule, BoardInstanceRoadNodeXml>(helperSettings, out m_copySettings, OnCopyRule, out m_pasteSettings, OnPasteRule, out _, null, OnLoadRule, GetRuleSerialized);

            AddCheckboxLocale("K45_WTS_ROADCORNER_ALLOW_DIRTROADS", out m_allowDirty, helperRoads, (x) => ToggleAllow(Level.Level1, x));
            AddCheckboxLocale("K45_WTS_ROADCORNER_ALLOW_ALLEYS", out m_allowAlleys, helperRoads, (x) => ToggleAllow((Level)5, x));
            AddCheckboxLocale("K45_WTS_ROADCORNER_ALLOW_SMALLROADS", out m_allowSmallRoads, helperRoads, (x) => ToggleAllow(Level.Level2, x));
            AddCheckboxLocale("K45_WTS_ROADCORNER_ALLOW_MEDIUMROADS", out m_allowMediumRoads, helperRoads, (x) => ToggleAllow(Level.Level3, x));
            AddCheckboxLocale("K45_WTS_ROADCORNER_ALLOW_LARGEROADS", out m_allowLargeRoads, helperRoads, (x) => ToggleAllow(Level.Level4, x));
            AddCheckboxLocale("K45_WTS_ROADCORNER_ALLOW_HIGHWAYS", out m_allowHighways, helperRoads, (x) => ToggleAllow(Level.Level5, x));

            AddSlider(Locale.Get("K45_WTS_ROADCORNER_SPAWN_CHANCE"), out m_spawnChance, helperSpawning, OnChangeSpawnChance, 0, 255, 1, (x) => (x / 255).ToString("P0"));
            AddVector2Field(Locale.Get("K45_WTS_ROADCORNER_MINMAXHALFWIDTH"), out m_minMaxHalfWidth, helperSpawning, OnSetMinMaxHalfWidth);
            AddCheckboxLocale("K45_WTS_ROADCORNER_IGNOREEMPTYNAMES", out m_ignoreEmpty, helperSpawning, OnChangeIgnoreEmpty);

            KlyteMonoUtils.CreateUIElement(out m_spawnInCornerOptions, m_tabSpawning.transform, "spawnInCorner", new Vector4(0, 0, 620, 0));
            var helperSpawningCorner = new UIHelperExtension(m_spawnInCornerOptions, LayoutDirection.Vertical);
            helperSpawningCorner.Self.width = 620;
            AddCheckboxLocale("K45_WTS_ROADCORNER_PLACEALSOONDISTRICTBORDER", out m_placeDistrictBorder, helperSpawningCorner, OnChangeSpawnOnDistrictBorder);
            AddCheckboxLocale("K45_WTS_ROADCORNER_PLACEONTUNNELBRIDGESTART", out m_placeRoadTransition, helperSpawningCorner, OnChangePlaceRoadTransition);

            AddCheckboxLocale("K45_WTS_ROADCORNER_USEDISTRICTCOLOR", out m_useDistrictColor, helperAppearence, OnChangeUseDistrictColor);

            AddCheckboxLocale("K45_WTS_ROADCORNER_DISTRICTSELECTIONASWHITELIST", out m_districtWhiteList, helperDistricts, OnSetDistrictsAsWhitelist);
            AddCheckboxLocale("K45_WTS_ROADCORNER_DISTRICTSELECTIONASBLACKLIST", out m_districtBlackList, helperDistricts, OnSetDistrictsAsBlacklist);
            AddDropdown(Locale.Get("K45_WTS_ROADCORNER_DISTRICTRESTRICTIONSOLVEORDER"), out m_districtResolutionOrder, helperDistricts, ColossalUIExtensions.GetDropdownOptions<DistrictRestrictionOrder>("K45_WTS_DISTRICTRESTRICTIONORDER"), OnChangeDistrictRestrictionOrder);
            KlyteMonoUtils.CreateUIElement(out m_listContainer, helperDistricts.Self.transform, "previewPanel", new UnityEngine.Vector4(0, 0, helperDistricts.Self.width, helperDistricts.Self.height - 160));
            KlyteMonoUtils.CreateScrollPanel(m_listContainer, out m_districtList, out _, m_listContainer.width - 20, m_listContainer.height);
            m_districtList.backgroundSprite = "OptionsScrollbarTrack";
            m_districtList.autoLayout = true;
            m_districtList.autoLayoutDirection = LayoutDirection.Vertical;

            CreateTemplateDistrict();
            m_checkboxTemplateList = new UITemplateList<UIPanel>(m_districtList, DISTRICT_SELECTOR_TEMPLATE);


            WTSRoadCornerEditor.Instance.RuleList.EventSelectionChanged += OnChangeTab;
            MainContainer.isVisible = false;
            m_pasteSettings.isVisible = false;
        }

        private IEnumerator OnFilterLayouts(string input, Wrapper<string[]> result)
        {
            yield return WTSPropLayoutData.Instance.FilterBy(input, TextRenderingClass.RoadNodes, result);
        }

        private void UpdateDistrictList(ref BoardInstanceRoadNodeXml reference)
        {
            var districts = DistrictUtils.GetValidParks().ToDictionary(x => x.Key, x => 0x100 | x.Value).Concat(DistrictUtils.GetValidDistricts()).OrderBy(x => x.Value == 0 ? "" : x.Key).ToDictionary(x => x.Key, x => x.Value);
            ref DistrictPark[] parkBuffer = ref Singleton<DistrictManager>.instance.m_parks.m_buffer;
            UIPanel[] districtChecks = m_checkboxTemplateList.SetItemCount(districts.Count);

            for (int i = 0; i < districts.Count; i++)
            {
                string districtName = districts.Keys.ElementAt(i);
                UICheckBox checkbox = districtChecks[i].GetComponentInChildren<UICheckBox>();
                checkbox.stringUserData = districts[districtName].ToString();
                if (checkbox.label.objectUserData == null)
                {
                    checkbox.eventCheckChanged += (x, y) =>
                    {
                        SafeObtain((ref BoardInstanceRoadNodeXml z) =>
                        {
                            if (ushort.TryParse(x.stringUserData, out ushort districtIdx))
                            {
                                if (y)
                                {
                                    z.SelectedDistricts.Add(districtIdx);
                                }
                                else
                                {
                                    z.SelectedDistricts.Remove(districtIdx);
                                }
                            }
                        });

                    };
                    KlyteMonoUtils.LimitWidthAndBox(checkbox.label, m_districtList.width - 50);


                    checkbox.label.objectUserData = true;
                }
                checkbox.text = districtName;
                if (districts[districtName] >= 256)
                {
                    int parkId = districts[districtName] & 0xFF;
                    if (parkBuffer[parkId].IsCampus)
                    {
                        checkbox.tooltip = Locale.Get("MAIN_TOOL", "PlayerEducation");
                        checkbox.label.textColor = Color.cyan;
                    }
                    else if (parkBuffer[parkId].IsIndustry)
                    {
                        checkbox.tooltip = Locale.Get("PARKSOVERVIEW_TOOLTIP", "Industry");
                        checkbox.label.textColor = Color.yellow;
                    }
                    else if (parkBuffer[parkId].IsPark)
                    {
                        checkbox.tooltip = Locale.Get("PARKSOVERVIEW_TOOLTIP", "Generic");
                        checkbox.label.textColor = Color.green;
                    }
                    else
                    {
                        checkbox.tooltip = Locale.Get("MAIN_AREAS");
                        checkbox.label.textColor = Color.Lerp(Color.magenta, Color.blue, 0.5f);
                    }
                }
                else
                {
                    checkbox.tooltip = Locale.Get("TUTORIAL_ADVISER_TITLE", "District");
                    checkbox.label.textColor = Color.white;
                }

            }
            for (int i = 0; i < m_checkboxTemplateList.items.Count; i++)
            {
                UICheckBox checkbox = m_checkboxTemplateList.items[i].GetComponentInChildren<UICheckBox>();
                if (ushort.TryParse(checkbox.stringUserData, out ushort districtIdx))
                {
                    checkbox.isChecked = reference.SelectedDistricts.Contains(districtIdx);
                }
            }
        }

        private void CreateTemplateDistrict()
        {
            var go = new GameObject();
            UIPanel panel = go.AddComponent<UIPanel>();
            panel.size = new Vector2(m_districtList.width, 36);
            panel.autoLayout = true;
            panel.wrapLayout = false;
            panel.autoLayoutDirection = LayoutDirection.Horizontal;

            UICheckBox uiCheckbox = UIHelperExtension.AddCheckbox(panel, "AAAAAA", false);
            uiCheckbox.name = "AssetCheckbox";
            uiCheckbox.height = 29f;
            uiCheckbox.width = 290f;
            uiCheckbox.label.processMarkup = true;
            uiCheckbox.label.textScale = 0.8f;

            UITemplateUtils.GetTemplateDict()[DISTRICT_SELECTOR_TEMPLATE] = panel;
        }



        private delegate void SafeObtainMethod(ref BoardInstanceRoadNodeXml x);
        private void SafeObtain(SafeObtainMethod action, int? targetTab = null)
        {
            int effTargetTab = Math.Max(-1, targetTab ?? m_currentIdx);
            if (effTargetTab < 0)
            {
                return;
            }

            if (effTargetTab < WTSRoadNodesData.Instance.DescriptorRulesOrder.Length)
            {
                action(ref WTSRoadNodesData.Instance.DescriptorRulesOrder[effTargetTab]);
                WTSRoadNodesData.Instance.ResetCacheDescriptors();
            }
        }
        private void OnChangeTab(int obj)
        {
            MainContainer.isVisible = obj >= 0;
            m_currentIdx = obj;
            ReloadData();
        }

        private void ReloadData() => SafeObtain((ref BoardInstanceRoadNodeXml x) =>
                                   {
                                       m_name.text = x.SaveName ?? "";
                                       m_propLayoutSelect.text = x.PropLayoutName ?? "";
                                       m_position[0].text = x.PropPosition.X.ToString("F3");
                                       m_position[1].text = x.PropPosition.Y.ToString("F3");
                                       m_position[2].text = x.PropPosition.Z.ToString("F3");
                                       m_rotation[0].text = x.PropRotation.X.ToString("F3");
                                       m_rotation[1].text = x.PropRotation.Y.ToString("F3");
                                       m_rotation[2].text = x.PropRotation.Z.ToString("F3");
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
                                       m_minMaxHalfWidth[0].text = x.MinRoadHalfWidth.ToString("F3");
                                       m_minMaxHalfWidth[1].text = x.MaxRoadHalfWidth.ToString("F3");
                                       m_placeDistrictBorder.isChecked = x.PlaceOnDistrictBorder;
                                       m_placeRoadTransition.isChecked = x.PlaceOnTunnelBridgeStart;
                                       m_ignoreEmpty.isChecked = x.IgnoreEmptyNameRoads;

                                       m_useDistrictColor.isChecked = x.UseDistrictColor;

                                       m_districtWhiteList.isChecked = !x.SelectedDistrictsIsBlacklist;
                                       m_districtBlackList.isChecked = x.SelectedDistrictsIsBlacklist;

                                       UpdateDistrictList(ref x);
                                   });

        private string m_clipboard;

        private string GetRuleSerialized()
        {
            int effTargetTab = Math.Max(-1, m_currentIdx);
            if (effTargetTab >= 0 && effTargetTab < WTSRoadNodesData.Instance.DescriptorRulesOrder.Length)
            {
                return XmlUtils.DefaultXmlSerialize(WTSRoadNodesData.Instance.DescriptorRulesOrder[effTargetTab]);
            }
            else
            {
                return null;
            }
        }

        private void OnLoadRule(string obj) => SafeObtain((ref BoardInstanceRoadNodeXml x) =>
        {
            x = XmlUtils.DefaultXmlDeserialize<BoardInstanceRoadNodeXml>(obj);
            WTSRoadCornerEditor.Instance.RuleList.FixTabstrip();
            ReloadData();
        });
        private void OnPasteRule() => OnLoadRule(m_clipboard);
        private void OnCopyRule() => SafeObtain((ref BoardInstanceRoadNodeXml x) =>
        {
            m_clipboard = XmlUtils.DefaultXmlSerialize(x);
            m_pasteSettings.isVisible = true;
        });

        private void OnSetDistrictsAsBlacklist(bool isChecked) => SafeObtain((ref BoardInstanceRoadNodeXml x) => { x.SelectedDistrictsIsBlacklist = isChecked; m_districtWhiteList.isChecked = !isChecked; });
        private void OnSetDistrictsAsWhitelist(bool isChecked) => SafeObtain((ref BoardInstanceRoadNodeXml x) => { x.SelectedDistrictsIsBlacklist = !isChecked; m_districtBlackList.isChecked = !isChecked; });
        private void OnChangeDistrictRestrictionOrder(DistrictRestrictionOrder sel) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.DistrictRestrictionOrder = sel);
        private void OnChangeSpawnOnDistrictBorder(bool isChecked) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.PlaceOnDistrictBorder = isChecked);

        private void OnChangePlaceRoadTransition(bool isChecked) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.PlaceOnTunnelBridgeStart = isChecked);
        private void OnChangeIgnoreEmpty(bool isChecked) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.IgnoreEmptyNameRoads = isChecked);

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
        private void OnRotationChanged(Vector3 obj) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.PropRotation = (Vector3Xml)obj);
        private void OnScaleChanged(Vector3 obj) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.PropScale = obj);
        private void OnPositionChanged(Vector3 obj) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.PropPosition = (Vector3Xml)obj);
        private string OnPropLayoutChange(string input, int sel, string[] args)
        {
            if (sel == -1)
            {
                sel = Array.IndexOf(args, input);
            }
            string result = null;
            SafeObtain((ref BoardInstanceRoadNodeXml x) =>
            {
                if (sel >= 0)
                {
                    x.PropLayoutName = args[sel];
                }
                else
                {
                    x.PropLayoutName = null;
                }
                result = x.PropLayoutName;
            });
            return result;
        }

        private void OnChangeUseDistrictColor(bool isChecked) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.UseDistrictColor = isChecked);

        private void OnSetMinMaxHalfWidth(Vector2 obj) => SafeObtain((ref BoardInstanceRoadNodeXml x) =>
        {
            x.MinRoadHalfWidth = Mathf.Max(0, Mathf.Min(obj.x, obj.y, 999));
            x.MaxRoadHalfWidth = Mathf.Min(999, Mathf.Max(obj.x, obj.y, 0));

            m_minMaxHalfWidth[0].text = x.MinRoadHalfWidth.ToString("F3");
            m_minMaxHalfWidth[1].text = x.MaxRoadHalfWidth.ToString("F3");
        });
        private void OnChangeSpawnChance(float val) => SafeObtain((ref BoardInstanceRoadNodeXml x) => x.SpawnChance = (byte)val);
        private void OnSetName(string text) => SafeObtain((ref BoardInstanceRoadNodeXml x) =>
        {
            if (!text.IsNullOrWhiteSpace())
            {
                x.SaveName = text;
                WTSRoadCornerEditor.Instance.RuleList.FixTabstrip();
                OnChangeTab(m_currentIdx);
            }
            else
            {
                m_name.text = x.SaveName;
            }
        });
    }

}
