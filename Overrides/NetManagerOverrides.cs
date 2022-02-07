using ColossalFramework.Math;
using Harmony;
using Klyte.Commons.Extensions;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Klyte.WriteTheSigns.Overrides
{
    public class NetManagerOverrides : MonoBehaviour, IRedirectable
    {
        public Redirector RedirectorInstance { get; set; }


        #region Events
        public static event Action<ushort> EventNodeChanged;
        public static event Action<ushort> EventSegmentChanged;
        public static event Action<ushort> EventSegmentReleased;
        public static event Action<ushort> EventSegmentNameChanged;

#pragma warning disable IDE0051 // Remover membros privados não utilizados
        private static void OnNodeChanged(ref ushort node)
        {
            ushort node_ = node;
            SimulationManager.instance.AddAction(() => EventNodeChanged?.Invoke(node_)).Execute();

            WTSBuildingDataCaches.PurgeStopCache(node);
        }
        private static void OnSegmentCreated(ref ushort segment, ref ushort startNode, ref ushort endNode)
        {
            ushort startNode_ = startNode;
            ushort segment_ = segment;
            ushort endNode_ = endNode;

            SimulationManager.instance.AddAction(() =>
            {
                EventNodeChanged?.Invoke(startNode_);
                EventNodeChanged?.Invoke(endNode_);
                EventSegmentChanged?.Invoke(segment_);
            }).Execute();
        }
        private static void OnSegmentReleased(ref ushort segment)
        {
            ushort segment_ = segment;
            SimulationManager.instance.AddAction(() =>
            {
                EventNodeChanged?.Invoke(NetManager.instance.m_segments.m_buffer[segment_].m_startNode);
                EventNodeChanged?.Invoke(NetManager.instance.m_segments.m_buffer[segment_].m_endNode);
                EventSegmentChanged?.Invoke(segment_);
                EventSegmentReleased?.Invoke(segment_);
            }).Execute();
        }
        private static void OnSegmentNameChanged(ref ushort segmentID)
        {
            ushort segment_ = segmentID;
            SimulationManager.instance.AddAction(() => EventSegmentNameChanged?.Invoke(segment_)).Execute();
        }



        public static IEnumerable<CodeInstruction> AfterTerrainUpdateTranspile(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            MethodInfo TerrainUpdateNetNode = typeof(NetManagerOverrides).GetMethod("TerrainUpdateNetNode", RedirectorUtils.allFlags);
            MethodInfo TerrainUpdateNetSegment = typeof(NetManagerOverrides).GetMethod("TerrainUpdateNetSegment", RedirectorUtils.allFlags);
            int i = 2;
            for (; i < instrList.Count; i++)
            {
                if (instrList[i - 2].opcode == OpCodes.Ldloc_S && instrList[i - 2].operand is LocalBuilder lb && lb.LocalIndex == 13
                    && instrList[i - 1].opcode == OpCodes.Ldc_R4 && instrList[i - 1].operand is float k && k == 0)
                {
                    instrList.InsertRange(i + 1, new List<CodeInstruction> {
                        new CodeInstruction(OpCodes.Ldloc_S, 10),
                        new CodeInstruction(OpCodes.Call, TerrainUpdateNetNode),
                    });
                    break;
                }
            }
            for (; i < instrList.Count; i++)
            {
                if (instrList[i - 2].opcode == OpCodes.Ldloc_S && instrList[i - 2].operand is LocalBuilder lb && lb.LocalIndex == 23
                    && instrList[i - 1].opcode == OpCodes.Ldc_R4 && instrList[i - 1].operand is float k && k == 0)
                {
                    instrList.InsertRange(i + 1, new List<CodeInstruction> {
                        new CodeInstruction(OpCodes.Ldloc_S, 16),
                        new CodeInstruction(OpCodes.Call, TerrainUpdateNetSegment),
                    });
                    break;
                }
            }

            LogUtils.PrintMethodIL(instrList);
            return instrList;
        }

        public static void TerrainUpdateNetNode(ushort netNode)
        {
            if (LoadingManager.instance.m_loadingComplete)
            {
                WriteTheSignsMod.Controller?.RoadPropsSingleton?.OnNodeChanged(netNode);
            }
        }

        public static void TerrainUpdateNetSegment(ushort segmentId)
        {
            if (LoadingManager.instance.m_loadingComplete)
            {
                WTSOnNetData.Instance.OnSegmentChanged(segmentId);
            }
        }

        #endregion
#pragma warning restore IDE0051 // Remover membros privados não utilizados

        #region Hooking

        public void Awake()
        {
            LogUtils.DoLog("Loading Net Manager Overrides");
            RedirectorInstance = KlyteMonoUtils.CreateElement<Redirector>(transform);
            #region Net Manager Hooks
            MethodInfo OnNodeChanged = GetType().GetMethod("OnNodeChanged", RedirectorUtils.allFlags);
            MethodInfo OnSegmentCreated = GetType().GetMethod("OnSegmentCreated", RedirectorUtils.allFlags);
            MethodInfo OnSegmentReleased = GetType().GetMethod("OnSegmentReleased", RedirectorUtils.allFlags);
            MethodInfo OnSegmentNameChanged = GetType().GetMethod("OnSegmentNameChanged", RedirectorUtils.allFlags);
            MethodInfo AfterTerrainUpdateTranspile = GetType().GetMethod("AfterTerrainUpdateTranspile", RedirectorUtils.allFlags);

            RedirectorInstance.AddRedirect(typeof(NetManager).GetMethod("CreateNode", RedirectorUtils.allFlags), null, OnNodeChanged);
            RedirectorInstance.AddRedirect(typeof(NetManager).GetMethod("ReleaseNode", RedirectorUtils.allFlags), null, OnNodeChanged);
            RedirectorInstance.AddRedirect(typeof(NetManager).GetMethod("CreateSegment", RedirectorUtils.allFlags, null, new[] { typeof(ushort).MakeByRefType(), typeof(Randomizer).MakeByRefType(), typeof(NetInfo), typeof(TreeInfo), typeof(ushort), typeof(ushort), typeof(Vector3), typeof(Vector3), typeof(uint), typeof(uint), typeof(bool) }, null), null, OnSegmentCreated);
            RedirectorInstance.AddRedirect(typeof(NetManager).GetMethod("ReleaseSegment", RedirectorUtils.allFlags), OnSegmentReleased);
            RedirectorInstance.AddRedirect(typeof(NetManager).GetMethod("SetSegmentNameImpl", RedirectorUtils.allFlags), null, OnSegmentNameChanged);
            RedirectorInstance.AddRedirect(typeof(NetManager).GetMethod("AfterTerrainUpdate", RedirectorUtils.allFlags), null, null, AfterTerrainUpdateTranspile);
            #endregion

        }
        #endregion


    }
}
