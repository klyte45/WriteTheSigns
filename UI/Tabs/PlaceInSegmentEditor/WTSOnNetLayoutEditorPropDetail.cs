using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensions;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Libraries;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Sprites;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSOnNetLayoutEditorPropDetail : UICustomControl
    {

        private OnNetInstanceCacheContainerXml CurrentEdited => WTSOnNetLayoutEditor.Instance.LayoutList.CurrentPropLayout;


        public UIPanel MainContainer { get; protected set; }

        private bool m_dirty;
        public bool Dirty
        {
            get => m_dirty; set
            {
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
        private UITextField m_propFilter;

        private UIDropDown m_positioningMode;
        private UISlider m_segmentPosition;
        private UISlider m_segmentPositionStart;
        private UISlider m_segmentPositionEnd;
        private UITextField m_segmentItemRepeatCount;
        private UITextField[] m_position;
        private UITextField[] m_rotation;
        private UITextField[] m_scale;
        private UICheckBox m_invertSide;

        private UILabel m_labelTarget1;
        private UILabel m_labelTarget2;
        private UILabel m_labelTarget3;
        private UILabel m_labelTarget4;

        private UITextField[] m_textParams;
        private UILabel[] m_textParamsLabels;
        private UIButton[] m_textParamsIsEmpty;


        private UIDropDown m_loadDD;
        private UIButton m_copySettings;
        private UIButton m_pasteSettings;
        private string m_clipboard;
        private UIButton m_libLoad;
        private UIButton m_libDelete;
        private UITextField m_libSaveNameField;
        private UIButton m_libSave;
        private UIButton m_gotoFileLib;


        public void Awake()
        {
            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.clipChildren = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.autoLayoutPadding = new RectOffset(0, 0, 4, 4);
            MainContainer.eventVisibilityChanged += (x, y) =>
            {
                if (y)
                {
                    SafeObtain(UpdateParams);
                }
            };

            KlyteMonoUtils.CreateTabsComponent(out m_tabstrip, out UITabContainer m_tabContainer, MainContainer.transform, "TextEditor", new Vector4(0, 0, MainContainer.width, 40), new Vector4(0, 0, MainContainer.width, MainContainer.height - 40));
            UIPanel m_tabSettings = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Settings), "K45_WTS_ONNETEDITOR_BASIC_SETTINGS", "RcSettings");
            UIPanel m_tabLocation = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_MoveCross), "K45_WTS_ONNETEDITOR_LOCATION_SETTINGS", "LcSettings");
            UIPanel m_tabTargets = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, "InfoIconEscapeRoutes", "K45_WTS_ONNETEDITOR_TARGET_SETTINGS", "TgSettings");
            UIScrollablePanel m_tabParameters = TabCommons.CreateScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_FontIcon), "K45_WTS_ONNETEDITOR_TEXT_PARAMETERS", "TpSettings");

            var helperSettings = new UIHelperExtension(m_tabSettings, LayoutDirection.Vertical);
            var helperLocation = new UIHelperExtension(m_tabLocation, LayoutDirection.Vertical);
            var helperTargets = new UIHelperExtension(m_tabTargets, LayoutDirection.Vertical);
            var helperParameters = new UIHelperExtension(m_tabParameters, LayoutDirection.Vertical);



            AddTextField(Locale.Get("K45_WTS_ONNETEDITOR_NAME"), out m_name, helperSettings, OnSetName);

            helperSettings.AddSpace(5);

            AddDropdown(Locale.Get("K45_WTS_BUILDINGEDITOR_PROPTYPE"), out m_propSelectionType, helperSettings, new string[] { Locale.Get("K45_WTS_ONNETEDITOR_PROPLAYOUT"), Locale.Get("K45_WTS_ONNETEDITOR_PROPMODELSELECT") }, OnPropSelecionClassChange);
            AddFilterableInput(Locale.Get("K45_WTS_BUILDINGEDITOR_MODELLAYOUTSELECT"), helperSettings, out m_propFilter, out _, OnFilterLayouts, OnConfigSelectionChange);

            AddDropdown(Locale.Get("K45_WTS_POSITIONINGMODE"), out m_positioningMode, helperLocation, new string[] { Locale.Get("K45_WTS_SINGLE"), Locale.Get("K45_WTS_MULTIPLE") }, OnPositioningModeChanged);
            AddSlider(Locale.Get("K45_WTS_ONNETEDITOR_SEGMENTPOSITION"), out m_segmentPosition, helperLocation, OnSegmentPositionChanged, 0, 1, 0.01f, (x) => x.ToString("F2"));
            AddSlider(Locale.Get("K45_WTS_ONNETEDITOR_SEGMENTPOSITION_START"), out m_segmentPositionStart, helperLocation, OnSegmentPositionStartChanged, 0, 1, 0.01f, (x) => x.ToString("F2"));
            AddSlider(Locale.Get("K45_WTS_ONNETEDITOR_SEGMENTPOSITION_END"), out m_segmentPositionEnd, helperLocation, OnSegmentPositionEndChanged, 0, 1, 0.01f, (x) => x.ToString("F2"));
            AddIntField(Locale.Get("K45_WTS_ONNETEDITOR_SEGMENTPOSITION_COUNT"), out m_segmentItemRepeatCount, helperLocation, OnSegmentPositionRepeatCountChanged, false);
            AddVector3Field(Locale.Get("K45_WTS_ONNETEDITOR_POSITIONOFFSET"), out m_position, helperLocation, OnPositionChanged);
            AddVector3Field(Locale.Get("K45_WTS_ONNETEDITOR_ROTATION"), out m_rotation, helperLocation, OnRotationChanged);
            AddVector3Field(Locale.Get("K45_WTS_ONNETEDITOR_SCALE"), out m_scale, helperLocation, OnScaleChanged);
            AddCheckboxLocale("K45_WTS_ONNETEDITOR_INVERTSIDE", out m_invertSide, helperLocation, OnInvertSideChanged);

            AddLabel(": ", helperTargets, out m_labelTarget1, out _, false);
            AddLabel(": ", helperTargets, out m_labelTarget2, out _, false);
            AddLabel(": ", helperTargets, out m_labelTarget3, out _, false);
            AddLabel(": ", helperTargets, out m_labelTarget4, out _, false);
            m_labelTarget1.prefix = Locale.Get("K45_WTS_ONNETEDITOR_TARGET1");
            m_labelTarget2.prefix = Locale.Get("K45_WTS_ONNETEDITOR_TARGET2");
            m_labelTarget3.prefix = Locale.Get("K45_WTS_ONNETEDITOR_TARGET3");
            m_labelTarget4.prefix = Locale.Get("K45_WTS_ONNETEDITOR_TARGET4");
            AddButtonInEditorRow(m_labelTarget1, CommonsSpriteNames.K45_Dropper, OnEnterPickTarget1, "K45_WTS_ONNETEDITOR_PICKNEWTARGET", true, 20).zOrder = 9999;
            AddButtonInEditorRow(m_labelTarget2, CommonsSpriteNames.K45_Dropper, OnEnterPickTarget2, "K45_WTS_ONNETEDITOR_PICKNEWTARGET", true, 20).zOrder = 9999;
            AddButtonInEditorRow(m_labelTarget3, CommonsSpriteNames.K45_Dropper, OnEnterPickTarget3, "K45_WTS_ONNETEDITOR_PICKNEWTARGET", true, 20).zOrder = 9999;
            AddButtonInEditorRow(m_labelTarget4, CommonsSpriteNames.K45_Dropper, OnEnterPickTarget4, "K45_WTS_ONNETEDITOR_PICKNEWTARGET", true, 20).zOrder = 9999;

            m_textParams = new UITextField[BoardInstanceOnNetXml.TEXT_PARAMETERS_COUNT];
            m_textParamsLabels = new UILabel[BoardInstanceOnNetXml.TEXT_PARAMETERS_COUNT];
            m_textParamsIsEmpty = new UIButton[BoardInstanceOnNetXml.TEXT_PARAMETERS_COUNT];
            for (int i = 0; i < BoardInstanceOnNetXml.TEXT_PARAMETERS_COUNT; i++)
            {
                var currentIdx = i;
                UISprite sprite = null;
                UILabel label = null;
                IEnumerator OnFilterParam(string x, Wrapper<string[]> result)
                {
                    yield return 0;
                    if (x.StartsWith(WTSAtlasesLibrary.PROTOCOL_IMAGE_ASSET) || x.StartsWith(WTSAtlasesLibrary.PROTOCOL_IMAGE))
                    {
                        yield return result.Value = OnFilterParamImages(sprite, x);
                    }
                    else if (x.StartsWith(CommandLevel.PROTOCOL_VARIABLE))
                    {
                        yield return result.Value = OnFilterParamVariable(label, x) ?? new string[0];
                    }
                }
                AddFilterableInput(string.Format(Locale.Get($"K45_WTS_ONNETEDITOR_TEXTPARAM"), currentIdx), helperParameters, out m_textParams[currentIdx], out m_textParamsLabels[currentIdx], out UIListBox lb, OnFilterParam, (t, x, y) => OnParamChanged(t, currentIdx, x, y, label));
                m_textParamsLabels[currentIdx].processMarkup = true;
                m_textParamsIsEmpty[currentIdx] = AddButtonInEditorRow(m_textParams[currentIdx], CommonsSpriteNames.K45_X, () => SafeObtain((x) =>
                   {
                       var isEmpty = x.GetTextParameter(currentIdx).IsEmpty;
                       x.SetTextParameter(currentIdx, isEmpty ? "" : null);
                       UpdateParamIsEmptied(x, currentIdx);
                   }), "K45_WTS_TOGGLETEXTISEMPTYTOOLTIP", true, 30);
                sprite = AddSpriteInEditorRow(lb, true, 300);
                label = AddLabelInEditorRow(lb, false, 300);
                label.padding = new RectOffset(5, 5, 5, 5);
                m_textParams[currentIdx].eventGotFocus += (x, y) =>
                {
                    var tf = ((UITextField)x);
                    var text = tf.text;
                    if (text.StartsWith(WTSAtlasesLibrary.PROTOCOL_IMAGE_ASSET) || text.StartsWith(WTSAtlasesLibrary.PROTOCOL_IMAGE))
                    {
                        sprite.spriteName = ((UITextField)x).text.Split('/').Last().Trim();
                        sprite.isVisible = true;
                        label.isVisible = false;
                    }
                    else if (text.StartsWith(CommandLevel.PROTOCOL_VARIABLE))
                    {
                        sprite.isVisible = false;
                        label.isVisible = true;
                        tf.selectOnFocus = false;
                        tf.selectionStart = text.Length;
                        tf.selectionEnd = text.Length;
                    }
                    else
                    {
                        tf.selectOnFocus = true;
                    }
                };
                lb.eventItemMouseHover += (x, y) =>
                {
                    if (m_textParams[currentIdx].text.StartsWith(WTSAtlasesLibrary.PROTOCOL_IMAGE_ASSET) || m_textParams[currentIdx].text.StartsWith(WTSAtlasesLibrary.PROTOCOL_IMAGE))
                    {
                        sprite.spriteName = lb.items[y].Split('/').Last().Trim();
                    }
                    else if (m_textParams[currentIdx].text.StartsWith(CommandLevel.PROTOCOL_VARIABLE))
                    {
                        if (label.objectUserData is CommandLevel cmd && !(cmd.nextLevelOptions is null))
                        {
                            var str = lb.items[y];
                            var key = cmd.nextLevelOptions.Where(z => z.Key.ToString() == str).FirstOrDefault().Key;
                            label.text = key is null ? "" : Locale.Get("K45_WTS_PARAMVARS_DESC", CommandLevel.ToLocaleVar(key));
                        }
                    }
                };
                lb.eventVisibilityChanged += (x, y) =>
                {
                    var text = m_textParams[currentIdx].text;
                    if (text.StartsWith(WTSAtlasesLibrary.PROTOCOL_IMAGE_ASSET) || text.StartsWith(WTSAtlasesLibrary.PROTOCOL_IMAGE))
                    {
                        sprite.spriteName = ((UITextField)x).text.Split('/').Last().Trim();
                        sprite.isVisible = true;
                        label.isVisible = y;
                    }
                    else if (text.StartsWith(CommandLevel.PROTOCOL_VARIABLE))
                    {
                        sprite.isVisible = false;
                        label.isVisible = y;
                    }
                };
                sprite.isVisible = false;

            }

            AddLibBox<WTSLibOnNetPropLayout, BoardInstanceOnNetXml>(helperSettings, out m_copySettings, OnCopyRule, out m_pasteSettings, OnPasteRule, out _, null, out m_loadDD, out m_libLoad, out m_libDelete, out m_libSaveNameField, out m_libSave, out m_gotoFileLib, OnLoadRule, GetRuleSerialized);

            WTSOnNetLayoutEditor.Instance.LayoutList.EventSelectionChanged += OnChangeTab;
            MainContainer.isVisible = false;
            m_pasteSettings.isVisible = false;



        }

        private string lastProtocol_searchedParam;

        private string OnParamChanged(string inputText, int paramIdx, int selIdx, string[] array, UILabel lbl)
        {
            if (inputText.StartsWith(CommandLevel.PROTOCOL_VARIABLE) && lbl != null && lbl.objectUserData is CommandLevel cl)
            {
                if (selIdx >= 0)
                {
                    StartCoroutine(RefocusParamIn2Frames(paramIdx));
                    var pathVal = string.Join("/", CommandLevel.GetParameterPath(inputText.Substring(CommandLevel.PROTOCOL_VARIABLE.Length)).Take(cl.level).ToArray());
                    inputText = $"{CommandLevel.PROTOCOL_VARIABLE}{pathVal}{(cl.level > 0 ? "/" : "")}{array[selIdx]}/";
                }
                CurrentEdited.SetTextParameter(paramIdx, inputText);
                return inputText;
            }
            else if (selIdx >= 0 && lastProtocol_searchedParam == WTSAtlasesLibrary.PROTOCOL_IMAGE && array[selIdx].EndsWith("/"))
            {
                StartCoroutine(RefocusParamIn2Frames(paramIdx));
                return lastProtocol_searchedParam + array[selIdx].Trim();
            }
            else
            {
                if (selIdx >= 0 && !(lastProtocol_searchedParam is null))
                {
                    CurrentEdited.SetTextParameter(paramIdx, lastProtocol_searchedParam + array[selIdx].Trim());
                }
                else
                {
                    CurrentEdited.SetTextParameter(paramIdx, inputText);
                }
                lastProtocol_searchedParam = null;
                return CurrentEdited?.GetTextParameter(paramIdx)?.ToString() ?? "";
            }
        }

        private IEnumerator RefocusParamIn2Frames(int paramIdx)
        {
            yield return new WaitForEndOfFrame();
            m_textParams[paramIdx].Focus();
        }

        private string[] OnFilterParamImages(UISprite sprite, string arg)
        {
            string[] results = null;
            SafeObtain((x) => results = WriteTheSignsMod.Controller.AtlasesLibrary.OnFilterParamImagesByText(sprite, arg, x.Descriptor?.CachedProp?.name ?? x.m_simpleCachedProp?.name, out lastProtocol_searchedParam));
            return results;
        }

        private string[] OnFilterParamVariable(UILabel lbl, string arg)
        {
            var cmdResult = CommandLevel.OnFilterParamImagesByText(arg, out string currentDescription);
            if (cmdResult is null)
            {
                lbl.isVisible = false;
                return null;
            }
            else {
                lbl.isVisible = true;
                lbl.objectUserData = cmdResult;
                lbl.prefix = cmdResult.regexValidValues.IsNullOrWhiteSpace() ? "" : $"Regex: <color yellow>{cmdResult.regexValidValues}</color>\n";
                lbl.text = Locale.Get("K45_WTS_PARAMVARS_DESC", currentDescription);
                return cmdResult.nextLevelOptions?.Select(x => x.Key.ToString()).OrderBy(x => x).ToArray();
            }
        }


        private IEnumerator OnFilterLayouts(string input, Wrapper<string[]> result)
        {
            if (m_propSelectionType.selectedIndex == 0)
            {
                yield return WTSPropLayoutData.Instance.FilterBy(input, TextRenderingClass.PlaceOnNet, result);
            }
            else
            {
                yield return PropIndexes.instance.BasicInputFiltering(input, result);
            }
        }

        private string OnConfigSelectionChange(string typed, int sel, string[] items)
        {
            if (sel == -1)
            {
                sel = Array.IndexOf(items, typed?.Trim());
            }
            bool isValidSelection = sel >= 0 && sel < items.Length;
            string targetValue = isValidSelection ? items[sel] : "";

            SafeObtain((OnNetInstanceCacheContainerXml x) =>
            {
                if (m_propSelectionType.selectedIndex == 0)
                {
                    x.PropLayoutName = targetValue;
                    x.m_simplePropName = null;
                    UpdateParams(x);
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


        private void OnPropSelecionClassChange(int sel) => SafeObtain((OnNetInstanceCacheContainerXml x) =>
        {
            m_propFilter.text = (sel == 0 ? x.PropLayoutName ?? "" : PropIndexes.GetListName(x.SimpleProp)) ?? "";
            UpdateTabsVisibility(sel);
            UpdateParams(x);
        });

        private void OnPositioningModeChanged(int sel) => SafeObtain((OnNetInstanceCacheContainerXml x) =>
        {
            var rept = sel == 1;
            x.SegmentPositionRepeating = rept;
            StartCoroutine(HideShowFieldsPosition(rept));
        });

        private IEnumerator HideShowFieldsPosition(bool rept)
        {
            yield return 0;
            yield return 0;
            m_segmentPosition.parent.isVisible = !rept;
            m_segmentPositionStart.parent.isVisible = rept;
            m_segmentPositionEnd.parent.isVisible = rept;
            m_segmentItemRepeatCount.parent.isVisible = rept;
            yield break;
        }

        private void UpdateTabsVisibility(int sel)
        {
            if (sel == 0)
            {
                m_tabstrip.ShowTab("TgSettings");
            }
            else
            {
                m_tabstrip.HideTab("TgSettings");
            }
;
        }


        private void OnEnterPickTarget1()
        {
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.OnSelectSegment += (k) => SafeObtain((OnNetInstanceCacheContainerXml x) =>
            {
                x.m_targetSegment1 = k;
                ReloadTargets(x);
            });
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.enabled = true;
        }
        private void OnEnterPickTarget2()
        {
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.OnSelectSegment += (k) => SafeObtain((OnNetInstanceCacheContainerXml x) =>
            {
                x.m_targetSegment2 = k;
                ReloadTargets(x);
            });
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.enabled = true;
        }
        private void OnEnterPickTarget3()
        {
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.OnSelectSegment += (k) => SafeObtain((OnNetInstanceCacheContainerXml x) =>
            {
                x.m_targetSegment3 = k;
                ReloadTargets(x);
            });
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.enabled = true;
        }
        private void OnEnterPickTarget4()
        {
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.OnSelectSegment += (k) => SafeObtain((OnNetInstanceCacheContainerXml x) =>
            {
                x.m_targetSegment4 = k;
                ReloadTargets(x);
            });
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.enabled = true;
        }



        private bool m_isLoading;

        private delegate void SafeObtainMethod(OnNetInstanceCacheContainerXml x);
        private void SafeObtain(SafeObtainMethod action)
        {
            if (CurrentEdited != null && !m_isLoading)
            {
                action(CurrentEdited);
            }
        }
        private void OnChangeTab(ref OnNetInstanceCacheContainerXml current)
        {
            MainContainer.isVisible = current != null;
            Dirty = true;
        }


        private void ReloadData()
        {
            SafeObtain((OnNetInstanceCacheContainerXml x) =>
            {
                m_isLoading = true;
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
                m_segmentPosition.value = x.SegmentPosition;
                m_segmentPositionStart.value = x.SegmentPositionStart;
                m_segmentPositionEnd.value = x.SegmentPositionEnd;
                m_segmentItemRepeatCount.text = x.SegmentPositionRepeatCount.ToString("0");
                m_positioningMode.selectedIndex = x.SegmentPositionRepeating ? 1 : 0;
                m_invertSide.isChecked = x.InvertSign;

                UpdateParams(x);
                ReloadTargets(x);
                UpdateTabsVisibility(m_propSelectionType.selectedIndex);
                StartCoroutine(HideShowFieldsPosition(x.SegmentPositionRepeating));
                m_isLoading = false;
            });
            Dirty = false;
        }

        private void UpdateParams(OnNetInstanceCacheContainerXml x)
        {
            var paramsUsed = x.GetAllParametersUsed();
            if ((paramsUsed?.Count ?? 0) > 0)
            {
                m_tabstrip.ShowTab("TpSettings");
                for (int i = 0; i < m_textParams.Length; i++)
                {
                    if (paramsUsed?.ContainsKey(i) ?? false)
                    {
                        var param = x.GetTextParameter(i);

                        m_textParamsLabels[i].suffix = $" - {Locale.Get("K45_WTS_USEDAS")}\n{string.Join("\n", paramsUsed[i])}";
                        m_textParams[i].text = param?.ToString() ?? "";
                        m_textParams[i].parent.isVisible = true;
                        UpdateParamIsEmptied(x, i);
                    }
                    else
                    {
                        m_textParams[i].parent.isVisible = false;
                    }
                }
            }
            else
            {
                m_tabstrip.HideTab("TpSettings");
            }
        }

        private void UpdateParamIsEmptied(OnNetInstanceCacheContainerXml x, int i)
        {
            if (x.GetTextParameter(i)?.IsEmpty ?? false)
            {
                m_textParamsIsEmpty[i].color = Color.red;
                m_textParamsIsEmpty[i].focusedColor = Color.red;
                m_textParamsIsEmpty[i].pressedColor = Color.red;
                m_textParamsIsEmpty[i].disabledColor = Color.red;
                m_textParams[i].Disable();
                m_textParams[i].text = "";
            }
            else
            {
                m_textParamsIsEmpty[i].color = Color.white;
                m_textParamsIsEmpty[i].focusedColor = Color.white;
                m_textParamsIsEmpty[i].pressedColor = Color.white;
                m_textParamsIsEmpty[i].disabledColor = Color.white;
                m_textParams[i].Enable();
            }
        }

        private void ReloadTargets(OnNetInstanceCacheContainerXml x)
        {
            m_labelTarget1.suffix = GetTextForSegment(x.m_targetSegment1) ?? "";
            m_labelTarget2.suffix = GetTextForSegment(x.m_targetSegment2) ?? "";
            m_labelTarget3.suffix = GetTextForSegment(x.m_targetSegment3) ?? "";
            m_labelTarget4.suffix = GetTextForSegment(x.m_targetSegment4) ?? "";
        }


        private string GetTextForSegment(ushort targetSegment)
        {
            if (targetSegment == 0)
            {
                return Locale.Get("K45_WTS_ONNETEDITOR_UNSETTARGETDESC");
            }
            else
            {
                var pos = NetManager.instance.m_segments.m_buffer[targetSegment].m_middlePosition;
                WriteTheSignsMod.Controller.ConnectorADR.GetAddressStreetAndNumber(pos, pos, out int num, out string streetName);
                return $"{((streetName?.Length ?? 0) == 0 ? NetManager.instance.m_segments.m_buffer[targetSegment].Info.GetLocalizedTitle() : streetName)}, ~{num}m";
            }
        }

        public void Update()
        {
            if (WTSOnNetLayoutEditor.Instance.LayoutList.MainContainer.isVisible)
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




        private string GetRuleSerialized() => CurrentEdited != null ? XmlUtils.DefaultXmlSerialize(CurrentEdited) : null;

        private void OnLoadRule(string obj)
        {
            WTSOnNetLayoutEditor.Instance.LayoutList.CurrentPropLayout = XmlUtils.DefaultXmlDeserialize<OnNetInstanceCacheContainerXml>(obj);
            WTSOnNetLayoutEditor.Instance.LayoutList.FixTabstrip();
            ReloadData();
        }
        private void OnPasteRule() => OnLoadRule(m_clipboard);
        private void OnCopyRule() => SafeObtain((OnNetInstanceCacheContainerXml x) =>
        {
            m_clipboard = XmlUtils.DefaultXmlSerialize(x);
            m_pasteSettings.isVisible = true;
        });


        private void OnRotationChanged(Vector3 obj) => SafeObtain((OnNetInstanceCacheContainerXml x) => x.PropRotation = (Vector3Xml)obj);
        private void OnScaleChanged(Vector3 obj) => SafeObtain((OnNetInstanceCacheContainerXml x) => x.PropScale = obj);
        private void OnPositionChanged(Vector3 obj) => SafeObtain((OnNetInstanceCacheContainerXml x) => x.PropPosition = (Vector3Xml)obj);
        private void OnSegmentPositionChanged(float val) => SafeObtain((OnNetInstanceCacheContainerXml x) => x.SegmentPosition = val);
        private void OnSegmentPositionStartChanged(float val) => SafeObtain((OnNetInstanceCacheContainerXml x) => x.SegmentPositionStart = val);
        private void OnSegmentPositionEndChanged(float val) => SafeObtain((OnNetInstanceCacheContainerXml x) => x.SegmentPositionEnd = val);
        private void OnSegmentPositionRepeatCountChanged(int val) => SafeObtain((OnNetInstanceCacheContainerXml x) => x.SegmentPositionRepeatCount = (ushort)val);
        private void OnInvertSideChanged(bool isChecked) => SafeObtain((OnNetInstanceCacheContainerXml x) => x.InvertSign = isChecked);



        private void OnSetName(string text) => SafeObtain((OnNetInstanceCacheContainerXml x) =>
        {
            if (!text.IsNullOrWhiteSpace())
            {
                x.SaveName = text;
            }
            else
            {
                m_name.text = x.SaveName;
            }
        });


    }


}
