﻿using Klyte.Commons.Extensions;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Klyte.WriteTheSigns.Overrides
{
    public class TransportManagerOverrides : MonoBehaviour, IRedirectable
    {
        public Redirector RedirectorInstance { get; private set; }


        #region Events
        public static event Action<ushort> EventOnLineUpdated;
        public static event Action<ushort> EventOnLineBuildingUpdated;
        private static readonly Stack<ushort> m_lineStack = new Stack<ushort>();
        private static readonly Stack<ushort> m_buildingStack = new Stack<ushort>();


        public static void PushIntoStackBuilding(bool __result, Vector3 position)
        {
            if (__result)
            {
                ushort buildingId = BuildingManager.instance.FindBuilding(position, 100f, ItemClass.Service.None, ItemClass.SubService.None, Building.Flags.None, Building.Flags.None);
                if (!m_buildingStack.Contains(buildingId))
                {
                    m_buildingStack.Push(buildingId);
                }
                m_cooldown = 2;
            }
        }
        public static void BeforeRemoveStop(ref TransportLine __instance, int index, ushort lineID)
        {
            if ((__instance.m_flags & TransportLine.Flags.Temporary) != TransportLine.Flags.None || __instance.m_stops > NetManager.MAX_NODE_COUNT)
            {
                return;
            }
            ushort num;
            if (index == -1)
            {
                index += __instance.CountStops(lineID);
            }
            num = __instance.m_stops;
            for (int i = 0; i < index && num <= NetManager.MAX_NODE_COUNT; i++)
            {
                num = TransportLine.GetNextStop(num);
                if (num == __instance.m_stops)
                {
                    break;
                }
            }
            WTSBuildingDataCaches.PurgeStopCache(num);
        }
        public static void AfterRemoveLine(ushort lineID) => WTSBuildingDataCaches.PurgeLineCache(lineID);

        public static void PushIntoStackLine(ushort lineID)
        {
            if (!m_lineStack.Contains(lineID))
            {
                m_lineStack.Push(lineID);
            }

            m_cooldown = 2;
        }

        private static uint m_cooldown = 0;
        public void Update()
        {
            if (m_cooldown == 1)
            {
                bool shouldDecrement = true;
                if (m_lineStack.Count > 0)
                {
                    EventOnLineUpdated?.Invoke(m_lineStack.Pop());
                    shouldDecrement = false;
                }
                if (m_buildingStack.Count > 0)
                {
                    EventOnLineBuildingUpdated?.Invoke(m_buildingStack.Pop());
                    shouldDecrement = false;
                }
                if (shouldDecrement)
                {
                    m_cooldown = 0;
                }
            }
            else if (m_cooldown > 0)
            {
                m_cooldown--;
            }
        }

        #endregion

        #region Hooking

        public void Awake()
        {
            RedirectorInstance = KlyteMonoUtils.CreateElement<Redirector>(transform);
            LogUtils.DoLog("Loading Transport Manager Overrides");
            #region Release Line Hooks
            MethodInfo posUpdate = typeof(TransportManagerOverrides).GetMethod("PushIntoStackLine", RedirectorUtils.allFlags);
            MethodInfo posAddStop = typeof(TransportManagerOverrides).GetMethod("PushIntoStackBuilding", RedirectorUtils.allFlags);
            MethodInfo preRemoveStop = typeof(TransportManagerOverrides).GetMethod("BeforeRemoveStop", RedirectorUtils.allFlags);
            MethodInfo posRemoveLine = typeof(TransportManagerOverrides).GetMethod("AfterRemoveLine", RedirectorUtils.allFlags);

            RedirectorInstance.AddRedirect(typeof(TransportManager).GetMethod("UpdateLine", RedirectorUtils.allFlags), null, posUpdate);
            RedirectorInstance.AddRedirect(typeof(TransportLine).GetMethod("AddStop", RedirectorUtils.allFlags), null, posAddStop);
            RedirectorInstance.AddRedirect(typeof(TransportLine).GetMethod("RemoveStop", RedirectorUtils.allFlags, null, new Type[] { typeof(ushort), typeof(int), typeof(Vector3).MakeByRefType() }, null), preRemoveStop);
            RedirectorInstance.AddRedirect(typeof(TransportManager).GetMethod("ReleaseLine", RedirectorUtils.allFlags), null, posRemoveLine);
            #endregion


        }
        #endregion



    }
}
