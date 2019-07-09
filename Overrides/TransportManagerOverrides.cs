using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using System;
using System.Reflection;
using UnityEngine;

namespace Klyte.DynamicTextBoards.Overrides
{
    public class TransportManagerOverrides : MonoBehaviour, IRedirectable
    {
        public Redirector RedirectorInstance { get; private set; }


        #region Events
        public static event Action<ushort> eventOnLineUpdated;

        private static void RunOnLineUpdated(ushort lineID)
        {
            var lineID_ = lineID;
            new AsyncAction(() =>
            {
                eventOnLineUpdated?.Invoke(lineID_);
            }).Execute();
        }
        #endregion

        #region Hooking

        public  void Awake()
        {
            RedirectorInstance = KlyteMonoUtils.CreateElement<Redirector>(transform);
            LogUtils.DoLog("Loading Transport Manager Overrides");
            #region Release Line Hooks
            MethodInfo posUpdate = typeof(TransportManagerOverrides).GetMethod("RunOnLineUpdated",RedirectorUtils. allFlags);

            RedirectorInstance.AddRedirect(typeof(TransportManager).GetMethod("UpdateLine", RedirectorUtils.allFlags), null, posUpdate);
            #endregion


        }
        #endregion



    }
}
