using ColossalFramework;
using UnityEngine;

namespace Klyte.DynamicTextProps.UI
{
    public class DTPPropPreviewRenderer : MonoBehaviour
    {
        private readonly Camera m_camera;

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

        public float CameraRotation { get; set; } = 120f;

        public float Zoom { get; set; } = 3f;

        public DTPPropPreviewRenderer()
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
            m_camera.name = "DTPCamera";
        }

        public Matrix4x4 RenderProp(PropInfo info) => RenderProp(info, default, info.m_color0);

        public Matrix4x4 RenderProp(PropInfo info, Vector3 offset, Color color)
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
            float magnitude = info.m_mesh.bounds.extents.magnitude;
            float num = magnitude + 16f;
            float num2 = magnitude * Zoom;
            m_camera.transform.position = Vector3.forward * num2;
            m_camera.transform.rotation = Quaternion.AngleAxis(180f, Vector3.up);
            m_camera.nearClipPlane = Mathf.Max(num2 - num * 1.5f, 0.01f);
            m_camera.farClipPlane = num2 + num * 1.5f;

            Quaternion quaternion = Quaternion.Euler(0f, 0f, 0f) * Quaternion.Euler(0f, CameraRotation, 0f);
            Vector3 pos = quaternion * -new Vector3(info.m_mesh.bounds.center.x, 0, info.m_mesh.bounds.max.z);
            Matrix4x4 matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one) * Matrix4x4.TRS(Vector3.zero, quaternion, Vector3.one);


            PropManager instance = Singleton<PropManager>.instance;
            MaterialPropertyBlock materialBlock = instance.m_materialBlock;
            materialBlock.Clear();
            materialBlock.SetColor(instance.ID_Color, color);
            if (info.m_rollLocation != null)
            {
                info.m_material.SetVectorArray(instance.ID_RollLocation, info.m_rollLocation);
                info.m_material.SetVectorArray(instance.ID_RollParams, info.m_rollParams);
            }
            PropManager propManager = instance;
            propManager.m_drawCallData.m_defaultCalls += 1;
            Graphics.DrawMesh(info.m_mesh, matrix, info.m_material, info.m_prefabDataLayer, m_camera, 0, materialBlock, false, false, false);







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

            return matrix;
        }
    }
}
