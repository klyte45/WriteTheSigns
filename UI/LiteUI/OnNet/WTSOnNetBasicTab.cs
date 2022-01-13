using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.LiteUI;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Xml;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Klyte.WriteTheSigns.UI
{
    public class WTSOnNetBasicTab
    {
        private const string f_base = "K45_WTS_OnNetInstanceCacheContainerXml_";
        private const string f_SaveName = f_base + "SaveName";
        private const string f_ModelSelect = f_base + "ModelSelect";
        private const string f_ModelSelectType = f_base + "ModelSelectType";


        private const string f_InstanceMode = f_base + "InstanceMode";
        private const string f_SegmentPathSingle = f_base + "SegmentPathSingle";
        private const string f_SegmentPathStart = f_base + "SegmentPathStart";
        private const string f_SegmentPathEnd = f_base + "SegmentPathEnd";
        private const string f_SegmentRepeatCount = f_base + "SegmentRepeatCount";


        private const string f_SegmentPositionOffset = f_base + "SegmentPositionOffset";
        private const string f_SegmentRotationOffset = f_base + "SegmentRotationOffset";
        private const string f_SegmentScaleOffset = f_base + "SegmentScaleonOffset";

        private int m_currentModelType;
        private string[] m_modelTypesStr = new string[] { Locale.Get("K45_WTS_ONNETEDITOR_PROPLAYOUT"), Locale.Get("K45_WTS_ONNETEDITOR_PROPMODELSELECT") };
        private string m_lastModelFilterText = null;
        private Vector2 m_resultViewScroll;
        private Vector2 m_tabViewScroll;
        private Coroutine m_searchCoroutine;
        private readonly Wrapper<string[]> m_searchResult = new Wrapper<string[]>();
        private bool m_filterSelectionView;

        private IEnumerator OnFilterLayouts()
        {
            yield return m_currentModelType == 0
                ? WTSPropLayoutData.Instance.FilterBy(m_lastModelFilterText, TextRenderingClass.PlaceOnNet, m_searchResult)
                : PropIndexes.instance.BasicInputFiltering(m_lastModelFilterText, m_searchResult);
        }

        public void Reset(OnNetInstanceCacheContainerXml item)
        {
            m_currentModelType = item?.SimpleProp is null ? 0 : 1;
            m_lastModelFilterText = null;
            m_filterSelectionView = false;
        }
        public void DrawArea(OnNetInstanceCacheContainerXml item, Rect areaRect)
           => GUIKlyteCommons.DoInScroll(ref m_tabViewScroll, () =>
             {
                 areaRect.width -= 20;
                 if (m_filterSelectionView || new[] { f_ModelSelect, f_ModelSelectType }.Contains(GUI.GetNameOfFocusedControl()))
                 {
                     DrawLayoutSelection(item, areaRect);
                 }
                 else
                 {
                     DrawRegularTab(item, areaRect);
                 }
             });


        private void DrawLayoutSelection(OnNetInstanceCacheContainerXml item, Rect areaRect)
        {
            bool dirtyType = false;
            GUIKlyteCommons.DoInHorizontal(() =>
            {
                GUI.SetNextControlName(f_ModelSelectType);
                var modelType = GUILayout.SelectionGrid(m_currentModelType, m_modelTypesStr, m_modelTypesStr.Length);
                dirtyType = m_currentModelType != modelType;
                if (dirtyType)
                {
                    item.PropLayoutName = null;
                    item.SimpleProp = null;
                    m_currentModelType = modelType;
                    GUI.FocusControl(f_ModelSelect);
                }
            }, GUILayout.Width(areaRect.width));

            bool dirtyInput = false;
            GUIKlyteCommons.DoInHorizontal(() =>
             {
                 GUI.SetNextControlName(f_ModelSelect);
                 var newInput = GUILayout.TextField(m_lastModelFilterText);
                 dirtyInput = newInput != m_lastModelFilterText;
                 if (dirtyInput)
                 {
                     m_lastModelFilterText = newInput;
                 }
             }, GUILayout.Width(areaRect.width));

            if (dirtyInput || dirtyType)
            {
                RestartLayoutFilterCoroutine();
            }
            GUIKlyteCommons.DoInScroll(ref m_resultViewScroll, false, true, () =>
            {
                var selectLayout = GUILayout.SelectionGrid(-1, m_searchResult.Value, 1, GUILayout.Width(areaRect.width - 25));
                if (selectLayout >= 0)
                {
                    if (m_currentModelType == 0)
                    {
                        item.PropLayoutName = m_searchResult.Value[selectLayout];
                        item.m_simplePropName = null;
                    }
                    else
                    {
                        item.PropLayoutName = null;
                        PropIndexes.instance.PrefabsLoaded.TryGetValue(m_searchResult.Value[selectLayout], out PropInfo info);
                        item.SimpleProp = info;
                    }
                }

            }, GUILayout.Width(areaRect.width), GUILayout.Height(areaRect.height - 80));

            if (GUILayout.Button("OK", GUILayout.Width(areaRect.width)))
            {
                m_filterSelectionView = false;
                GUI.FocusControl(null);
            }
        }

        private void RestartLayoutFilterCoroutine()
        {
            if (m_searchCoroutine != null)
            {
                WriteTheSignsMod.Controller.StopCoroutine(m_searchCoroutine);
            }
            m_searchCoroutine = WriteTheSignsMod.Controller.StartCoroutine(OnFilterLayouts());
        }

        private void DrawRegularTab(OnNetInstanceCacheContainerXml item, Rect areaRect)
        {
            GUIKlyteCommons.DoInHorizontal(() =>
            {
                GUILayout.Label(Locale.Get("K45_WTS_ONNETEDITOR_NAME"));
                var newName = GUITextField.TextField(f_SaveName, item.SaveName);
                if (!newName.IsNullOrWhiteSpace() && newName != item.SaveName)
                {
                    item.SaveName = newName;
                }
            });

            GUIKlyteCommons.DoInHorizontal(() =>
            {
                GUILayout.Label(Locale.Get("K45_WTS_BUILDINGEDITOR_MODELLAYOUTSELECT"));
                GUI.SetNextControlName(f_ModelSelect);
                if (GUILayout.Button((m_currentModelType != 1 ? item.PropLayoutName : PropIndexes.GetListName(item.SimpleProp)) ?? "<color=#FF00FF>--NULL--</color>"))
                {
                    m_filterSelectionView = true;
                    m_lastModelFilterText = (m_currentModelType != 1 ? item.PropLayoutName : PropIndexes.GetListName(item.SimpleProp)) ?? "";
                    m_searchResult.Value = null;
                    RestartLayoutFilterCoroutine();
                }
            });

            GUIKlyteCommons.DoInHorizontal(() =>
            {
                GUI.SetNextControlName(f_InstanceMode);
                item.SegmentPositionRepeating = GUILayout.Toggle(item.SegmentPositionRepeating, Locale.Get("K45_WTS_POSITIONINGMODE_ISMULTIPLE"));
            });

            if (item.SegmentPositionRepeating)
            {
                GUIKlyteCommons.DoInHorizontal(() =>
                {
                    GUILayout.Label(Locale.Get("K45_WTS_ONNETEDITOR_SEGMENTPOSITION_START"));
                    GUILayout.Space(areaRect.width / 3);
                    var rect = GUILayoutUtility.GetLastRect();
                    item.SegmentPositionStart = GUI.HorizontalSlider(new Rect(rect.x, rect.yMin + 7, rect.width, 15), item.SegmentPositionStart, 0, 1);
                    item.SegmentPositionStart = GUIFloatField.FloatField(f_SegmentPathStart, item.SegmentPositionStart, 0, 1);
                });
                GUIKlyteCommons.DoInHorizontal(() =>
                {
                    GUILayout.Label(Locale.Get("K45_WTS_ONNETEDITOR_SEGMENTPOSITION_END"));
                    GUILayout.Space(areaRect.width / 3);
                    var rect = GUILayoutUtility.GetLastRect();
                    item.SegmentPositionEnd = GUI.HorizontalSlider(new Rect(rect.x, rect.yMin + 7, rect.width, 15), item.SegmentPositionEnd, 0, 1);
                    item.SegmentPositionEnd = GUIFloatField.FloatField(f_SegmentPathEnd, item.SegmentPositionEnd, 0, 1);
                });
                GUIKlyteCommons.DoInHorizontal(() =>
                {
                    GUILayout.Label(Locale.Get("K45_WTS_ONNETEDITOR_SEGMENTPOSITION_COUNT"));
                    item.SegmentPositionRepeatCount = (ushort)GUIIntField.IntField(f_SegmentRepeatCount, item.SegmentPositionRepeatCount, 1, ushort.MaxValue);
                });
            }
            else
            {
                GUIKlyteCommons.DoInHorizontal(() =>
                {
                    GUILayout.Label(Locale.Get("K45_WTS_ONNETEDITOR_SEGMENTPOSITION"));
                    GUILayout.Space(areaRect.width / 3);
                    var rect = GUILayoutUtility.GetLastRect();
                    item.SegmentPosition = GUI.HorizontalSlider(new Rect(rect.x, rect.yMin + 7, rect.width, 15), item.SegmentPosition, 0, 1);
                    item.SegmentPosition = GUIFloatField.FloatField(f_SegmentPathSingle, item.SegmentPosition, 0, 1);
                });
            }
            GUILayout.Space(12);

            GUIKlyteCommons.DoInHorizontal(() => GUILayout.Label(Locale.Get("K45_WTS_ONNETEDITOR_LOCATION_SETTINGS")));

            GUIKlyteCommons.AddVector3Field(item.PropPosition, "K45_WTS_ONNETEDITOR_POSITIONOFFSET", f_SegmentPositionOffset, areaRect.width);
            GUIKlyteCommons.AddVector3Field(item.PropRotation, "K45_WTS_ONNETEDITOR_ROTATION", f_SegmentRotationOffset, areaRect.width);
            GUIKlyteCommons.AddVector3Field(item.Scale, "K45_WTS_ONNETEDITOR_SCALE", f_SegmentScaleOffset, areaRect.width);
        }

        internal bool ShowTabsOnTop() => m_currentModelType == 0 && !m_filterSelectionView;
    }
}
