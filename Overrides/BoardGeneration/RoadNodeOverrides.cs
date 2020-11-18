using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using System;

namespace Klyte.WriteTheSigns.Overrides
{
    public class RoadNodeOverrides : Redirector, IRedirectable
    {
        public void Awake()
        {
            #region Hooks
            System.Reflection.MethodInfo postRenderMeshs = GetType().GetMethod("AfterRenderSegment", RedirectorUtils.allFlags);
            System.Reflection.MethodInfo orig = typeof(NetSegment).GetMethod("RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int) });
            LogUtils.DoLog($"Patching: {orig} => {postRenderMeshs} {postRenderMeshs.IsStatic}");
            AddRedirect(orig, null, postRenderMeshs);
            #endregion
        }


        public static void AfterRenderSegment(RenderManager.CameraInfo cameraInfo, ushort segmentID)
        {
            ref NetSegment data = ref NetManager.instance.m_segments.m_buffer[segmentID];
            WriteTheSignsMod.Controller?.RoadPropsSingleton?.AfterRenderInstanceImpl(cameraInfo, data.m_startNode, ref NetManager.instance.m_nodes.m_buffer[data.m_startNode]);
            WriteTheSignsMod.Controller?.RoadPropsSingleton?.AfterRenderInstanceImpl(cameraInfo, data.m_endNode, ref NetManager.instance.m_nodes.m_buffer[data.m_endNode]);
            WriteTheSignsMod.Controller?.OnNetPropsSingleton?.AfterRenderInstanceImpl(cameraInfo, segmentID, ref data);
        }
    }
}
