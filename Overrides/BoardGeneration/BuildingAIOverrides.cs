using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns;
using Klyte.WriteTheSigns.Singleton;

namespace Klyte.DynamicTextProps.Overrides
{

    public partial class BuildingAIOverrides : Redirector, IRedirectable
    {

        public void Awake()
        {


            #region Hooks
            System.Reflection.MethodInfo postRenderMeshs = GetType().GetMethod("AfterRenderMeshes", RedirectorUtils.allFlags);
            LogUtils.DoLog($"Patching=> {postRenderMeshs}");
            AddRedirect(typeof(BuildingAI).GetMethod("RenderInstance", RedirectorUtils.allFlags), null, postRenderMeshs);
            System.Reflection.MethodInfo afterEndOverlayImpl = typeof(WTSBuildingPropsSingleton).GetMethod("AfterEndOverlayImpl", RedirectorUtils.allFlags);
            AddRedirect(typeof(ToolManager).GetMethod("EndOverlayImpl", RedirectorUtils.allFlags), null, afterEndOverlayImpl);

            #endregion
        }

        public static void AfterRenderMeshes(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance) => WriteTheSignsMod.Controller?.BuildingPropsSingleton?.AfterRenderInstanceImpl(cameraInfo, buildingID, ref data, layerMask, ref instance);
    }
}