using ColossalFramework;
using Klyte.WriteTheCity.Rendering;
using Klyte.WriteTheCity.Xml;
using SpriteFontPlus;
using System.Linq;
using UnityEngine;

namespace Klyte.WriteTheCity.UI
{
    public class WTCPropPreviewRenderer : MonoBehaviour
    {
        private readonly Camera m_camera;
        private readonly MaterialPropertyBlock m_block = new MaterialPropertyBlock();
        private readonly BoardPreviewInstanceXml m_defaultInstance = new BoardPreviewInstanceXml();


        public Vector2 Size
        {
            get => new Vector2(m_camera.targetTexture.width, m_camera.targetTexture.height);
            set {
                if (Size != value)
                {
                    m_camera.targetTexture = new RenderTexture((int)value.x, (int)value.y, 24, RenderTextureFormat.ARGB32);
                    m_camera.pixelRect = new Rect(0f, 0f, value.x, value.y);
                }
            }
        }

        public RenderTexture Texture => m_camera.targetTexture;
        public float Zoom { get; set; } = 3f;

        public WTCPropPreviewRenderer()
        {
            m_camera = new GameObject("Camera").AddComponent<Camera>();
            m_camera.transform.SetParent(base.transform);
            m_camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            m_camera.fieldOfView = 30f;
            m_camera.nearClipPlane = 0.0001f;
            m_camera.farClipPlane = 1000f;
            m_camera.allowHDR = true;
            m_camera.enabled = false;
            m_camera.targetTexture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
            m_camera.pixelRect = new Rect(0f, 0f, 512f, 512f);
            m_camera.clearFlags = CameraClearFlags.Color;
            m_camera.name = "WTCCamera";
        }

        public Matrix4x4 RenderProp(PropInfo info, BoardDescriptorGeneralXml descriptor, int referenceIdx) => RenderProp(info, default, default, descriptor, referenceIdx);

        public Matrix4x4 RenderProp(PropInfo info, Vector3 offsetPosition, Vector3 offsetRotation, BoardDescriptorGeneralXml descriptor, int referenceIdx)
        {
            InfoManager instanceInfo = Singleton<InfoManager>.instance;
            InfoManager.InfoMode currentMode = instanceInfo.CurrentMode;
            InfoManager.SubInfoMode currentSubMode = instanceInfo.CurrentSubMode;
            instanceInfo.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
            instanceInfo.UpdateInfoMode();
            Light sunLightSource = DayNightProperties.instance.sunLightSource;
            float intensity = sunLightSource.intensity;
            Color color2 = sunLightSource.color;
            Vector3 eulerAngles = sunLightSource.transform.eulerAngles;
            sunLightSource.intensity = 2f;
            sunLightSource.color = Color.white;
            sunLightSource.transform.eulerAngles = new Vector3(50f, 180f, 70f);
            Light mainLight = Singleton<RenderManager>.instance.MainLight;
            Singleton<RenderManager>.instance.MainLight = sunLightSource;
            if (mainLight == DayNightProperties.instance.moonLightSource)
            {
                DayNightProperties.instance.sunLightSource.enabled = true;
                DayNightProperties.instance.moonLightSource.enabled = false;
            }

            if (info.m_material.shader == WTCController.DISALLOWED_SHADER_PROP)
            {
                info.m_material.shader = WTCController.DEFAULT_SHADER_TEXT;
            }

            m_defaultInstance.Descriptor = descriptor;

            Matrix4x4 propMatrix;
            float magnitude;
            float dist;
            float zoom = 1;
            if (referenceIdx < 0 || referenceIdx >= descriptor.m_textDescriptors.Length)
            {
                magnitude = info.m_mesh.bounds.extents.magnitude;
                propMatrix = Matrix4x4.TRS(offsetPosition, Quaternion.Euler(offsetRotation.x, offsetRotation.y, offsetRotation.z), Vector3.one);
            }
            else
            {
                var sourceMatrix = Matrix4x4.Inverse(WTCPropRenderingRules.CalculateTextMatrix(m_defaultInstance, descriptor.m_textDescriptors[referenceIdx], WTCPropRenderingRules.GetTextMesh(FontServer.instance[WTCController.DEFAULT_FONT_KEY], descriptor.m_textDescriptors[referenceIdx], 0, 0, referenceIdx, m_defaultInstance), true).FirstOrDefault());
                magnitude = Mathf.Max(info.m_mesh.bounds.extents.magnitude, WTCPropRenderingRules.GetTextMesh(FontServer.instance[descriptor.FontName ?? WTCController.DEFAULT_FONT_KEY], descriptor.m_textDescriptors[referenceIdx], 0, 0, referenceIdx, m_defaultInstance)?.m_mesh?.bounds.extents.magnitude ?? 0);
                propMatrix = Matrix4x4.TRS(offsetPosition, Quaternion.Euler(offsetRotation.x, offsetRotation.y, offsetRotation.z), Vector3.one) * sourceMatrix;
            }
            dist = magnitude + 16f;
            zoom *= magnitude * Zoom;
            m_camera.transform.position = Vector3.forward * zoom;
            m_camera.transform.rotation = Quaternion.AngleAxis(180f, Vector3.up);
            m_camera.nearClipPlane = Mathf.Max(zoom - dist * 1.5f, 0.01f);
            m_camera.farClipPlane = zoom + dist * 1.5f;



            PropManager instance = Singleton<PropManager>.instance;
            MaterialPropertyBlock materialBlock = instance.m_materialBlock;
            materialBlock.Clear();
            materialBlock.SetColor(instance.ID_Color, WTCPropRenderingRules.GetColor(0, 0, 0, m_defaultInstance) ?? Color.white);
            if (info.m_rollLocation != null)
            {
                info.m_material.SetVectorArray(instance.ID_RollLocation, info.m_rollLocation);
                info.m_material.SetVectorArray(instance.ID_RollParams, info.m_rollParams);
            }
            PropManager propManager = instance;
            propManager.m_drawCallData.m_defaultCalls += 1;
            Graphics.DrawMesh(info.m_mesh, propMatrix, info.m_material, info.m_prefabDataLayer, m_camera, 0, materialBlock, false, false, false);

            m_defaultInstance.Descriptor = descriptor;
            for (ushort i = 0; i < descriptor.m_textDescriptors.Length; i++)
            {
                WTCPropRenderingRules.RenderTextMesh(0, 0, i, m_defaultInstance, propMatrix, descriptor.m_textDescriptors[i], m_block, FontServer.instance[descriptor.FontName ?? WTCController.DEFAULT_FONT_KEY], m_camera);
            }




            m_camera.RenderWithShader(info.m_material.shader, "");
            sunLightSource.intensity = intensity;
            sunLightSource.color = color2;
            sunLightSource.transform.eulerAngles = eulerAngles;
            Singleton<RenderManager>.instance.MainLight = mainLight;
            if (mainLight == DayNightProperties.instance.moonLightSource)
            {
                DayNightProperties.instance.sunLightSource.enabled = false;
                DayNightProperties.instance.moonLightSource.enabled = true;
            }
            instanceInfo.SetCurrentMode(currentMode, currentSubMode);
            instanceInfo.UpdateInfoMode();

            return propMatrix;
        }
    }
}
