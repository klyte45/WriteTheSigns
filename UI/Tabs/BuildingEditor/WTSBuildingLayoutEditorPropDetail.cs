using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
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
        private BuildingGroupDescriptorXml CurrentEdited => WTSBuildingLayoutEditor.Instance.CurrentEditingInstance;
        private ConfigurationSource Source => WTSBuildingLayoutEditor.Instance.CurrentConfigurationSource;
        public UIPanel MainContainer { get; protected set; }

        private const string PLATFORM_SELECTION_TEMPLATE_NAME = "K45_WTS_PlataformSelectionTemplate";

        private int m_currentIdx = -1;
        private bool m_dirty;

        private UITabstrip m_tabstrip;

        private UITextField m_name;
        private UIDropDown m_propLayoutSelect;
        private UITextField[] m_position;
        private UITextField[] m_rotation;
        private UITextField[] m_scale;

        private UICheckBox m_chkShowIfNoLine;
        private UITemplateList<UIPanel> m_checkboxTemplateList;
        private UIScrollablePanel m_platformList;

        private UITextField[] m_repeatArrayDistance;
        private UITextField m_repeatTimes;

        private UIDropDown m_colorModeDD;

        private UIPanel m_clipboardArea;
        private UIButton m_copySettings;
        private UIButton m_pasteSettings;
        private string m_clipboard;
        private UIComponent[] m_allFields = null;

        private IEnumerable<UIComponent> AllFields
        {
            get {
                if (m_allFields == null)
                {

                    m_allFields = new UIComponent[]
                        {
                            m_name,
                            m_propLayoutSelect,
                            m_chkShowIfNoLine,
                            m_repeatTimes,
                            m_colorModeDD
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

            AddDropdown(Locale.Get("K45_WTS_BUILDINGEDITOR_PROPLAYOUT"), out m_propLayoutSelect, helperSettings, new string[0], OnPropLayoutChange);
            AddButtonInEditorRow(m_propLayoutSelect, CommonsSpriteNames.K45_Reload, LoadAvailableLayouts);
            AddVector3Field(Locale.Get("K45_WTS_BUILDINGEDITOR_POSITION"), out m_position, helperSettings, OnPositionChanged);
            AddVector3Field(Locale.Get("K45_WTS_BUILDINGEDITOR_ROTATION"), out m_rotation, helperSettings, OnRotationChanged);
            AddVector3Field(Locale.Get("K45_WTS_BUILDINGEDITOR_SCALE"), out m_scale, helperSettings, OnScaleChanged);

            var clipboardSettings = helperSettings.AddGroupExtended("!!!", out UILabel dummy, out m_clipboardArea);
            Destroy(dummy);

            AddLibBox<WTSLibBuildingPropLayout, BoardInstanceBuildingXml>(clipboardSettings, out m_copySettings, OnCopyRule, out m_pasteSettings, OnPasteRule, out _, null, OnLoadRule, GetRuleSerialized);

            AddCheckboxLocale("K45_WTS_SHOW_IF_NO_LINE", out m_chkShowIfNoLine, helperPublicTransport, OnShowIfNoLineChanged);

            AddIntField(Locale.Get("K45_WTS_ARRAY_REPEAT_TIMES"), out m_repeatTimes, helperSpawning, OnRepeatTimesChanged, false);
            AddVector3Field(Locale.Get("K45_WTS_ARRAY_REPEAT_DISTANCE"), out m_repeatArrayDistance, helperSpawning, OnRepeatArrayDistanceChanged);

            AddDropdown(Locale.Get("K45_WTS_COLOR_MODE_SELECT"), out m_colorModeDD, helperAppearence, Enum.GetNames(typeof(ColoringMode)).Select(x => Locale.Get("K45_WTS_PROP_COLOR_MODE", x)).ToArray(), OnColoringModeChanged);

            WTSBuildingLayoutEditor.Instance.LayoutList.EventSelectionChanged += OnChangeTab;
            LoadAvailableLayouts();
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

        private void LoadAvailableLayouts()
        {
            m_propLayoutSelect.items = WTSPropLayoutData.Instance.ListWhere(x => x.m_allowedRenderClass == TextRenderingClass.Buildings).ToArray();
            SafeObtain((ref BoardInstanceBuildingXml x) => m_propLayoutSelect.selectedIndex = Math.Max(0, Array.IndexOf(m_propLayoutSelect.items, x.PropLayoutName)));
        }


        private void UpdatePlatformList(ref BoardInstanceBuildingXml reference)
        {
            var platforms = reference?.m_platforms ?? new int[0];
            var descriptorStops = WTSBuildingPropsSingleton.GetStopPointsDescriptorFor(CurrentEdited.BuildingName);
            UIPanel[] checks = m_checkboxTemplateList.SetItemCount(descriptorStops.Length);

            for (int i = 0; i < descriptorStops.Length; i++)
            {
                UICheckBox checkbox = checks[i].GetComponentInChildren<UICheckBox>();
                var platformData = new PlatformItem(descriptorStops[i], i);
                checkbox.objectUserData = platformData;
                if (checkbox.label.objectUserData == null)
                {
                    checkbox.eventCheckChanged += (x, y) =>
                    {
                        SafeObtain((ref BoardInstanceBuildingXml z) =>
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
                                    z.m_platforms = z.m_platforms.Where(x => x != pi.index).ToArray();
                                    x.parent.zOrder = z.m_platforms.Length;
                                }
                            }
                        });

                    };
                    KlyteMonoUtils.LimitWidthAndBox(checkbox.label, m_platformList.width - 50);


                    checkbox.label.objectUserData = true;
                }
                checkbox.text = platformData.ToString();
                checkbox.isChecked = false;
                checkbox.zOrder = i;
            }
            for (int i = 0; i < platforms.Length; i++)
            {
                UICheckBox checkbox = m_checkboxTemplateList.items[platforms[i]].GetComponentInChildren<UICheckBox>();
                checkbox.isChecked = true;
                checkbox.parent.zOrder = i;
            }
        }


        private delegate void SafeObtainMethod(ref BoardInstanceBuildingXml x);
        private void SafeObtain(SafeObtainMethod action, int? targetTab = null)
        {
            int effTargetTab = Math.Max(-1, targetTab ?? m_currentIdx);
            if (effTargetTab < 0)
            {
                return;
            }

            if (effTargetTab < CurrentEdited.PropInstances.Length)
            {
                action(ref CurrentEdited.PropInstances[effTargetTab]);
            }
        }
        private void OnChangeTab(int obj)
        {
            MainContainer.isVisible = obj >= 0;
            m_currentIdx = obj;
            m_dirty = true;
        }

        //    [XmlArray("platformOrder")]
        //    [XmlArrayItem("p")]
        //    public int[] m_platforms = new int[0];
        //    [XmlAttribute("showIfNoLine")]
        //    public bool m_showIfNoLine = true;
        //
        //    [XmlElement("arrayRepeatOffset")]
        //    public Vector3Xml ArrayRepeat { get; set; }
        //
        //
        //    [XmlAttribute("arrayRepeatTimes")]
        //    public int m_arrayRepeatTimes = 0;
        //
        //    [XmlAttribute("coloringMode")]
        //    public ColoringMode ColorModeProp { get; set; } = ColoringMode.Fixed;

        private void ReloadData()
        {
            SafeObtain((ref BoardInstanceBuildingXml x) =>
            {
                var isPublicTransportStation = PrefabCollection<BuildingInfo>.FindLoaded(CurrentEdited.BuildingName)?.m_buildingAI is TransportStationAI;

                m_name.text = x.SaveName ?? "";
                m_propLayoutSelect.selectedValue = x.PropLayoutName;
                m_position[0].text = x.PropPosition.X.ToString("F3");
                m_position[1].text = x.PropPosition.Y.ToString("F3");
                m_position[2].text = x.PropPosition.Z.ToString("F3");
                m_rotation[0].text = x.PropRotation.X.ToString("F3");
                m_rotation[1].text = x.PropRotation.Y.ToString("F3");
                m_rotation[2].text = x.PropRotation.Z.ToString("F3");
                m_scale[0].text = x.PropScale.x.ToString("F3");
                m_scale[1].text = x.PropScale.y.ToString("F3");
                m_scale[2].text = x.PropScale.z.ToString("F3");

                m_chkShowIfNoLine.isChecked = x.m_showIfNoLine;

                m_repeatArrayDistance[0].text = x.ArrayRepeat.X.ToString("F3");
                m_repeatArrayDistance[1].text = x.ArrayRepeat.Y.ToString("F3");
                m_repeatArrayDistance[2].text = x.ArrayRepeat.Z.ToString("F3");

                m_repeatTimes.text = x.m_arrayRepeatTimes.ToString();

                m_colorModeDD.selectedIndex = (int)x.ColorModeProp;

                UpdatePlatformList(ref x);







                if (Source != ConfigurationSource.CITY)
                {
                    AllFields.ForEach(x => x.Disable());
                    m_clipboardArea.isVisible = false;
                }
                else
                {
                    AllFields.ForEach(x => x.Enable());
                    m_clipboardArea.isVisible = true;
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
        }

        public void Update()
        {
            if (WTSBuildingLayoutEditor.Instance.LayoutList.MainContainer.isVisible)
            {
                if (m_dirty)
                {
                    ReloadData();
                    m_dirty = false;
                }
            }
        }




        private string GetRuleSerialized()
        {
            int effTargetTab = Math.Max(-1, m_currentIdx);
            if (effTargetTab >= 0 && effTargetTab < CurrentEdited.PropInstances.Length)
            {
                return XmlUtils.DefaultXmlSerialize(CurrentEdited.PropInstances[effTargetTab]);
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

        private void OnRepeatArrayDistanceChanged(Vector3 obj) => SafeObtain((ref BoardInstanceBuildingXml x) => x.ArrayRepeat = (Vector3Xml)obj);
        private void OnRepeatTimesChanged(int obj) => SafeObtain((ref BoardInstanceBuildingXml x) => x.m_arrayRepeatTimes = obj);

        private void OnColoringModeChanged(int sel) => SafeObtain((ref BoardInstanceBuildingXml x) => x.ColorModeProp = (ColoringMode)sel);
        private void OnPropLayoutChange(int sel) => SafeObtain((ref BoardInstanceBuildingXml x) =>
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

        private void OnSetName(string text) => SafeObtain((ref BoardInstanceBuildingXml x) =>
        {
            if (!text.IsNullOrWhiteSpace())
            {
                x.SaveName = text;
                WTSBuildingLayoutEditor.Instance.LayoutList.FixTabstrip();
                OnChangeTab(m_currentIdx);
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
    }


}
