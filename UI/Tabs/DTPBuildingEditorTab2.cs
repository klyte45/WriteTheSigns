using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Data;
using Klyte.DynamicTextProps.Libraries;
using Klyte.DynamicTextProps.Overrides;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorBuildings;

namespace Klyte.DynamicTextProps.UI
{

    internal class DTPBuildingEditorTab2 : DTPXmlEditorParentTab<BoardGeneratorBuildings, BoardBunchContainerBuilding, DTPBuildingsData, BoardDescriptorBuildingXml, BoardTextDescriptorBuildingsXml, DTPLibTextMeshBuildingSigns>
    {

        private UIButton m_buttonTool;

        private UIButton m_saveOnAssetFolderButton;


        private UIHelperExtension m_contentContainer;
        private UITextField m_mappingThresold;
        private UITabstripAutoResize m_pseudoTabstripProps;

        private UIHelperExtension m_pseudoTabPropsHelper;
        private UILabel m_buildingName;

        private UITextField m_propItemName;
        private UICheckBox m_showIfNoLineCheck;
        private UITextField[] m_posVectorEditor;
        private UITextField[] m_rotVectorEditor;
        private UITextField[] m_scaleVectorEditor;
        private UITextField[] m_arrayRepeatEditor;
        private UITextField m_arrayTimes;


        private UIDropDown m_colorModeDropdown;
        private UIColorField m_colorEditor;
        private UICheckBox m_useContrastColorTextCheckbox;
        private UICheckBox m_allCapsCheckbox;
        private UITextField m_textPrefix;
        private UITextField m_textSuffix;
        private CheckboxOrdernatedListPlatformItem m_checkboxOrdernatedListPlatform;

        private UIDropDown m_overrideFontText;

        private UIButton m_pasteGroupButton;
        private UIButton m_pasteButton;

        private UIDropDown m_loadPropItem;

        public string m_currentBuildingName;
        private int CurrentTab
        {
            get => m_pseudoTabstripProps.selectedIndex;
            set => m_pseudoTabstripProps.selectedIndex = value;
        }

        protected override BoardTextDescriptorBuildingsXml[] CurrentSelectedDescriptorArray
        {
            get => BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[CurrentTab].m_textDescriptors;
            set => BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[CurrentTab].m_textDescriptors = value;
        }

        private string m_clipboard;
        private string m_clipboardGroup;

        protected override string GetFontLabelString() => Locale.Get("K45_DTP_FONT_STATIONS");
        protected override bool IsTextEditionAvailable() => m_currentBuildingName != null && CurrentTab >= 0;
        protected override void SetPropModel(int idx)
        {
            if (idx > 0)
            {
                SafeActionInBoard(descriptor => descriptor.m_propName = BoardGeneratorHighwaySigns.Instance.LoadedProps[idx - 1]);
            }
            else if (idx == 0)
            {
                SafeActionInBoard(descriptor => descriptor.m_propName = null);
            }
        }

        #region Awake
        protected override void AwakePropEditor(out UIScrollablePanel scrollTabs, out UIHelperExtension referenceHelperTabs)
        {


            m_buttonTool = (UIButton) m_uiHelperHS.AddButton(Locale.Get("K45_DTP_PICK_A_BUILDING"), EnablePickTool);
            KlyteMonoUtils.LimitWidth(m_buttonTool, (m_uiHelperHS.Self.width - 20), true);

            m_contentContainer = m_uiHelperHS.AddGroupExtended(Locale.Get("K45_DTP_PICKED_BUILDING_DATA"));
            ((UIPanel) m_contentContainer.Self).backgroundSprite = "";
            m_contentContainer.Self.width = MainContainer.width - 30;

            m_buildingName = m_contentContainer.Self.parent.GetComponentInChildren<UILabel>();
            m_buildingName.prefix = Locale.Get("K45_DTP_BUILDING_NAME_LABEL_PREFIX") + ": ";
            KlyteMonoUtils.LimitWidth(m_buildingName, m_contentContainer.Self.width, true);

            m_loadPropGroup = AddLibBox<DTPLibPropGroupBuildingSigns, BuildingGroupDescriptorXml>(Locale.Get("K45_DTP_PROP_GROUP_LIB_TITLE"), m_contentContainer,
            out UIButton m_copyGroupButton, DoCopyGroup,
            out m_pasteGroupButton, DoPasteGroup,
            out UIButton m_deleteGroupButton, DoDeleteGroup,
            (x) =>
                {
                    if (m_currentBuildingName == null)
                    {
                        return;
                    }

                    BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName] = XmlUtils.DefaultXmlDeserialize<BuildingGroupDescriptorXml>(XmlUtils.DefaultXmlSerialize(x));
                    BoardGeneratorBuildings.Instance.OnDescriptorChanged();
                    ReloadBuilding();
                    OnChangeTab(-1);
                },
                () => BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName],
                (helper) =>
                {
                    helper.AddButton(Locale.Get("K45_DTP_SAVE_OVERRIDE_FOLDER"), () => BoardGeneratorBuildings.SaveInCommonFolder(m_currentBuildingName));
                    m_saveOnAssetFolderButton = (UIButton) helper.AddButton(Locale.Get("K45_DTP_SAVE_ASSET_FOLDER"), () => BoardGeneratorBuildings.SaveInAssetFolder(m_currentBuildingName));
                    helper.AddButton(Locale.Get("K45_DTP_RELOAD_CONFIGS"), LoadAllBuildingConfigurations);
                });
            AddFloatField(Locale.Get("K45_DTP_PLATFORM_MAPPING_DISTANCE_THRESOLD"), out m_mappingThresold, m_contentContainer, SetMappingThresold, false);

            KlyteMonoUtils.CreateHorizontalScrollPanel(m_contentContainer.Self, out scrollTabs, out UIScrollbar bar, m_contentContainer.Self.width - 20, 40, Vector3.zero);
            KlyteMonoUtils.CreateUIElement(out m_pseudoTabstripProps, scrollTabs.transform, "DTPTabstrip", new Vector4(5, 40, scrollTabs.width - 10, 40));
            m_pseudoTabstripProps.startSelectedIndex = -1;
            m_pseudoTabstripProps.selectedIndex = -1;
            m_pseudoTabstripProps.closeOnReclick = true;
            m_pseudoTabstripProps.AutoFitChildrenHorizontally = true;

            m_pseudoTabstripProps.AddTab("+", CreateTabTemplate(), false);

            m_pseudoTabstripProps.eventSelectedIndexChanged += (x, idx) =>
            {
                if (idx == m_pseudoTabstripProps.tabCount - 1)
                {
                    int nextIdx = BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors?.Length ?? 0;
                    LogUtils.DoLog($"nextIdx = {nextIdx}");
                    EnsureBoardsArrayIdx(nextIdx);
                    ReloadBuilding();
                    OnChangeTab(nextIdx);
                    ReloadTabInfo();
                }
                else
                {
                    ReloadTabInfo();
                }
            };

            m_pseudoTabPropsHelper = m_uiHelperHS.AddGroupExtended(Locale.Get("K45_DTP_PROP_CONFIGURATION"), out UILabel lbl, out UIPanel voide3);
            Destroy(lbl);
            ((UIPanel) m_pseudoTabPropsHelper.Self).backgroundSprite = "";

            m_pseudoTabPropsHelper.Self.eventVisibilityChanged += (x, y) => { if (y) { ReloadTabInfo(); } };
            m_pseudoTabPropsHelper.Self.width = MainContainer.width - 30;


            m_loadPropItem = AddLibBox<DTPLibPropSingleBuildingSigns, BoardDescriptorBuildingXml>(Locale.Get("K45_DTP_PROP_ITEM_LIB_TITLE"), m_pseudoTabPropsHelper,
                                    out UIButton m_copyButton, DoCopy,
                                    out m_pasteButton, DoPaste,
                                    out UIButton m_deleteButton, DoDelete,
                                    (x) =>
                                    {
                                        if (m_currentBuildingName == null || CurrentTab < 0)
                                        {
                                            return;
                                        }

                                        BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[CurrentTab] = XmlUtils.DefaultXmlDeserialize<BoardDescriptorBuildingXml>(XmlUtils.DefaultXmlSerialize(x));
                                        BoardGeneratorBuildings.Instance.OnDescriptorChanged();
                                        ReloadTabInfo();
                                    },
                            () => BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors?.ElementAtOrDefault(CurrentTab));


            UIHelperExtension groupProp = m_pseudoTabPropsHelper.AddTogglableGroup(Locale.Get("K45_DTP_PROP_CONFIGURATION"));
            AddTextField(Locale.Get("K45_DTP_PROP_TAB_TITLE"), out m_propItemName, groupProp, SetPropItemName);

            AddDropdown(Locale.Get("K45_DTP_PROP_MODEL_SELECT"), out m_propsDropdown, groupProp, new string[0], SetPropModel);


            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_POS"), out m_posVectorEditor, groupProp, SetPropRelPosition);
            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_ROT"), out m_rotVectorEditor, groupProp, SetPropRelRotation);
            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_SCALE"), out m_scaleVectorEditor, groupProp, SetPropRelScale);
            AddIntField(Locale.Get("K45_DTP_ARRAY_REPEAT_TIMES"), out m_arrayTimes, groupProp, SetArrayRepeatTimes, false);
            AddVector3Field(Locale.Get("K45_DTP_ARRAY_REPEAT_DISTANCE"), out m_arrayRepeatEditor, groupProp, SetArrayRepeatDistance);



            UIHelperExtension groupPropColorMode = m_pseudoTabPropsHelper.AddTogglableGroup(Locale.Get("K45_DTP_PROP_COLOR"));
            AddDropdown(Locale.Get("K45_DTP_COLOR_MODE_SELECT"), out m_colorModeDropdown, groupPropColorMode, Enum.GetNames(typeof(ColoringMode)).Select(x => Locale.Get("K45_DTP_PROP_COLOR_MODE", x)).ToArray(), SetColoringMode);
            m_colorModeDropdown.selectedIndex = -1;
            m_colorEditor = groupPropColorMode.AddColorPicker(Locale.Get("K45_DTP_PAINTING_COLOR"), Color.white, SetPropColor);
            KlyteMonoUtils.LimitWidth(m_colorEditor.parent.GetComponentInChildren<UILabel>(), groupPropColorMode.Self.width / 2, true);
            groupPropColorMode.AddCheckboxOrdenatedList<CheckboxOrdernatedListPlatformItem, PlatformItem>(out m_checkboxOrdernatedListPlatform, Locale.Get("K45_DTP_PLATFORM_ORDER_SELECTION"), SetPlatformOrder);
            m_showIfNoLineCheck = groupPropColorMode.AddCheckboxLocale("K45_DTP_SHOW_IF_NO_LINE", false, SetShowIfNoLine);
            m_colorModeDropdown.eventSelectedIndexChanged += (e, idx) =>
            {
                m_colorEditor.GetComponentInParent<UIPanel>().isVisible = idx == (int) ColoringMode.Fixed;
                m_showIfNoLineCheck.isVisible = idx == (int) ColoringMode.ByPlatform;
                m_checkboxOrdernatedListPlatform.transform.parent.GetComponentInParent<UIPanel>().isVisible = idx == (int) ColoringMode.ByPlatform;
            };


            KlyteMonoUtils.CreateHorizontalScrollPanel(m_pseudoTabPropsHelper.Self, out scrollTabs, out bar, m_pseudoTabPropsHelper.Self.width - 20, 40, Vector3.zero);
            referenceHelperTabs = m_pseudoTabPropsHelper;

        }

        protected override void OnTextTabStripChanged()
        {
            EnsureBoardsArrayIdx(-1, BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors?.ElementAtOrDefault(CurrentTab)?.m_textDescriptors?.Length ?? 0);
            ReloadTabInfo();
            OnChangeTabTexts(BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors.ElementAtOrDefault(CurrentTab).m_textDescriptors.Length - 1);
        }
        protected override string GetLocaleNameForContentTypes() => "K45_DTP_OWN_NAME_CONTENT_BUILDING";
        protected override void OnDropdownTextTypeSelectionChanged(int idx)
        {
            bool isFixedMode = BoardGeneratorBuildings.AVAILABLE_TEXT_TYPES[idx] == TextType.Fixed;
            m_overrideFontText.parent.isVisible = isFixedMode;
            m_allCapsCheckbox.isVisible = !isFixedMode;
            m_textPrefix.GetComponentInParent<UIPanel>().isVisible = !isFixedMode;
            m_textSuffix.GetComponentInParent<UIPanel>().isVisible = !isFixedMode;
        }

        protected override void OnLoadTextLibItem() => ReloadBuilding();
        protected override void DoInTextCommonTabGroupUI(UIHelperExtension groupTexts)
        {
            base.DoInTextCommonTabGroupUI(groupTexts);
            m_useContrastColorTextCheckbox = groupTexts.AddCheckboxLocale("K45_DTP_USE_CONTRAST_COLOR", false, SetUseContrastColor);
            m_allCapsCheckbox = groupTexts.AddCheckboxLocale("K45_DTP_TEXT_ALL_CAPS", false, SetAllCaps);
            AddTextField(Locale.Get("K45_DTP_PREFIX"), out m_textPrefix, groupTexts, SetPrefix);
            AddTextField(Locale.Get("K45_DTP_SUFFIX"), out m_textSuffix, groupTexts, SetSuffix);
            AddDropdown(Locale.Get("K45_DTP_OVERRIDE_FONT"), out m_overrideFontText, groupTexts, new string[0], SetOverrideFont);
        }
        protected override void PostAwake() => OnBuildingSet(null);
        #endregion

        #region Lib Actions
        private void DoCopy()
        {
            if (m_currentBuildingName != null && CurrentTab >= 0)
            {
                m_clipboard = XmlUtils.DefaultXmlSerialize(BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[CurrentTab]);
                m_pasteButton.isVisible = true;
            }
        }

        private void DoPaste()
        {
            if (m_currentBuildingName != null && CurrentTab >= 0 && m_clipboard != null)
            {
                BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[CurrentTab] = XmlUtils.DefaultXmlDeserialize<BoardDescriptorBuildingXml>(m_clipboard);
                BoardGeneratorBuildings.Instance.OnDescriptorChanged();
                ReloadTabInfo();
            }
        }

        private void DoDelete()
        {
            if (m_currentBuildingName != null && CurrentTab >= 0)
            {
                LogUtils.DoLog($"  BoardGeneratorBuildings.m_loadedDescriptors[m_currentSelectedSegment].m_boardsData.length = {  BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors.Length }");
                var tempList = BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors.ToList();
                tempList.RemoveAt(CurrentTab);
                BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors = tempList.ToArray();
                LogUtils.DoLog($"  BoardGeneratorBuildings.m_loadedDescriptors[m_currentSelectedSegment].m_boardsData.length pos = {  BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors.Length }");

                BoardGeneratorBuildings.Instance.OnDescriptorChanged();
                OnBuildingSet(m_currentBuildingName);
            }
        }

        private void DoCopyGroup()
        {
            if (m_currentBuildingName != null)
            {
                m_clipboardGroup = XmlUtils.DefaultXmlSerialize(BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]);
                m_pasteGroupButton.isVisible = true;
            }
        }

        private void DoPasteGroup()
        {
            if (m_currentBuildingName != null && m_clipboardGroup != null)
            {
                BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName] = XmlUtils.DefaultXmlDeserialize<BuildingGroupDescriptorXml>(m_clipboardGroup);
                ReloadBuilding();
            }
        }

        private void DoDeleteGroup()
        {
            if (m_currentBuildingName != null)
            {
                BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName] = new BuildingGroupDescriptorXml();
                OnBuildingSet(m_currentBuildingName);
            }
        }

        #endregion

        #region UI Actions

        private void LoadAllBuildingConfigurations() => BoardGeneratorBuildings.Instance.LoadAllBuildingConfigurations();

        private void SetMappingThresold(float f)
        {
            if (m_currentBuildingName != null && !m_isLoading)
            {
                if (!LoadedDescriptors.ContainsKey(m_currentBuildingName))
                {
                    LoadedDescriptors[m_currentBuildingName] = new BuildingGroupDescriptorXml();
                }
                LoadedDescriptors[m_currentBuildingName].StopMappingThresold = f;
                BoardGeneratorBuildings.ClearStopMapping(m_currentBuildingName);
            }
        }
        private void SetShowIfNoLine(bool val) => SafeActionInBoard(descriptor => descriptor.m_showIfNoLine = val);
        private void SetPlatformOrder(List<PlatformItem> val) => SafeActionInBoard(descriptor => descriptor.m_platforms = val.Select(x => x.index).ToArray());
        private void SetPropItemName(string txt)
        {
            SafeActionInBoard(descriptor =>
            {
                descriptor.SaveName = txt;
                EnsureTabQuantity(BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors?.Length ?? -1);
            });
        }
        private void SetColoringMode(int mode) => SafeActionInBoard(descriptor => descriptor.ColorModeProp = (ColoringMode) mode);
        private void SetPropColor(Color color) => SafeActionInBoard(descriptor => descriptor.FixedColor = color);
        private void SetPropRelPosition(Vector3 value) => SafeActionInBoard(descriptor => descriptor.m_propPosition = value);
        private void SetPropRelRotation(Vector3 value) => SafeActionInBoard(descriptor => descriptor.m_propRotation = value);
        private void SetPropRelScale(Vector3 value) => SafeActionInBoard(descriptor => descriptor.PropScale = value);
        private void SetArrayRepeatDistance(Vector3 value) => SafeActionInBoard(descriptor => descriptor.ArrayRepeat = value);
        private void SetArrayRepeatTimes(int value) => SafeActionInBoard(descriptor => descriptor.m_arrayRepeatTimes = value);
        private void SafeActionInBoard(Action<BoardDescriptorBuildingXml> toDo)
        {
            if (m_currentBuildingName != null && !m_isLoading)
            {
                EnsureBoardsArrayIdx(CurrentTab);
                BoardDescriptorBuildingXml descriptor = BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors[CurrentTab];
                toDo(descriptor);
            }
        }

        protected void SetAllCaps(bool val) => SafeActionInTextBoard(descriptor => descriptor.m_allCaps = val);
        protected void SetPrefix(string prefix) => SafeActionInTextBoard(descriptor => descriptor.m_prefix = prefix);
        protected void SetSuffix(string suffix) => SafeActionInTextBoard(descriptor => descriptor.m_suffix = suffix);

        private void SetOverrideFont(int idx)
        {
            SafeActionInTextBoard(descriptor =>
            {
                descriptor.m_overrideFont = idx > 0 ? m_overrideFontText.selectedValue : null;
                descriptor.GeneratedFixedTextRenderInfo = null;
            });
        }

        #endregion
        #region Load Data Ensure
        private void EnsureBoardsArrayIdx(int idx, int textIdx = -1)
        {
            if (idx >= 0)
            {
                if (BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName] == null)
                {
                    BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName] = new BuildingGroupDescriptorXml();
                }
                if (BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors == null || BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors.Length <= idx)
                {
                    BoardDescriptorBuildingXml[] oldArr = BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors;
                    BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors = new BoardDescriptorBuildingXml[idx + 1];
                    if (oldArr != null && oldArr.Length > 0)
                    {
                        for (int i = 0; i < oldArr.Length && i <= idx; i++)
                        {
                            BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[i] = oldArr[i];
                        }
                    }
                }
                if (BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[idx] == null)
                {
                    BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[idx] = new BoardDescriptorBuildingXml();
                }

                EnsureTabQuantity(idx + 1);
            }
            if (textIdx < 0)
            {
                return;
            }

            if (idx < 0)
            {
                idx = CurrentTab;
            }

            if (idx < 0)
            {
                return;
            }

            if (BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[idx].m_textDescriptors == null || BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[idx].m_textDescriptors.Length <= textIdx)
            {
                BoardTextDescriptorBuildingsXml[] oldArr = BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[idx].m_textDescriptors;
                BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[idx].m_textDescriptors = new BoardTextDescriptorBuildingsXml[textIdx + 1];
                if (oldArr != null && oldArr.Length > 0)
                {
                    for (int i = 0; i < oldArr.Length && i <= textIdx; i++)
                    {
                        BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[idx].m_textDescriptors[i] = oldArr[i];
                    }
                }
            }
            if (BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[idx].m_textDescriptors[textIdx] == null)
            {
                BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[idx].m_textDescriptors[textIdx] = new BoardTextDescriptorBuildingsXml
                {
                    m_defaultColor = Color.white,
                    m_useContrastColor = false
                };
            }
            EnsureTabQuantityTexts(textIdx + 1);
        }

        private void EnsureTabQuantity(int size)
        {
            int targetCount = Mathf.Max(m_pseudoTabstripProps.tabCount, size + 1);
            for (int i = 0; i < targetCount; i++)
            {
                if (i >= m_pseudoTabstripProps.tabCount)
                {
                    m_pseudoTabstripProps.AddTab($"{i}", CreateTabTemplate(), false);
                }
                if (i == targetCount - 1)
                {
                    ((UIButton) m_pseudoTabstripProps.tabs[i]).text = "+";
                }
                else if (m_currentBuildingName != null && i < (BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors?.Length ?? 0))
                {
                    ((UIButton) m_pseudoTabstripProps.tabs[i]).text = (BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors[i]?.SaveName).IsNullOrWhiteSpace() ? $"P{i + 1}" : BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors[i]?.SaveName;
                }
                else
                {
                    ((UIButton) m_pseudoTabstripProps.tabs[i]).text = $"P{i + 1}";
                }

            }
        }

        private void ConfigureTabsShown(int quantity)
        {

            for (int i = 0; i < m_pseudoTabstripProps.tabCount; i++)
            {
                m_pseudoTabstripProps.tabs[i].isVisible = i < quantity || i == m_pseudoTabstripProps.tabCount - 1;
            }

        }


        #endregion
        #region Selection Action
        private void EnablePickTool()
        {
            OnBuildingSet(null);
            DynamicTextPropsMod.Controller.BuildingEditorToolInstance.OnBuildingSelect += OnBuildingSet;
            DynamicTextPropsMod.Controller.BuildingEditorToolInstance.enabled = true;
        }

        private void OnBuildingSet(ushort id) => OnBuildingSet(BoardGeneratorBuildings.GetReferenceModelName(ref BuildingManager.instance.m_buildings.m_buffer[id]));

        private void OnBuildingSet(string buildingId)
        {
            m_currentBuildingName = buildingId;
            ReloadBuilding();
            OnChangeTab(-1);
        }

        private void ReloadBuilding()
        {
            m_propsDropdown.selectedIndex = -1;
            m_buildingName.isVisible = m_currentBuildingName != null;
            if (m_currentBuildingName != null)
            {
                m_isLoading = true;
                m_buildingName.text = m_currentBuildingName.EndsWith("_Data") ? Locale.Get("BUILDING_TITLE", m_currentBuildingName) : m_currentBuildingName;
                if (!BoardGeneratorBuildings.LoadedDescriptors.ContainsKey(m_currentBuildingName))
                {
                    BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName] = new BuildingGroupDescriptorXml();
                }
                EnsureTabQuantity(BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors?.Length ?? -1);
                ConfigureTabsShown(BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors?.Length ?? -1);
                m_mappingThresold.text = LoadedDescriptors[m_currentBuildingName].StopMappingThresold.ToString();
                m_isLoading = false;
            }
            m_pseudoTabPropsHelper.Self.isVisible = m_currentBuildingName != null && CurrentTab >= 0;
            m_contentContainer.Self.isVisible = m_currentBuildingName != null;
            m_loadPropGroup.items = DTPLibPropGroupBuildingSigns.Instance.List().ToArray();
            m_saveOnAssetFolderButton.isVisible = m_currentBuildingName?.EndsWith("_Data") ?? false;
        }

        private void OnChangeTab(int tabVal)
        {
            EnsureBoardsArrayIdx(tabVal);
            CurrentTab = tabVal;
        }
        #endregion
        #region Load Data copy
        protected override void ReloadTabInfo()
        {
            m_pseudoTabPropsHelper.Self.isVisible = m_currentBuildingName != null && CurrentTab >= 0;
            if (m_currentBuildingName == null || CurrentTab < 0 || CurrentTab >= (BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors?.Length ?? 0))
            {
                return;
            }

            m_isLoading = true;
            LoadTabInfo(BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors[CurrentTab]);
            EnsureTabQuantityTexts(BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors[CurrentTab]?.m_textDescriptors?.Length ?? 0);
            ConfigureTabsShownText(BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors[CurrentTab]?.m_textDescriptors?.Length ?? 0);
            m_isLoading = false;
            m_loadPropItem.items = DTPLibPropSingleBuildingSigns.Instance.List().ToArray();
            OnChangeTabTexts(-1);
        }
        private void LoadTabInfo(BoardDescriptorBuildingXml descriptor)
        {
            m_propsDropdown.selectedIndex = BoardGeneratorHighwaySigns.Instance.LoadedProps.IndexOf(descriptor?.m_propName ?? "") + 1;
            m_propItemName.text = descriptor?.SaveName ?? "";
            m_showIfNoLineCheck.isChecked = descriptor?.m_showIfNoLine ?? false;
            m_posVectorEditor[0].text = (descriptor?.PropPositionX ?? 0).ToString();
            m_posVectorEditor[1].text = (descriptor?.PropPositionY ?? 0).ToString();
            m_posVectorEditor[2].text = (descriptor?.PropPositionZ ?? 0).ToString();
            m_rotVectorEditor[0].text = (descriptor?.PropRotationX ?? 0).ToString();
            m_rotVectorEditor[1].text = (descriptor?.PropRotationY ?? 0).ToString();
            m_rotVectorEditor[2].text = (descriptor?.PropRotationZ ?? 0).ToString();
            m_scaleVectorEditor[0].text = (descriptor?.ScaleX ?? 1).ToString();
            m_scaleVectorEditor[1].text = (descriptor?.ScaleY ?? 1).ToString();
            m_scaleVectorEditor[2].text = (descriptor?.ScaleZ ?? 1).ToString();
            m_arrayRepeatEditor[0].text = (descriptor?.ArrayRepeatX ?? 0).ToString();
            m_arrayRepeatEditor[1].text = (descriptor?.ArrayRepeatY ?? 0).ToString();
            m_arrayRepeatEditor[2].text = (descriptor?.ArrayRepeatZ ?? 0).ToString();
            m_arrayTimes.text = (descriptor?.m_arrayRepeatTimes ?? 0).ToString();
            m_colorEditor.selectedColor = descriptor?.FixedColor ?? Color.white;
            m_colorModeDropdown.selectedIndex = (int) (descriptor?.ColorModeProp ?? 0);
            m_checkboxOrdernatedListPlatform.SetData(PlatformItem.CreateFrom(BoardGeneratorBuildings.GetStopPointsDescriptorFor(m_currentBuildingName), descriptor?.m_platforms ?? new int[0]));
        }
        protected override void ReloadTabInfoText()
        {
            m_pseudoTabTextsContainer.Self.isVisible = CurrentTabText >= 0;
            if (m_currentBuildingName == null || CurrentTab < 0 || CurrentTabText < 0)
            {
                return;
            }

            m_isLoading = true;
            LoadTabTextInfo(BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors[CurrentTab]?.m_textDescriptors[CurrentTabText]);
            m_loadTextDD.items = DTPLibTextMeshBuildingSigns.Instance.List().ToArray();
            m_isLoading = false;
        }

        protected override void AfterLoadingTabTextInfo(BoardTextDescriptorBuildingsXml descriptor)
        {
            TextType textType = descriptor?.m_textType ?? TextType.OwnName;

            m_useContrastColorTextCheckbox.isChecked = descriptor?.m_useContrastColor ?? false;
            m_allCapsCheckbox.isChecked = descriptor?.m_allCaps ?? false;
            m_textPrefix.text = descriptor?.m_prefix ?? "";
            m_textSuffix.text = descriptor?.m_suffix ?? "";
            m_colorEditorText.parent.isVisible = !m_useContrastColorTextCheckbox.isChecked;
            ReloadFontsOverride(m_overrideFontText, descriptor?.m_overrideFont);


            bool isFixedMode = textType == TextType.Fixed;
            m_overrideFontText.parent.isVisible = isFixedMode;
            m_allCapsCheckbox.isVisible = !isFixedMode;
            m_textPrefix.GetComponentInParent<UIPanel>().isVisible = !isFixedMode;
            m_textSuffix.GetComponentInParent<UIPanel>().isVisible = !isFixedMode;
        }
        private void ReloadFontsOverride(UIDropDown target, string currentVal)
        {
            var items = Font.GetOSInstalledFontNames().ToList();
            items.Insert(0, Locale.Get("K45_DTP_USE_DEFAULT_FONT_HS"));
            target.items = items.ToArray();
            if (items.Contains(currentVal))
            {
                target.selectedIndex = items.IndexOf(currentVal);
            }
            else
            {
                target.selectedIndex = 0;
            }
        }

        protected override TextType[] GetAvailableTextTypes() => AVAILABLE_TEXT_TYPES;

        #endregion

        private class PlatformItem : ICheckable
        {
            public bool IsChecked { get; set; }

            private Color32 OwnColor => BoardGeneratorBuildings.m_colorOrder[index % BoardGeneratorBuildings.m_colorOrder.Length];

            public StopSearchUtils.StopPointDescriptorLanes descriptorLane;
            public int index;
            public override string ToString() => $"<color #{OwnColor.ToRGB()}><sprite EmptySprite></color> #{index} - {descriptorLane.vehicleType} @ {descriptorLane.platformLine.GetBounds().center}";

            public static List<PlatformItem> CreateFrom(IEnumerable<StopSearchUtils.StopPointDescriptorLanes> list, int[] order)
            {
                var result = list.Select((x, i) => new PlatformItem
                {
                    descriptorLane = x,
                    index = i,
                    IsChecked = Array.IndexOf(order, i) > -1
                }).ToList();

                result.Sort((x, y) =>
                 {
                     if (x.IsChecked != y.IsChecked)
                     {
                         return x.IsChecked ? 1 : -1;
                     }
                     else if (x.IsChecked)
                     {
                         return Array.IndexOf(order, x.index).CompareTo(Array.IndexOf(order, y.index));
                     }
                     else
                     {
                         return x.index.CompareTo(y.index);
                     }
                 });

                return result;
            }
        }

        private class CheckboxOrdernatedListPlatformItem : CheckboxOrdernatedList<PlatformItem> { }
    }


}
