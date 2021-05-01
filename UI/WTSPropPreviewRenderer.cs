using ColossalFramework;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Xml;
using UnityEngine;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSPropPreviewRenderer : WTSPrefabPreviewRenderer<PropInfo>
    {
        protected override ref Material GetMaterial(PropInfo info) => ref info.m_material;
        protected override ref Mesh GetMesh(PropInfo info) => ref info.m_mesh;


        protected override Matrix4x4 RenderMesh(PropInfo info, BoardTextDescriptorGeneralXml[] descriptor, Vector3 position, Quaternion rotation, Vector3 scale, Matrix4x4 sourceMatrix, out Color targetColor, ref int defaultCallsCounter)
        {
            var propMatrix = Matrix4x4.TRS(position, rotation, scale) * sourceMatrix;
            PropManager instance = Singleton<PropManager>.instance;
            MaterialPropertyBlock materialBlock = instance.m_materialBlock;
            materialBlock.Clear();
            targetColor = WTSDynamicTextRenderingRules.GetPropColor(0, 0, 0, m_defaultInstance, null, out bool colorFound);
            materialBlock.SetColor(instance.ID_Color, colorFound ? targetColor : Color.white);

            PropManager propManager = instance;
            propManager.m_drawCallData.m_batchedCalls += 1;

            if (info.m_rollLocation != null)
            {
                GetMaterial(info).SetVectorArray(instance.ID_RollLocation, info.m_rollLocation);
                GetMaterial(info).SetVectorArray(instance.ID_RollParams, info.m_rollParams);
            }
            defaultCallsCounter++;
            Graphics.DrawMesh(GetMesh(info), propMatrix, GetMaterial(info), info.m_prefabDataLayer, m_camera, 0, materialBlock, false, false, false);
            return propMatrix;
        }

    }
}
