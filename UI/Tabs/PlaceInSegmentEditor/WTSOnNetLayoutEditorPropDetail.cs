using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Libraries;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Linq;
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
        private UITextField m_propFilter;
        private UISlider m_segmentPosition;
        private UITextField[] m_position;
        private UITextField[] m_rotation;
        private UITextField[] m_scale;
        private UICheckBox m_invertSide;

        private UILabel m_labelTarget1;
        private UILabel m_labelTarget2;
        private UILabel m_labelTarget3;
        private UILabel m_labelTarget4;

        private UITextField[] m_textParams;


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


        public void Awake()
        {
            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.clipChildren = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.autoLayoutPadding = new RectOffset(0, 0, 4, 4);

            KlyteMonoUtils.CreateTabsComponent(out m_tabstrip, out UITabContainer m_tabContainer, MainContainer.transform, "TextEditor", new Vector4(0, 0, MainContainer.width, 40), new Vector4(0, 0, MainContainer.width, MainContainer.height - 40));
            UIPanel m_tabSettings = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Settings), "K45_WTS_ONNETEDITOR_BASIC_SETTINGS", "RcSettings");
            UIPanel m_tabTargets = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, "InfoIconEscapeRoutes", "K45_WTS_ONNETEDITOR_TARGET_SETTINGS", "TgSettings");
            UIPanel m_tabParameters = TabCommons.CreateNonScrollableTabLocalized(m_tabstrip, KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_FontIcon), "K45_WTS_ONNETEDITOR_TEXT_PARAMETERS", "TpSettings");

            var helperSettings = new UIHelperExtension(m_tabSettings, LayoutDirection.Vertical);
            var helperTargets = new UIHelperExtension(m_tabTargets, LayoutDirection.Vertical);
            var helperParameters = new UIHelperExtension(m_tabParameters, LayoutDirection.Vertical);



            AddTextField(Locale.Get("K45_WTS_ONNETEDITOR_NAME"), out m_name, helperSettings, OnSetName);

            helperSettings.AddSpace(5);

            AddDropdown(Locale.Get("K45_WTS_BUILDINGEDITOR_PROPTYPE"), out m_propSelectionType, helperSettings, new string[] { Locale.Get("K45_WTS_ONNETEDITOR_PROPLAYOUT"), Locale.Get("K45_WTS_ONNETEDITOR_PROPMODELSELECT") }, OnPropSelecionClassChange);
            AddFilterableInput(Locale.Get("K45_WTS_BUILDINGEDITOR_MODELLAYOUTSELECT"), helperSettings, out m_propFilter, out _, OnFilterLayouts, OnConfigSelectionChange);


            AddSlider(Locale.Get("K45_WTS_ONNETEDITOR_SEGMENTPOSITION"), out m_segmentPosition, helperSettings, OnSegmentPositionChanged, 0, 1, 0.01f, (x) => x.ToString("F2"));
            AddVector3Field(Locale.Get("K45_WTS_ONNETEDITOR_POSITIONOFFSET"), out m_position, helperSettings, OnPositionChanged);
            AddVector3Field(Locale.Get("K45_WTS_ONNETEDITOR_ROTATION"), out m_rotation, helperSettings, OnRotationChanged);
            AddVector3Field(Locale.Get("K45_WTS_ONNETEDITOR_SCALE"), out m_scale, helperSettings, OnScaleChanged);
            AddCheckboxLocale("K45_WTS_ONNETEDITOR_INVERTSIDE", out m_invertSide, helperSettings, OnInvertSideChanged);

            AddLabel(": ", helperTargets, out m_labelTarget1, out _, false);
            AddLabel(": ", helperTargets, out m_labelTarget2, out _, false);
            AddLabel(": ", helperTargets, out m_labelTarget3, out _, false);
            AddLabel(": ", helperTargets, out m_labelTarget4, out _, false);
            m_labelTarget1.prefix = Locale.Get("K45_WTS_ONNETEDITOR_TARGET1");
            m_labelTarget2.prefix = Locale.Get("K45_WTS_ONNETEDITOR_TARGET2");
            m_labelTarget3.prefix = Locale.Get("K45_WTS_ONNETEDITOR_TARGET3");
            m_labelTarget4.prefix = Locale.Get("K45_WTS_ONNETEDITOR_TARGET4");
            AddButtonInEditorRow(m_labelTarget1, CommonsSpriteNames.K45_Pipette, OnEnterPickTarget1, "K45_WTS_ONNETEDITOR_PICKNEWTARGET", true, 20).zOrder = 9999;
            AddButtonInEditorRow(m_labelTarget2, CommonsSpriteNames.K45_Pipette, OnEnterPickTarget2, "K45_WTS_ONNETEDITOR_PICKNEWTARGET", true, 20).zOrder = 9999;
            AddButtonInEditorRow(m_labelTarget3, CommonsSpriteNames.K45_Pipette, OnEnterPickTarget3, "K45_WTS_ONNETEDITOR_PICKNEWTARGET", true, 20).zOrder = 9999;
            AddButtonInEditorRow(m_labelTarget4, CommonsSpriteNames.K45_Pipette, OnEnterPickTarget4, "K45_WTS_ONNETEDITOR_PICKNEWTARGET", true, 20).zOrder = 9999;

            m_textParams = new UITextField[BoardInstanceOnNetXml.TEXT_PARAMETERS_COUNT];
            for (int i = 0; i < BoardInstanceOnNetXml.TEXT_PARAMETERS_COUNT; i++)
            {
                var currentIdx = i;
                AddFilterableInput(string.Format(Locale.Get($"K45_WTS_ONNETEDITOR_TEXTPARAM"), currentIdx), helperParameters, out m_textParams[i], out UIListBox lb, OnFilterParamImages, (t, x, y) => OnParamChanged(t, currentIdx, x, y));
                lb.processMarkup = true;
                var sprite = AddSpriteInEditorRow(lb, true, 300);
                m_textParams[i].eventGotFocus += (x, y) => sprite.spriteName = ((UITextField)x).text.Length >= 4 ? ((UITextField)x).text.Substring(4) : "";
                lb.eventItemMouseHover += (x, y) => sprite.spriteName = lb.items[y].Split(">".ToCharArray(), 2)[1].Trim();
                lb.eventVisibilityChanged += (x, y) => sprite.isVisible = y;
                sprite.isVisible = false;
            }

            AddLibBox<WTSLibOnNetPropLayout, BoardInstanceOnNetXml>(helperSettings, out m_copySettings, OnCopyRule, out m_pasteSettings, OnPasteRule, out _, null, out m_loadDD, out m_libLoad, out m_libDelete, out m_libSaveNameField, out m_libSave, out m_gotoFileLib, OnLoadRule, GetRuleSerialized);

            WTSOnNetLayoutEditor.Instance.LayoutList.EventSelectionChanged += OnChangeTab;
            MainContainer.isVisible = false;
            m_pasteSettings.isVisible = false;



        }

        private string OnParamChanged(string inputText, int paramIdx, int selIdx, string[] array)
        {
            if (selIdx >= 0)
            {
                CurrentEdited.SetTextParameter(paramIdx, "IMG_" + array[selIdx].Split(new char[] { '>' }, 2)[1].Trim());
            }
            else
            {
                CurrentEdited.SetTextParameter(paramIdx, inputText);
            }
            return CurrentEdited?.GetTextParameter(paramIdx) ?? "";
        }
        private string[] OnFilterParamImages(string arg)
        {
            if (arg.Length >= 4 && arg?.ToUpper().StartsWith("IMG_") == true)
            {
                return m_textParams[0].atlas.spriteNames.Where(x => x.ToLower().Contains(arg.Substring(4).ToLower())).OrderBy(x => x).Select(x => $"<sprite {x}> {x}").ToArray();
            }
            else
            {
                return null;
            }
        }


        private string[] OnFilterLayouts(string input) => m_propSelectionType.selectedIndex == 0 ? WTSPropLayoutData.Instance.FilterBy(input, TextRenderingClass.PlaceOnNet) : PropIndexes.instance.BasicInputFiltering(input);

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
            m_propFilter.text = sel == 0 ? x.PropLayoutName ?? "" : PropIndexes.GetListName(x.SimpleProp);
            UpdateTabsVisibility(sel);
        });

        private void UpdateTabsVisibility(int sel)
        {
            if (sel == 0)
            {
                m_tabstrip.ShowTab("TpSettings");
                m_tabstrip.ShowTab("TgSettings");
            }
            else
            {
                m_tabstrip.HideTab("TpSettings");
                m_tabstrip.HideTab("TgSettings");
            }
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
                m_invertSide.isChecked = x.InvertSign;

                for (int i = 0; i < m_textParams.Length; i++)
                {
                    m_textParams[i].text = x.GetTextParameter(i) ?? "";
                }
                ReloadTargets(x);
                UpdateTabsVisibility(m_propSelectionType.selectedIndex);
                m_isLoading = false;
            });
            Dirty = false;
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
                SegmentUtils.GetAddressStreetAndNumber(pos, pos, out int num, out string streetName);
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
