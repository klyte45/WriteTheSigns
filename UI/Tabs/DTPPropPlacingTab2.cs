using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Data;
using Klyte.DynamicTextProps.Libraries;
using Klyte.DynamicTextProps.Overrides;
using System;
using System.Linq;
using UnityEngine;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorHighwaySigns;

namespace Klyte.DynamicTextProps.UI
{

    internal class DTPPropPlacingTab2 : DTPXmlEditorParentTab<BoardGeneratorHighwaySigns, BoardBunchContainerHighwaySignXml, DTPHighwaySignsData, BoardDescriptorHigwaySignXml, BoardTextDescriptorHighwaySignsXml, DTPLibTextMeshHighwaySigns>
    {
        private UIButton m_buttonTool;

        private UIHelperExtension m_contentContainer;
        private UITabstripAutoResize m_pseudoTabstripProps;

        private UIHelperExtension m_pseudoTabPropsHelper;
        private UILabel m_selectionAddress;

        private UITextField m_propItemName;
        private UISlider m_segmentPosition;
        private UICheckBox m_invertOrientation;
        private UITextField[] m_posVectorEditor;
        private UITextField[] m_rotVectorEditor;
        private UITextField[] m_scaleVectorEditor;
        private UIColorField m_colorEditor;

        private UIButton m_pasteGroupButton;

        private UIButton m_pasteButton;

        //private UIDropDown m_overrideFontText;

        private UIDropDown m_loadPropItem;



        public ushort m_currentSelectedSegment;
        private int CurrentTab
        {
            get => m_pseudoTabstripProps.selectedIndex;
            set => m_pseudoTabstripProps.selectedIndex = value;
        }
        protected override BoardTextDescriptorHighwaySignsXml[] CurrentSelectedDescriptorArray
        {
            get => BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor.m_textDescriptors;
            set => BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor.m_textDescriptors = value;
        }

        private string m_clipboard;
        private string m_clipboardGroup;

        protected override bool IsTextEditionAvailable() => m_currentSelectedSegment > 0 && CurrentTab >= 0;
        #region Awake
        protected override string GetFontLabelString() => Locale.Get("K45_DTP_FONT_PLACED_PROPS");

        protected override void AwakePropEditor(out UIScrollablePanel scrollTabs, out UIHelperExtension referenceHelperTabs)
        {
            m_buttonTool = (UIButton)m_uiHelperHS.AddButton(Locale.Get("K45_DTP_PICK_A_SEGMENT"), EnablePickTool);
            KlyteMonoUtils.LimitWidth(m_buttonTool, m_uiHelperHS.Self.width - 20, true);

            m_contentContainer = m_uiHelperHS.AddGroupExtended(Locale.Get("K45_DTP_PICKED_SEGMENT_DATA"));
            ((UIPanel)m_contentContainer.Self).backgroundSprite = "";
            m_contentContainer.Self.width = MainContainer.width - 30;

            m_selectionAddress = m_contentContainer.Self.parent.GetComponentInChildren<UILabel>();
            m_selectionAddress.prefix = Locale.Get("K45_DTP_ADDRESS_LABEL_PREFIX") + " ";
            KlyteMonoUtils.LimitWidth(m_selectionAddress, m_contentContainer.Self.width, true);

            m_loadPropGroup = AddLibBox<DTPLibPropGroupHigwaySigns, BoardBunchContainerHighwaySignXml>(Locale.Get("K45_DTP_PROP_GROUP_LIB_TITLE"), m_contentContainer,
            out UIButton m_copyGroupButton, DoCopyGroup,
            out m_pasteGroupButton, DoPasteGroup,
            out UIButton m_deleteGroupButton, DoDeleteGroup,
            (x) =>
                {
                    if (m_currentSelectedSegment == 0)
                    {
                        return;
                    }

                    BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment] = XmlUtils.DefaultXmlDeserialize<BoardBunchContainerHighwaySignXml>(XmlUtils.DefaultXmlSerialize(x));
                    ReloadSegment();
                    OnChangeTab(-1);
                },
                () => BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment]);

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
                    int nextIdx = BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment]?.m_boardsData?.Length ?? 0;
                    LogUtils.DoLog($"nextIdx = {nextIdx}");
                    EnsureBoardsArrayIdx(nextIdx);
                    ReloadSegment();
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
            ((UIPanel)m_pseudoTabPropsHelper.Self).backgroundSprite = "";

            m_pseudoTabPropsHelper.Self.eventVisibilityChanged += (x, y) => { if (y) { ReloadTabInfo(); } };
            m_pseudoTabPropsHelper.Self.width = MainContainer.width - 30;


            m_loadPropItem = AddLibBox<DTPLibPropSingleHighwaySigns, BoardDescriptorHigwaySignXml>(Locale.Get("K45_DTP_PROP_ITEM_LIB_TITLE"), m_pseudoTabPropsHelper,
                                    out UIButton m_copyButton, DoCopy,
                                    out m_pasteButton, DoPaste,
                                    out UIButton m_deleteButton, DoDelete,
                                    (x) =>
                                    {
                                        if (m_currentSelectedSegment == 0 || CurrentTab < 0)
                                        {
                                            return;
                                        }

                                        BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor = XmlUtils.DefaultXmlDeserialize<BoardDescriptorHigwaySignXml>(XmlUtils.DefaultXmlSerialize(x));
                                        BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].cached = false;
                                        ReloadTabInfo();
                                    },
                            () => BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers?.ElementAtOrDefault(m_currentSelectedSegment)?.m_boardsData?.ElementAtOrDefault(CurrentTab)?.descriptor);


            UIHelperExtension groupProp = m_pseudoTabPropsHelper.AddTogglableGroup(Locale.Get("K45_DTP_PROP_CONFIGURATION"));
            AddTextField(Locale.Get("K45_DTP_PROP_TAB_TITLE"), out m_propItemName, groupProp, SetPropItemName);

            AddDropdown(Locale.Get("K45_DTP_PROP_MODEL_SELECT"), out m_propsDropdown, groupProp, new string[0], SetPropModel);
            AddSlider(Locale.Get("K45_DTP_SEGMENT_RELATIVE_POS"), out m_segmentPosition, groupProp, SetPropSegPosition, 0, 1, 0.01f);
            m_invertOrientation = (UICheckBox)groupProp.AddCheckbox(Locale.Get("K45_DTP_INVERT_SIGN_SIDE"), false, SetInvertSignSide);

            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_POS"), out m_posVectorEditor, groupProp, SetPropRelPosition);
            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_ROT"), out m_rotVectorEditor, groupProp, SetPropRelRotation);
            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_SCALE"), out m_scaleVectorEditor, groupProp, SetPropRelScale);

            m_colorEditor = groupProp.AddColorPicker(Locale.Get("K45_DTP_PAINTING_COLOR"), Color.white, SetPropColor);
            KlyteMonoUtils.LimitWidth(m_colorEditor.parent.GetComponentInChildren<UILabel>(), groupProp.Self.width / 2, true);


            KlyteMonoUtils.CreateHorizontalScrollPanel(m_pseudoTabPropsHelper.Self, out scrollTabs, out bar, m_pseudoTabPropsHelper.Self.width - 20, 40, Vector3.zero);
            referenceHelperTabs = m_pseudoTabPropsHelper;

        }

        protected override void OnTextTabStripChanged()
        {
            EnsureBoardsArrayIdx(-1, BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor?.m_textDescriptors?.Length ?? 0);
            ReloadTabInfo();
            OnChangeTabTexts(BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor.m_textDescriptors.Length - 1);
            ReloadTabInfoText();
        }

        protected override void DoInTextCommonTabGroupUI(UIHelperExtension groupTexts) => base.DoInTextCommonTabGroupUI(groupTexts);//    AddDropdown(Locale.Get("K45_DTP_OVERRIDE_FONT"), out m_overrideFontText, groupTexts, new string[0], SetOverrideFont);
        protected override void PostAwake() => OnSegmentSet(0);
        #endregion

        #region Clipboard
        private void DoCopy()
        {
            if (m_currentSelectedSegment > 0 && CurrentTab >= 0)
            {
                m_clipboard = (BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].Serialize());
                m_pasteButton.isVisible = true;
            }
        }

        private void DoPaste()
        {
            if (m_currentSelectedSegment > 0 && CurrentTab >= 0 && m_clipboard != null)
            {
                BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].cached = false;
                BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].Deserialize(m_clipboard);
                ReloadTabInfo();
            }
        }

        private void DoDelete()
        {
            if (m_currentSelectedSegment > 0 && CurrentTab >= 0)
            {
                LogUtils.DoLog($"  BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData.length = {  BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData.Length }");
                var tempList = BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData.ToList();
                tempList.RemoveAt(CurrentTab);
                BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData = tempList.ToArray();
                LogUtils.DoLog($"  BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData.length pos = {  BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData.Length }");
                OnSegmentSet(m_currentSelectedSegment);
            }
        }

        private void DoCopyGroup()
        {
            if (m_currentSelectedSegment > 0)
            {
                m_clipboardGroup = XmlUtils.DefaultXmlSerialize(BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment]);
                m_pasteGroupButton.isVisible = true;
            }
        }

        private void DoPasteGroup()
        {
            if (m_currentSelectedSegment > 0 && m_clipboardGroup != null)
            {
                BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment] = XmlUtils.DefaultXmlDeserialize<BoardBunchContainerHighwaySignXml>(m_clipboardGroup);
                ReloadSegment();
            }
        }

        private void DoDeleteGroup()
        {
            if (m_currentSelectedSegment > 0)
            {
                BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment] = new BoardBunchContainerHighwaySignXml();
                OnSegmentSet(m_currentSelectedSegment);
            }
        }

        protected override void OnPasteText() => BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].cached = false;
        #endregion
        protected override void SetPropModel(int idx)
        {
            if (idx >= 0)
            {
                SafeActionInBoard(descriptor => descriptor.m_propName = idx > 0 ? BoardGeneratorHighwaySigns.Instance.LoadedProps[idx - 1] : null);
            }
        }

        private void SetPropItemName(string txt)
        {
            SafeActionInBoard(descriptor =>
            {
                descriptor.SaveName = txt;
                EnsureTabQuantity(BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment]?.m_boardsData?.Length ?? -1);
            });
        }
        private void SetPropColor(Color color) => SafeActionInBoard(descriptor => descriptor.m_color = color);
        private void SetPropSegPosition(float value) => SafeActionInBoard(descriptor => descriptor.m_segmentPosition = value);
        private void SetPropRelPosition(Vector3 value) => SafeActionInBoard(descriptor => descriptor.m_propPosition = value);
        private void SetPropRelRotation(Vector3 value) => SafeActionInBoard(descriptor => descriptor.m_propRotation = value);
        private void SetPropRelScale(Vector3 value) => SafeActionInBoard(descriptor => descriptor.PropScale = value);
        private void SetInvertSignSide(bool value) => SafeActionInBoard(descriptor => descriptor.m_invertSign = value);
        private void SafeActionInBoard(Action<BoardDescriptorHigwaySignXml> toDo)
        {
            if (m_currentSelectedSegment != 0 && !m_isLoading)
            {
                EnsureBoardsArrayIdx(CurrentTab);
                BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].cached = false;
                BoardDescriptorHigwaySignXml descriptor = BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor;
                toDo(descriptor);
            }
        }

        private void EnsureBoardsArrayIdx(int idx, int textIdx = -1)
        {
            if (idx >= 0)
            {
                if (BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment] == null)
                {
                    BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment] = new BoardBunchContainerHighwaySignXml();
                }
                if (BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData == null || BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData.Length <= idx)
                {
                    CacheControlHighwaySign[] oldArr = BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData;
                    BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData = new CacheControlHighwaySign[idx + 1];
                    if (oldArr != null && oldArr.Length > 0)
                    {
                        for (int i = 0; i < oldArr.Length && i <= idx; i++)
                        {
                            BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[i] = oldArr[i];
                        }
                    }
                }
                if (BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[idx] == null)
                {
                    BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[idx] = new CacheControlHighwaySign();
                }
                if (BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor == null)
                {
                    BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor = new BoardDescriptorHigwaySignXml();
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

            if (BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors == null || BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors.Length <= textIdx)
            {
                BoardTextDescriptorHighwaySignsXml[] oldArr = BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors;
                BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors = new BoardTextDescriptorHighwaySignsXml[textIdx + 1];
                if (oldArr != null && oldArr.Length > 0)
                {
                    for (int i = 0; i < oldArr.Length && i <= textIdx; i++)
                    {
                        BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors[i] = oldArr[i];
                    }
                }
            }
            if (BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors[textIdx] == null)
            {
                BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors[textIdx] = new BoardTextDescriptorHighwaySignsXml
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
                    ((UIButton)m_pseudoTabstripProps.tabs[i]).text = "+";
                }
                else if (m_currentSelectedSegment > 0 && i < (BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment]?.m_boardsData?.Length ?? 0))
                {
                    ((UIButton)m_pseudoTabstripProps.tabs[i]).text = (BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment]?.m_boardsData[i]?.descriptor?.SaveName).IsNullOrWhiteSpace() ? $"P{i + 1}" : BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment]?.m_boardsData[i]?.descriptor?.SaveName;
                }
                else
                {
                    ((UIButton)m_pseudoTabstripProps.tabs[i]).text = $"P{i + 1}";
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

        private void EnablePickTool()
        {
            OnSegmentSet(0);
            DynamicTextPropsMod.Controller.RoadSegmentToolInstance.OnSelectSegment += OnSegmentSet;
            DynamicTextPropsMod.Controller.RoadSegmentToolInstance.enabled = true;
        }

        private void OnSegmentSet(ushort segmentId)
        {
            m_currentSelectedSegment = segmentId;
            ReloadSegment();
            OnChangeTab(-1);
        }

        private void ReloadSegment()
        {
            m_propsDropdown.selectedIndex = -1;
            m_selectionAddress.isVisible = m_currentSelectedSegment > 0;
            if (m_currentSelectedSegment > 0)
            {
                m_isLoading = true;
                int endNodeNum = SegmentUtils.GetNumberAt(m_currentSelectedSegment, false);
                int startNodeNum = SegmentUtils.GetNumberAt(m_currentSelectedSegment, true);
                m_selectionAddress.text = $"{NetManager.instance.GetSegmentName(m_currentSelectedSegment)}, {Mathf.Min(startNodeNum, endNodeNum)} - {Mathf.Max(startNodeNum, endNodeNum)}";

                EnsureTabQuantity(BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment]?.m_boardsData?.Length ?? -1);
                ConfigureTabsShown(BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment]?.m_boardsData?.Length ?? -1);
                m_isLoading = false;
            }
            m_pseudoTabPropsHelper.Self.isVisible = m_currentSelectedSegment > 0 && CurrentTab >= 0;
            m_contentContainer.Self.isVisible = m_currentSelectedSegment > 0;
            m_loadPropGroup.items = DTPLibPropGroupHigwaySigns.Instance.List().ToArray();
        }

        private void OnChangeTab(int tabVal)
        {
            EnsureBoardsArrayIdx(tabVal);
            CurrentTab = tabVal;
        }


        protected override void ReloadTabInfo()
        {
            m_pseudoTabPropsHelper.Self.isVisible = m_currentSelectedSegment > 0 && CurrentTab >= 0;
            if (m_currentSelectedSegment <= 0 || CurrentTab < 0 || CurrentTab >= (BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment]?.m_boardsData?.Length ?? 0))
            {
                return;
            }

            m_isLoading = true;
            LoadTabInfo(BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor);
            EnsureTabQuantityTexts(BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor?.m_textDescriptors?.Length ?? 0);
            ConfigureTabsShownText(BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor?.m_textDescriptors?.Length ?? 0);
            m_isLoading = false;
            m_loadPropItem.items = DTPLibPropSingleHighwaySigns.Instance.List().ToArray();
            OnChangeTabTexts(-1);
        }
        private void LoadTabInfo(BoardDescriptorHigwaySignXml descriptor)
        {
            m_propsDropdown.selectedIndex = BoardGeneratorHighwaySigns.Instance.LoadedProps.IndexOf(descriptor?.m_propName) + 1;
            m_segmentPosition.value = descriptor?.m_segmentPosition ?? 0.5f;
            m_propItemName.text = descriptor?.SaveName ?? "";
            m_invertOrientation.isChecked = descriptor?.m_invertSign ?? false;
            m_posVectorEditor[0].text = (descriptor?.PropPositionX ?? 0).ToString();
            m_posVectorEditor[1].text = (descriptor?.PropPositionY ?? 0).ToString();
            m_posVectorEditor[2].text = (descriptor?.PropPositionZ ?? 0).ToString();
            m_rotVectorEditor[0].text = (descriptor?.PropRotationX ?? 0).ToString();
            m_rotVectorEditor[1].text = (descriptor?.PropRotationY ?? 0).ToString();
            m_rotVectorEditor[2].text = (descriptor?.PropRotationZ ?? 0).ToString();
            m_scaleVectorEditor[0].text = (descriptor?.ScaleX ?? 1).ToString();
            m_scaleVectorEditor[1].text = (descriptor?.ScaleY ?? 1).ToString();
            m_scaleVectorEditor[2].text = (descriptor?.ScaleZ ?? 1).ToString();
            m_colorEditor.selectedColor = descriptor?.m_color ?? Color.white;
        }

        protected override void AfterLoadingTabTextInfo(BoardTextDescriptorHighwaySignsXml descriptor) { }// => ReloadFontsOverride(m_overrideFontText, descriptor?.m_overrideFont);

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

        protected override string GetLocaleNameForContentTypes() => "K45_DTP_OWN_NAME_CONTENT_HS";
        protected override void OnDropdownTextTypeSelectionChanged(int idx) { }// => m_overrideFontText.parent.isVisible = BoardGeneratorBuildings.AVAILABLE_TEXT_TYPES[idx] == TextType.Fixed;
        protected override void OnLoadTextLibItem() => ReloadSegment();
        protected override void ReloadTabInfoText()
        {
            m_pseudoTabTextsContainer.Self.isVisible = CurrentTabText >= 0;
            if (m_currentSelectedSegment <= 0 || CurrentTab < 0 || CurrentTabText < 0)
            {
                return;
            }

            m_isLoading = true;
            LoadTabTextInfo(BoardGeneratorHighwaySigns.Instance.Data.BoardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor.m_textDescriptors[CurrentTabText]);
            m_loadTextDD.items = DTPLibTextMeshHighwaySigns.Instance.List().ToArray();
            m_isLoading = false;
        }

        protected override TextType[] GetAvailableTextTypes() => AVAILABLE_TEXT_TYPES;
    }


}
