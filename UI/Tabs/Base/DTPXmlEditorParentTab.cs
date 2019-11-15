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

namespace Klyte.DynamicTextProps.UI
{

    internal abstract class DTPXmlEditorParentTab<BG, BBC, CC, BRI, BD, BTD, LIBTXT> : UICustomControl
        where BG : BoardGeneratorParent<BG, BBC, CC, BRI, BD, BTD>
        where BBC : IBoardBunchContainer<CC, BRI>
        where BD : BoardDescriptorParentXml<BD, BTD>
        where BTD : BoardTextDescriptorParentXml<BTD>, ILibable
        where CC : CacheControl
        where BRI : BasicRenderInformation, new()
        where LIBTXT : BasicLib<LIBTXT, BTD>, new()
    {
        public static DTPXmlEditorParentTab<BG, BBC, CC, BRI, BD, BTD, LIBTXT> Instance { get; private set; }
        public UIScrollablePanel MainContainer { get; protected set; }

        protected UIHelperExtension m_uiHelperHS;

        protected UIDropDown m_fontSelect;
        protected UIDropDown m_propsDropdown;

        protected UIHelperExtension m_pseudoTabTextsContainer;
        protected UITabstripAutoResize m_pseudoTabstripTexts;

        protected UITextField m_textItemName;
        protected UIDropDown m_dropdownTextContent;
        protected UITextField m_customText;

        protected UITextField[] m_posVectorEditorText;
        protected UITextField[] m_rotVectorEditorText;
        protected UIColorField m_colorEditorText;
        protected UIDropDown m_dropdownTextAlignHorizontal;
        protected UIDropDown m_dropdownTextAlignVertical;
        protected UITextField m_maxWidthText;
        protected UITextField m_scaleText;
        protected UICheckBox m_textResizeYOnOverflow;
        protected UISlider m_textLuminosityDay;
        protected UISlider m_textLuminosityNight;

        protected UIButton m_pasteButtonText;

        protected UIDropDown m_loadPropGroup;
        protected UIDropDown m_loadTextDD;

        protected string m_clipboardText;

        protected int CurrentTabText
        {
            get => m_pseudoTabstripTexts.selectedIndex;
            set => m_pseudoTabstripTexts.selectedIndex = value;
        }

        protected bool m_isLoading = false;

        protected abstract BTD[] CurrentSelectedDescriptorArray { get; set; }
        protected abstract string GetFontLabelString();
        protected abstract void OnTextTabStripChanged();
        protected abstract void AwakePropEditor(out UIScrollablePanel scrollTabs, out UIHelperExtension referenceHelperTabs);
        protected abstract void OnDropdownTextTypeSelectionChanged(int idx);
        protected abstract void OnLoadTextLibItem();
        protected abstract void PostAwake();
        protected abstract void AfterLoadingTabTextInfo(BTD descriptor);
        protected abstract TextType[] GetAvailableTextTypes();
        protected abstract void ReloadTabInfoText();
        protected abstract string GetLocaleNameForContentTypes();
        protected abstract bool IsTextEditionAvailable();
        protected virtual void OnChangeCustomText(BTD descriptor) { }
        protected virtual void DoInTextCommonTabGroupUI(UIHelperExtension groupTexts) { }
        protected virtual void DoInTextSizeTabGroupUI(UIHelperExtension groupTexts) { }
        protected virtual void DoInTextAlignmentTabGroupUI(UIHelperExtension groupTexts) { }
        protected virtual void DoInTextEffectsTabGroupUI(UIHelperExtension groupTexts) { }
        protected virtual void OnPasteText() { }

        protected string[] GetTextTypeOptions() => GetAvailableTextTypes().Select(x => Locale.Get(GetLocaleNameForContentTypes(), x.ToString())).ToArray();

        public void Awake()
        {
            Instance = this;

            MainContainer = GetComponent<UIScrollablePanel>();

            m_uiHelperHS = new UIHelperExtension(MainContainer);

            AddDropdown(GetFontLabelString(), out m_fontSelect, m_uiHelperHS, new string[0], OnSetFont);

            m_fontSelect.width -= 40;
            UIPanel parent = m_fontSelect.GetComponentInParent<UIPanel>();
            UIButton actionButton = ConfigureActionButton(parent);
            SetIcon(actionButton, CommonSpriteNames.Reload, Color.white);
            actionButton.eventClick += (x, t) => DTPUtils.ReloadFontsOf<BG>(m_fontSelect);
            DTPUtils.ReloadFontsOf<BG>(m_fontSelect);


            AwakePropEditor(out UIScrollablePanel scrollTabs, out UIHelperExtension referenceHelper);
            KlyteMonoUtils.CreateUIElement(out m_pseudoTabstripTexts, scrollTabs.transform, "DTPTabstrip", new Vector4(5, 40, referenceHelper.Self.width - 10, 40));
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
                    OnTextTabStripChanged();
                    MainContainer.verticalScrollbar.relativePosition = pos;
                }
                else
                {
                    ReloadTabInfoText();
                }
            };

            m_pseudoTabTextsContainer = referenceHelper.AddGroupExtended("))))");
            Destroy(m_pseudoTabTextsContainer.Self.parent.GetComponentInChildren<UILabel>().gameObject);
            ((UIPanel) m_pseudoTabTextsContainer.Self).backgroundSprite = "";
            m_pseudoTabTextsContainer.Self.width = MainContainer.width - 50;

            m_loadTextDD = AddLibBox<LIBTXT, BTD>(Locale.Get("K45_DTP_PROP_TEXT_LIB_TITLE"), m_pseudoTabTextsContainer,
                                  out UIButton m_copyButtonText, DoCopyText,
                                  out m_pasteButtonText, DoPasteText,
                                  out UIButton m_deleteButtonText, DoDeleteText,
                                  (x) =>
                                  {
                                      if (!IsTextEditionAvailable() || CurrentTabText < 0)
                                      {
                                          return;
                                      }

                                      CurrentSelectedDescriptorArray[CurrentTabText] = XmlUtils.DefaultXmlDeserialize<BTD>(XmlUtils.DefaultXmlSerialize(x));
                                      OnLoadTextLibItem();
                                  },
                          () => CurrentSelectedDescriptorArray?.ElementAtOrDefault(CurrentTabText));

            UIHelperExtension groupTexts = m_pseudoTabTextsContainer.AddTogglableGroup(Locale.Get("K45_DTP_TEXTS_COMMON_CONFIGURATION"), out UILabel lblTxt);

            AddTextField(Locale.Get("K45_DTP_TEXT_TAB_TITLE"), out m_textItemName, groupTexts, SetTextItemName);
            m_colorEditorText = groupTexts.AddColorPicker(Locale.Get("K45_DTP_TEXT_COLOR"), Color.white, SetTextColor);
            KlyteMonoUtils.LimitWidth(m_colorEditorText.parent.GetComponentInChildren<UILabel>(), groupTexts.Self.width / 2, true);
            AddDropdown(Locale.Get("K45_DTP_TEXT_CONTENT"), out m_dropdownTextContent, groupTexts, GetTextTypeOptions(), SetTextOwnNameContent);
            AddTextField(Locale.Get("K45_DTP_CUSTOM_TEXT"), out m_customText, groupTexts, SetTextCustom);

            m_dropdownTextContent.eventSelectedIndexChanged += (e, idx) =>
            {
                m_customText.GetComponentInParent<UIPanel>().isVisible = GetAvailableTextTypes()[idx] == TextType.Fixed;
                OnDropdownTextTypeSelectionChanged(idx);
            };
            DoInTextCommonTabGroupUI(groupTexts);

            groupTexts = m_pseudoTabTextsContainer.AddTogglableGroup(Locale.Get("K45_DTP_TEXTS_SIZE_POSITION"));
            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_POS"), out m_posVectorEditorText, groupTexts, SetTextRelPosition);
            AddVector3Field(Locale.Get("K45_DTP_RELATIVE_ROT"), out m_rotVectorEditorText, groupTexts, SetTextRelRotation);
            AddFloatField(Locale.Get("K45_DTP_TEXT_SCALE"), out m_scaleText, groupTexts, SetTextScale, false);
            AddFloatField(Locale.Get("K45_DTP_MAX_WIDTH_METERS"), out m_maxWidthText, groupTexts, SetTextMaxWidth, false);
            m_textResizeYOnOverflow = (UICheckBox) groupTexts.AddCheckbox(Locale.Get("K45_DTP_RESIZE_Y_TEXT_OVERFLOW"), false, SetTextResizeYOnOverflow);
            DoInTextSizeTabGroupUI(groupTexts);

            groupTexts = m_pseudoTabTextsContainer.AddTogglableGroup(Locale.Get("K45_DTP_TEXTS_2D_ALIGNMENT"));
            AddDropdown(Locale.Get("K45_DTP_TEXT_ALIGN_HOR"), out m_dropdownTextAlignHorizontal, groupTexts, Enum.GetNames(typeof(UIHorizontalAlignment)).Select(x => Locale.Get("K45_ALIGNMENT", x)).ToArray(), SetTextAlignmentHorizontal);
            AddDropdown(Locale.Get("K45_DTP_TEXT_ALIGN_VER"), out m_dropdownTextAlignVertical, groupTexts, Enum.GetNames(typeof(UIVerticalAlignment)).Select(x => Locale.Get("K45_VERT_ALIGNMENT", x)).ToArray(), SetTextAlignmentVertical);
            DoInTextAlignmentTabGroupUI(groupTexts);

            groupTexts = m_pseudoTabTextsContainer.AddTogglableGroup(Locale.Get("K45_DTP_TEXT_EFFECTS"));
            AddSlider(Locale.Get("K45_DTP_LUMINOSITY_DAY"), out m_textLuminosityDay, groupTexts, SetTextLumDay, 0, 10f, 0.25f);
            AddSlider(Locale.Get("K45_DTP_LUMINOSITY_NIGHT"), out m_textLuminosityNight, groupTexts, SetTextLumNight, 0, 10f, 0.25f);
            DoInTextEffectsTabGroupUI(groupTexts);

            PostAwake();
        }


        protected UIDropDown AddLibBox<LIB, DESC>(string groupTitle, UIHelperExtension parentHelper,
    out UIButton copyButton, Action actionCopy,
    out UIButton pasteButton, Action actionPaste,
    out UIButton deleteButton, Action actionDelete,
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
                    loadDD.items = BasicLib<LIB, DESC>.Instance.List().ToArray();
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
                    loadDD.items = BasicLib<LIB, DESC>.Instance.List().ToArray();
                    loadDD.selectedValue = saveTxt.text;
                }
            };
            doWithLibGroup?.Invoke(groupLibPropGroup);
            return loadDD;
        }


        #region UI Utils
        protected static void SetIcon(UIButton copyButton, CommonSpriteNames spriteName, Color color)
        {
            UISprite icon = copyButton.AddUIComponent<UISprite>();
            icon.relativePosition = new Vector3(2, 2);
            icon.width = 36;
            icon.height = 36;
            icon.spriteName = DTPResourceLoader.instance.GetDefaultSpriteNameFor(spriteName);
            icon.color = color;
        }

        protected static UIButton ConfigureActionButton(UIComponent parent)
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

        protected void AddSlider(string label, out UISlider slider, UIHelperExtension parentHelper, OnValueChanged onChange, float min, float max, float step)
        {
            slider = (UISlider) parentHelper.AddSlider(label, min, max, step, min, onChange);
            slider.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            slider.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            KlyteMonoUtils.LimitWidth(slider.parent.GetComponentInChildren<UILabel>(), (parentHelper.Self.width / 2) - 10, true);
        }

        protected void AddVector3Field(string label, out UITextField[] fieldArray, UIHelperExtension parentHelper, Action<Vector3> onChange)
        {
            fieldArray = parentHelper.AddVector3Field(label, Vector3.zero, onChange);
            KlyteMonoUtils.LimitWidth(fieldArray[0].parent.GetComponentInChildren<UILabel>(), (parentHelper.Self.width / 2) - 10, true);
        }

        protected void AddFloatField(string label, out UITextField field, UIHelperExtension parentHelper, Action<float> onChange, bool acceptNegative)
        {
            field = parentHelper.AddFloatField(label, 0, onChange, acceptNegative);
            KlyteMonoUtils.LimitWidth(field.parent.GetComponentInChildren<UILabel>(), (parentHelper.Self.width / 2) - 10, true);
        }
        protected void AddIntField(string label, out UITextField field, UIHelperExtension parentHelper, Action<int> onChange, bool acceptNegative)
        {
            field = parentHelper.AddIntField(label, 0, onChange, acceptNegative);
            KlyteMonoUtils.LimitWidth(field.parent.GetComponentInChildren<UILabel>(), (parentHelper.Self.width / 2) - 10, true);
        }
        protected void AddDropdown(string title, out UIDropDown dropdown, UIHelperExtension parentHelper, string[] options, OnDropdownSelectionChanged onChange) => AddDropdown(title, out dropdown, out UILabel label, parentHelper, options, onChange);
        protected void AddDropdown(string title, out UIDropDown dropdown, out UILabel label, UIHelperExtension parentHelper, string[] options, OnDropdownSelectionChanged onChange)
        {
            dropdown = (UIDropDown) parentHelper.AddDropdown(title, options, 0, onChange);
            dropdown.width = (parentHelper.Self.width / 2) - 10;
            dropdown.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            dropdown.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            label = dropdown.parent.GetComponentInChildren<UILabel>();
            KlyteMonoUtils.LimitWidth(label, (parentHelper.Self.width / 2) - 10, true);
        }

        protected void AddTextField(string title, out UITextField textField, UIHelperExtension parentHelper, OnTextSubmitted onChange) => AddTextField(title, out textField, out UILabel label, parentHelper, onChange);

        protected void AddTextField(string title, out UITextField textField, out UILabel label, UIHelperExtension parentHelper, OnTextSubmitted onChange)
        {
            textField = parentHelper.AddTextField(title, (x) => { }, "", onChange);
            textField.width = (parentHelper.Self.width / 2) - 10;
            textField.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            textField.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            label = textField.parent.GetComponentInChildren<UILabel>();
            KlyteMonoUtils.LimitWidth(label, (parentHelper.Self.width / 2) - 10, true);
        }

        protected UIButton m_tabModel;

        protected UIButton CreateTabTemplate()
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
        #region Text clipboard actions 


        protected void DoCopyText()
        {
            if (IsTextEditionAvailable() && CurrentTabText >= 0)
            {
                m_clipboardText = XmlUtils.DefaultXmlSerialize(CurrentSelectedDescriptorArray[CurrentTabText]);
                m_pasteButtonText.isVisible = true;
            }
        }

        protected void DoPasteText()
        {
            if (IsTextEditionAvailable() && CurrentTabText >= 0 && m_clipboardText != null)
            {
                CurrentSelectedDescriptorArray[CurrentTabText] = XmlUtils.DefaultXmlDeserialize<BTD>(m_clipboardText);
                OnPasteText();
                ReloadTabInfoText();
            }
        }

        protected void DoDeleteText()
        {
            if (IsTextEditionAvailable() && CurrentTabText >= 0)
            {
                var tempList = CurrentSelectedDescriptorArray.ToList();
                tempList.RemoveAt(CurrentTabText);
                CurrentSelectedDescriptorArray = tempList.ToArray();
                ReloadTabInfo();

            }
        }


        #endregion
        public void Start()
        {
            m_propsDropdown.items = new string[] { Locale.Get("K45_DTP_NONE_PROP_ITEM") }.Concat(BoardGeneratorHighwaySigns.Instance.LoadedProps.Select(x => x.EndsWith("_Data") ? Locale.Get("PROPS_TITLE", x) : x)).ToArray();
            OnStart();
        }

        protected virtual void OnStart() { }

        protected abstract void SetPropModel(int idx);
        protected abstract void ReloadTabInfo();
        protected void SetTextItemName(string txt)
        {
            SafeActionInTextBoard(descriptor =>
            {
                descriptor.SaveName = txt;
                EnsureTabQuantityTexts(CurrentSelectedDescriptorArray?.Length ?? -1);
            });
        }
        protected void SetTextRelPosition(Vector3 value) => SafeActionInTextBoard(descriptor => descriptor.m_textRelativePosition = value);
        protected void SetTextRelRotation(Vector3 value) => SafeActionInTextBoard(descriptor => descriptor.m_textRelativeRotation = value);

        protected void SetTextColor(Color color) => SafeActionInTextBoard(descriptor => descriptor.m_defaultColor = color);
        protected void SetUseContrastColor(bool val) => SafeActionInTextBoard(descriptor =>
        {
            descriptor.m_useContrastColor = val;
            m_colorEditorText.parent.isVisible = !val;
        });

        protected void SetTextAlignmentHorizontal(int idx) => SafeActionInTextBoard(descriptor => descriptor.m_textAlign = (UIHorizontalAlignment) idx);

        protected void SetTextAlignmentVertical(int idx) => SafeActionInTextBoard(descriptor => descriptor.m_verticalAlign = (UIVerticalAlignment) idx);

        protected void SetTextMaxWidth(float val) => SafeActionInTextBoard(descriptor => descriptor.m_maxWidthMeters = val);
        protected void SetTextScale(float val) => SafeActionInTextBoard(descriptor => descriptor.m_textScale = val);
        protected void SetTextResizeYOnOverflow(bool value) => SafeActionInTextBoard(descriptor => descriptor.m_applyOverflowResizingOnY = value);
        protected void SetTextLumDay(float val) => SafeActionInTextBoard(descriptor => descriptor.m_dayEmissiveMultiplier = val);
        protected void SetTextLumNight(float val) => SafeActionInTextBoard(descriptor => descriptor.m_nightEmissiveMultiplier = val);

        protected void OnSetFont(int idx)
        {
            if (idx >= 0)
            {
                BoardGeneratorParent<BG>.Instance.ChangeFont(idx == 0 ? null : m_fontSelect.items[idx]);
            }
        }

        protected void SafeActionInTextBoard(Action<BTD> toDo)
        {
            if (IsTextEditionAvailable() && !m_isLoading)
            {
                EnsureTabQuantityTexts(CurrentTabText);
                BTD descriptor = CurrentSelectedDescriptorArray[CurrentTabText];
                toDo(descriptor);
            }
        }

        protected void EnsureTabQuantityTexts(int size)
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
                else if (IsTextEditionAvailable() && i < (CurrentSelectedDescriptorArray?.Length ?? 0))
                {
                    ((UIButton) m_pseudoTabstripTexts.tabs[i]).text = (CurrentSelectedDescriptorArray?.ElementAtOrDefault(i)?.SaveName).IsNullOrWhiteSpace() ? $"T{i + 1}" : (CurrentSelectedDescriptorArray[i]?.SaveName);
                }
                else
                {
                    ((UIButton) m_pseudoTabstripTexts.tabs[i]).text = $"T{i + 1}";
                }
            }
        }
        protected void OnChangeTabTexts(int tabVal)
        {
            EnsureTabQuantityTexts(tabVal);
            CurrentTabText = tabVal;
            ReloadTabInfoText();

        }

        protected void LoadTabTextInfo(BTD descriptor)
        {
            m_textItemName.text = descriptor?.SaveName ?? "";
            TextType textType = descriptor?.m_textType ?? TextType.OwnName;
            m_dropdownTextContent.selectedIndex = Array.IndexOf(GetAvailableTextTypes(), textType);
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
            AfterLoadingTabTextInfo(descriptor);
        }
        protected void ConfigureTabsShownText(int quantity)
        {

            for (int i = 0; i < m_pseudoTabstripTexts.tabCount; i++)
            {
                m_pseudoTabstripTexts.tabs[i].isVisible = i < quantity || i == m_pseudoTabstripTexts.tabCount - 1;
            }

        }

        protected void SetTextOwnNameContent(int idx) => SafeActionInTextBoard(descriptor => descriptor.m_textType = GetAvailableTextTypes()[idx]);
        protected void SetTextCustom(string txt) => SafeActionInTextBoard(descriptor =>
        {
            descriptor.m_fixedText = txt?.Replace("\\n", "\n");
            OnChangeCustomText(descriptor);
        });

    }
}
