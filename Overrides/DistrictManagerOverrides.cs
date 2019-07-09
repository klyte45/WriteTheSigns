using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Klyte.DynamicTextBoards.Overrides
{
    public class DistrictManagerOverrides : MonoBehaviour, IRedirectable
    {

        public Redirector RedirectorInstance { get; private set; }

        #region Events
        public static event Action eventOnDistrictChanged;
        private static int m_cooldown;


        public static void OnDistrictChanged()
        {
            m_cooldown = 15;
        }

        private void Update()
        {
            if (m_cooldown == 1)
            {

                eventOnDistrictChanged?.Invoke();

            }
            if (m_cooldown > 0)
            {
                m_cooldown--;
            }
        }

        #endregion



        #region Hooking

        public void Awake()
        {
            RedirectorInstance = KlyteMonoUtils.CreateElement<Redirector>(transform);
            LogUtils.DoLog("Loading District Manager Overrides");
            #region Release Line Hooks
            MethodInfo posChange = typeof(DistrictManagerOverrides).GetMethod("OnDistrictChanged", RedirectorUtils.allFlags);

            RedirectorInstance.AddRedirect(typeof(DistrictManager).GetMethod("SetDistrictName", RedirectorUtils.allFlags), null, posChange);
            RedirectorInstance.AddRedirect(typeof(DistrictManager).GetMethod("AreaModified", RedirectorUtils.allFlags), null, posChange);
            #endregion


        }
        #endregion



    }
}
