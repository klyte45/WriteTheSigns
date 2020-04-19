using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.WriteTheCity.Utils;
using System.Reflection;
using UnityEngine;

namespace Klyte.WriteTheCity.Overrides
{
    public class BuildingManagerOverrides : MonoBehaviour, IRedirectable
    {
        public Redirector RedirectorInstance { get; private set; }


        public static void OnBuildingRemoved(ushort building) => WTCLineUtils.PurgeBuildingCache(building);

        #region Hooking

        public void Awake()
        {
            RedirectorInstance = KlyteMonoUtils.CreateElement<Redirector>(transform);
            LogUtils.DoLog("Loading Building Manager Overrides");
            MethodInfo posRename = typeof(BuildingManagerOverrides).GetMethod("OnBuildingRemoved", RedirectorUtils.allFlags);

            RedirectorInstance.AddRedirect(typeof(BuildingManager).GetMethod("ReleaseBuilding", RedirectorUtils.allFlags), null, posRename);


        }
        #endregion


    }
}
