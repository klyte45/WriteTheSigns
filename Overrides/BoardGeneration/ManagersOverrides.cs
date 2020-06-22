using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Singleton;
using UnityEngine;

namespace Klyte.WriteTheSigns.Overrides
{

    public class ManagersOverrides : Redirector, IRedirectable
    {

        public void Awake()
        {


            #region Hooks
            System.Reflection.MethodInfo postRenderMeshs = GetType().GetMethod("AfterRenderMeshes", RedirectorUtils.allFlags);
            LogUtils.DoLog($"Patching=> {postRenderMeshs}");
            var orMeth = typeof(BuildingManager).GetMethod("EndRenderingImpl", RedirectorUtils.allFlags);
            AddRedirect(orMeth, null, postRenderMeshs);
            System.Reflection.MethodInfo afterEndOverlayImpl = typeof(WTSBuildingPropsSingleton).GetMethod("AfterEndOverlayImpl", RedirectorUtils.allFlags);
            AddRedirect(typeof(ToolManager).GetMethod("EndOverlayImpl", RedirectorUtils.allFlags), null, afterEndOverlayImpl);
            m_lastUpdateTime = 0;
            #endregion 
        }

        private static uint m_lastUpdateTime = 0;
        public static void AfterRenderMeshes(RenderManager.CameraInfo cameraInfo)
        {
            if (WriteTheSignsMod.Controller?.BuildingPropsSingleton == null || m_lastUpdateTime == SimulationManager.instance.m_currentTickIndex)
            {
                return;
            }

            ref Building[] buildings = ref BuildingManager.instance.m_buildings.m_buffer;
            RenderManager renderManager = RenderManager.instance;
            for (ushort buildingID = 1; buildingID < BuildingManager.MAX_BUILDING_COUNT; buildingID++)
            {
                ref Building building = ref buildings[buildingID];
                BuildingInfo info = building.Info;
                if ((building.m_flags & Building.Flags.Created) == 0 || info.m_mesh == null)
                {
                    continue;
                }
                Vector3 position = building.m_position;
                float radius = info.m_renderSize + info.m_mesh.bounds.extents.sqrMagnitude;
                position.y += (info.m_size.y - building.m_baseHeight) * 0.5f;
                var shallRender = cameraInfo.Intersect(position, radius);
                if (!shallRender && !(info.m_buildingAI is TransportStationAI) && !(info.m_buildingAI is OutsideConnectionAI))
                {
                    continue;
                }
                if (renderManager.RequireInstance(buildingID, 1u, out uint num))
                {
                    ref RenderManager.Instance renderInstance = ref renderManager.m_instances[num];
                    if (!shallRender)
                    {
                        if (renderInstance.m_dirty)
                        {
                            renderInstance.m_dirty = false;
                            info.m_buildingAI.RefreshInstance(cameraInfo, buildingID, ref building, -1, ref renderInstance);
                        }
                        WriteTheSignsMod.Controller?.BuildingPropsSingleton?.UpdateLinesBuilding(buildingID, ref building, ref renderInstance.m_dataMatrix1);
                    }
                    else
                    {
                        WriteTheSignsMod.Controller?.BuildingPropsSingleton?.AfterRenderInstanceImpl(cameraInfo, buildingID, ref building, -1, ref renderInstance);
                    }
                }
            }

            m_lastUpdateTime = SimulationManager.instance.m_currentTickIndex;

        }


    }
}