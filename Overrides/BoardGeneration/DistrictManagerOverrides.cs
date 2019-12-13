using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using System;
using System.Reflection;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{
    public class DistrictManagerOverrides : MonoBehaviour, IRedirectable
    {

        public Redirector RedirectorInstance { get; private set; }

        #region Events
        public static event Action EventOnDistrictChanged;
        private static int m_cooldown;


        public static void OnDistrictChanged() => m_cooldown = 15;

        public void Update()
        {
            if (m_cooldown == 1)
            {

                EventOnDistrictChanged?.Invoke();

            }
            if (m_cooldown > 0)
            {
                m_cooldown--;
            }
        }

        public static bool AvoidRedrawingStackOverflowOnFontChange(DistrictManager __instance, Font font)
        {
            if (__instance.m_properties != null && __instance.m_properties.m_areaNameFont.baseFont == font)
            {
                m_namesModifiedField.SetValue(__instance, true);
            }
            return false;
        }
        public static bool AvoidRedrawingStackOverflowOnLocaleChange(DistrictManager __instance)
        {
            if (__instance.m_properties != null)
            {
                m_namesModifiedField.SetValue(__instance, true);
            }
            return false;
        }

        private static readonly FieldInfo m_namesModifiedField = typeof(DistrictManager).GetField("m_namesModified", RedirectorUtils.allFlags);

        #endregion



        #region Hooking 

        public void Awake()
        {
            RedirectorInstance = KlyteMonoUtils.CreateElement<Redirector>(transform);
            LogUtils.DoLog("Loading District Manager Overrides");
            #region Release Line Hooks
            MethodInfo posChange = typeof(DistrictManagerOverrides).GetMethod("OnDistrictChanged", RedirectorUtils.allFlags);
            MethodInfo avoidRedrawingStackOverflowOnFontChange = typeof(DistrictManagerOverrides).GetMethod("AvoidRedrawingStackOverflowOnFontChange", RedirectorUtils.allFlags);
            MethodInfo avoidRedrawingStackOverflowOnLocaleChange = typeof(DistrictManagerOverrides).GetMethod("AvoidRedrawingStackOverflowOnLocaleChange", RedirectorUtils.allFlags);

            RedirectorInstance.AddRedirect(typeof(DistrictManager).GetMethod("SetDistrictName", RedirectorUtils.allFlags), null, posChange);
            RedirectorInstance.AddRedirect(typeof(DistrictManager).GetMethod("AreaModified", RedirectorUtils.allFlags), null, posChange);
            #endregion
        }
        public void Start()
        {

            FieldInfo eventField = ReflectionUtils.GetEventField(typeof(Font), "textureRebuilt");
            var value = (Action<Font>) eventField.GetValue(null);
            eventField.SetValue(null, (Action<Font>) ((Font f) =>
            {
                if (m_isRunningActionCallback)
                {
                    return;
                }

                m_isRunningActionCallback = true;
                value(f);
                m_isRunningActionCallback = false;
            }));


        }

        private static bool m_isRunningActionCallback = false;
        #endregion



    }
}
