using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using SpriteFontPlus;
using SpriteFontPlus.Utility;
using System.Collections.Generic;
using UnityEngine;
using static Klyte.Commons.Utils.StopSearchUtils;

namespace Klyte.WriteTheSigns.Singleton
{
    public class WTSVehicleTextsSingleton : MonoBehaviour
    {
        public DynamicSpriteFont DrawFont => FontServer.instance[Data.DefaultFont] ?? FontServer.instance[WTSController.DEFAULT_FONT_KEY];
        private readonly Dictionary<string, StopPointDescriptorLanes[]> m_buildingStopsDescriptor = new Dictionary<string, StopPointDescriptorLanes[]>();
        public WTSVehicleData Data => WTSVehicleData.Instance;


        #region Initialize
        public void Awake()
        {

        }

        public void Start()
        {
        }


        #endregion



        public void AfterRenderExtraStuff(VehicleAI thiz, ushort vehicleID, ref Vehicle vehicleData, RenderManager.CameraInfo cameraInfo, InstanceID id, Vector3 position, Quaternion rotation, Vector4 tyrePosition, Vector4 lightState, Vector3 scale, Vector3 swayPosition, bool underground, bool overground)
        {


            if (thiz.m_info == null || thiz.m_info.m_vehicleAI == null || thiz.m_info.m_subMeshes == null)
            {
                return;
            }

            Vehicle.Flags flags = VehicleManager.instance.m_vehicles.m_buffer[vehicleID].m_flags;
            Matrix4x4 matrix4x = thiz.m_info.m_vehicleAI.CalculateBodyMatrix(flags, ref position, ref rotation, ref scale, ref swayPosition);
            MaterialPropertyBlock materialBlock = VehicleManager.instance.m_materialBlock;
            materialBlock.Clear();


            BasicRenderInformation renderInfo = RenderUtils.GetTextData($"{vehicleID}", "", "", FontServer.instance[Data.DefaultFont ?? WTSController.DEFAULT_FONT_KEY], null);

            WTSPropRenderingRules.DrawTextBri(vehicleID, 0, 0, matrix4x, basicDescr, materialBlock, renderInfo, KlyteMonoUtils.ContrastColor(thiz.GetColor(vehicleID, ref vehicleData, InfoManager.InfoMode.None)), new Vector3(thiz.m_info.m_mesh.bounds.max.x, 1, 0), new Vector3(0, -90), Vector3.one, false);
            WTSPropRenderingRules.DrawTextBri(vehicleID, 0, 0, matrix4x, basicDescr, materialBlock, renderInfo, KlyteMonoUtils.ContrastColor(thiz.GetColor(vehicleID, ref vehicleData, InfoManager.InfoMode.None)), new Vector3(thiz.m_info.m_mesh.bounds.min.x, 1, 0), new Vector3(0, 90), Vector3.one, false);
            ;
        }
        private BoardTextDescriptorGeneralXml basicDescr = new BoardTextDescriptorGeneralXml
        {
            m_textScale = 2.5f
        };
        private void RenderDescriptor(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance renderInstance, ref BuildingGroupDescriptorXml parentDescriptor, int idx)
        {
            BoardDescriptorGeneralXml propLayout = parentDescriptor.GetDescriptorOf(idx);
            if (propLayout?.m_propName == null)
            {
                return;
            }
            ref BoardInstanceBuildingXml targetDescriptor = ref parentDescriptor.PropInstances[idx];
            if (Data.BoardsContainers[buildingID, 0, 0] == null || Data.BoardsContainers[buildingID, 0, 0].Length != parentDescriptor.PropInstances.Length)
            {
                Data.BoardsContainers[buildingID, 0, 0] = new BoardBunchContainerBuilding[parentDescriptor.PropInstances.Length];
            }
            ref BoardBunchContainerBuilding item = ref Data.BoardsContainers[buildingID, 0, 0][idx];

            if (item == null)
            {
                item = new BoardBunchContainerBuilding();
            }
            if (item.m_cachedProp?.name != propLayout.m_propName)
            {
                item.m_cachedProp = null;
            }
            if (item.m_cachedPosition == null || item.m_cachedRotation == null)
            {
                item.m_cachedPosition = renderInstance.m_dataMatrix1.MultiplyPoint(targetDescriptor.PropPosition);
                item.m_cachedRotation = targetDescriptor.PropRotation;
                LogUtils.DoLog($"[B{buildingID}/{idx}]Cached position: {item.m_cachedPosition} | Cached rotation: {item.m_cachedRotation}");
            }
            Vector3 targetPostion = item.m_cachedPosition ?? default;
            for (int i = 0; i <= targetDescriptor.m_arrayRepeatTimes; i++)
            {
                if (i > 0)
                {
                    targetPostion = renderInstance.m_dataMatrix1.MultiplyPoint(targetDescriptor.PropPosition + (i * (Vector3)targetDescriptor.ArrayRepeat));
                }
                RenderSign(ref data, cameraInfo, buildingID, idx, targetPostion, item.m_cachedRotation ?? default, layerMask, propLayout, ref targetDescriptor, ref item.m_cachedProp);

            }
        }

        private void RenderSign(ref Building data, RenderManager.CameraInfo cameraInfo, ushort buildingId, int boardIdx, Vector3 position, Vector3 rotation, int layerMask, BoardDescriptorGeneralXml propLayout, ref BoardInstanceBuildingXml targetDescriptor, ref PropInfo cachedProp)
        {
            WTSPropRenderingRules.RenderPropMesh(ref cachedProp, cameraInfo, buildingId, boardIdx, 0, layerMask, data.m_angle, position, Vector4.zero, ref propLayout.m_propName, rotation, targetDescriptor.PropScale, propLayout, targetDescriptor, out Matrix4x4 propMatrix, out bool rendered, new InstanceID { Building = buildingId });
            if (rendered)
            {
                for (int j = 0; j < propLayout.m_textDescriptors.Length; j++)
                {
                    if (cameraInfo.CheckRenderDistance(position, 200 * propLayout.m_textDescriptors[j].m_textScale * (propLayout.m_textDescriptors[j].ColoringConfig.MaterialType == FontStashSharp.MaterialType.OPAQUE ? 1 : 2)))
                    {
                        MaterialPropertyBlock properties = PropManager.instance.m_materialBlock;
                        properties.Clear();
                        WTSPropRenderingRules.RenderTextMesh(buildingId, boardIdx, 0, targetDescriptor, propMatrix, propLayout, ref propLayout.m_textDescriptors[j], properties, DrawFont);
                    }
                }
            }
        }




    }
}
