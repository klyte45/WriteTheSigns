using ColossalFramework;
using Harmony;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Klyte.WriteTheSigns.Overrides
{
    public class BuildingManagerOverrides : MonoBehaviour, IRedirectable
    {
        public Redirector RedirectorInstance { get; private set; }

        #region Hooking
        public void Awake()
        {
            RedirectorInstance = KlyteMonoUtils.CreateElement<Redirector>(transform);
            LogUtils.DoLog("Loading Building Manager Overrides");
            MethodInfo posRename = typeof(WTSController).GetMethod("OnBuildingNameChanged", RedirectorUtils.allFlags);
            MethodInfo calcGroup = typeof(BuildingManager).GetMethod("CalculateGroupData");
            MethodInfo popGroup = typeof(BuildingManager).GetMethod("PopulateGroupData");


            System.Reflection.MethodInfo postRenderMeshs = GetType().GetMethod("TranspileEndRenderingImpl", RedirectorUtils.allFlags);
            var orMeth = typeof(BuildingManager).GetMethod("EndRenderingImpl", RedirectorUtils.allFlags & ~BindingFlags.Public);
            MethodInfo AfterCalculateGroupData = GetType().GetMethod("AfterCalculateGroupData", RedirectorUtils.allFlags);
            MethodInfo AfterPopulateGroupData = GetType().GetMethod("AfterPopulateGroupData", RedirectorUtils.allFlags);

            LogUtils.DoLog($"Patching=> {posRename}");
            RedirectorInstance.AddRedirect(BuildingManager.instance.SetBuildingName(0, "").GetType().GetMethod("MoveNext", RedirectorUtils.allFlags), null, posRename);
            LogUtils.DoLog($"Patching=> {AfterCalculateGroupData}");
            RedirectorInstance.AddRedirect(calcGroup, null, AfterCalculateGroupData);
            LogUtils.DoLog($"Patching=> {AfterPopulateGroupData}");
            RedirectorInstance.AddRedirect(popGroup, null, AfterPopulateGroupData);
            LogUtils.DoLog($"Patching=> {postRenderMeshs}");
            RedirectorInstance.AddRedirect(orMeth, null, null, postRenderMeshs);
        }
        #endregion


        public static void AfterCalculateGroupData(BuildingManager __instance, int groupX, int groupZ, int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays, ref bool __result)
        {
            if (WriteTheSignsMod.Controller?.BuildingPropsSingleton == null)
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
                    ushort num6 = __instance.m_buildingGrid[num5];
                    int num7 = 0;
                    while (num6 != 0)
                    {
                        if (WriteTheSignsMod.Controller.BuildingPropsSingleton.CalculateGroupData(num6, layer, ref vertexCount, ref triangleCount, ref objectCount, ref vertexArrays))
                        {
                            __result = true;
                        }
                        num6 = __instance.m_buildings.m_buffer[num6].m_nextGridBuilding;
                        if (++num7 >= 49152)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
        }

        public static void AfterPopulateGroupData(BuildingManager __instance, int groupX, int groupZ, int layer, ref int vertexIndex, ref int triangleIndex, ref Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance)
        {
            if (WriteTheSignsMod.Controller?.BuildingPropsSingleton == null)
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
                    ushort num6 = __instance.m_buildingGrid[num5];
                    int num7 = 0;
                    while (num6 != 0)
                    {
                        WriteTheSignsMod.Controller.BuildingPropsSingleton.PopulateGroupData(num6, layer, ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
                        num6 = __instance.m_buildings.m_buffer[num6].m_nextGridBuilding;
                        if (++num7 >= 49152)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
        }

        public static IEnumerable<CodeInstruction> TranspileEndRenderingImpl(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            MethodInfo postRenderMeshs = typeof(BuildingManagerOverrides).GetMethod("AfterRenderMeshes", RedirectorUtils.allFlags);
            for (int i = 1; i < instrList.Count; i++)
            {
                if (instrList[i - 1].opcode == OpCodes.Call && instrList[i - 1].operand is MethodInfo mi && mi.Name == "RenderInstance" && mi.DeclaringType == typeof(Building))
                {
                    instrList.InsertRange(i, new List<CodeInstruction> {
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldloc_S,11),
                        new CodeInstruction(OpCodes.Call,postRenderMeshs),
                    });
                    break;
                }
            }

            LogUtils.PrintMethodIL(instrList);
            return instrList;
        }

        public static void AfterRenderMeshes(RenderManager.CameraInfo cameraInfo, ushort buildingID)
        {
            if (WriteTheSignsMod.Controller?.BuildingPropsSingleton == null)
            {
                return;
            }

            ref Building[] buildings = ref BuildingManager.instance.m_buildings.m_buffer;
            RenderManager renderManager = RenderManager.instance;
            ref Building building = ref buildings[buildingID];
            BuildingInfo info = building.Info;
            if ((building.m_flags & Building.Flags.Created) == 0 || info.m_mesh == null)
            {
                return;
            }
            Vector3 position = building.m_position;
            float radius = info.m_renderSize + info.m_mesh.bounds.extents.magnitude;
            position.y += (info.m_size.y - building.m_baseHeight) * 0.5f;
            var shallRender = cameraInfo.Intersect(position, radius);
            if (!shallRender && ((!(info.m_buildingAI is TransportStationAI) && !(info.m_buildingAI is OutsideConnectionAI)) || building.m_parentBuilding != 0))
            {
                return;
            }
            if (renderManager.RequireInstance(buildingID, 1u, out uint num))
            {
                ref RenderManager.Instance renderInstance = ref renderManager.m_instances[num];
                if (renderInstance.m_dirty)
                {
                    renderInstance.m_dirty = false;
                    info.m_buildingAI.RefreshInstance(cameraInfo, buildingID, ref building, -1, ref renderInstance);
                }
                if (!shallRender)
                {
                    WriteTheSignsMod.Controller?.BuildingPropsSingleton?.UpdateLinesBuilding(buildingID, ref building, ref renderInstance.m_dataMatrix1);
                }
                else
                {
                    WriteTheSignsMod.Controller?.BuildingPropsSingleton?.AfterRenderInstanceImpl(cameraInfo, buildingID, -1, ref renderInstance);
                }

            }


        }
    }
}
