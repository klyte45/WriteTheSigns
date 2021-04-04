using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensions;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.UI.Sprites;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Libraries;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Singleton;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSBuildingLayoutEditorPropDetail : UICustomControl
    {
        private string CurrentBuildingName { get; set; }

        private ref BoardInstanceBuildingXml CurrentEdited => ref WTSBuildingLayoutEditor.Instance.LayoutList.CurrentPropLayout;
        private ConfigurationSource Source { get; set; }

        public UIPanel MainContainer { get; protected set; }

        private const string PLATFORM_SELECTION_TEMPLATE_NAME = "K45_WTS_PlataformSelectionTemplate";

        private bool m_dirty;
        public bool Dirty
        {
            get => m_dirty; set {
                if (value && MainContainer.isVisible)
                {
                    ReloadData();
                }
                else
                {
                    m_dirty = value;
                }
            }
        }

        private UITabstrip m_tabstrip;

        private UITextField m_name;
        private UIDropDown m_propSelectionType;
        private UITextField[] m_position;
        private UITextField[] m_rotation;
        private UITextField[] m_scale;

        private UICheckBox m_chkShowIfNoLine;
        private UITemplateList<UIPanel> m_checkboxTemplateList;
        private UIScrollablePanel m_platformList;

        private UITextField[] m_repeatArrayDistance;
        private UITextField m_repeatTimes;

        private UIDropDown m_colorModeDD;

        private UIDropDown m_loadDD;
        private UIButton m_copySettings;
        private UIButton m_pasteSettings;
        private string m_clipboard;
        private UIComponent[] m_allFields = null;
        private UIButton m_libLoad;
        private UIButton m_libDelete;
        private UITextField m_libSaveNameField;
        private UIButton m_libSave;
        private UIButton m_gotoFileLib;
        private UIDropDown m_subBuildingSelect;
        private UICheckBox m_chkUseFixedIfMulti;

        private UITextField m_propFilter;

        private IEnumerable<UIComponent> AllFields
        {
            get {
                if (m_allFields == null)
                {

                    m_allFields = new UIComponent[]
                        {
                            m_name,
                            m_propSelectionType,
                            m_chkShowIfNoLine,
                            m_repeatTimes,
                            m_colorModeDD,
                            m_pasteSettings,
                            m_libLoad,
                            m_subBuildingSelect,
                            m_chkUseFixedIfMulti,
                            m_propFilter
                }.Union(m_position).Union(m_rotation).Union(m_repeatArrayDistance).Union(m_scale).ToArray();
                }
                return m_allFields.Union(m_checkboxTemplateList.items.Select(x => x as UIComponent));
            }
        }


        public void Awake()
        {
            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.clipChildren = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.autoLayoutPadding = new RectOffset(0, 0, 4, 4);

            KlyteMonoUtils.CreateTabsComponent(out m_tabstrip, out UITabContainer m_tabContainer, MainContainer.transform, "TextEditor", new Vector4(0, 0, MainContainer.width, 40), new Vector4(0, 0, MainContainer.width, MainContainer.height - 40));
            UIPanel m_tabSettings = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Settings), "K45_WTS_BUILDINGEDITOR_BASIC_SETTINGS", "RcSettings");
            UIPanel m_tabPublicTransport = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, "InfoIconPublicTransport", "K45_WTS_BUILDINGEDITOR_PUBLICTRANSPORTSTATION_SETTINGS", "PublicTransport");
            UIPanel m_tabSpawning = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Reload), "K45_WTS_BUILDINGEDITOR_SPAWNING_SETTINGS", "RcSpawning");
            UIPanel m_tabAppearence = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_AutoColorIcon), "K45_WTS_BUILDINGEDITOR_APPEARANCE_SETTINGS", "RcAppearence");

            var helperSettings = new UIHelperExtension(m_tabSettings, LayoutDirection.Vertical);
            var helperPublicTransport = new UIHelperExtension(m_tabPublicTransport, LayoutDirection.Vertical);
            var helperSpawning = new UIHelperExtension(m_tabSpawning, LayoutDirection.Vertical);
            var helperAppearence = new UIHelperExtension(m_tabAppearence, LayoutDirection.Vertical);




            AddTextField(Locale.Get("K45_WTS_BUILDINGEDITOR_NAME"), out m_name, helperSettings, OnSetName);

            helperSettings.AddSpace(5);

            AddDropdown(Locale.Get("K45_WTS_BUILDINGEDITOR_PROPTYPE"), out m_propSelectionType, helperSettings, new string[] { Locale.Get("K45_WTS_BUILDINGEDITOR_PROPLAYOUT"), Locale.Get("K45_WTS_BUILDINGEDITOR_PROPMODELSELECT") }, OnPropSelecionClassChange);
            AddFilterableInput(Locale.Get("K45_WTS_BUILDINGEDITOR_MODELLAYOUTSELECT"), helperSettings, out m_propFilter, out _, OnFilterLayouts, OnConfigSelectionChange);

            AddVector3Field(Locale.Get("K45_WTS_BUILDINGEDITOR_POSITION"), out m_position, helperSettings, OnPositionChanged);
            AddVector3Field(Locale.Get("K45_WTS_BUILDINGEDITOR_ROTATION"), out m_rotation, helperSettings, OnRotationChanged);
            AddVector3Field(Locale.Get("K45_WTS_BUILDINGEDITOR_SCALE"), out m_scale, helperSettings, OnScaleChanged);
            AddDropdown(Locale.Get("K45_WTS_SUBBUILDINGPIVOTREFERENCE"), out m_subBuildingSelect, helperSettings, new string[0], OnSubBuildingRefChanged);


            AddLibBox<WTSLibBuildingPropLayout, BoardInstanceBuildingXml>(helperSettings, out m_copySettings, OnCopyRule, out m_pasteSettings, OnPasteRule, out _, null, out m_loadDD, out m_libLoad, out m_libDelete, out m_libSaveNameField, out m_libSave, out m_gotoFileLib, OnLoadRule, GetRuleSerialized);

            AddCheckboxLocale("K45_WTS_SHOW_IF_NO_LINE", out m_chkShowIfNoLine, helperPublicTransport, OnShowIfNoLineChanged);

            AddIntField(Locale.Get("K45_WTS_ARRAY_REPEAT_TIMES"), out m_repeatTimes, helperSpawning, OnRepeatTimesChanged, false);
            AddVector3Field(Locale.Get("K45_WTS_ARRAY_REPEAT_DISTANCE"), out m_repeatArrayDistance, helperSpawning, OnRepeatArrayDistanceChanged);

            AddDropdown(Locale.Get("K45_WTS_COLOR_MODE_SELECT"), out m_colorModeDD, helperAppearence, Enum.GetNames(typeof(ColoringMode)).Select(x => Locale.Get("K45_WTS_PROP_COLOR_MODE", x)).ToArray(), OnColoringModeChanged);
            AddCheckboxLocale("K45_WTS_USEFIXEDIFMULTILINES", out m_chkUseFixedIfMulti, helperAppearence, OnUseFixedIfMultiChanged);

            WTSBuildingLayoutEditor.Instance.LayoutList.EventSelectionChanged += OnChangeTab;
            MainContainer.isVisible = false;
            m_pasteSettings.isVisible = false;

            AddLabel(Locale.Get("K45_WTS_PLATFORM_ORDER_SELECTION"), helperPublicTransport, out _, out _);
            KlyteMonoUtils.CreateUIElement(out UIPanel listContainer, helperPublicTransport.Self.transform, "previewPanel", new UnityEngine.Vector4(0, 0, helperPublicTransport.Self.width - 10, helperPublicTransport.Self.height - 160));
            listContainer.padding = new RectOffset(5, 5, 5, 5);
            listContainer.autoLayout = true;
            listContainer.backgroundSprite = "OptionsScrollbarTrack";
            KlyteMonoUtils.CreateScrollPanel(listContainer, out m_platformList, out _, listContainer.width, listContainer.height - 10);
            m_platformList.autoLayout = true;
            m_platformList.autoLayoutDirection = LayoutDirection.Vertical;

            CreateTemplatePlatform();
            m_checkboxTemplateList = new UITemplateList<UIPanel>(m_platformList, PLATFORM_SELECTION_TEMPLATE_NAME);
        }


        private void CreateTemplatePlatform()
        {
            var go = new GameObject();
            UIPanel panel = go.AddComponent<UIPanel>();
            panel.size = new Vector2(m_platformList.width, 36);
            panel.autoLayout = true;
            panel.wrapLayout = false;
            panel.autoLayoutDirection = LayoutDirection.Horizontal;

            UICheckBox uiCheckbox = UIHelperExtension.AddCheckbox(panel, "AAAAAA", false);
            uiCheckbox.name = "AssetCheckbox";
            uiCheckbox.height = 40;
            uiCheckbox.width = 290f;
            uiCheckbox.label.processMarkup = true;
            uiCheckbox.label.textScale = 1f;
            uiCheckbox.label.padding.bottom = -20;
            UITemplateUtils.GetTemplateDict()[PLATFORM_SELECTION_TEMPLATE_NAME] = panel;
        }

        private void UpdatePlatformList(ref BoardInstanceBuildingXml reference)
        {
            var platforms = reference?.m_platforms ?? new int[0];
            var descriptorStops = WTSBuildingPropsSingleton.GetStopPointsDescriptorFor(CurrentBuildingName);
            UIPanel[] checks = m_checkboxTemplateList.SetItemCount(descriptorStops.Length);

            for (int i = 0; i < descriptorStops.Length; i++)
            {
                UICheckBox checkbox = checks[i].GetComponentInChildren<UICheckBox>();
                var platformData = new PlatformItem(descriptorStops[i], i);
                checkbox.objectUserData = platformData;
                if (checkbox.label.objectUserData == null)
                {
                    checkbox.eventCheckChanged += (x, y) => SafeObtain((ref BoardInstanceBuildingXml z) =>
                    {
                        if (x.objectUserData is PlatformItem pi)
                        {
                            if (y)
                            {
                                z.m_platforms = z.m_platforms.Union(new int[] { pi.index }).ToArray();
                                x.parent.zOrder = z.m_platforms.Length - 1;
                            }
                            else
                            {
                                z.m_platforms = z.m_platforms.Where(w => w != pi.index).ToArray();
                                x.parent.zOrder = z.m_platforms.Length;
                            }
                        }
                    });
                    KlyteMonoUtils.LimitWidthAndBox(checkbox.label, m_platformList.width - 50);


                    checkbox.label.objectUserData = true;
                }
                checkbox.text = platformData.ToString();
                checkbox.isChecked = false;
                checkbox.zOrder = i;
            }
            for (int i = 0; i < platforms.Length; i++)
            {
                if (platforms[i] < m_checkboxTemplateList.items.Count)
                {
                    UICheckBox checkbox = m_checkboxTemplateList.items[platforms[i]].GetComponentInChildren<UICheckBox>();
                    checkbox.isChecked = true;
                    checkbox.parent.zOrder = i;
                }
            }
        }


        private delegate void SafeObtainMethod(ref BoardInstanceBuildingXml x);
        private void SafeObtain(SafeObtainMethod action)
        {
            if (CurrentEdited != null)
            {
                action(ref CurrentEdited);
                WTSBuildingsData.Instance.CleanCache();
            }
        }
        private void OnChangeTab(string buildingName, ref BoardInstanceBuildingXml current, ConfigurationSource source)
        {
            CurrentBuildingName = buildingName;
            Source = source;
            MainContainer.isVisible = current != null;
            Dirty = true;
        }

        private void ReloadData()
        {
            SafeObtain((ref BoardInstanceBuildingXml x) =>
            {
                var isPublicTransportStation = PrefabCollection<BuildingInfo>.FindLoaded(CurrentBuildingName)?.m_buildingAI is TransportStationAI;

                m_name.text = x.SaveName ?? "";
                m_propSelectionType.selectedIndex = x.PropLayoutName == null ? 1 : x.SimpleProp == null ? 0 : 1; 
                m_propFilter.text = x.PropLayoutName ?? PropIndexes.GetListName(x.SimpleProp) ?? "";
                m_position[0].text = x.PropPosition.X.ToString("F3");
                m_position[1].text = x.PropPosition.Y.ToString("F3");
                m_position[2].text = x.PropPosition.Z.ToString("F3");
                m_rotation[0].text = x.PropRotation.X.ToString("F3");
                m_rotation[1].text = x.PropRotation.Y.ToString("F3");
                m_rotation[2].text = x.PropRotation.Z.ToString("F3");
                m_scale[0].text = x.PropScale.x.ToString("F3");
                m_scale[1].text = x.PropScale.y.ToString("F3");
                m_scale[2].text = x.PropScale.z.ToString("F3");

                var buildingInfo = PrefabCollection<BuildingInfo>.FindLoaded(CurrentBuildingName);
                if ((buildingInfo.m_subBuildings?.Length ?? 0) > 0)
                {
                    m_subBuildingSelect.items = new string[] { Locale.Get("K45_WTS_MAINBUILIDING") }.Union(buildingInfo.m_subBuildings?.Select((z, y) => $"{y}: {z.m_buildingInfo.name.Split(new char[] { '.' }, 2).LastOrDefault()}")).ToArray();
                    m_subBuildingSelect.selectedIndex = x.SubBuildingPivotReference + 1;
                }
                else
                {
                    m_subBuildingSelect.parent.isVisible = false;
                }

                m_chkShowIfNoLine.isChecked = x.m_showIfNoLine;

                m_repeatArrayDistance[0].text = x.ArrayRepeat.X.ToString("F3");
                m_repeatArrayDistance[1].text = x.ArrayRepeat.Y.ToString("F3");
                m_repeatArrayDistance[2].text = x.ArrayRepeat.Z.ToString("F3");

                m_repeatTimes.text = x.ArrayRepeatTimes.ToString();

                m_colorModeDD.selectedIndex = (int)x.ColorModeProp;

                UpdatePlatformList(ref x);

                m_chkUseFixedIfMulti.isChecked = x.UseFixedIfMultiline;

                m_chkUseFixedIfMulti.isVisible = x.ColorModeProp == ColoringMode.ByPlatform;




                if (Source != ConfigurationSource.CITY)
                {
                    AllFields.ForEach(y => y.Disable());
                }
                else
                {
                    AllFields.ForEach(y => y.Enable());
                }
                if (isPublicTransportStation)
                {
                    m_tabstrip.ShowTab("PublicTransport");
                }
                else
                {
                    m_tabstrip.HideTab("PublicTransport");
                }
            });
            Dirty = false;
        }


        private string[] OnFilterLayouts(string input) => m_propSelectionType.selectedIndex == 0 ? WTSPropLayoutData.Instance.FilterBy(input, TextRenderingClass.Buildings) : PropIndexes.instance.BasicInputFiltering(input);

        private string OnConfigSelectionChange(string typed, int sel, string[] items)
        {
            if (sel == -1)
            {
                sel = Array.IndexOf(items, typed?.Trim());
            }
            bool isValidSelection = sel >= 0 && sel < items.Length;
            string targetValue = isValidSelection ? items[sel] : "";

            SafeObtain((ref BoardInstanceBuildingXml x) =>
            {
                if (m_propSelectionType.selectedIndex == 0)
                {
                    x.PropLayoutName = targetValue;
                    x.m_simplePropName = null;
                }
                else
                {
                    x.PropLayoutName = null;
                    PropIndexes.instance.PrefabsLoaded.TryGetValue(targetValue, out PropInfo info);
                    x.SimpleProp = info;
                }
            });

            return targetValue;
        }


        private void OnPropSelecionClassChange(int sel) => SafeObtain((ref BoardInstanceBuildingXml x) => m_propFilter.text = sel == 0 ? x.PropLayoutName ?? "" : PropIndexes.GetListName(x.SimpleProp));

        public void Update()
        {
            if (WTSBuildingLayoutEditor.Instance.LayoutList.MainContainer.isVisible)
            {
                if (Dirty)
                {
                    MainContainer.isVisible = CurrentEdited != null;
                    if (CurrentEdited != null)
                    {
                        ReloadData();
                    }

                    Dirty = false;
                }
            }
        }




        private string GetRuleSerialized()
        {
            if (CurrentEdited != null)
            {
                return XmlUtils.DefaultXmlSerialize(CurrentEdited);
            }
            else
            {
                return null;
            }
        }

        private void OnLoadRule(string obj) => SafeObtain((ref BoardInstanceBuildingXml x) =>
        {
            x = XmlUtils.DefaultXmlDeserialize<BoardInstanceBuildingXml>(obj);
            WTSBuildingLayoutEditor.Instance.LayoutList.FixTabstrip();
            ReloadData();
        });
        private void OnPasteRule() => OnLoadRule(m_clipboard);
        private void OnCopyRule() => SafeObtain((ref BoardInstanceBuildingXml x) =>
        {
            m_clipboard = XmlUtils.DefaultXmlSerialize(x);
            m_pasteSettings.isVisible = true;
        });


        private void OnRotationChanged(Vector3 obj) => SafeObtain((ref BoardInstanceBuildingXml x) => x.PropRotation = (Vector3Xml)obj);
        private void OnScaleChanged(Vector3 obj) => SafeObtain((ref BoardInstanceBuildingXml x) => x.PropScale = obj);
        private void OnPositionChanged(Vector3 obj) => SafeObtain((ref BoardInstanceBuildingXml x) => x.PropPosition = (Vector3Xml)obj);
        private void OnShowIfNoLineChanged(bool isChecked) => SafeObtain((ref BoardInstanceBuildingXml x) => x.m_showIfNoLine = isChecked);
        private void OnSubBuildingRefChanged(int sel) => SafeObtain((ref BoardInstanceBuildingXml x) =>
        {
            if (sel >= 0)
            {
                x.SubBuildingPivotReference = sel - 1;
            }
        });

        private void OnRepeatArrayDistanceChanged(Vector3 obj) => SafeObtain((ref BoardInstanceBuildingXml x) => x.ArrayRepeat = (Vector3Xml)obj);
        private void OnRepeatTimesChanged(int obj) => SafeObtain((ref BoardInstanceBuildingXml x) => x.ArrayRepeatTimes = obj);

        private void OnColoringModeChanged(int sel) => SafeObtain((ref BoardInstanceBuildingXml x) =>
        {
            x.ColorModeProp = (ColoringMode)sel;
            m_chkUseFixedIfMulti.isVisible = x.ColorModeProp == ColoringMode.ByPlatform;
        });
        private void OnUseFixedIfMultiChanged(bool isChecked) => SafeObtain((ref BoardInstanceBuildingXml x) => x.UseFixedIfMultiline = isChecked);


        private void OnSetName(string text) => SafeObtain((ref BoardInstanceBuildingXml x) =>
        {
            if (!text.IsNullOrWhiteSpace())
            {
                x.SaveName = text;
                WTSBuildingLayoutEditor.Instance.LayoutList.FixTabstrip();
                Dirty = true;
            }
            else
            {
                m_name.text = x.SaveName;
            }
        });
        private class PlatformItem
        {
            private Color32 OwnColor => WTSBuildingPropsSingleton.m_colorOrder[index % WTSBuildingPropsSingleton.m_colorOrder.Length];

            public StopSearchUtils.StopPointDescriptorLanes descriptorLane;
            public int index;
            public override string ToString() => $"<k45Symbol {KlyteResourceLoader.GetDefaultSpriteNameFor(LineIconSpriteNames.K45_CircleIcon)},{OwnColor.ToRGB()},{index}>{descriptorLane.vehicleType} @ {descriptorLane.platformLine.GetBounds().center}";

            public PlatformItem(StopSearchUtils.StopPointDescriptorLanes item, int index)
            {
                descriptorLane = item;
                this.index = index;
            }
        }

        #region Simple Prop selection



        private string OnSetProp(string typed, int sel, string[] items)
        {
            if (sel >= 0)
            {
                PropIndexes.instance.PrefabsLoaded.TryGetValue(items[sel], out PropInfo targetProp);
                SafeObtain((ref BoardInstanceBuildingXml x) => x.SimpleProp = targetProp);
                return items[sel];
            }
            else
            {
                SafeObtain((ref BoardInstanceBuildingXml x) => x.SimpleProp = null);
                return null;
            }

        }


        #endregion


    }


}
