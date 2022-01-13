using ColossalFramework.Globalization;
using Klyte.Commons.LiteUI;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Xml;
using UnityEngine;

namespace Klyte.WriteTheSigns.UI
{
    public class WTSOnNetTargetsTab
    {
        private const string f_base = "K45_WTS_OnNetInstanceCacheContainerXml_";
        private const string f_Targets = f_base + "Target";

        private Vector2 m_tabViewScroll;

        public void Reset() => CurrentSegmentInSelect = -1;
        public void DrawArea(OnNetInstanceCacheContainerXml item, Rect areaRect)
           => GUIKlyteCommons.DoInScroll(ref m_tabViewScroll, () =>
             {
                 areaRect.width -= 20;
                 DrawTargetSegmentSelectionList(item, areaRect);

             });


        private void DrawTargetSegmentSelectionList(OnNetInstanceCacheContainerXml item, Rect areaRect)
        {
            for (int i = 1; i <= 4; i++)
            {
                GUIKlyteCommons.DoInHorizontal(() =>
                {
                    GUILayout.Label(string.Format(Locale.Get("K45_WTS_ONNETEDITOR_TARGET"), i), GUILayout.Width(areaRect.width / 4));
                    GUI.SetNextControlName(f_Targets + i);
                    if (GUILayout.Button(GetSegmentName(item, i)))
                    {
                        OnEnterPickTarget(item, i);
                    }
                }, GUILayout.Width(areaRect.width));
            }
        }

        private string GetSegmentName(OnNetInstanceCacheContainerXml item, int i)
        {
            if (WriteTheSignsMod.Controller.RoadSegmentToolInstance.enabled && CurrentSegmentInSelect == i)
            {
                return Locale.Get("ANIMAL_STATUS_WAITING");
            }
            var targSeg = item.GetTargetSegment(i);
            if (cachedSegmentNames[i - 1] is null || targSeg != cachedSegmentNames[i - 1].First)
            {
                if (targSeg == 0)
                {
                    cachedSegmentNames[i - 1] = Tuple.New(targSeg, Locale.Get("K45_WTS_ONNETEDITOR_UNSETTARGETDESC"));
                }
                else
                {
                    var pos = NetManager.instance.m_segments.m_buffer[targSeg].m_middlePosition;
                    WriteTheSignsMod.Controller.ConnectorADR.GetAddressStreetAndNumber(pos, pos, out int num, out string streetName);
                    cachedSegmentNames[i - 1] = Tuple.New(targSeg, $"{((streetName?.Length ?? 0) == 0 ? NetManager.instance.m_segments.m_buffer[targSeg].Info.GetLocalizedTitle() : streetName)}, ~{num}m");
                }
            }
            return cachedSegmentNames[i - 1].Second;
        }

        private void OnEnterPickTarget(OnNetInstanceCacheContainerXml item, int idx)
        {
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.enabled = false;
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.OnSelectSegment += (k) => item.SetTargetSegment(idx, k);
            CurrentSegmentInSelect = idx;
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.enabled = true;
        }

        private int CurrentSegmentInSelect = -1;
        private Tuple<ushort, string>[] cachedSegmentNames = new Tuple<ushort, string>[4];
    }
}
