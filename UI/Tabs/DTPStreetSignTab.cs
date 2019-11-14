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
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorRoadNodes;

namespace Klyte.DynamicTextProps.UI
{

    internal class DTPStreetSignTab : UICustomControl
    {
        public UIScrollablePanel MainContainer { get; private set; }

        private UIHelperExtension m_uiHelperHS;

        private UIDropDown m_fontSelect;

        private UIDropDown m_propsDropdown;


        private UIHelperExtension m_pseudoTabTextsContainer;
        private UITabstripAutoResize m_pseudoTabstripTexts;

        private UITextField m_textItemName;
        private UIDropDown m_dropdownTextContent;
        private UITextField m_customText;

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
        private UIDropDown m_loadTextDD;
        private UICheckBox m_useDistrictColorCheck;
        private UIColorField m_propColorPicker;

        private int CurrentTabText
        {
            get => m_pseudoTabstripTexts.selectedIndex;
            set => m_pseudoTabstripTexts.selectedIndex = value;
        }

        private string m_clipboardText;

        private bool m_isLoading = false;

        #region Awake
        public void Awake()
        {
            MainContainer = GetComponent<UIScrollablePanel>();

            m_uiHelperHS = new UIHelperExtension(MainContainer);

            AddDropdown(Locale.Get("K45_DTP_FONT_ST_CORNERS"), out m_fontSelect, m_uiHelperHS, new string[0], OnSetFont);
            m_fontSelect.width -= 40;
            UIPanel parent = m_fontSelect.GetComponentInParent<UIPanel>();
            UIButton actionButton = ConfigureActionButton(parent);
            SetIcon(actionButton, CommonSpriteNames.Reload, Color.white);
            actionButton.eventClick += (x, t) => DTPUtils.ReloadFontsOf<BoardGeneratorRoadNodes>(m_fontSelect);
            DTPUtils.ReloadFontsOf<BoardGeneratorRoadNodes>(m_fontSelect);
            BoardGeneratorRoadNodes.GenerateDefaultSignModelAtLibrary();

            m_loadPropGroup = AddLibBox<DTPLibStreetPropGroup, BoardDescriptorStreetSignXml>(Locale.Get("K45_DTP_STREET_SIGNS_LIB_TITLE"), m_uiHelperHS,
                        out _, null,
                        out _, null,
                        out _, null,
                        (x) =>
                        {
                            BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor = XmlUtils.DefaultXmlDeserialize<BoardDescriptorStreetSignXml>(XmlUtils.DefaultXmlSerialize(x));
                            Start();
                        },
                () => BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor);

            var buttonErase = (UIButton) m_uiHelperHS.AddButton(Locale.Get("K45_DTP_ERASE_CURRENT_CONFIG"), DoDeleteGroup);
            KlyteMonoUtils.LimitWidth(buttonErase, m_uiHelperHS.Self.width - 20, true);
            buttonErase.color = Color.red;

            AddDropdown(Locale.Get("K45_DTP_PROP_MODEL_SELECT"), out m_propsDropdown, m_uiHelperHS, new string[0], SetPropModel);
            m_useDistrictColorCheck = m_uiHelperHS.AddCheckboxLocale("K45_DTP_USE_DISTRICT_COLOR", BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.UseDistrictColor, OnChangeUseDistrictColor);
            KlyteMonoUtils.LimitWidth(m_useDistrictColorCheck.label, m_uiHelperHS.Self.width - 50);
            KlyteMonoUtils.LimitWidth(m_uiHelperHS.AddCheckboxLocale("K45_DTP_PLACE_ON_DISTRICT_BORDER", BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.PlaceOnDistrictBorder, SetPlaceOnDistrictBorder).label, m_uiHelperHS.Self.width - 50);
            m_propColorPicker = m_uiHelperHS.AddColorPicker(Locale.Get("K45_DTP_PROP_COLOR"), BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.PropColor, OnChangePropColor);

            KlyteMonoUtils.CreateHorizontalScrollPanel(m_uiHelperHS.Self, out UIScrollablePanel scrollTabs, out _, m_uiHelperHS.Self.width - 20, 40, Vector3.zero);

            KlyteMonoUtils.CreateUIElement(out m_pseudoTabstripTexts, scrollTabs.transform, "DTPTabstrip", new Vector4(5, 40, m_uiHelperHS.Self.width - 10, 40));
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
                    OnChangeTabTexts(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors?.Length ?? 0);
                    MainContainer.verticalScrollbar.relativePosition = pos;
                }
                else
                {
                    ReloadTabInfoText();
                }
            };

            m_pseudoTabTextsContainer = m_uiHelperHS.AddGroupExtended("))))");
            Destroy(m_pseudoTabTextsContainer.Self.parent.GetComponentInChildren<UILabel>().gameObject);
            ((UIPanel) m_pseudoTabTextsContainer.Self).backgroundSprite = "";
            m_pseudoTabTextsContainer.Self.width = MainContainer.width - 50;

            m_loadTextDD = AddLibBox<DTPLibTextMeshStreetPlate, BoardTextDescriptorSteetSignXml>(Locale.Get("K45_DTP_PROP_TEXT_LIB_TITLE"), m_pseudoTabTextsContainer,
                                    out _, DoCopyText,
                                    out m_pasteButtonText, DoPasteText,
                                    out _, DoDeleteText,
                                    (x) =>
                                    {
                                        if (CurrentTabText < 0)
                                        {
                                            return;
                                        }

                                        BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors[CurrentTabText] = XmlUtils.DefaultXmlDeserialize<BoardTextDescriptorSteetSignXml>(XmlUtils.DefaultXmlSerialize(x));
                                        ReloadTabInfoText();
                                    },
                            () => BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors?.ElementAtOrDefault(CurrentTabText));


            UIHelperExtension groupTexts = m_pseudoTabTextsContainer.AddTogglableGroup(Locale.Get("K45_DTP_TEXTS_COMMON_CONFIGURATION"), out UILabel lblTxt);

            AddTextField(Locale.Get("K45_DTP_TEXT_TAB_TITLE"), out m_textItemName, groupTexts, SetTextItemName);
            m_colorEditorText = groupTexts.AddColorPicker(Locale.Get("K45_DTP_TEXT_COLOR"), Color.white, SetTextColor);
            m_useContrastColorTextCheckbox = groupTexts.AddCheckboxLocale("K45_DTP_USE_CONTRAST_COLOR", false, SetUseContrastColor);
            KlyteMonoUtils.LimitWidth(m_colorEditorText.parent.GetComponentInChildren<UILabel>(), groupTexts.Self.width / 2, true);
            AddDropdown(Locale.Get("K45_DTP_TEXT_CONTENT"), out m_dropdownTextContent, groupTexts, AVAILABLE_TEXT_TYPES.Select(x => Locale.Get("K45_DTP_OWN_NAME_CONTENT", x.ToString())).ToArray(), SetTextOwnNameContent);
            AddTextField(Locale.Get("K45_DTP_CUSTOM_TEXT"), out m_customText, groupTexts, SetTextCustom);
            m_dropdownTextContent.eventSelectedIndexChanged += (e, idx) => m_customText.GetComponentInParent<UIPanel>().isVisible = idx == (int) TextType.Fixed;

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
        }

        private void OnChangeUseDistrictColor(bool isChecked)
        {
            BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.UseDistrictColor = isChecked;
            m_propColorPicker.parent.isVisible = !isChecked;
            BoardGeneratorRoadNodes.Instance.SoftReset();
        }
        private void SetPlaceOnDistrictBorder(bool isChecked)
        {
            BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.PlaceOnDistrictBorder = isChecked;
            BoardGeneratorRoadNodes.Instance.SoftReset();
        }
        private void OnChangePropColor(Color c)
        {
            BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.PropColor = c;
            BoardGeneratorRoadNodes.Instance.SoftReset();
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

            if (actionCopy != null)
            {
                copyButton = ConfigureActionButton(subPanelActionsBar.Self);
                copyButton.eventClick += (x, y) => actionCopy();
                SetIcon(copyButton, CommonSpriteNames.Copy, Color.white);
            }
            else
            {
                copyButton = null;
            }
            if (actionPaste != null)
            {
                pasteButton = ConfigureActionButton(subPanelActionsBar.Self);
                pasteButton.eventClick += (x, y) => actionPaste();
                SetIcon(pasteButton, CommonSpriteNames.Paste, Color.white);
                pasteButton.isVisible = false;
            }
            else
            {
                pasteButton = null;
            }
            if (actionDelete != null)
            {
                deleteButton = ConfigureActionButton(subPanelActionsBar.Self);
                deleteButton.eventClick += (x, y) => actionDelete();
                SetIcon(deleteButton, CommonSpriteNames.RemoveIcon, Color.white);
                deleteButton.color = Color.red;
            }
            else
            {
                deleteButton = null;
            }

            AddDropdown(Locale.Get("K45_DTP_LOAD_FROM_LIB"), out UIDropDown loadDD, groupLibPropGroup, BasicLib<LIB, DESC>.Instance.List().ToArray(), (x) => { });
            loadDD.width -= 80;
            UIPanel parent = loadDD.GetComponentInParent<UIPanel>();
            UIButton actionButton = ConfigureActionButton(parent);
            SetIcon(actionButton, CommonSpriteNames.Load, Color.white);
            actionButton.eventClick += (x, t) =>
            {
                DESC groupInfo = BasicLib<LIB, DESC>.Instance.Get(loadDD.selectedValue);
                if (groupInfo != null)
                {
                    onLoad(groupInfo);
                }
            };
            KlyteMonoUtils.CreateUIElement(out actionButton, parent.transform, "DelBtn");
            actionButton = ConfigureActionButton(parent);
            actionButton.color = Color.red;
            SetIcon(actionButton, CommonSpriteNames.RemoveIcon, Color.white);
            actionButton.eventClick += (x, t) =>
            {
                DESC groupInfo = BasicLib<LIB, DESC>.Instance.Get(loadDD.selectedValue);
                if (groupInfo != null)
                {
                    BasicLib<LIB, DESC>.Instance.Remove(loadDD.selectedValue);
                    ReloadLib<LIB, DESC>(loadDD);
                }
            };

            AddTextField(Locale.Get("K45_DTP_SAVE_TO_LIB"), out UITextField saveTxt, groupLibPropGroup, (x) => { });
            saveTxt.width -= 40;
            parent = saveTxt.GetComponentInParent<UIPanel>();
            actionButton = ConfigureActionButton(parent);
            SetIcon(actionButton, CommonSpriteNames.Save, Color.white);
            actionButton.eventClick += (x, t) =>
            {
                if (!saveTxt.text.IsNullOrWhiteSpace())
                {
                    BasicLib<LIB, DESC>.Instance.Add(saveTxt.text, getContentToSave());
                    ReloadLib<LIB, DESC>(loadDD);
                    loadDD.selectedValue = saveTxt.text;
                }
            };
            ReloadLib<LIB, DESC>(loadDD);
            return loadDD;
        }

        public void ReloadGroupLib() => ReloadLib<DTPLibStreetPropGroup, BoardDescriptorStreetSignXml>(m_loadPropGroup);
        public void ReloadTextLib() => ReloadLib<DTPLibTextMeshStreetPlate, BoardTextDescriptorSteetSignXml>(m_loadTextDD);

        private static void ReloadLib<LIB, DESC>(UIDropDown loadDD)
            where LIB : BasicLib<LIB, DESC>, new()
            where DESC : ILibable => loadDD.items = BasicLib<LIB, DESC>.Instance.List().ToArray();
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
        private void DoCopyText()
        {
            if (CurrentTabText >= 0)
            {
                m_clipboardText = (BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors[CurrentTabText].Serialize());
                m_pasteButtonText.isVisible = true;
            }
        }

        private void DoPasteText()
        {
            if (CurrentTabText >= 0 && m_clipboardText != null)
            {
                BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors[CurrentTabText] = BoardTextDescriptorSteetSignXml.Deserialize(m_clipboardText);
                ReloadTabInfoText();
                BoardGeneratorRoadNodes.Instance.SoftReset();
            }
        }

        private void DoDeleteText()
        {
            if (CurrentTabText >= 0)
            {
                var tempList = BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors.ToList();
                tempList.RemoveAt(CurrentTabText);
                BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors = tempList.ToArray();
                ReloadTabInfoText();
            }
        }
        private void DoDeleteGroup()
        {
            BoardGeneratorRoadNodes.Instance.CleanDescriptor();
            m_propsDropdown.selectedIndex = -1;
            OnChangeTabTexts(-1);

        }

        public void Start()
        {
            m_propsDropdown.items = BoardGeneratorHighwaySigns.Instance.LoadedProps.Select(x => x.EndsWith("_Data") ? Locale.Get("PROPS_TITLE", x) : x).ToArray();
            m_propsDropdown.selectedIndex = BoardGeneratorHighwaySigns.Instance.LoadedProps.IndexOf(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_propName);
            m_useDistrictColorCheck.isChecked = BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.UseDistrictColor;
            m_propColorPicker.parent.isVisible = !m_useDistrictColorCheck.isChecked;
            BoardGeneratorRoadNodes.Instance.ChangeFont(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.FontName);
            DTPUtils.ReloadFontsOf<BoardGeneratorRoadNodes>(m_fontSelect);
            OnChangeUseDistrictColor(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.UseDistrictColor);
            OnChangePropColor(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.PropColor);
            OnChangeTabTexts(-1);
        }

        private void SetPropModel(int idx)
        {
            if (!m_isLoading && idx >= 0)
            {
                BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_propName = BoardGeneratorHighwaySigns.Instance.LoadedProps[idx];
                BoardGeneratorRoadNodes.Instance.SoftReset();
            }
        }
        private void SetTextOwnNameContent(int idx) => SafeActionInTextBoard(descriptor =>
        {
            descriptor.m_textType = AVAILABLE_TEXT_TYPES[idx];
            m_customText.parent.isVisible = descriptor.m_textType == TextType.Fixed;
            descriptor.GeneratedFixedTextRenderInfo = null;
        });
        private void SetTextCustom(string txt)
        {
            SafeActionInTextBoard(descriptor =>
            {
                descriptor.m_fixedText = txt?.Replace("\\n", "\n");
                descriptor.GeneratedFixedTextRenderInfo = null;
            });
        }
        private void SetTextItemName(string txt)
        {
            SafeActionInTextBoard(descriptor =>
            {
                descriptor.SaveName = txt;
                EnsureTabQuantityTexts(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors?.Length ?? -1);
            });
        }

        private void SetTextRelPosition(Vector3 value) => SafeActionInTextBoard(descriptor => descriptor.m_textRelativePosition = value);
        private void SetTextRelRotation(Vector3 value) => SafeActionInTextBoard(descriptor => descriptor.m_textRelativeRotation = value);

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
                BoardGeneratorRoadNodes.Instance.ChangeFont(idx == 0 ? null : m_fontSelect.items[idx]);
            }
        }

        private void SafeActionInTextBoard(Action<BoardTextDescriptorSteetSignXml> toDo)
        {
            if (!m_isLoading)
            {
                BoardTextDescriptorSteetSignXml descriptor = BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors[CurrentTabText];
                toDo(descriptor);
                BoardGeneratorRoadNodes.Instance.SoftReset();
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
                else if (i < (BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors?.Length ?? 0))
                {
                    ((UIButton) m_pseudoTabstripTexts.tabs[i]).text = (BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors?.ElementAtOrDefault(i)?.SaveName).IsNullOrWhiteSpace() ? $"T{i + 1}" : (BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors[i]?.SaveName);
                }
                else
                {
                    ((UIButton) m_pseudoTabstripTexts.tabs[i]).text = $"T{i + 1}";
                }
            }
        }

        private void ConfigureTabsShownText(int quantity)
        {

            for (int i = 0; i < m_pseudoTabstripTexts.tabCount; i++)
            {
                m_pseudoTabstripTexts.tabs[i].isVisible = i < quantity || i == m_pseudoTabstripTexts.tabCount - 1;
            }

        }
        private void OnChangeTabTexts(int tabVal)
        {
            CurrentTabText = tabVal;
            ReloadTabInfoText();
        }

        private void ReloadTabInfoText()
        {

            m_isLoading = true;
            EnsureTabQuantityTexts(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors?.Length ?? -1);
            ConfigureTabsShownText(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors?.Length ?? 0);
            m_pseudoTabTextsContainer.Self.isVisible = CurrentTabText >= 0;
            if (CurrentTabText >= 0)
            {
                LoadTabTextInfo(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.m_textDescriptors[CurrentTabText]);
                m_loadPropGroup.items = DTPLibTextMeshStreetPlate.Instance.List().ToArray();
            }
            m_isLoading = false;
            ReloadTextLib();
            ReloadGroupLib();
        }

        private void LoadTabTextInfo(BoardTextDescriptorSteetSignXml descriptor)
        {
            m_textItemName.text = descriptor?.SaveName ?? "";
            m_dropdownTextContent.selectedIndex = (int) (descriptor?.m_textType ?? TextType.OwnName);
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
            m_useContrastColorTextCheckbox.isChecked = descriptor?.m_useContrastColor ?? false;
            m_colorEditorText.parent.isVisible = !m_useContrastColorTextCheckbox.isChecked;
            m_customText.parent.isVisible = descriptor?.m_textType == TextType.Fixed;
        }

        #endregion

    }


}
