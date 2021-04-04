using Klyte.Commons.Extensions;
using Klyte.Commons.Utils;
using System.Reflection;
using UnityEngine;

namespace Klyte.WriteTheSigns.Overrides
{
    public class DistrictManagerOverrides : MonoBehaviour, IRedirectable
    {

        public Redirector RedirectorInstance { get; private set; }

        #region Hooking 

        public void Start()
        {
            RedirectorInstance = KlyteMonoUtils.CreateElement<Redirector>(transform);
            LogUtils.DoLog("Loading District Manager Overrides");
            #region Release Line Hooks
            MethodInfo posChange = typeof(WTSController).GetMethod("OnDistrictChanged", RedirectorUtils.allFlags);
            MethodInfo posChangePark = typeof(WTSController).GetMethod("OnParkChanged", RedirectorUtils.allFlags);

            RedirectorInstance.AddRedirect(typeof(DistrictManager).GetMethod("SetDistrictName", RedirectorUtils.allFlags), null, posChange);
            RedirectorInstance.AddRedirect(typeof(DistrictManager).GetMethod("AreaModified", RedirectorUtils.allFlags), null, posChange);
            RedirectorInstance.AddRedirect(typeof(DistrictManager).GetMethod("SetParkName", RedirectorUtils.allFlags), null, posChangePark);
            RedirectorInstance.AddRedirect(typeof(DistrictManager).GetMethod("ParksAreaModified", RedirectorUtils.allFlags), null, posChangePark);

            RedirectorInstance.AddRedirect(typeof(InfoPanel).GetMethod("SetName", RedirectorUtils.allFlags), null, typeof(WTSController).GetMethod("OnCityNameChanged", RedirectorUtils.allFlags));
            #endregion
        }
        #endregion


    }
}
