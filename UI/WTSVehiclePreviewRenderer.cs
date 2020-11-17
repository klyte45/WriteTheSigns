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

        protected override Matrix4x4 RenderMesh(VehicleInfo info, BoardTextDescriptorGeneralXml[] textDescriptors, Vector3 position, Quaternion rotation, Vector3 scale, Matrix4x4 sourceMatrix, out Color targetColor)
        {
            targetColor = WTSDynamicTextRenderingRules.GetPropColor(0, 0, 0, m_defaultInstance, null, out _);
            VehicleManager instance2 = Singleton<VehicleManager>.instance;
            Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale) * sourceMatrix;
            Matrix4x4 value = info.m_vehicleAI.CalculateTyreMatrix(Vehicle.Flags.Created, ref position, ref rotation, ref scale, ref matrix);
            MaterialPropertyBlock materialBlock = instance2.m_materialBlock;
            materialBlock.Clear();
            materialBlock.SetMatrix(instance2.ID_TyreMatrix, value);
            materialBlock.SetVector(instance2.ID_TyrePosition, Vector3.zero);
            materialBlock.SetVector(instance2.ID_LightState, Vector3.zero);
            materialBlock.SetColor(instance2.ID_Color, targetColor * new Color(1, 1, 1, 0));
            instance2.m_drawCallData.m_defaultCalls += 1;
            info.m_material.SetVectorArray(instance2.ID_TyreLocation, info.m_generatedInfo.m_tyres);
            Graphics.DrawMesh(GetMesh(info), matrix, GetMaterial(info), info.m_prefabDataLayer, m_camera, 0, materialBlock, true, true);

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
            ////Vector3 one = Vector3.one;
            ////float magnitude = info.m_mesh.bounds.extents.magnitude;
            ////float num = magnitude + 16f;
            ////float num2 = magnitude * 3f;
            ////m_camera.transform.position = Vector3.forward * num2;
            ////m_camera.transform.rotation = Quaternion.AngleAxis(180f, Vector3.up);
            ////m_camera.nearClipPlane = Mathf.Max(num2 - num * 1.5f, 0.01f);
            ////m_camera.farClipPlane = num2 + num * 1.5f;
            ////Quaternion quaternion = Quaternion.Euler(20f, 0f, 0f)* rotation;
            ////Vector3 pos = quaternion * -info.m_mesh.bounds.center;
            ////VehicleManager instance2 = Singleton<VehicleManager>.instance;
            ////Matrix4x4 matrix = Matrix4x4.TRS(pos, quaternion, Vector3.one);
            ////Matrix4x4 value = info.m_vehicleAI.CalculateTyreMatrix(Vehicle.Flags.Created, ref pos, ref quaternion, ref one, ref matrix);
            ////MaterialPropertyBlock materialBlock = instance2.m_materialBlock;
            ////materialBlock.Clear();
            ////materialBlock.SetMatrix(instance2.ID_TyreMatrix, value);
            ////materialBlock.SetVector(instance2.ID_TyrePosition, Vector3.zero);
            ////materialBlock.SetVector(instance2.ID_LightState, Vector3.zero);
            ////materialBlock.SetColor(instance2.ID_VehicleColor, targetColor);

            ////instance2.m_drawCallData.m_defaultCalls = instance2.m_drawCallData.m_defaultCalls + 1;
            ////info.m_material.SetVectorArray(instance2.ID_TyreLocation, info.m_generatedInfo.m_tyres);
            ////Graphics.DrawMesh(info.m_mesh, matrix, info.m_material, 0, m_camera, 0, materialBlock, true, true);

            ////m_camera.Render();


            return matrix;
        }

    }
}
