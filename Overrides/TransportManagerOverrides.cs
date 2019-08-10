using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
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
                var buildingId = BuildingManager.instance.FindBuilding(position, 100f, ItemClass.Service.None, ItemClass.SubService.None, Building.Flags.None, Building.Flags.None);
                if (!m_buildingStack.Contains(buildingId))
                {
                    m_buildingStack.Push(buildingId);
                }
                m_cooldown = 2;
            }
        }

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
                var shouldDecrement = true;
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

            RedirectorInstance.AddRedirect(typeof(TransportManager).GetMethod("UpdateLine", RedirectorUtils.allFlags), null, posUpdate);
            RedirectorInstance.AddRedirect(typeof(TransportLine).GetMethod("AddStop", RedirectorUtils.allFlags), null, posAddStop);
            #endregion


        }
        #endregion



    }
}
