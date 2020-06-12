using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
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
            //System.Reflection.MethodInfo afterEndOverlayImpl = GetType().GetMethod("AfterEndOverlayImpl", RedirectorUtils.allFlags);
            //AddRedirect(typeof(ToolManager).GetMethod("EndOverlayImpl", RedirectorUtils.allFlags), null, afterEndOverlayImpl);

            #endregion
        }

        //public static void AfterEndOverlayImpl(RenderManager.CameraInfo cameraInfo)
        //{
        //    if (Instance.EditorInstance.component.isVisible)
        //    {
        //        foreach (Tuple<Vector3, float, Color> tuple in Instance.m_onOverlayRenderQueue)
        //        {
        //            Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo,
        //               tuple.Third,
        //               tuple.First,
        //               tuple.Second * 2,
        //               -1, 1280f, false, true);
        //        }
        //        Instance.m_onOverlayRenderQueue.Clear();
        //    }
        //}


        public static void AfterRenderMeshes(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance) => WTSBuildingPropsSingleton.instance.AfterRenderInstanceImpl(cameraInfo, buildingID, ref data, layerMask, ref instance);
    }
}