using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Singleton;
using System;

namespace Klyte.WriteTheSigns.Overrides
{
    public class RoadNodeOverrides : Redirector, IRedirectable
    {
        public void Awake()
        {
            var adrEventsType = Type.GetType("Klyte.Addresses.ModShared.AdrEvents, KlyteAddresses");
            if (adrEventsType != null)
            {
                static void RegisterEvent(string eventName, Type adrEventsType, Action action) => adrEventsType.GetEvent(eventName)?.AddEventHandler(null, action);
                RegisterEvent("EventZeroMarkerBuildingChange", adrEventsType, new Action(WTSRoadPropsSingleton.OnZeroMarkChanged));
                RegisterEvent("EventRoadNamingChange", adrEventsType, new Action(WTSRoadPropsSingleton.OnZeroMarkChanged));
                RegisterEvent("EventDistrictColorChanged", adrEventsType, new Action(WTSRoadPropsSingleton.OnDistrictChanged));
                RegisterEvent("EventBuildingNameStrategyChanged", adrEventsType, new Action(WTSRoadPropsSingleton.OnZeroMarkChanged));
            }

            #region Hooks
            System.Reflection.MethodInfo postRenderMeshs = GetType().GetMethod("AfterRenderSegment", RedirectorUtils.allFlags);
            System.Reflection.MethodInfo orig = typeof(NetSegment).GetMethod("RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int) });
            LogUtils.DoLog($"Patching: {orig} => {postRenderMeshs} {postRenderMeshs.IsStatic}");
            AddRedirect(orig, null, postRenderMeshs);
            #endregion
        }


        public static void AfterRenderSegment(RenderManager.CameraInfo cameraInfo, ushort segmentID)
        {
            WTSRoadPropsSingleton.instance.AfterRenderInstanceImpl(cameraInfo, NetManager.instance.m_segments.m_buffer[segmentID].m_startNode, ref NetManager.instance.m_nodes.m_buffer[NetManager.instance.m_segments.m_buffer[segmentID].m_startNode]);
            WTSRoadPropsSingleton.instance.AfterRenderInstanceImpl(cameraInfo, NetManager.instance.m_segments.m_buffer[segmentID].m_endNode, ref NetManager.instance.m_nodes.m_buffer[NetManager.instance.m_segments.m_buffer[segmentID].m_endNode]);
        }
    }
}
