using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Utils;
using System.Reflection;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{
    public class BuildingManagerOverrides : MonoBehaviour, IRedirectable
    {
        public Redirector RedirectorInstance { get; private set; }


        public static void OnBuildingRemoved(ushort building) => DTPLineUtils.PurgeBuildingCache(building);

        #region Hooking

        public void Awake()
        {
            RedirectorInstance = KlyteMonoUtils.CreateElement<Redirector>(transform);
            LogUtils.DoLog("Loading Building Manager Overrides");
            MethodInfo posRename = typeof(BuildingManagerOverrides).GetMethod("OnInstanceRenamed", RedirectorUtils.allFlags);

            RedirectorInstance.AddRedirect(typeof(BuildingManager).GetMethod("ReleaseBuilding", RedirectorUtils.allFlags), null, posRename);


        }
        #endregion


    }
}
