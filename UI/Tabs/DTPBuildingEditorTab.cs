using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ICities;
using Klyte.Commons.Extensors;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Libraries;
using Klyte.DynamicTextProps.Overrides;
using Klyte.DynamicTextProps.TextureAtlas;
using Klyte.DynamicTextProps.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.DynamicTextProps.UI
{

    internal class DTPBuildingEditorTab : UICustomControl
    {
        public static DTPBuildingEditorTab Instance { get; private set; }

        public UIScrollablePanel MainContainer { get; private set; }

        private UIHelperExtension m_uiHelperHS;

        private UIDropDown m_fontSelect;


        private UIButton m_buttonTool;

        private UIButton m_saveOnAssetFolderButton;


        private UIHelperExtension m_contentContainer;
        private UITabstripAutoResize m_pseudoTabstripProps;

        private UIHelperExtension m_pseudoTabPropsHelper;
        private UILabel m_buildingName;

        private UITextField m_propItemName;
        private UIDropDown m_propsDropdown;
        private UICheckBox m_showIfNoLineCheck;
        private UITextField[] m_posVectorEditor;
        private UITextField[] m_rotVectorEditor;
        private UITextField[] m_scaleVectorEditor;
        private UITextField[] m_arrayRepeatEditor;
        private UITextField m_arrayTimes;


        private UIDropDown m_colorModeDropdown;
        private UIColorField m_colorEditor;
        private CheckboxOrdernatedListPlatformItem m_checkboxOrdernatedListPlatform;

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
        private UICheckBox m_useContrastColorTextCheckbox;
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


        public string m_currentBuildingName;
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
            Instance = this;
            MainContainer = GetComponent<UIScrollablePanel>();

            m_uiHelperHS = new UIHelperExtension(MainContainer);

            AddDropdown(Locale.Get("K45_DTP_FONT_STATIONS"), out m_fontSelect, m_uiHelperHS, new string[0], OnSetFont);
            m_fontSelect.width -= 40;
            UIPanel parent = m_fontSelect.GetComponentInParent<UIPanel>();
            UIButton actionButton = ConfigureActionButton(parent);
            SetIcon(actionButton, "Reload", Color.white);
            actionButton.eventClick += (x, t) => DTPUtils.ReloadFontsOf<BoardGeneratorBuildings>(m_fontSelect);
            DTPUtils.ReloadFontsOf<BoardGeneratorBuildings>(m_fontSelect);



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
                    ReloadBuilding();
                },
                () => BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName],
                (helper) =>
                {
                    helper.AddButton(Locale.Get("K45_DTP_SAVE_OVERRIDE_FOLDER"), () => BoardGeneratorBuildings.SaveInCommonFolder(m_currentBuildingName));
                    m_saveOnAssetFolderButton = (UIButton) helper.AddButton(Locale.Get("K45_DTP_SAVE_ASSET_FOLDER"), () => BoardGeneratorBuildings.SaveInAssetFolder(m_currentBuildingName));
                    helper.AddButton(Locale.Get("K45_DTP_RELOAD_CONFIGS"), LoadAllBuildingConfigurations);
                });

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
                                        ReloadBuilding();
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
                    EnsureBoardsArrayIdx(-1, BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors?.ElementAtOrDefault(CurrentTab)?.m_textDescriptors?.Length ?? 0);
                    ReloadTabInfo();
                    OnChangeTabTexts(BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors.ElementAtOrDefault(CurrentTab).m_textDescriptors.Length - 1);
                    MainContainer.verticalScrollbar.relativePosition = pos;
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

            m_loadText = AddLibBox<DTPLibTextMeshBuildingSigns, BoardTextDescriptorBuildingsXml>(Locale.Get("K45_DTP_PROP_TEXT_LIB_TITLE"), m_pseudoTabTextsContainer,
                                    out UIButton m_copyButtonText, DoCopyText,
                                    out m_pasteButtonText, DoPasteText,
                                    out UIButton m_deleteButtonText, DoDeleteText,
                                    (x) =>
                                    {
                                        if (m_currentBuildingName == null || CurrentTab < 0 || CurrentTabText < 0)
                                        {
                                            return;
                                        }

                                        BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[CurrentTab].m_textDescriptors[CurrentTabText] = XmlUtils.DefaultXmlDeserialize<BoardTextDescriptorBuildingsXml>(XmlUtils.DefaultXmlSerialize(x));
                                        ReloadBuilding();
                                    },
                            () => BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors?.ElementAtOrDefault(CurrentTab)?.m_textDescriptors?.ElementAtOrDefault(CurrentTabText));


            UIHelperExtension groupTexts = m_pseudoTabTextsContainer.AddTogglableGroup(Locale.Get("K45_DTP_TEXTS_COMMON_CONFIGURATION"), out UILabel lblTxt);

            AddTextField(Locale.Get("K45_DTP_TEXT_TAB_TITLE"), out m_textItemName, groupTexts, SetTextItemName);
            m_colorEditorText = groupTexts.AddColorPicker(Locale.Get("K45_DTP_TEXT_COLOR"), Color.white, SetTextColor);
            KlyteMonoUtils.LimitWidth(m_colorEditorText.parent.GetComponentInChildren<UILabel>(), groupTexts.Self.width / 2, true);
            m_useContrastColorTextCheckbox = groupTexts.AddCheckboxLocale("K45_DTP_USE_CONTRAST_COLOR", false, SetUseContrastColor);
            AddDropdown(Locale.Get("K45_DTP_TEXT_CONTENT"), out m_dropdownTextContent, groupTexts, BoardGeneratorBuildings.AVAILABLE_TEXT_TYPES.Select(x => Locale.Get("K45_DTP_OWN_NAME_CONTENT_BUILDING", x.ToString())).ToArray(), SetTextOwnNameContent);
            AddTextField(Locale.Get("K45_DTP_CUSTOM_TEXT"), out m_customText, groupTexts, SetTextCustom);
            m_dropdownTextContent.selectedIndex = -1;
            m_dropdownTextContent.eventSelectedIndexChanged += (e, idx) =>
            {
                m_customText.GetComponentInParent<UIPanel>().isVisible = BoardGeneratorBuildings.AVAILABLE_TEXT_TYPES[idx] == TextType.Fixed;
                m_overrideFontText.parent.isVisible = BoardGeneratorBuildings.AVAILABLE_TEXT_TYPES[idx] == TextType.OwnName || BoardGeneratorBuildings.AVAILABLE_TEXT_TYPES[idx] == TextType.Fixed;
            };

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


            OnBuildingSet(null);
        }
        #endregion

        #region Utils
        private UIDropDown AddLibBox<LIB, DESC>(string groupTitle, UIHelperExtension parentHelper,
            out UIButton copyButton, OnButtonClicked actionCopy,
            out UIButton pasteButton, OnButtonClicked actionPaste,
            out UIButton deleteButton, OnButtonClicked actionDelete,
            Action<DESC> onLoad, Func<DESC> getContentToSave, Action<UIHelperExtension> doWithLibGroup = null) where LIB : BasicLib<LIB, DESC>, new() where DESC : ILibable
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

            SetIcon(copyButton, "Copy", Color.white);
            SetIcon(pasteButton, "Paste", Color.white);
            SetIcon(deleteButton, "RemoveIcon", Color.white);

            deleteButton.color = Color.red;

            pasteButton.isVisible = false;


            AddDropdown(Locale.Get("K45_DTP_LOAD_FROM_LIB"), out UIDropDown loadDD, groupLibPropGroup, BasicLib<LIB, DESC>.Instance.List().ToArray(), (x) => { });
            loadDD.width -= 80;
            UIPanel parent = loadDD.GetComponentInParent<UIPanel>();
            UIButton actionButton = ConfigureActionButton(parent);
            SetIcon(actionButton, "Load", Color.white);
            actionButton.eventClick += (x, t) =>
            {
                if (m_currentBuildingName != null)
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
            SetIcon(actionButton, "RemoveIcon", Color.white);
            actionButton.eventClick += (x, t) =>
            {
                if (m_currentBuildingName != null)
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
            SetIcon(actionButton, "Save", Color.white);
            actionButton.eventClick += (x, t) =>
            {
                if (m_currentBuildingName != null && !saveTxt.text.IsNullOrWhiteSpace())
                {
                    BasicLib<LIB, DESC>.Instance.Add(saveTxt.text, getContentToSave());
                    loadDD.items = BasicLib<LIB, DESC>.Instance.List().ToArray();
                    loadDD.selectedValue = saveTxt.text;
                }
            };
            doWithLibGroup?.Invoke(groupLibPropGroup);
            return loadDD;
        }

        private static void SetIcon(UIButton copyButton, string spriteName, Color color)
        {
            UISprite icon = copyButton.AddUIComponent<UISprite>();
            icon.relativePosition = new Vector3(2, 2);
            icon.atlas = DTPCommonTextureAtlas.instance.Atlas;
            icon.width = 36;
            icon.height = 36;
            icon.spriteName = spriteName;
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
        private void AddIntField(string label, out UITextField field, UIHelperExtension parentHelper, Action<int> onChange, bool acceptNegative)
        {
            field = parentHelper.AddIntField(label, 0, onChange, acceptNegative);
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

        private void DoCopyText()
        {
            if (m_currentBuildingName != null && CurrentTab >= 0 && CurrentTabText >= 0)
            {
                m_clipboardText = XmlUtils.DefaultXmlSerialize(BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[CurrentTab].m_textDescriptors[CurrentTabText]);
                m_pasteButtonText.isVisible = true;
            }
        }

        private void DoPasteText()
        {
            if (m_currentBuildingName != null && CurrentTab >= 0 && CurrentTabText >= 0 && m_clipboardText != null)
            {
                BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[CurrentTab].m_textDescriptors[CurrentTabText] = XmlUtils.DefaultXmlDeserialize<BoardTextDescriptorBuildingsXml>(m_clipboardText);

                ReloadTabInfoText();
            }
        }

        private void DoDeleteText()
        {
            if (m_currentBuildingName != null && CurrentTab >= 0 && CurrentTabText >= 0)
            {
                var tempList = BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[CurrentTab].m_textDescriptors.ToList();
                tempList.RemoveAt(CurrentTabText);
                BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName].BoardDescriptors[CurrentTab].m_textDescriptors = tempList.ToArray();
                ReloadTabInfo();

            }
        }

        #endregion
        public void Start() => m_propsDropdown.items = new string[] { Locale.Get("K45_DTP_NONE_PROP_ITEM") }.Concat(BoardGeneratorHighwaySigns.Instance.LoadedProps.Select(x => x.EndsWith("_Data") ? Locale.Get("PROPS_TITLE", x) : x)).ToArray();
        #region UI Actions

        private void LoadAllBuildingConfigurations() => BoardGeneratorBuildings.Instance.LoadAllBuildingConfigurations();
        private void SetPropModel(int idx)
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

        private void SetTextItemName(string txt)
        {
            SafeActionInTextBoard(descriptor =>
            {
                descriptor.SaveName = txt;
                EnsureTabQuantityTexts(BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors?.ElementAtOrDefault(CurrentTab).m_textDescriptors?.Length ?? -1);
            });
        }

        private void SetTextRelPosition(Vector3 value) => SafeActionInTextBoard(descriptor => descriptor.m_textRelativePosition = value);
        private void SetTextRelRotation(Vector3 value) => SafeActionInTextBoard(descriptor => descriptor.m_textRelativeRotation = value);

        private void SetTextOwnNameContent(int idx) => SafeActionInTextBoard(descriptor => descriptor.m_textType = BoardGeneratorBuildings.AVAILABLE_TEXT_TYPES[idx]);
        private void SetTextCustom(string txt) => SafeActionInTextBoard(descriptor => descriptor.m_fixedText = txt?.Replace("\\n", "\n"));
        private void SetOverrideFont(int idx)
        {
            SafeActionInTextBoard(descriptor =>
            {
                descriptor.m_overrideFont = idx > 0 ? m_overrideFontText.selectedValue : null;
                descriptor.GeneratedFixedTextRenderInfo = null;
            });
        }
        private void SetTextColor(Color color) => SafeActionInTextBoard(descriptor => descriptor.m_defaultColor = color);
        private void SetUseContrastColor(bool val) => SafeActionInTextBoard(descriptor =>
        {
            descriptor.m_useContrastColor = val;
            m_colorEditorText.parent.isVisible = !val;
        });

        private void SetTextAlignmentHorizontal(int idx) => SafeActionInTextBoard(descriptor => descriptor.m_textAlign = (UIHorizontalAlignment) idx);

        private void SetTextAlignmentVertical(int idx) => SafeActionInTextBoard(descriptor => descriptor.m_verticalAlign = (UIVerticalAlignment) idx);

        private void SetTextMaxWidth(float val) => SafeActionInTextBoard(descriptor => descriptor.m_maxWidthMeters = val);
        private void SetTextScale(float val) => SafeActionInTextBoard(descriptor => descriptor.m_textScale = val);
        private void SetTextResizeYOnOverflow(bool value) => SafeActionInTextBoard(descriptor => descriptor.m_applyOverflowResizingOnY = value);
        private void SetTextLumDay(float val) => SafeActionInTextBoard(descriptor => descriptor.m_dayEmissiveMultiplier = val);
        private void SetTextLumNight(float val) => SafeActionInTextBoard(descriptor => descriptor.m_nightEmissiveMultiplier = val);

        private void OnSetFont(int idx)
        {
            if (idx >= 0)
            {
                BoardGeneratorBuildings.Instance.ChangeFont(idx == 0 ? null : m_fontSelect.items[idx]);
            }
        }

        private void SafeActionInTextBoard(Action<BoardTextDescriptorBuildingsXml> toDo)
        {
            if (m_currentBuildingName != null && !m_isLoading)
            {
                EnsureBoardsArrayIdx(CurrentTab, CurrentTabText);
                BoardTextDescriptorBuildingsXml descriptor = BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors[CurrentTab].m_textDescriptors[CurrentTabText];
                toDo(descriptor);
            }
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
                else if (m_currentBuildingName != null && CurrentTab >= 0 && i < (BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors?.ElementAtOrDefault(CurrentTab)?.m_textDescriptors?.Length ?? 0))
                {
                    ((UIButton) m_pseudoTabstripTexts.tabs[i]).text = (BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors[CurrentTab]?.m_textDescriptors?.ElementAtOrDefault(i)?.SaveName).IsNullOrWhiteSpace() ? $"T{i + 1}" : (BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors[CurrentTab]?.m_textDescriptors[i]?.SaveName);
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

        #endregion
        #region Selection Action
        private void EnablePickTool()
        {
            OnBuildingSet(null);
            DynamicTextPropsMod.Instance.Controller.BuildingEditorToolInstance.OnBuildingSelect += OnBuildingSet;
            DynamicTextPropsMod.Instance.Controller.BuildingEditorToolInstance.enabled = true;
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


        private void OnChangeTabTexts(int tabVal)
        {
            EnsureBoardsArrayIdx(-1, tabVal);
            CurrentTabText = tabVal;
        }

        #endregion
        #region Load Data copy
        private void ReloadTabInfo()
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
        private void ReloadTabInfoText()
        {
            m_pseudoTabTextsContainer.Self.isVisible = CurrentTabText >= 0;
            if (m_currentBuildingName == null || CurrentTab < 0 || CurrentTabText < 0)
            {
                return;
            }

            m_isLoading = true;
            LoadTabTextInfo(BoardGeneratorBuildings.LoadedDescriptors[m_currentBuildingName]?.BoardDescriptors[CurrentTab]?.m_textDescriptors[CurrentTabText]);
            m_loadText.items = DTPLibTextMeshBuildingSigns.Instance.List().ToArray();
            m_isLoading = false;
        }

        private void LoadTabTextInfo(BoardTextDescriptorBuildingsXml descriptor)
        {
            m_textItemName.text = descriptor?.SaveName ?? "";
            TextType textType = descriptor?.m_textType ?? TextType.OwnName;
            m_dropdownTextContent.selectedIndex = Array.IndexOf(BoardGeneratorBuildings.AVAILABLE_TEXT_TYPES, textType);
            m_customText.text = (descriptor?.m_fixedText?.Replace("\n", "\\n") ?? "");
            m_posVectorEditorText[0].text = (descriptor?.m_textRelativePosition.x ?? 0).ToString();
            m_posVectorEditorText[1].text = (descriptor?.m_textRelativePosition.y ?? 0).ToString();
            m_posVectorEditorText[2].text = (descriptor?.m_textRelativePosition.z ?? 0).ToString();
            m_rotVectorEditorText[0].text = (descriptor?.m_textRelativeRotation.x ?? 0).ToString();
            m_rotVectorEditorText[1].text = (descriptor?.m_textRelativeRotation.y ?? 0).ToString();
            m_rotVectorEditorText[2].text = (descriptor?.m_textRelativeRotation.z ?? 0).ToString();
            m_colorEditorText.selectedColor = descriptor?.m_defaultColor ?? default;
            m_dropdownTextAlignHorizontal.selectedIndex = (int) (descriptor?.m_textAlign ?? UIHorizontalAlignment.Center);
            m_dropdownTextAlignVertical.selectedIndex = (int) (descriptor?.m_verticalAlign ?? UIVerticalAlignment.Middle);
            m_maxWidthText.text = (descriptor?.m_maxWidthMeters ?? 0).ToString();
            m_scaleText.text = (descriptor?.m_textScale ?? 0).ToString();
            m_textResizeYOnOverflow.isChecked = (descriptor?.m_applyOverflowResizingOnY ?? false);
            m_textLuminosityDay.value = (descriptor?.m_dayEmissiveMultiplier ?? 0);
            m_textLuminosityNight.value = (descriptor?.m_nightEmissiveMultiplier ?? 0);
            m_useContrastColorTextCheckbox.isChecked = descriptor?.m_useContrastColor ?? false;
            m_colorEditorText.parent.isVisible = !m_useContrastColorTextCheckbox.isChecked;
            ReloadFontsOverride(m_overrideFontText, descriptor?.m_overrideFont);
            m_overrideFontText.parent.isVisible = textType == TextType.OwnName || textType == TextType.Fixed;
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
