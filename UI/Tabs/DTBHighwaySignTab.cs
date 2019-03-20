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
using Klyte.DynamicTextBoards.Tools;
using static Klyte.DynamicTextBoards.Overrides.BoardGeneratorHighwaySigns;

namespace Klyte.DynamicTextBoards.UI
{

    internal class DTBHighwaySignTab : UICustomControl
    {
        public UIScrollablePanel mainContainer { get; private set; }

        private UIHelperExtension m_uiHelperHS;

        private UIButton m_buttonTool;



        private UIHelperExtension m_contentContainer;
        private UITabstrip m_pseudoTabstripProps;

        private UIHelperExtension m_pseudoTabPropsHelper;
        private UILabel m_selectionAddress;
        private UIDropDown m_propsDropdown;
        private UISlider m_segmentPosition;
        private UICheckBox m_invertOrientation;
        private UITextField[] m_posVectorEditor;
        private UITextField[] m_rotVectorEditor;
        private UITextField[] m_scaleVectorEditor;
        private UIColorField m_colorEditor;

        private UIHelperExtension m_actionsBarHelper;
        private UIButton m_copyButton;
        private UIButton m_pasteButton;
        private UIButton m_deleteButton;


        private UIHelperExtension m_pseudoTabTextsContainer;
        private UITabstrip m_pseudoTabstripTexts;
        private UIDropDown m_dropdownTextContent;
        private UITextField m_customText;
        private UITextField[] m_posVectorEditorText;
        private UITextField[] m_rotVectorEditorText;
        private UIColorField m_colorEditorText;
        private UIDropDown m_dropdownTextAlignHorizontal;
        private UIDropDown m_dropdownTextAlignVertical;
        private UITextField m_maxWidthText;
        private UITextField m_scaleText;
        private UICheckBox m_textResizeYOnOverflow;
        private UISlider m_textLuminosityDay;
        private UISlider m_textLuminosityNight;


        private UIHelperExtension m_actionsBarTextHelper;
        private UIButton m_copyButtonText;
        private UIButton m_pasteButtonText;
        private UIButton m_deleteButtonText;


        public ushort m_currentSelectedSegment;
        private int CurrentTab
        {
            get => m_pseudoTabstripProps.selectedIndex;
            set => m_pseudoTabstripProps.selectedIndex = value;
        }
        private int CurrentTabText
        {
            get => m_pseudoTabstripTexts.selectedIndex;
            set => m_pseudoTabstripTexts.selectedIndex = value;
        }

        private string m_clipboard;
        private string m_clipboardText;

        private bool m_isLoading = false;

        #region Awake
        private void Awake()
        {
            mainContainer = GetComponent<UIScrollablePanel>();

            m_uiHelperHS = new UIHelperExtension(mainContainer);

            m_buttonTool = (UIButton)m_uiHelperHS.AddButton(Locale.Get("DTB_PICK_A_SEGMENT"), EnablePickTool);

            m_contentContainer = m_uiHelperHS.AddGroupExtended(Locale.Get("DTB_PICKED_SEGMENT_DATA"));
            ((UIPanel)m_contentContainer.self).backgroundSprite = "";
            m_contentContainer.self.width = mainContainer.width - 10;

            m_selectionAddress = m_contentContainer.self.parent.GetComponentInChildren<UILabel>();
            m_selectionAddress.prefix = Locale.Get("DTB_ADDRESS_LABEL_PREFIX") + " ";
            DTBUtils.LimitWidth(m_selectionAddress, m_contentContainer.self.width, true);


            DTBUtils.createUIElement(out m_pseudoTabstripProps, m_contentContainer.self.transform, "DTBTabstrip", new Vector4(5, 40, mainContainer.width - 10, 40));
            m_pseudoTabstripProps.startSelectedIndex = -1;
            m_pseudoTabstripProps.selectedIndex = -1;

            m_pseudoTabstripProps.AddTab("+", CreateTab("+"), false);

            m_pseudoTabstripProps.eventSelectedIndexChanged += (x, idx) =>
            {
                if (idx == m_pseudoTabstripProps.tabCount - 1)
                {
                    var nextIdx = BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData?.Length ?? 0;
                    DTBUtils.doLog($"nextIdx = {nextIdx}");
                    EnsureBoardsArrayIdx(nextIdx);
                    ReloadSegment();
                    OnChangeTab(nextIdx);
                }
                else
                {
                    ReloadTabInfo();
                }
            };

            m_pseudoTabPropsHelper = m_uiHelperHS.AddGroupExtended(Locale.Get("DTB_PROP_CONFIGURATION"), out UILabel lbl);
            Destroy(lbl);
            ((UIPanel)m_pseudoTabPropsHelper.self).backgroundSprite = "";

            m_pseudoTabPropsHelper.self.eventVisibilityChanged += (x, y) => { if (y) ReloadTabInfo(); };

            m_actionsBarHelper = m_pseudoTabPropsHelper.AddTogglableGroup(Locale.Get("DTB_AVAILABLE_ACTIONS"));

            var subPanelActionsBar = m_actionsBarHelper.AddGroupExtended("!!!!", out UILabel voide);
            Destroy(voide);
            ((UIPanel)subPanelActionsBar.self).autoLayoutDirection = LayoutDirection.Horizontal;
            ((UIPanel)subPanelActionsBar.self).wrapLayout = false;
            ((UIPanel)subPanelActionsBar.self).autoLayout = true;
            ((UIPanel)subPanelActionsBar.self).autoFitChildrenHorizontally = true;
            ((UIPanel)subPanelActionsBar.self).autoFitChildrenVertically = true;

            m_copyButton = (UIButton)subPanelActionsBar.AddButton("Copy", DoCopy);
            m_pasteButton = (UIButton)subPanelActionsBar.AddButton("Paste", DoPaste);
            m_deleteButton = (UIButton)subPanelActionsBar.AddButton("Delete", DoDelete);
            m_pasteButton.isVisible = false;
            //Destroy();

            var groupProp = m_pseudoTabPropsHelper.AddTogglableGroup(Locale.Get("DTB_PROP_CONFIGURATION"));

            AddDropdown(Locale.Get("DTB_PROP_MODEL_SELECT"), out m_propsDropdown, groupProp, new string[0], SetPropModel);
            AddSlider(Locale.Get("DTB_SEGMENT_RELATIVE_POS"), out m_segmentPosition, groupProp, SetPropSegPosition, 0, 1, 0.01f);
            m_invertOrientation = (UICheckBox)groupProp.AddCheckbox(Locale.Get("DTB_INVERT_SIGN_SIDE"), false, SetInvertSignSide);

            AddVector3Field(Locale.Get("DTB_RELATIVE_POS"), out m_posVectorEditor, groupProp, SetPropRelPosition);
            AddVector3Field(Locale.Get("DTB_RELATIVE_ROT"), out m_rotVectorEditor, groupProp, SetPropRelRotation);
            AddVector3Field(Locale.Get("DTB_RELATIVE_SCALE"), out m_scaleVectorEditor, groupProp, SetPropRelScale);

            m_colorEditor = groupProp.AddColorPicker(Locale.Get("DTB_PAINTING_COLOR"), Color.white, SetPropColor);
            DTBUtils.LimitWidth(m_colorEditor.parent.GetComponentInChildren<UILabel>(), groupProp.self.width / 2, true);


            DTBUtils.createUIElement(out m_pseudoTabstripTexts, m_pseudoTabPropsHelper.self.transform, "DTBTabstrip", new Vector4(5, 40, mainContainer.width - 10, 40));
            m_pseudoTabstripTexts.startSelectedIndex = -1;
            m_pseudoTabstripTexts.selectedIndex = -1;

            m_pseudoTabstripTexts.AddTab("+", CreateTab("+"), false);

            m_pseudoTabstripTexts.eventSelectedIndexChanged += (x, idx) =>
            {
                if (idx == m_pseudoTabstripTexts.tabCount - 1)
                {
                    var pos = mainContainer.verticalScrollbar.relativePosition;
                    EnsureBoardsArrayIdx(-1, BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor?.m_textDescriptors?.Length ?? 0);
                    ReloadTabInfo();
                    OnChangeTabTexts(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor.m_textDescriptors.Length - 1);
                    mainContainer.verticalScrollbar.relativePosition = pos;
                }
                else
                {
                    ReloadTabInfoText();
                }
            };

            m_pseudoTabTextsContainer = m_pseudoTabPropsHelper.AddGroupExtended("))))");
            Destroy(m_pseudoTabTextsContainer.self.parent.GetComponentInChildren<UILabel>().gameObject);
            ((UIPanel)m_pseudoTabTextsContainer.self).backgroundSprite = "";
            m_pseudoTabTextsContainer.self.isVisible = false;

            m_actionsBarTextHelper = m_pseudoTabTextsContainer.AddTogglableGroup(Locale.Get("DTB_AVAILABLE_ACTIONS"));

            var subPanelActionsBarText = m_actionsBarTextHelper.AddGroupExtended("!!!!", out voide);
            Destroy(voide);
            ((UIPanel)subPanelActionsBarText.self).autoLayoutDirection = LayoutDirection.Horizontal;
            ((UIPanel)subPanelActionsBarText.self).wrapLayout = false;
            ((UIPanel)subPanelActionsBarText.self).autoLayout = true;
            ((UIPanel)subPanelActionsBarText.self).autoFitChildrenHorizontally = true;
            ((UIPanel)subPanelActionsBarText.self).autoFitChildrenVertically = true;

            m_copyButtonText = (UIButton)subPanelActionsBarText.AddButton("Copy", DoCopyText);
            m_pasteButtonText = (UIButton)subPanelActionsBarText.AddButton("Paste", DoPasteText);
            m_deleteButtonText = (UIButton)subPanelActionsBarText.AddButton("Delete", DoDeleteText);
            m_pasteButtonText.isVisible = false;


            var groupTexts = m_pseudoTabTextsContainer.AddTogglableGroup(Locale.Get("DTB_TEXTS_COMMON_CONFIGURATION"), out UILabel lblTxt);

            m_colorEditorText = groupTexts.AddColorPicker(Locale.Get("DTB_TEXT_COLOR"), Color.white, SetTextColor);
            DTBUtils.LimitWidth(m_colorEditorText.parent.GetComponentInChildren<UILabel>(), groupTexts.self.width / 2, true);
            AddDropdown(Locale.Get("DTB_TEXT_CONTENT"), out m_dropdownTextContent, groupTexts, Enum.GetNames(typeof(OwnNameContent)).Select(x => Locale.Get("DTB_OWN_NAME_CONTENT", x)).ToArray(), SetTextOwnNameContent);
            AddTextField(Locale.Get("DTB_CUSTOM_TEXT"), out m_customText, groupTexts, SetTextCustom);
            m_dropdownTextContent.eventSelectedIndexChanged += (e, idx) => m_customText.GetComponentInParent<UIPanel>().isVisible = idx == (int)OwnNameContent.Custom;

            groupTexts = m_pseudoTabTextsContainer.AddTogglableGroup(Locale.Get("DTB_TEXTS_SIZE_POSITION"));
            AddVector3Field(Locale.Get("DTB_RELATIVE_POS"), out m_posVectorEditorText, groupTexts, SetTextRelPosition);
            AddVector3Field(Locale.Get("DTB_RELATIVE_ROT"), out m_rotVectorEditorText, groupTexts, SetTextRelRotation);
            AddFloatField(Locale.Get("DTB_TEXT_SCALE"), out m_scaleText, groupTexts, SetTextScale, false);
            AddFloatField(Locale.Get("DTB_MAX_WIDTH_METERS"), out m_maxWidthText, groupTexts, SetTextMaxWidth, false);
            m_textResizeYOnOverflow = (UICheckBox)groupTexts.AddCheckbox(Locale.Get("DTB_RESIZE_Y_TEXT_OVERFLOW"), false, SetTextResizeYOnOverflow);


            groupTexts = m_pseudoTabTextsContainer.AddTogglableGroup(Locale.Get("DTB_TEXTS_2D_ALIGNMENT"));
            AddDropdown(Locale.Get("DTB_TEXT_ALIGN_HOR"), out m_dropdownTextAlignHorizontal, groupTexts, Enum.GetNames(typeof(UIHorizontalAlignment)).Select(x => Locale.Get("KC_ALIGNMENT", x)).ToArray(), SetTextAlignmentHorizontal);
            AddDropdown(Locale.Get("DTB_TEXT_ALIGN_VER"), out m_dropdownTextAlignVertical, groupTexts, Enum.GetNames(typeof(UIVerticalAlignment)).Select(x => Locale.Get("KC_VERT_ALIGNMENT", x)).ToArray(), SetTextAlignmentVertical);

            groupTexts = m_pseudoTabTextsContainer.AddTogglableGroup(Locale.Get("DTB_TEXT_EFFECTS"));
            AddSlider(Locale.Get("DTB_LUMINOSITY_DAY"), out m_textLuminosityDay, groupTexts, SetTextLumDay, 0, 10f, 0.25f);
            AddSlider(Locale.Get("DTB_LUMINOSITY_NIGHT"), out m_textLuminosityNight, groupTexts, SetTextLumNight, 0, 10f, 0.25f);


            OnSegmentSet(0);
        }

        private void AddSlider(string label, out UISlider slider, UIHelperExtension parentHelper, OnValueChanged onChange, float min, float max, float step)
        {
            slider = (UISlider)parentHelper.AddSlider(label, min, max, step, min, onChange);
            slider.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            slider.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            DTBUtils.LimitWidth(slider.parent.GetComponentInChildren<UILabel>(), parentHelper.self.width / 2 - 10, true);
        }

        private void AddVector3Field(string label, out UITextField[] fieldArray, UIHelperExtension parentHelper, Action<Vector3> onChange)
        {
            fieldArray = parentHelper.AddVector3Field(label, Vector3.zero, onChange);
            DTBUtils.LimitWidth(fieldArray[0].parent.GetComponentInChildren<UILabel>(), parentHelper.self.width / 2 - 10, true);
        }

        private void AddFloatField(string label, out UITextField field, UIHelperExtension parentHelper, Action<float> onChange, bool acceptNegative)
        {
            field = parentHelper.AddFloatField(label, 0, onChange, acceptNegative);
            DTBUtils.LimitWidth(field.parent.GetComponentInChildren<UILabel>(), parentHelper.self.width / 2 - 10, true);
        }

        private void AddDropdown(string label, out UIDropDown dropdown, UIHelperExtension parentHelper, string[] options, OnDropdownSelectionChanged onChange)
        {
            dropdown = (UIDropDown)parentHelper.AddDropdown(label, options, 0, onChange);
            dropdown.width = parentHelper.self.width / 2 - 10;
            dropdown.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            dropdown.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            DTBUtils.LimitWidth(dropdown.parent.GetComponentInChildren<UILabel>(), parentHelper.self.width / 2 - 10, true);
        }

        private void AddTextField(string label, out UITextField textField, UIHelperExtension parentHelper, OnTextSubmitted onChange)
        {
            textField = parentHelper.AddTextField(label, (x) => { }, "", onChange);
            textField.width = parentHelper.self.width / 2 - 10;
            textField.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            textField.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            DTBUtils.LimitWidth(textField.parent.GetComponentInChildren<UILabel>(), parentHelper.self.width / 2 - 10, true);
        }

        private static UIButton CreateTabTemplate()
        {
            DTBUtils.createUIElement(out UIButton tabTemplate, null, "DTBTabTemplate");
            DTBUtils.initButton(tabTemplate, false, "GenericTab");
            tabTemplate.autoSize = false;
            tabTemplate.width = 40;
            tabTemplate.height = 40;
            tabTemplate.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            return tabTemplate;
        }

        private UIButton CreateTab(string text)
        {
            UIButton tab = CreateTabTemplate();
            tab.text = text;

            return tab;
        }

        private void DoCopy()
        {
            if (m_currentSelectedSegment > 0 && CurrentTab >= 0)
            {
                m_clipboard = (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].Serialize());
                m_pasteButton.isVisible = true;
            }
        }

        private void DoPaste()
        {
            if (m_currentSelectedSegment > 0 && CurrentTab >= 0 && m_clipboard != null)
            {
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].cached = false;
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].Deserialize(m_clipboard);
                ReloadTabInfo();
            }
        }

        private void DoDelete()
        {
            if (m_currentSelectedSegment > 0 && CurrentTab >= 0)
            {
                DTBUtils.doLog($"  BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData.length = {  BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData.Length }");
                var tempList = BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData.ToList();
                tempList.RemoveAt(CurrentTab);
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData = tempList.ToArray();
                DTBUtils.doLog($"  BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData.length pos = {  BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData.Length }");
                OnSegmentSet(m_currentSelectedSegment);
            }
        }



        private void DoCopyText()
        {
            if (m_currentSelectedSegment > 0 && CurrentTab >= 0 && CurrentTabText >= 0)
            {
                m_clipboardText = (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor.m_textDescriptors[CurrentTabText].Serialize());
                m_pasteButtonText.isVisible = true;
            }
        }

        private void DoPasteText()
        {
            if (m_currentSelectedSegment > 0 && CurrentTab >= 0 && CurrentTabText >= 0 && m_clipboardText != null)
            {
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].cached = false;
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor.m_textDescriptors[CurrentTabText] = BoardTextDescriptorHigwaySign.Deserialize(m_clipboardText);
                ReloadTabInfoText();
            }
        }

        private void DoDeleteText()
        {
            if (m_currentSelectedSegment > 0 && CurrentTab >= 0 && CurrentTabText >= 0)
            {
                var tempList = BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor.m_textDescriptors.ToList();
                tempList.RemoveAt(CurrentTabText);
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor.m_textDescriptors = tempList.ToArray();
                ReloadTabInfo();

            }
        }

        private void Start()
        {
            m_propsDropdown.items = BoardGeneratorHighwaySigns.instance.LoadedProps.Select(x => Locale.Get("PROPS_TITLE", x)).ToArray();
        }

        private void SetPropModel(int idx)
        {
            if (idx > 0) SafeActionInBoard(descriptor => descriptor.m_propName = BoardGeneratorHighwaySigns.instance.LoadedProps[idx]);
        }

        private void SetPropColor(Color color)
        {
            SafeActionInBoard(descriptor => descriptor.m_color = color);
        }
        private void SetPropSegPosition(float value)
        {
            SafeActionInBoard(descriptor => descriptor.m_segmentPosition = value);
        }
        private void SetPropRelPosition(Vector3 value)
        {
            SafeActionInBoard(descriptor => descriptor.m_propPosition = value);
        }
        private void SetPropRelRotation(Vector3 value)
        {
            SafeActionInBoard(descriptor => descriptor.m_propRotation = value);
        }
        private void SetPropRelScale(Vector3 value)
        {
            SafeActionInBoard(descriptor => descriptor.PropScale = value);
        }
        private void SetInvertSignSide(bool value)
        {
            SafeActionInBoard(descriptor => descriptor.m_invertSign = value);
        }
        private void SafeActionInBoard(Action<BoardDescriptorHigwaySign> toDo)
        {
            if (m_currentSelectedSegment != 0 && !m_isLoading)
            {
                EnsureBoardsArrayIdx(CurrentTab);
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].cached = false;
                var descriptor = BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor;
                toDo(descriptor);
            }
        }

        private void SetTextOwnNameContent(int idx)
        {
            SafeActionInTextBoard(descriptor => descriptor.m_ownTextContent = (OwnNameContent)idx);
        }
        private void SetTextCustom(string txt)
        {
            SafeActionInTextBoard(descriptor =>
            {
                descriptor.m_fixedText = txt?.Replace("\\n", "\n");
                descriptor.m_cachedTextContent = (OwnNameContent)(-1);
            });
        }

        private void SetTextRelPosition(Vector3 value)
        {
            SafeActionInTextBoard(descriptor => descriptor.m_textRelativePosition = value);
        }
        private void SetTextRelRotation(Vector3 value)
        {
            SafeActionInTextBoard(descriptor => descriptor.m_textRelativeRotation = value);
        }

        private void SetTextColor(Color color)
        {
            SafeActionInTextBoard(descriptor => descriptor.m_defaultColor = color);
        }

        private void SetTextAlignmentHorizontal(int idx)
        {
            SafeActionInTextBoard(descriptor => descriptor.m_textAlign = (UIHorizontalAlignment)idx);
        }

        private void SetTextAlignmentVertical(int idx)
        {
            SafeActionInTextBoard(descriptor => descriptor.m_verticalAlign = (UIVerticalAlignment)idx);
        }

        private void SetTextMaxWidth(float val)
        {
            SafeActionInTextBoard(descriptor => descriptor.m_maxWidthMeters = val);
        }
        private void SetTextScale(float val)
        {
            SafeActionInTextBoard(descriptor => descriptor.m_textScale = val);
        }
        private void SetTextResizeYOnOverflow(bool value)
        {
            SafeActionInTextBoard(descriptor => descriptor.m_applyOverflowResizingOnY = value);
        }
        private void SetTextLumDay(float val)
        {
            SafeActionInTextBoard(descriptor => descriptor.m_dayEmissiveMultiplier = val);
        }
        private void SetTextLumNight(float val)
        {
            SafeActionInTextBoard(descriptor => descriptor.m_nightEmissiveMultiplier = val);
        }

        private void SafeActionInTextBoard(Action<BoardTextDescriptorHigwaySign> toDo)
        {
            if (m_currentSelectedSegment != 0 && !m_isLoading)
            {
                EnsureBoardsArrayIdx(CurrentTab, CurrentTabText);
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].cached = false;
                var descriptor = BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor.m_textDescriptors[CurrentTabText];
                toDo(descriptor);
            }
        }

        private void EnsureBoardsArrayIdx(int idx, int textIdx = -1)
        {
            if (idx >= 0)
            {
                if (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment] == null)
                {
                    BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment] = new BoardGeneratorHighwaySigns.BoardBunchContainerHighwaySign();
                }
                if (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData == null || BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData.Length <= idx)
                {
                    var oldArr = BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData;
                    BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData = new BoardGeneratorHighwaySigns.CacheControlHighwaySign[idx + 1];
                    if (oldArr != null && oldArr.Length > 0)
                    {
                        for (int i = 0; i < oldArr.Length && i <= idx; i++)
                        {
                            BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[i] = oldArr[i];
                        }
                    }
                }
                if (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx] == null)
                {
                    BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx] = new BoardGeneratorHighwaySigns.CacheControlHighwaySign();
                }
                if (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor == null)
                {
                    BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor = new BoardGeneratorHighwaySigns.BoardDescriptorHigwaySign();
                }

                EnsureTabQuantity(idx + 1);
            }
            if (textIdx < 0) return;
            if (idx < 0) idx = CurrentTab;
            if (idx < 0) return;

            if (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors == null || BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors.Length <= textIdx)
            {
                var oldArr = BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors;
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors = new BoardTextDescriptorHigwaySign[textIdx + 1];
                if (oldArr != null && oldArr.Length > 0)
                {
                    for (int i = 0; i < oldArr.Length && i <= textIdx; i++)
                    {
                        BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors[i] = oldArr[i];
                    }
                }
            }
            if (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors[textIdx] == null)
            {
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors[textIdx] = new BoardTextDescriptorHigwaySign
                {
                    m_defaultColor = Color.white,
                    m_useContrastColor = false
                };
            }
            EnsureTabQuantityTexts(textIdx + 1);
        }

        private void EnsureTabQuantity(int size)
        {
            var targetCount = Mathf.Max(m_pseudoTabstripProps.tabCount, size + 1);
            for (int i = 0; i < targetCount; i++)
            {
                if (i >= m_pseudoTabstripProps.tabCount)
                {
                    m_pseudoTabstripProps.AddTab($"{i}", CreateTab($"{i + 1}"), false);
                }

                ((UIButton)m_pseudoTabstripProps.tabs[i]).text = i == targetCount - 1 ? "+" : $"P{i + 1}";
            }
        }

        private void EnsureTabQuantityTexts(int size)
        {
            var targetCount = Mathf.Max(m_pseudoTabstripTexts.tabCount, size + 1);
            for (int i = 0; i < targetCount; i++)
            {
                if (i >= m_pseudoTabstripTexts.tabCount)
                {
                    m_pseudoTabstripTexts.AddTab($"{i}", CreateTab($"{i + 1}"), false);
                }

                ((UIButton)m_pseudoTabstripTexts.tabs[i]).text = i == targetCount - 1 ? "+" : $"T{i + 1}";
            }
        }

        private void ConfigureTabsShown(int quantity)
        {

            for (int i = 0; i < m_pseudoTabstripProps.tabCount; i++)
            {
                m_pseudoTabstripProps.tabs[i].isVisible = i < quantity || i == m_pseudoTabstripProps.tabCount - 1;
            }

        }
        private void ConfigureTabsShownText(int quantity)
        {

            for (int i = 0; i < m_pseudoTabstripTexts.tabCount; i++)
            {
                m_pseudoTabstripTexts.tabs[i].isVisible = i < quantity || i == m_pseudoTabstripTexts.tabCount - 1;
            }

        }

        private void EnablePickTool()
        {
            OnSegmentSet(0);
            DynamicTextBoardsMod.instance.controller.RoadSegmentToolInstance.OnSelectSegment += OnSegmentSet;
            DynamicTextBoardsMod.instance.controller.RoadSegmentToolInstance.enabled = true;
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
                var endNodeNum = DTBUtils.GetNumberAt(m_currentSelectedSegment, false);
                var startNodeNum = DTBUtils.GetNumberAt(m_currentSelectedSegment, true);
                m_selectionAddress.text = $"{NetManager.instance.GetSegmentName(m_currentSelectedSegment)}, {Mathf.Min(startNodeNum, endNodeNum)} - {Mathf.Max(startNodeNum, endNodeNum)}";

                EnsureTabQuantity(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData?.Length ?? -1);
                ConfigureTabsShown(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData?.Length ?? -1);
                m_isLoading = false;
            }
            m_pseudoTabPropsHelper.self.isVisible = m_currentSelectedSegment > 0 && CurrentTab >= 0;
            m_contentContainer.self.isVisible = m_currentSelectedSegment > 0;
        }

        private void OnChangeTab(int tabVal)
        {
            EnsureBoardsArrayIdx(tabVal);
            CurrentTab = tabVal;
        }


        private void OnChangeTabTexts(int tabVal)
        {
            EnsureBoardsArrayIdx(-1, tabVal);
            CurrentTabText = tabVal;
        }

        private void ReloadTabInfo()
        {
            m_pseudoTabPropsHelper.self.isVisible = m_currentSelectedSegment > 0 && CurrentTab >= 0;
            if (m_currentSelectedSegment <= 0 || CurrentTab < 0 || CurrentTab >= (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData?.Length ?? 0)) return;
            m_isLoading = true;
            LoadTabInfo(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor);
            EnsureTabQuantityTexts(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor?.m_textDescriptors?.Length ?? 0);
            ConfigureTabsShownText(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor?.m_textDescriptors?.Length ?? 0);
            m_isLoading = false;
            OnChangeTabTexts(-1);
        }
        private void LoadTabInfo(BoardDescriptorHigwaySign descriptor)
        {
            m_propsDropdown.selectedIndex = BoardGeneratorHighwaySigns.instance.LoadedProps.IndexOf(descriptor?.m_propName);
            m_segmentPosition.value = descriptor?.m_segmentPosition ?? 0.5f;
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
        private void ReloadTabInfoText()
        {
            m_pseudoTabTextsContainer.self.isVisible = CurrentTabText >= 0;
            if (m_currentSelectedSegment <= 0 || CurrentTab < 0 || CurrentTabText < 0) return;
            m_isLoading = true;
            LoadTabTextInfo(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor.m_textDescriptors[CurrentTabText]);
            m_isLoading = false;
        }

        private void LoadTabTextInfo(BoardTextDescriptorHigwaySign descriptor)
        {
            m_dropdownTextContent.selectedIndex = (int)(descriptor?.m_ownTextContent ?? OwnNameContent.None);
            m_customText.text = (descriptor?.m_fixedText?.Replace("\n", "\\n") ?? "");
            m_posVectorEditorText[0].text = (descriptor?.m_textRelativePosition.x ?? 0).ToString();
            m_posVectorEditorText[1].text = (descriptor?.m_textRelativePosition.y ?? 0).ToString();
            m_posVectorEditorText[2].text = (descriptor?.m_textRelativePosition.z ?? 0).ToString();
            m_rotVectorEditorText[0].text = (descriptor?.m_textRelativeRotation.x ?? 0).ToString();
            m_rotVectorEditorText[1].text = (descriptor?.m_textRelativeRotation.y ?? 0).ToString();
            m_rotVectorEditorText[2].text = (descriptor?.m_textRelativeRotation.z ?? 0).ToString();
            m_colorEditorText.selectedColor = descriptor?.m_defaultColor ?? Color.white;
            m_dropdownTextAlignHorizontal.selectedIndex = (int)(descriptor?.m_textAlign ?? UIHorizontalAlignment.Center);
            m_dropdownTextAlignVertical.selectedIndex = (int)(descriptor?.m_verticalAlign ?? UIVerticalAlignment.Middle);
            m_maxWidthText.text = (descriptor?.m_maxWidthMeters ?? 0).ToString();
            m_scaleText.text = (descriptor?.m_textScale ?? 0).ToString();
            m_textResizeYOnOverflow.isChecked = (descriptor?.m_applyOverflowResizingOnY ?? false);
            m_textLuminosityDay.value = (descriptor?.m_dayEmissiveMultiplier ?? 0);
            m_textLuminosityNight.value = (descriptor?.m_nightEmissiveMultiplier ?? 0);
        }

        private void CreateGroupFileSelect(string i18n, OnDropdownSelectionChanged onChanged, out UIDropDown dropDown)
        {
            dropDown = m_uiHelperHS.AddDropdownLocalized(i18n, new String[0], -1, onChanged);
            dropDown.width = 370;
            m_uiHelperHS.AddSpace(20);
        }

        #endregion

    }


}
