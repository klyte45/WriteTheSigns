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

            var helperSettings = new UIHelperExtension(m_tabSettings, LayoutDirection.Vertical); ;



            AddTextField(Locale.Get("K45_WTS_ONNETEDITOR_NAME"), out m_name, helperSettings, OnSetName);

            helperSettings.AddSpace(5);

            AddDropdown(Locale.Get("K45_WTS_ONNETEDITOR_PROPLAYOUT"), out m_propLayoutSelect, helperSettings, new string[0], OnPropLayoutChange);
            AddButtonInEditorRow(m_propLayoutSelect, CommonsSpriteNames.K45_Reload, LoadAvailableLayouts);
            AddSlider(Locale.Get("K45_WTS_ONNETEDITOR_SEGMENTPOSITION"), out m_segmentPosition, helperSettings, OnSegmentPositionChanged, 0, 1, 0.01f, (x) => x.ToString("F2"));
            AddVector3Field(Locale.Get("K45_WTS_ONNETEDITOR_POSITIONOFFSET"), out m_position, helperSettings, OnPositionChanged);
            AddVector3Field(Locale.Get("K45_WTS_ONNETEDITOR_ROTATION"), out m_rotation, helperSettings, OnRotationChanged);
            AddVector3Field(Locale.Get("K45_WTS_ONNETEDITOR_SCALE"), out m_scale, helperSettings, OnScaleChanged);
            AddCheckboxLocale("K45_WTS_ONNETEDITOR_INVERTSIDE", out m_invertSide, helperSettings, OnInvertSideChanged);


            AddLibBox<WTSLibOnNetPropLayout, BoardInstanceOnNetXml>(helperSettings, out m_copySettings, OnCopyRule, out m_pasteSettings, OnPasteRule, out _, null, out m_loadDD, out m_libLoad, out m_libDelete, out m_libSaveNameField, out m_libSave, out m_gotoFileLib, OnLoadRule, GetRuleSerialized);


            WTSOnNetLayoutEditor.Instance.LayoutList.EventSelectionChanged += OnChangeTab;
            LoadAvailableLayouts();
            MainContainer.isVisible = false;
            m_pasteSettings.isVisible = false;



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
            });
            Dirty = false;
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
        private void OnInvertSideChanged(bool isChecked) =>  SafeObtain((ref OnNetInstanceCacheContainerXml x) => x.InvertSign = isChecked);

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

    }


}
