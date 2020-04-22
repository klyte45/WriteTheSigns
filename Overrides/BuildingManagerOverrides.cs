using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Utils;
using System.Reflection;
using UnityEngine;

namespace Klyte.WriteTheSigns.Overrides
{
    public class BuildingManagerOverrides : MonoBehaviour, IRedirectable
    {
        public Redirector RedirectorInstance { get; private set; }


        public static void OnBuildingRemoved(ushort building) => WTSLineUtils.PurgeBuildingCache(building);

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
