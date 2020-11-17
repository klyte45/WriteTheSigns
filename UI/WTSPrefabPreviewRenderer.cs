using ColossalFramework;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Xml;
using SpriteFontPlus.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.WriteTheSigns.UI
{
    public abstract class WTSPrefabPreviewRenderer<PI> : MonoBehaviour where PI : PrefabInfo
    {
        protected readonly Camera m_camera;
        private readonly MaterialPropertyBlock m_block = new MaterialPropertyBlock();
        protected readonly BoardPreviewInstanceXml m_defaultInstance = new BoardPreviewInstanceXml();

        public BoardPreviewInstanceXml GetDefaultInstance() => m_defaultInstance;

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

        public WTSPrefabPreviewRenderer()
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
            m_camera.name = "WTSCamera";
        }

        public Matrix4x4 RenderPrefab(PI info, Vector3 offsetPosition, Vector3 offsetRotation, BoardTextDescriptorGeneralXml[] TextDescriptors, int referenceIdx, string overrideText, BoardDescriptorGeneralXml descriptor = null)
        {

            PrepareScene(out InfoManager instanceInfo, out InfoManager.InfoMode currentMode, out InfoManager.SubInfoMode currentSubMode, out Light sunLightSource, out float intensity, out Color color2, out Vector3 eulerAngles, out Light mainLight);

            m_defaultInstance.Descriptor = descriptor ?? new BoardDescriptorGeneralXml
            {
                TextDescriptors = TextDescriptors
            };
            m_defaultInstance.m_overrideText = overrideText;

            Matrix4x4 sourceMatrix;
            float magnitude;
            float dist;
            float zoom = 1;
            Vector3 positon, scale;
            Quaternion rotation;
            if (TextDescriptors == null || referenceIdx < 0 || referenceIdx >= TextDescriptors.Length)
            {
                magnitude = GetMesh(info).bounds.extents.magnitude;
                positon = offsetPosition + new Vector3(0, -1000, 0);
                rotation = Quaternion.Euler(offsetRotation);
                scale = Vector3.one;
                sourceMatrix = Matrix4x4.identity;
            }
            else
            {
                BasicRenderInformation refer = WTSDynamicTextRenderingRules.GetTextMesh(TextDescriptors[referenceIdx], 0, 0, referenceIdx, m_defaultInstance, descriptor, out IEnumerable<BasicRenderInformation> briArr);
                refer ??= briArr?.FirstOrDefault();
                if (refer == null)
                {
                    return default;
                }

                sourceMatrix = Matrix4x4.Inverse(WTSDynamicTextRenderingRules.CalculateTextMatrix(TextDescriptors[referenceIdx].PlacingConfig.Position, TextDescriptors[referenceIdx].PlacingConfig.Rotation, Vector3.one, TextDescriptors[referenceIdx].m_textAlign, TextDescriptors[referenceIdx].m_maxWidthMeters, TextDescriptors[referenceIdx], refer, TextDescriptors[referenceIdx].PlacingConfig.m_create180degYClone, true).FirstOrDefault().First);
                float regularMagn = GetMesh(info).bounds.extents.magnitude / WTSDynamicTextRenderingRules.SCALING_FACTOR;
                Vector3 textExt = refer?.m_mesh?.bounds.extents ?? default;
                if (TextDescriptors[referenceIdx].IsMultiItemText())
                {
                    textExt *= Mathf.Max(TextDescriptors[referenceIdx].MultiItemSettings.SubItemsPerColumn, TextDescriptors[referenceIdx].MultiItemSettings.SubItemsPerRow);
                }

                if (TextDescriptors[referenceIdx].m_maxWidthMeters > 0)
                {
                    textExt.x = Mathf.Min(textExt.x * TextDescriptors[referenceIdx].m_textScale, TextDescriptors[referenceIdx].m_maxWidthMeters / WTSDynamicTextRenderingRules.SCALING_FACTOR) / TextDescriptors[referenceIdx].m_textScale;
                }
                magnitude = Mathf.Min(regularMagn * 3, Mathf.Max(0.1f / WTSDynamicTextRenderingRules.SCALING_FACTOR, (textExt * TextDescriptors[referenceIdx].m_textScale).magnitude));
                positon = offsetPosition + new Vector3(0, -1000, 0);
                rotation = Quaternion.Euler(offsetRotation.x, offsetRotation.y, offsetRotation.z);
                scale = Vector3.one;
            }
            dist = magnitude + 16f;
            zoom *= magnitude * Zoom;
            m_camera.transform.position = Vector3.forward * zoom + new Vector3(0, -1000, 0);
            m_camera.transform.rotation = Quaternion.AngleAxis(180f, Vector3.up);
            m_camera.nearClipPlane = Mathf.Max(zoom - dist * 1.5f, 0.01f);
            m_camera.farClipPlane = zoom + dist * 1.5f;

            var propMatrix = RenderMesh(info, TextDescriptors, positon, rotation, scale, sourceMatrix, out Color targetColor);

            for (ushort i = 0; i < TextDescriptors?.Length; i++)
            {
                WTSDynamicTextRenderingRules.RenderTextMesh(0, 0, i, m_defaultInstance, propMatrix, descriptor, ref TextDescriptors[i], m_block, -1, targetColor, info, m_camera);
            }

            m_camera.Render();





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


        private static void PrepareScene(out InfoManager instanceInfo, out InfoManager.InfoMode currentMode, out InfoManager.SubInfoMode currentSubMode, out Light sunLightSource, out float intensity, out Color color2, out Vector3 eulerAngles, out Light mainLight)
        {
            instanceInfo = Singleton<InfoManager>.instance;
            currentMode = instanceInfo.CurrentMode;
            currentSubMode = instanceInfo.CurrentSubMode;
            instanceInfo.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
            instanceInfo.UpdateInfoMode();
            sunLightSource = DayNightProperties.instance.sunLightSource;
            intensity = sunLightSource.intensity;
            color2 = sunLightSource.color;
            eulerAngles = sunLightSource.transform.eulerAngles;
            sunLightSource.intensity = 2f;
            sunLightSource.color = Color.white;
            sunLightSource.transform.eulerAngles = new Vector3(50f, 180f, 70f);
            mainLight = Singleton<RenderManager>.instance.MainLight;
            Singleton<RenderManager>.instance.MainLight = sunLightSource;
            if (mainLight == DayNightProperties.instance.moonLightSource)
            {
                DayNightProperties.instance.sunLightSource.enabled = true;
                DayNightProperties.instance.moonLightSource.enabled = false;
            }
        }

        protected abstract Matrix4x4 RenderMesh(PI info, BoardTextDescriptorGeneralXml[] textDescriptors, Vector3 position, Quaternion rotation, Vector3 scale, Matrix4x4 sourceMatrix, out Color targetColor);
        protected abstract ref Mesh GetMesh(PI info);
        protected abstract ref Material GetMaterial(PI info);

    }
}
