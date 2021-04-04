using ColossalFramework;
using Klyte.Commons.Extensions;
using Klyte.Commons.Utils;
using System;
using UnityEngine;

namespace Klyte.WriteTheSigns.Overrides
{
    public class RoadNodeOverrides : Redirector, IRedirectable
    {
        public void Awake()
        {
            #region Hooks
            System.Reflection.MethodInfo postRenderMeshs = GetType().GetMethod("AfterRenderSegment", RedirectorUtils.allFlags);
            System.Reflection.MethodInfo orig = typeof(NetSegment).GetMethod("RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int) });
            System.Reflection.MethodInfo calcGroup = typeof(NetManager).GetMethod("CalculateGroupData");
            System.Reflection.MethodInfo popGroup = typeof(NetManager).GetMethod("PopulateGroupData");

            System.Reflection.MethodInfo AfterCalculateGroupData = GetType().GetMethod("AfterCalculateGroupData", RedirectorUtils.allFlags);
            System.Reflection.MethodInfo AfterPopulateGroupData = GetType().GetMethod("AfterPopulateGroupData", RedirectorUtils.allFlags);

            LogUtils.DoLog($"Patching: {orig} => {postRenderMeshs} ");
            AddRedirect(orig, null, postRenderMeshs);
            LogUtils.DoErrorLog($"PatchingC: {calcGroup} => {AfterCalculateGroupData} ");
            AddRedirect(calcGroup, null, AfterCalculateGroupData);
            LogUtils.DoErrorLog($"PatchingP: {popGroup} => {AfterPopulateGroupData} ");
            AddRedirect(popGroup, null, AfterPopulateGroupData);
            #endregion
        }


        public static void AfterRenderSegment(RenderManager.CameraInfo cameraInfo, ushort segmentID)
        {
            ref NetSegment data = ref NetManager.instance.m_segments.m_buffer[segmentID];
            WriteTheSignsMod.Controller?.RoadPropsSingleton?.AfterRenderInstanceImpl(cameraInfo, data.m_startNode, ref NetManager.instance.m_nodes.m_buffer[data.m_startNode]);
            WriteTheSignsMod.Controller?.RoadPropsSingleton?.AfterRenderInstanceImpl(cameraInfo, data.m_endNode, ref NetManager.instance.m_nodes.m_buffer[data.m_endNode]);
            WriteTheSignsMod.Controller?.OnNetPropsSingleton?.AfterRenderInstanceImpl(cameraInfo, segmentID, ref data);
        }


        public static void AfterCalculateGroupData(ref NetManager __instance, int groupX, int groupZ, int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays, ref bool __result)
        {
            if (WriteTheSignsMod.Controller?.RoadPropsSingleton == null)
            {
                return;
            }

            int num = groupX * 270 / 45;
            int num2 = groupZ * 270 / 45;
            int num3 = (groupX + 1) * 270 / 45 - 1;
            int num4 = (groupZ + 1) * 270 / 45 - 1;
            for (int i = num2; i <= num4; i++)
            {
                for (int j = num; j <= num3; j++)
                {
                    int num5 = i * 270 + j;
                    ushort num6 = __instance.m_nodeGrid[num5];
                    int num7 = 0;
                    while (num6 != 0)
                    {
                        if (WriteTheSignsMod.Controller.RoadPropsSingleton.CalculateGroupData(num6, layer, ref vertexCount, ref triangleCount, ref objectCount, ref vertexArrays))
                        {
                            __result = true;
                        }
                        num6 = __instance.m_nodes.m_buffer[num6].m_nextGridNode;
                        if (++num7 >= 32768)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            for (int k = num2; k <= num4; k++)
            {
                for (int l = num; l <= num3; l++)
                {
                    int num8 = k * 270 + l;
                    ushort num9 = __instance.m_segmentGrid[num8];
                    int num10 = 0;
                    while (num9 != 0)
                    {
                        WriteTheSignsMod.Controller?.OnNetPropsSingleton?.CalculateGroupData(num9, layer, ref vertexCount, ref triangleCount, ref objectCount, ref vertexArrays);
                        num9 = __instance.m_segments.m_buffer[num9].m_nextGridSegment;
                        if (++num10 >= 36864)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
        }

        public static void AfterPopulateGroupData(ref NetManager __instance, int groupX, int groupZ, int layer, ref int vertexIndex, ref int triangleIndex, ref Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance, ref bool requireSurfaceMaps)
        {
            int num = groupX * 270 / 45;
            int num2 = groupZ * 270 / 45;
            int num3 = (groupX + 1) * 270 / 45 - 1;
            int num4 = (groupZ + 1) * 270 / 45 - 1;
            for (int i = num2; i <= num4; i++)
            {
                for (int j = num; j <= num3; j++)
                {
                    int num5 = i * 270 + j;
                    ushort num6 = __instance.m_nodeGrid[num5];
                    int num7 = 0;
                    while (num6 != 0)
                    {

                        WriteTheSignsMod.Controller?.RoadPropsSingleton?.PopulateGroupData(num6, layer, ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
                        num6 = __instance.m_nodes.m_buffer[num6].m_nextGridNode;
                        if (++num7 >= 32768)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            for (int k = num2; k <= num4; k++)
            {
                for (int l = num; l <= num3; l++)
                {
                    int num8 = k * 270 + l;
                    ushort num9 = __instance.m_segmentGrid[num8];
                    int num10 = 0;
                    while (num9 != 0)
                    {
                        WriteTheSignsMod.Controller?.OnNetPropsSingleton?.PopulateGroupData(num9, layer, ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
                        num9 = __instance.m_segments.m_buffer[num9].m_nextGridSegment;
                        if (++num10 >= 36864)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
        }

        //public static void AfterCalculateGroupData(ref ushort segmentID, ref int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays)
        //{
        //    LogUtils.DoLog("CALCULATE!");
        //    ref NetSegment data = ref NetManager.instance.m_segments.m_buffer[segmentID];
        //    WriteTheSignsMod.Controller?.RoadPropsSingleton?.CalculateGroupData(data.m_startNode, layer, ref vertexCount, ref triangleCount, ref objectCount, ref vertexArrays);
        //    WriteTheSignsMod.Controller?.RoadPropsSingleton?.CalculateGroupData(data.m_endNode, layer, ref vertexCount, ref triangleCount, ref objectCount, ref vertexArrays);
        //    WriteTheSignsMod.Controller?.OnNetPropsSingleton?.CalculateGroupData(segmentID, layer, ref vertexCount, ref triangleCount, ref objectCount, ref vertexArrays);
        //}
        //public static void AfterPopulateGroupData(ref ushort segmentID, ref int layer, ref int vertexIndex, ref int triangleIndex, ref Vector3 groupPosition, ref RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance)
        //{
        //    LogUtils.DoLog("POPULATE!");
        //    ref NetSegment netData = ref NetManager.instance.m_segments.m_buffer[segmentID];
        //    WriteTheSignsMod.Controller?.RoadPropsSingleton?.PopulateGroupData(netData.m_startNode, layer, ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
        //    WriteTheSignsMod.Controller?.RoadPropsSingleton?.PopulateGroupData(netData.m_endNode, layer, ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
        //    WriteTheSignsMod.Controller?.OnNetPropsSingleton?.PopulateGroupData(segmentID, layer, ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
        //}

    }
}
