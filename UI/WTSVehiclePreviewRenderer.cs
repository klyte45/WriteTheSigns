using ColossalFramework;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Xml;
using UnityEngine;

namespace Klyte.WriteTheSigns.UI
{
    internal class WTSVehiclePreviewRenderer : WTSPrefabPreviewRenderer<VehicleInfo>
    {
        protected override ref Material GetMaterial(VehicleInfo info) => ref info.m_material;
        protected override ref Mesh GetMesh(VehicleInfo info) => ref info.m_mesh;
        protected override Matrix4x4 RenderMesh(VehicleInfo info, BoardTextDescriptorGeneralXml[] textDescriptors, Vector3 position, Quaternion rotation, Vector3 scale, Matrix4x4 sourceMatrix)
        {
            VehicleManager instance2 = Singleton<VehicleManager>.instance;
            Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale) * sourceMatrix;
            var targetColor = WTSPropRenderingRules.GetColor(0, 0, 0, m_defaultInstance, null, out bool colorFound);
            Matrix4x4 value = info.m_vehicleAI.CalculateTyreMatrix(Vehicle.Flags.Created, ref position, ref rotation, ref scale, ref matrix);
            MaterialPropertyBlock materialBlock = instance2.m_materialBlock;
            materialBlock.Clear();
            materialBlock.SetMatrix(instance2.ID_TyreMatrix, value);
            materialBlock.SetVector(instance2.ID_TyrePosition, Vector3.zero);
            materialBlock.SetVector(instance2.ID_LightState, Vector3.zero);
            materialBlock.SetColor(instance2.ID_Color, targetColor);
            instance2.m_drawCallData.m_defaultCalls += 1;
            info.m_material.SetVectorArray(instance2.ID_TyreLocation, info.m_generatedInfo.m_tyres);
            Graphics.DrawMesh(GetMesh(info), matrix, GetMaterial(info), info.m_prefabDataLayer, m_camera, 0, materialBlock, false, false, false);

            for (int j = 0; j < info.m_subMeshes.Length; j++)
            {
                VehicleInfo.MeshInfo meshInfo = info.m_subMeshes[j];
                var subInfo = meshInfo.m_subInfo as VehicleInfoSub;
                if (subInfo != null && (meshInfo.m_vehicleFlagsRequired & Vehicle.Flags.LeftHandDrive) == 0)
                {
                    VehicleManager.instance.m_drawCallData.m_defaultCalls = VehicleManager.instance.m_drawCallData.m_defaultCalls + 1;

                    subInfo.m_material.SetVectorArray(VehicleManager.instance.ID_TyreLocation, subInfo.m_generatedInfo.m_tyres);
                    Graphics.DrawMesh(subInfo.m_mesh, matrix, subInfo.m_material, info.m_prefabDataLayer, null, 0, materialBlock);

                }

            }



            return matrix;
        }

    }
}
