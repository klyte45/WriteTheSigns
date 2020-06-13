using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using System.Reflection;
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

            RedirectorInstance.AddRedirect(BuildingManager.instance.SetBuildingName(0, "").GetType().GetMethod("MoveNext", RedirectorUtils.allFlags), null, posRename);


        }
        #endregion


    }
}
