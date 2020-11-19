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
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSOnNetLayoutEditorPropDetail : UICustomControl
    {

        private ref OnNetInstanceCacheContainerXml CurrentEdited => ref WTSOnNetLayoutEditor.Instance.LayoutList.CurrentPropLayout;


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
        private UIDropDown m_propLayoutSelect;
        private UISlider m_segmentPosition;
        private UITextField[] m_position;
        private UITextField[] m_rotation;
        private UITextField[] m_scale;
        private UICheckBox m_invertSide;

        private UILabel m_labelTarget1;
        private UILabel m_labelTarget2;
        private UILabel m_labelTarget3;
        private UILabel m_labelTarget4;

        private UITextField m_textParam1;
        private UITextField m_textParam2;
        private UITextField m_textParam3;
        private UITextField m_textParam4;
        private UITextField m_textParam5;
        private UITextField m_textParam6;
        private UITextField m_textParam7;
        private UITextField m_textParam8;


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

            AddDropdown(Locale.Get("K45_WTS_ONNETEDITOR_PROPLAYOUT"), out m_propLayoutSelect, helperSettings, new string[0], OnPropLayoutChange);
            AddButtonInEditorRow(m_propLayoutSelect, CommonsSpriteNames.K45_Reload, LoadAvailableLayouts);
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
            AddButtonInEditorRow(m_labelTarget1, CommonsSpriteNames.K45_Pipette, OnEnterPickTarget1, Locale.Get("K45_WTS_ONNETEDITOR_PICKNEWTARGET"), true, 20).zOrder = 9999;
            AddButtonInEditorRow(m_labelTarget2, CommonsSpriteNames.K45_Pipette, OnEnterPickTarget2, Locale.Get("K45_WTS_ONNETEDITOR_PICKNEWTARGET"), true, 20).zOrder = 9999;
            AddButtonInEditorRow(m_labelTarget3, CommonsSpriteNames.K45_Pipette, OnEnterPickTarget3, Locale.Get("K45_WTS_ONNETEDITOR_PICKNEWTARGET"), true, 20).zOrder = 9999;
            AddButtonInEditorRow(m_labelTarget4, CommonsSpriteNames.K45_Pipette, OnEnterPickTarget4, Locale.Get("K45_WTS_ONNETEDITOR_PICKNEWTARGET"), true, 20).zOrder = 9999;

            AddTextField(Locale.Get("K45_WTS_ONNETEDITOR_TEXTPARAM1"), out m_textParam1, helperParameters, (txt) => OnChangeTextParameter(1, txt));
            AddTextField(Locale.Get("K45_WTS_ONNETEDITOR_TEXTPARAM2"), out m_textParam2, helperParameters, (txt) => OnChangeTextParameter(2, txt));
            AddTextField(Locale.Get("K45_WTS_ONNETEDITOR_TEXTPARAM3"), out m_textParam3, helperParameters, (txt) => OnChangeTextParameter(3, txt));
            AddTextField(Locale.Get("K45_WTS_ONNETEDITOR_TEXTPARAM4"), out m_textParam4, helperParameters, (txt) => OnChangeTextParameter(4, txt));
            AddTextField(Locale.Get("K45_WTS_ONNETEDITOR_TEXTPARAM5"), out m_textParam5, helperParameters, (txt) => OnChangeTextParameter(5, txt));
            AddTextField(Locale.Get("K45_WTS_ONNETEDITOR_TEXTPARAM6"), out m_textParam6, helperParameters, (txt) => OnChangeTextParameter(6, txt));
            AddTextField(Locale.Get("K45_WTS_ONNETEDITOR_TEXTPARAM7"), out m_textParam7, helperParameters, (txt) => OnChangeTextParameter(7, txt));
            AddTextField(Locale.Get("K45_WTS_ONNETEDITOR_TEXTPARAM8"), out m_textParam8, helperParameters, (txt) => OnChangeTextParameter(8, txt));

            AddLibBox<WTSLibOnNetPropLayout, BoardInstanceOnNetXml>(helperSettings, out m_copySettings, OnCopyRule, out m_pasteSettings, OnPasteRule, out _, null, out m_loadDD, out m_libLoad, out m_libDelete, out m_libSaveNameField, out m_libSave, out m_gotoFileLib, OnLoadRule, GetRuleSerialized);

            WTSOnNetLayoutEditor.Instance.LayoutList.EventSelectionChanged += OnChangeTab;
            LoadAvailableLayouts();
            MainContainer.isVisible = false;
            m_pasteSettings.isVisible = false;



        }


        private void OnEnterPickTarget1()
        {
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.OnSelectSegment += (k) => SafeObtain((ref OnNetInstanceCacheContainerXml x) =>
            {
                x.m_targetSegment1 = k;
                ReloadTargets(x);
            });
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.enabled = true;
        }
        private void OnEnterPickTarget2()
        {
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.OnSelectSegment += (k) => SafeObtain((ref OnNetInstanceCacheContainerXml x) =>
            {
                x.m_targetSegment2 = k;
                ReloadTargets(x);
            });
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.enabled = true;
        }
        private void OnEnterPickTarget3()
        {
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.OnSelectSegment += (k) => SafeObtain((ref OnNetInstanceCacheContainerXml x) =>
            {
                x.m_targetSegment3 = k;
                ReloadTargets(x);
            });
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.enabled = true;
        }
        private void OnEnterPickTarget4()
        {
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.OnSelectSegment += (k) => SafeObtain((ref OnNetInstanceCacheContainerXml x) =>
            {
                x.m_targetSegment4 = k;
                ReloadTargets(x);
            });
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.enabled = true;
        }

        private void LoadAvailableLayouts()
        {
            m_propLayoutSelect.items = WTSPropLayoutData.Instance.ListWhere(x => x.m_allowedRenderClass == TextRenderingClass.PlaceOnNet).ToArray();
            SafeObtain((ref OnNetInstanceCacheContainerXml x) =>
            {
                if (x.PropLayoutName != null && !m_propLayoutSelect.items.Contains(x.PropLayoutName))
                {
                    m_propLayoutSelect.items = m_propLayoutSelect.items.Union(new string[] { x.PropLayoutName }).OrderBy(x => x).ToArray();
                }
                m_propLayoutSelect.selectedValue = x.PropLayoutName;
            });
        }




        private delegate void SafeObtainMethod(ref OnNetInstanceCacheContainerXml x);
        private void SafeObtain(SafeObtainMethod action)
        {
            if (CurrentEdited != null)
            {
                action(ref CurrentEdited);
            }
        }
        private void OnChangeTab(ref OnNetInstanceCacheContainerXml current)
        {
            MainContainer.isVisible = current != null;
            LoadAvailableLayouts();
            Dirty = true;
        }


        private void ReloadData()
        {
            SafeObtain((ref OnNetInstanceCacheContainerXml x) =>
            {
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
                m_segmentPosition.value = x.SegmentPosition;
                m_invertSide.isChecked = x.InvertSign;

                m_textParam1.text = x.m_textParameter1 ?? "";
                m_textParam2.text = x.m_textParameter2 ?? "";
                m_textParam3.text = x.m_textParameter3 ?? "";
                m_textParam4.text = x.m_textParameter4 ?? "";
                m_textParam5.text = x.m_textParameter5 ?? "";
                m_textParam6.text = x.m_textParameter6 ?? "";
                m_textParam7.text = x.m_textParameter7 ?? "";
                m_textParam8.text = x.m_textParameter8 ?? "";

                ReloadTargets(x);
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

        private void OnLoadRule(string obj) => SafeObtain((ref OnNetInstanceCacheContainerXml x) =>
        {
            x = XmlUtils.DefaultXmlDeserialize<OnNetInstanceCacheContainerXml>(obj);
            WTSOnNetLayoutEditor.Instance.LayoutList.FixTabstrip();
            ReloadData();
        });
        private void OnPasteRule() => OnLoadRule(m_clipboard);
        private void OnCopyRule() => SafeObtain((ref OnNetInstanceCacheContainerXml x) =>
        {
            m_clipboard = XmlUtils.DefaultXmlSerialize(x);
            m_pasteSettings.isVisible = true;
        });


        private void OnRotationChanged(Vector3 obj) => SafeObtain((ref OnNetInstanceCacheContainerXml x) => x.PropRotation = (Vector3Xml)obj);
        private void OnScaleChanged(Vector3 obj) => SafeObtain((ref OnNetInstanceCacheContainerXml x) => x.PropScale = obj);
        private void OnPositionChanged(Vector3 obj) => SafeObtain((ref OnNetInstanceCacheContainerXml x) => x.PropPosition = (Vector3Xml)obj);
        private void OnSegmentPositionChanged(float val) => SafeObtain((ref OnNetInstanceCacheContainerXml x) => x.SegmentPosition = val);
        private void OnInvertSideChanged(bool isChecked) => SafeObtain((ref OnNetInstanceCacheContainerXml x) => x.InvertSign = isChecked);

        private void OnPropLayoutChange(int sel) => SafeObtain((ref OnNetInstanceCacheContainerXml x) =>
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

        private void OnSetName(string text) => SafeObtain((ref OnNetInstanceCacheContainerXml x) =>
        {
            if (!text.IsNullOrWhiteSpace())
            {
                x.SaveName = text;
                WTSOnNetLayoutEditor.Instance.LayoutList.FixTabstrip();
                Dirty = true;
            }
            else
            {
                m_name.text = x.SaveName;
            }
        });


        private void OnChangeTextParameter(int v, string txt) => SafeObtain((ref OnNetInstanceCacheContainerXml x) =>
        {
            switch (v)
            {
                case 1: x.m_textParameter1 = txt; break;
                case 2: x.m_textParameter2 = txt; break;
                case 3: x.m_textParameter3 = txt; break;
                case 4: x.m_textParameter4 = txt; break;
                case 5: x.m_textParameter5 = txt; break;
                case 6: x.m_textParameter6 = txt; break;
                case 7: x.m_textParameter7 = txt; break;
                case 8: x.m_textParameter8 = txt; break;
            }
        });
    }


}
