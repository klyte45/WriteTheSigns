using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ICities;
using Klyte.Commons.Extensors;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Libraries;
using Klyte.DynamicTextProps.Overrides;
using Klyte.DynamicTextProps.UI.Images;
using Klyte.DynamicTextProps.Utils;
using System;
using System.Linq;
using UnityEngine;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorHighwaySigns;

namespace Klyte.DynamicTextProps.UI
{

    internal class DTPPropPlacingTab : UICustomControl
    {
        public UIScrollablePanel MainContainer { get; private set; }

        private UIHelperExtension m_uiHelperHS;

        private UIDropDown m_fontSelect;


        private UIButton m_buttonTool;



        private UIHelperExtension m_contentContainer;
        private UITabstripAutoResize m_pseudoTabstripProps;

        private UIHelperExtension m_pseudoTabPropsHelper;
        private UILabel m_selectionAddress;

        private UITextField m_propItemName;
        private UIDropDown m_propsDropdown;
        private UISlider m_segmentPosition;
        private UICheckBox m_invertOrientation;
        private UITextField[] m_posVectorEditor;
        private UITextField[] m_rotVectorEditor;
        private UITextField[] m_scaleVectorEditor;
        private UIColorField m_colorEditor;

        private UIButton m_pasteGroupButton;

        private UIButton m_pasteButton;


        private UIHelperExtension m_pseudoTabTextsContainer;
        private UITabstripAutoResize m_pseudoTabstripTexts;

        private UITextField m_textItemName;
        private UIDropDown m_dropdownTextContent;
        private UITextField m_customText;
        private UIDropDown m_overrideFontText;
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

        private UIButton m_pasteButtonText;

        private UIDropDown m_loadPropGroup;
        private UIDropDown m_loadPropItem;
        private UIDropDown m_loadText;


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
        private string m_clipboardGroup;

        private bool m_isLoading = false;

        #region Awake
        public void Awake()
        {
            MainContainer = GetComponent<UIScrollablePanel>();

            m_uiHelperHS = new UIHelperExtension(MainContainer);

            AddDropdown(Locale.Get("K45_DTP_FONT_PLACED_PROPS"), out m_fontSelect, m_uiHelperHS, new string[0], OnSetFont);
            m_fontSelect.width -= 40;
            UIPanel parent = m_fontSelect.GetComponentInParent<UIPanel>();
            UIButton actionButton = ConfigureActionButton(parent);
            SetIcon(actionButton, CommonSpriteNames.Reload, Color.white);
            actionButton.eventClick += (x, t) => DTPUtils.ReloadFontsOf<BoardGeneratorHighwaySigns>(m_fontSelect);
            DTPUtils.ReloadFontsOf<BoardGeneratorHighwaySigns>(m_fontSelect);

            m_buttonTool = (UIButton) m_uiHelperHS.AddButton(Locale.Get("K45_DTP_PICK_A_SEGMENT"), EnablePickTool);
            KlyteMonoUtils.LimitWidth(m_buttonTool, m_uiHelperHS.Self.width - 20, true);

            m_contentContainer = m_uiHelperHS.AddGroupExtended(Locale.Get("K45_DTP_PICKED_SEGMENT_DATA"));
            ((UIPanel) m_contentContainer.Self).backgroundSprite = "";
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

                    BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment] = XmlUtils.DefaultXmlDeserialize<BoardBunchContainerHighwaySignXml>(XmlUtils.DefaultXmlSerialize(x));
                    ReloadSegment();
                },
                () => BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]);

            KlyteMonoUtils.CreateHorizontalScrollPanel(m_contentContainer.Self, out UIScrollablePanel scrollTabs, out UIScrollbar bar, m_contentContainer.Self.width - 20, 40, Vector3.zero);


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
                    int nextIdx = BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData?.Length ?? 0;
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
            ((UIPanel) m_pseudoTabPropsHelper.Self).backgroundSprite = "";

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

                                        BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor = XmlUtils.DefaultXmlDeserialize<BoardDescriptorHigwaySignXml>(XmlUtils.DefaultXmlSerialize(x));
                                        ReloadSegment();
                                    },
                            () => BoardGeneratorHighwaySigns.m_boardsContainers?.ElementAtOrDefault(m_currentSelectedSegment)?.m_boardsData?.ElementAtOrDefault(CurrentTab)?.descriptor);


            UIHelperExtension groupProp = m_pseudoTabPropsHelper.AddTogglableGroup(Locale.Get("K45_DTP_PROP_CONFIGURATION"));
            AddTextField(Locale.Get("K45_DTP_PROP_TAB_TITLE"), out m_propItemName, groupProp, SetPropItemName);

            AddDropdown(Locale.Get("K45_DTP_PROP_MODEL_SELECT"), out m_propsDropdown, groupProp, new string[0], SetPropModel);
            AddSlider(Locale.Get("K45_DTP_SEGMENT_RELATIVE_POS"), out m_segmentPosition, groupProp, SetPropSegPosition, 0, 1, 0.01f);
            m_invertOrientation = (UICheckBox) groupProp.AddCheckbox(Locale.Get("K45_DTP_INVERT_SIGN_SIDE"), false, SetInvertSignSide);

            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_POS"), out m_posVectorEditor, groupProp, SetPropRelPosition);
            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_ROT"), out m_rotVectorEditor, groupProp, SetPropRelRotation);
            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_SCALE"), out m_scaleVectorEditor, groupProp, SetPropRelScale);

            m_colorEditor = groupProp.AddColorPicker(Locale.Get("K45_DTP_PAINTING_COLOR"), Color.white, SetPropColor);
            KlyteMonoUtils.LimitWidth(m_colorEditor.parent.GetComponentInChildren<UILabel>(), groupProp.Self.width / 2, true);


            KlyteMonoUtils.CreateHorizontalScrollPanel(m_pseudoTabPropsHelper.Self, out scrollTabs, out bar, m_pseudoTabPropsHelper.Self.width - 20, 40, Vector3.zero);

            KlyteMonoUtils.CreateUIElement(out m_pseudoTabstripTexts, scrollTabs.transform, "DTPTabstrip", new Vector4(5, 40, m_pseudoTabPropsHelper.Self.width - 10, 40));
            m_pseudoTabstripTexts.startSelectedIndex = -1;
            m_pseudoTabstripTexts.selectedIndex = -1;
            m_pseudoTabstripTexts.closeOnReclick = true;
            m_pseudoTabstripTexts.AutoFitChildrenHorizontally = true;

            m_pseudoTabstripTexts.AddTab("+", CreateTabTemplate(), false);

            m_pseudoTabstripTexts.eventSelectedIndexChanged += (x, idx) =>
            {
                if (idx == m_pseudoTabstripTexts.tabCount - 1)
                {
                    Vector3 pos = MainContainer.verticalScrollbar.relativePosition;
                    EnsureBoardsArrayIdx(-1, BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor?.m_textDescriptors?.Length ?? 0);
                    ReloadTabInfo();
                    OnChangeTabTexts(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor.m_textDescriptors.Length - 1);
                    MainContainer.verticalScrollbar.relativePosition = pos;
                    ReloadTabInfoText();
                }
                else
                {
                    ReloadTabInfoText();
                }
            };

            m_pseudoTabTextsContainer = m_pseudoTabPropsHelper.AddGroupExtended("))))");
            Destroy(m_pseudoTabTextsContainer.Self.parent.GetComponentInChildren<UILabel>().gameObject);
            ((UIPanel) m_pseudoTabTextsContainer.Self).backgroundSprite = "";
            m_pseudoTabTextsContainer.Self.isVisible = false;
            m_pseudoTabTextsContainer.Self.width = MainContainer.width - 50;

            m_loadText = AddLibBox<DTPLibTextMeshHighwaySigns, BoardTextDescriptorHighwaySignsXml>(Locale.Get("K45_DTP_PROP_TEXT_LIB_TITLE"), m_pseudoTabTextsContainer,
                                    out UIButton m_copyButtonText, DoCopyText,
                                    out m_pasteButtonText, DoPasteText,
                                    out UIButton m_deleteButtonText, DoDeleteText,
                                    (x) =>
                                    {
                                        if (m_currentSelectedSegment == 0 || CurrentTab < 0 || CurrentTabText < 0)
                                        {
                                            return;
                                        }

                                        BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor.m_textDescriptors[CurrentTabText] = XmlUtils.DefaultXmlDeserialize<BoardTextDescriptorHighwaySignsXml>(XmlUtils.DefaultXmlSerialize(x));
                                        ReloadSegment();
                                    },
                            () => BoardGeneratorHighwaySigns.m_boardsContainers?.ElementAtOrDefault(m_currentSelectedSegment)?.m_boardsData?.ElementAtOrDefault(CurrentTab)?.descriptor?.m_textDescriptors?.ElementAtOrDefault(CurrentTabText));


            UIHelperExtension groupTexts = m_pseudoTabTextsContainer.AddTogglableGroup(Locale.Get("K45_DTP_TEXTS_COMMON_CONFIGURATION"), out UILabel lblTxt);

            AddTextField(Locale.Get("K45_DTP_TEXT_TAB_TITLE"), out m_textItemName, groupTexts, SetTextItemName);
            m_colorEditorText = groupTexts.AddColorPicker(Locale.Get("K45_DTP_TEXT_COLOR"), Color.white, SetTextColor);
            KlyteMonoUtils.LimitWidth(m_colorEditorText.parent.GetComponentInChildren<UILabel>(), groupTexts.Self.width / 2, true);
            AddDropdown(Locale.Get("K45_DTP_TEXT_CONTENT"), out m_dropdownTextContent, groupTexts, Enum.GetNames(typeof(OwnNameContent)).Select(x => Locale.Get("K45_DTP_OWN_NAME_CONTENT", x)).ToArray(), SetTextOwnNameContent);
            AddTextField(Locale.Get("K45_DTP_CUSTOM_TEXT"), out m_customText, groupTexts, SetTextCustom);
            m_dropdownTextContent.eventSelectedIndexChanged += (e, idx) => m_customText.GetComponentInParent<UIPanel>().isVisible = idx == (int) OwnNameContent.Custom;
            AddDropdown(Locale.Get("K45_DTP_OVERRIDE_FONT"), out m_overrideFontText, groupTexts, new string[0], SetOverrideFont);

            groupTexts = m_pseudoTabTextsContainer.AddTogglableGroup(Locale.Get("K45_DTP_TEXTS_SIZE_POSITION"));
            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_POS"), out m_posVectorEditorText, groupTexts, SetTextRelPosition);
            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_ROT"), out m_rotVectorEditorText, groupTexts, SetTextRelRotation);
            AddFloatField(Locale.Get("K45_DTP_TEXT_SCALE"), out m_scaleText, groupTexts, SetTextScale, false);
            AddFloatField(Locale.Get("K45_DTP_MAX_WIDTH_METERS"), out m_maxWidthText, groupTexts, SetTextMaxWidth, false);
            m_textResizeYOnOverflow = (UICheckBox) groupTexts.AddCheckbox(Locale.Get("K45_DTP_RESIZE_Y_TEXT_OVERFLOW"), false, SetTextResizeYOnOverflow);

            groupTexts = m_pseudoTabTextsContainer.AddTogglableGroup(Locale.Get("K45_DTP_TEXTS_2D_ALIGNMENT"));
            AddDropdown(Locale.Get("K45_DTP_TEXT_ALIGN_HOR"), out m_dropdownTextAlignHorizontal, groupTexts, Enum.GetNames(typeof(UIHorizontalAlignment)).Select(x => Locale.Get("K45_ALIGNMENT", x)).ToArray(), SetTextAlignmentHorizontal);
            AddDropdown(Locale.Get("K45_DTP_TEXT_ALIGN_VER"), out m_dropdownTextAlignVertical, groupTexts, Enum.GetNames(typeof(UIVerticalAlignment)).Select(x => Locale.Get("K45_VERT_ALIGNMENT", x)).ToArray(), SetTextAlignmentVertical);

            groupTexts = m_pseudoTabTextsContainer.AddTogglableGroup(Locale.Get("K45_DTP_TEXT_EFFECTS"));
            AddSlider(Locale.Get("K45_DTP_LUMINOSITY_DAY"), out m_textLuminosityDay, groupTexts, SetTextLumDay, 0, 10f, 0.25f);
            AddSlider(Locale.Get("K45_DTP_LUMINOSITY_NIGHT"), out m_textLuminosityNight, groupTexts, SetTextLumNight, 0, 10f, 0.25f);


            OnSegmentSet(0);
        }

        private UIDropDown AddLibBox<LIB, DESC>(string groupTitle, UIHelperExtension parentHelper,
            out UIButton copyButton, OnButtonClicked actionCopy,
            out UIButton pasteButton, OnButtonClicked actionPaste,
            out UIButton deleteButton, OnButtonClicked actionDelete,
            Action<DESC> onLoad, Func<DESC> getContentToSave) where LIB : BasicLib<LIB, DESC>, new() where DESC : ILibable
        {
            UIHelperExtension groupLibPropGroup = parentHelper.AddTogglableGroup(groupTitle);

            UIHelperExtension subPanelActionsBar = groupLibPropGroup.AddGroupExtended("!!!!", out UILabel voide, out UIPanel voide2);
            Destroy(voide);
            ((UIPanel) subPanelActionsBar.Self).autoLayoutDirection = LayoutDirection.Horizontal;
            ((UIPanel) subPanelActionsBar.Self).wrapLayout = false;
            ((UIPanel) subPanelActionsBar.Self).autoLayout = true;
            ((UIPanel) subPanelActionsBar.Self).autoFitChildrenHorizontally = true;
            ((UIPanel) subPanelActionsBar.Self).autoFitChildrenVertically = true;

            copyButton = ConfigureActionButton(subPanelActionsBar.Self);
            copyButton.eventClick += (x, y) => actionCopy();
            pasteButton = ConfigureActionButton(subPanelActionsBar.Self);
            pasteButton.eventClick += (x, y) => actionPaste();
            deleteButton = ConfigureActionButton(subPanelActionsBar.Self);
            deleteButton.eventClick += (x, y) => actionDelete();

            SetIcon(copyButton, CommonSpriteNames.Copy, Color.white);
            SetIcon(pasteButton, CommonSpriteNames.Paste, Color.white);
            SetIcon(deleteButton, CommonSpriteNames.RemoveIcon, Color.white);

            deleteButton.color = Color.red;

            pasteButton.isVisible = false;


            AddDropdown(Locale.Get("K45_DTP_LOAD_FROM_LIB"), out UIDropDown loadDD, groupLibPropGroup, BasicLib<LIB, DESC>.Instance.List().ToArray(), (x) => { });
            loadDD.width -= 80;
            UIPanel parent = loadDD.GetComponentInParent<UIPanel>();
            UIButton actionButton = ConfigureActionButton(parent);
            SetIcon(actionButton, CommonSpriteNames.Load, Color.white);
            actionButton.eventClick += (x, t) =>
            {
                if (m_currentSelectedSegment > 0)
                {
                    DESC groupInfo = BasicLib<LIB, DESC>.Instance.Get(loadDD.selectedValue);
                    if (groupInfo != null)
                    {
                        onLoad(groupInfo);
                    }
                }
            };
            KlyteMonoUtils.CreateUIElement(out actionButton, parent.transform, "DelBtn");
            actionButton = ConfigureActionButton(parent);
            actionButton.color = Color.red;
            SetIcon(actionButton, CommonSpriteNames.RemoveIcon, Color.white);
            actionButton.eventClick += (x, t) =>
            {
                if (m_currentSelectedSegment > 0)
                {
                    DESC groupInfo = BasicLib<LIB, DESC>.Instance.Get(loadDD.selectedValue);
                    if (groupInfo != null)
                    {
                        BasicLib<LIB, DESC>.Instance.Remove(loadDD.selectedValue);
                        loadDD.items = BasicLib<LIB, DESC>.Instance.List().ToArray();
                    }
                }
            };

            AddTextField(Locale.Get("K45_DTP_SAVE_TO_LIB"), out UITextField saveTxt, groupLibPropGroup, (x) => { });
            saveTxt.width -= 40;
            parent = saveTxt.GetComponentInParent<UIPanel>();
            actionButton = ConfigureActionButton(parent);
            SetIcon(actionButton, CommonSpriteNames.Save, Color.white);
            actionButton.eventClick += (x, t) =>
            {
                if (m_currentSelectedSegment > 0 && !saveTxt.text.IsNullOrWhiteSpace())
                {
                    BasicLib<LIB, DESC>.Instance.Add(saveTxt.text, getContentToSave());
                    loadDD.items = BasicLib<LIB, DESC>.Instance.List().ToArray();
                    loadDD.selectedValue = saveTxt.text;
                }
            };

            return loadDD;
        }

        private static void SetIcon(UIButton copyButton, CommonSpriteNames spriteName, Color color)
        {
            UISprite icon = copyButton.AddUIComponent<UISprite>();
            icon.relativePosition = new Vector3(2, 2);
            icon.width = 36;
            icon.height = 36;
            icon.spriteName = DTPResourceLoader.instance.GetDefaultSpriteNameFor(spriteName);
            icon.color = color;
        }

        private static UIButton ConfigureActionButton(UIComponent parent)
        {
            KlyteMonoUtils.CreateUIElement(out UIButton actionButton, parent.transform, "DTPBtn");
            KlyteMonoUtils.InitButton(actionButton, false, "ButtonMenu");
            actionButton.focusedBgSprite = "";
            actionButton.autoSize = false;
            actionButton.width = 40;
            actionButton.height = 40;
            actionButton.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            return actionButton;
        }

        private void AddSlider(string label, out UISlider slider, UIHelperExtension parentHelper, OnValueChanged onChange, float min, float max, float step)
        {
            slider = (UISlider) parentHelper.AddSlider(label, min, max, step, min, onChange);
            slider.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            slider.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            KlyteMonoUtils.LimitWidth(slider.parent.GetComponentInChildren<UILabel>(), (parentHelper.Self.width / 2) - 10, true);
        }

        private void AddVector3Field(string label, out UITextField[] fieldArray, UIHelperExtension parentHelper, Action<Vector3> onChange)
        {
            fieldArray = parentHelper.AddVector3Field(label, Vector3.zero, onChange);
            KlyteMonoUtils.LimitWidth(fieldArray[0].parent.GetComponentInChildren<UILabel>(), (parentHelper.Self.width / 2) - 10, true);
        }

        private void AddFloatField(string label, out UITextField field, UIHelperExtension parentHelper, Action<float> onChange, bool acceptNegative)
        {
            field = parentHelper.AddFloatField(label, 0, onChange, acceptNegative);
            KlyteMonoUtils.LimitWidth(field.parent.GetComponentInChildren<UILabel>(), (parentHelper.Self.width / 2) - 10, true);
        }

        private void AddDropdown(string title, out UIDropDown dropdown, UIHelperExtension parentHelper, string[] options, OnDropdownSelectionChanged onChange) => AddDropdown(title, out dropdown, out UILabel label, parentHelper, options, onChange);
        private void AddDropdown(string title, out UIDropDown dropdown, out UILabel label, UIHelperExtension parentHelper, string[] options, OnDropdownSelectionChanged onChange)
        {
            dropdown = (UIDropDown) parentHelper.AddDropdown(title, options, 0, onChange);
            dropdown.width = (parentHelper.Self.width / 2) - 10;
            dropdown.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            dropdown.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            label = dropdown.parent.GetComponentInChildren<UILabel>();
            KlyteMonoUtils.LimitWidth(label, (parentHelper.Self.width / 2) - 10, true);
        }

        private void AddTextField(string title, out UITextField textField, UIHelperExtension parentHelper, OnTextSubmitted onChange) => AddTextField(title, out textField, out UILabel label, parentHelper, onChange);

        private void AddTextField(string title, out UITextField textField, out UILabel label, UIHelperExtension parentHelper, OnTextSubmitted onChange)
        {
            textField = parentHelper.AddTextField(title, (x) => { }, "", onChange);
            textField.width = (parentHelper.Self.width / 2) - 10;
            textField.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            textField.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            label = textField.parent.GetComponentInChildren<UILabel>();
            KlyteMonoUtils.LimitWidth(label, (parentHelper.Self.width / 2) - 10, true);
        }

        private UIButton m_tabModel;

        private UIButton CreateTabTemplate()
        {
            if (m_tabModel == null)
            {
                KlyteMonoUtils.CreateUIElement(out UIButton tabTemplate, DynamicTextPropsMod.Instance.Controller.gameObject.transform, "DTPTabTemplate");
                KlyteMonoUtils.InitButton(tabTemplate, false, "GenericTab");
                tabTemplate.autoSize = true;
                tabTemplate.textPadding = new RectOffset(10, 10, 10, 7);
                tabTemplate.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
                m_tabModel = tabTemplate;
            }
            return m_tabModel;
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
                LogUtils.DoLog($"  BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData.length = {  BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData.Length }");
                var tempList = BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData.ToList();
                tempList.RemoveAt(CurrentTab);
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData = tempList.ToArray();
                LogUtils.DoLog($"  BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData.length pos = {  BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData.Length }");
                OnSegmentSet(m_currentSelectedSegment);
            }
        }

        private void DoCopyGroup()
        {
            if (m_currentSelectedSegment > 0)
            {
                m_clipboardGroup = XmlUtils.DefaultXmlSerialize(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]);
                m_pasteGroupButton.isVisible = true;
            }
        }

        private void DoPasteGroup()
        {
            if (m_currentSelectedSegment > 0 && m_clipboardGroup != null)
            {
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment] = XmlUtils.DefaultXmlDeserialize<BoardBunchContainerHighwaySignXml>(m_clipboardGroup);
                ReloadSegment();
            }
        }

        private void DoDeleteGroup()
        {
            if (m_currentSelectedSegment > 0)
            {
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment] = new BoardBunchContainerHighwaySignXml();
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
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor.m_textDescriptors[CurrentTabText] = BoardTextDescriptorHighwaySignsXml.Deserialize(m_clipboardText);
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

        public void Start() => m_propsDropdown.items = BoardGeneratorHighwaySigns.Instance.LoadedProps.Select(x => x.EndsWith("_Data") ? Locale.Get("PROPS_TITLE", x) : x).ToArray();

        private void SetPropModel(int idx)
        {
            if (idx >= 0)
            {
                SafeActionInBoard(descriptor => descriptor.m_propName = BoardGeneratorHighwaySigns.Instance.LoadedProps[idx]);
            }
        }

        private void SetPropItemName(string txt)
        {
            SafeActionInBoard(descriptor =>
            {
                descriptor.SaveName = txt;
                EnsureTabQuantity(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData?.Length ?? -1);
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
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].cached = false;
                BoardDescriptorHigwaySignXml descriptor = BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor;
                toDo(descriptor);
            }
        }

        private void SetTextOwnNameContent(int idx) => SafeActionInTextBoard(descriptor => descriptor.m_ownTextContent = (OwnNameContent) idx);
        private void SetTextCustom(string txt)
        {
            SafeActionInTextBoard(descriptor =>
            {
                descriptor.m_fixedText = txt?.Replace("\\n", "\n");
                descriptor.m_cachedTextContent = (OwnNameContent) (-1);
            });
        }
        private void SetTextItemName(string txt)
        {
            SafeActionInTextBoard(descriptor =>
            {
                descriptor.SaveName = txt;
                EnsureTabQuantityTexts(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData?.ElementAtOrDefault(CurrentTab).descriptor?.m_textDescriptors?.Length ?? -1);
            });
        }

        private void SetTextRelPosition(Vector3 value) => SafeActionInTextBoard(descriptor => descriptor.m_textRelativePosition = value);
        private void SetTextRelRotation(Vector3 value) => SafeActionInTextBoard(descriptor => descriptor.m_textRelativeRotation = value);

        private void SetTextColor(Color color) => SafeActionInTextBoard(descriptor => descriptor.m_defaultColor = color);

        private void SetTextAlignmentHorizontal(int idx) => SafeActionInTextBoard(descriptor => descriptor.m_textAlign = (UIHorizontalAlignment) idx);

        private void SetTextAlignmentVertical(int idx) => SafeActionInTextBoard(descriptor => descriptor.m_verticalAlign = (UIVerticalAlignment) idx);

        private void SetTextMaxWidth(float val) => SafeActionInTextBoard(descriptor => descriptor.m_maxWidthMeters = val);
        private void SetTextScale(float val) => SafeActionInTextBoard(descriptor => descriptor.m_textScale = val);
        private void SetTextResizeYOnOverflow(bool value) => SafeActionInTextBoard(descriptor => descriptor.m_applyOverflowResizingOnY = value);
        private void SetTextLumDay(float val) => SafeActionInTextBoard(descriptor => descriptor.m_dayEmissiveMultiplier = val);
        private void SetTextLumNight(float val) => SafeActionInTextBoard(descriptor => descriptor.m_nightEmissiveMultiplier = val);

        private void SetOverrideFont(int idx)
        {
            SafeActionInTextBoard(descriptor =>
            {
                descriptor.m_overrideFont = idx > 0 ? m_overrideFontText.selectedValue : null;
                descriptor.m_cachedTextContent = (OwnNameContent) (-1);
            });
        }

        private void OnSetFont(int idx)
        {
            if (idx >= 0)
            {
                BoardGeneratorHighwaySigns.Instance.ChangeFont(idx == 0 ? null : m_fontSelect.items[idx]);
            }
        }

        private void SafeActionInTextBoard(Action<BoardTextDescriptorHighwaySignsXml> toDo)
        {
            if (m_currentSelectedSegment != 0 && !m_isLoading)
            {
                EnsureBoardsArrayIdx(CurrentTab, CurrentTabText);
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].cached = false;
                BoardTextDescriptorHighwaySignsXml descriptor = BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[CurrentTab].descriptor.m_textDescriptors[CurrentTabText];
                toDo(descriptor);
            }
        }

        private void EnsureBoardsArrayIdx(int idx, int textIdx = -1)
        {
            if (idx >= 0)
            {
                if (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment] == null)
                {
                    BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment] = new BoardGeneratorHighwaySigns.BoardBunchContainerHighwaySignXml();
                }
                if (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData == null || BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData.Length <= idx)
                {
                    CacheControlHighwaySign[] oldArr = BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData;
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
                    BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor = new BoardGeneratorHighwaySigns.BoardDescriptorHigwaySignXml();
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

            if (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors == null || BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors.Length <= textIdx)
            {
                BoardTextDescriptorHighwaySignsXml[] oldArr = BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors;
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors = new BoardTextDescriptorHighwaySignsXml[textIdx + 1];
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
                BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor.m_textDescriptors[textIdx] = new BoardTextDescriptorHighwaySignsXml
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
                else if (m_currentSelectedSegment > 0 && i < (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData?.Length ?? 0))
                {
                    ((UIButton) m_pseudoTabstripProps.tabs[i]).text = (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData[i]?.descriptor?.SaveName).IsNullOrWhiteSpace() ? $"P{i + 1}" : BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData[i]?.descriptor?.SaveName;
                }
                else
                {
                    ((UIButton) m_pseudoTabstripProps.tabs[i]).text = $"P{i + 1}";
                }

            }
        }

        private void EnsureTabQuantityTexts(int size)
        {
            int targetCount = Mathf.Max(m_pseudoTabstripTexts.tabCount, size + 1);
            for (int i = 0; i < targetCount; i++)
            {
                if (i >= m_pseudoTabstripTexts.tabCount)
                {
                    m_pseudoTabstripTexts.AddTab($"{i}", CreateTabTemplate(), false);
                }
                if (i == targetCount - 1)
                {
                    ((UIButton) m_pseudoTabstripTexts.tabs[i]).text = "+";
                }
                else if (m_currentSelectedSegment > 0 && CurrentTab >= 0 && i < (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData?.ElementAtOrDefault(CurrentTab)?.descriptor.m_textDescriptors?.Length ?? 0))
                {
                    ((UIButton) m_pseudoTabstripTexts.tabs[i]).text = (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor.m_textDescriptors?.ElementAtOrDefault(i)?.SaveName).IsNullOrWhiteSpace() ? $"T{i + 1}" : (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor.m_textDescriptors[i]?.SaveName);
                }
                else
                {
                    ((UIButton) m_pseudoTabstripTexts.tabs[i]).text = $"T{i + 1}";
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
            DynamicTextPropsMod.Instance.Controller.RoadSegmentToolInstance.OnSelectSegment += OnSegmentSet;
            DynamicTextPropsMod.Instance.Controller.RoadSegmentToolInstance.enabled = true;
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

                EnsureTabQuantity(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData?.Length ?? -1);
                ConfigureTabsShown(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData?.Length ?? -1);
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


        private void OnChangeTabTexts(int tabVal)
        {
            EnsureBoardsArrayIdx(-1, tabVal);
            CurrentTabText = tabVal;
        }

        private void ReloadTabInfo()
        {
            m_pseudoTabPropsHelper.Self.isVisible = m_currentSelectedSegment > 0 && CurrentTab >= 0;
            if (m_currentSelectedSegment <= 0 || CurrentTab < 0 || CurrentTab >= (BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData?.Length ?? 0))
            {
                return;
            }

            m_isLoading = true;
            LoadTabInfo(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor);
            EnsureTabQuantityTexts(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor?.m_textDescriptors?.Length ?? 0);
            ConfigureTabsShownText(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor?.m_textDescriptors?.Length ?? 0);
            m_isLoading = false;
            m_loadPropItem.items = DTPLibPropSingleHighwaySigns.Instance.List().ToArray();
            OnChangeTabTexts(-1);
        }
        private void LoadTabInfo(BoardDescriptorHigwaySignXml descriptor)
        {
            m_propsDropdown.selectedIndex = BoardGeneratorHighwaySigns.Instance.LoadedProps.IndexOf(descriptor?.m_propName);
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
        private void ReloadTabInfoText()
        {
            m_pseudoTabTextsContainer.Self.isVisible = CurrentTabText >= 0;
            if (m_currentSelectedSegment <= 0 || CurrentTab < 0 || CurrentTabText < 0)
            {
                return;
            }

            m_isLoading = true;
            LoadTabTextInfo(BoardGeneratorHighwaySigns.m_boardsContainers[m_currentSelectedSegment]?.m_boardsData[CurrentTab]?.descriptor.m_textDescriptors[CurrentTabText]);
            m_loadText.items = DTPLibTextMeshHighwaySigns.Instance.List().ToArray();
            m_isLoading = false;
        }

        private void LoadTabTextInfo(BoardTextDescriptorHighwaySignsXml descriptor)
        {
            m_textItemName.text = descriptor?.SaveName ?? "";
            m_dropdownTextContent.selectedIndex = (int) (descriptor?.m_ownTextContent ?? OwnNameContent.None);
            m_customText.text = (descriptor?.m_fixedText?.Replace("\n", "\\n") ?? "");
            m_posVectorEditorText[0].text = (descriptor?.m_textRelativePosition.x ?? 0).ToString();
            m_posVectorEditorText[1].text = (descriptor?.m_textRelativePosition.y ?? 0).ToString();
            m_posVectorEditorText[2].text = (descriptor?.m_textRelativePosition.z ?? 0).ToString();
            m_rotVectorEditorText[0].text = (descriptor?.m_textRelativeRotation.x ?? 0).ToString();
            m_rotVectorEditorText[1].text = (descriptor?.m_textRelativeRotation.y ?? 0).ToString();
            m_rotVectorEditorText[2].text = (descriptor?.m_textRelativeRotation.z ?? 0).ToString();
            m_colorEditorText.selectedColor = descriptor?.m_defaultColor ?? Color.white;
            m_dropdownTextAlignHorizontal.selectedIndex = (int) (descriptor?.m_textAlign ?? UIHorizontalAlignment.Center);
            m_dropdownTextAlignVertical.selectedIndex = (int) (descriptor?.m_verticalAlign ?? UIVerticalAlignment.Middle);
            m_maxWidthText.text = (descriptor?.m_maxWidthMeters ?? 0).ToString();
            m_scaleText.text = (descriptor?.m_textScale ?? 0).ToString();
            m_textResizeYOnOverflow.isChecked = (descriptor?.m_applyOverflowResizingOnY ?? false);
            m_textLuminosityDay.value = (descriptor?.m_dayEmissiveMultiplier ?? 0);
            m_textLuminosityNight.value = (descriptor?.m_nightEmissiveMultiplier ?? 0);
            ReloadFontsOverride(m_overrideFontText, descriptor?.m_overrideFont);
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

        #endregion

    }


}
