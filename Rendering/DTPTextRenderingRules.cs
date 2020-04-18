using ColossalFramework.UI;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Data;
using Klyte.DynamicTextProps.Utils;
using Klyte.DynamicTextProps.Xml;
using SpriteFontPlus;
using SpriteFontPlus.Utility;
using UnityEngine;

namespace Klyte.DynamicTextProps.Rendering
{


    internal static class DTPPropRenderingRules
    {
        public const float SCALING_FACTOR = 0.005f;
        public static readonly int SHADER_PROP_COLOR = Shader.PropertyToID("_Color");
        public static readonly int SHADER_PROP_COLOR0 = Shader.PropertyToID("_ColorV0");
        public static readonly int SHADER_PROP_COLOR1 = Shader.PropertyToID("_ColorV1");
        public static readonly int SHADER_PROP_COLOR2 = Shader.PropertyToID("_ColorV2");
        public static readonly int SHADER_PROP_COLOR3 = Shader.PropertyToID("_ColorV3");
        public static readonly int SHADER_PROP_SPECULAR = Shader.PropertyToID("_SpecColor");




        public static void RenderPropMesh(ref PropInfo propInfo, RenderManager.CameraInfo cameraInfo, ushort refId, int boardIdx, int secIdx, int layerMask, float refAngleRad, Vector3 position, Vector4 dataVector, ref string propName, Vector3 propAngle, Vector3 propScale, BoardInstanceXml descriptor, out Matrix4x4 propMatrix, out bool rendered, InstanceID propRenderID)
        {
            Color? propColor = DTPPropRenderingRules.GetColor(refId, boardIdx, secIdx, descriptor);
            if (propColor == null)
            {
                rendered = false;
                propMatrix = new Matrix4x4();
                return;
            }

            if (!string.IsNullOrEmpty(propName))
            {
                if (propInfo == null || propInfo.name != propName)
                {
                    propInfo = PrefabCollection<PropInfo>.FindLoaded(propName);
                    if (propInfo == null)
                    {
                        LogUtils.DoErrorLog($"PREFAB NOT FOUND: {propName}");
                        propName = null;
                    }
                }
                propInfo.m_color0 = propColor.GetValueOrDefault();
            }
            else
            {
                propInfo = null;
            }
            propMatrix = RenderUtils.RenderProp(refId, refAngleRad, cameraInfo, propInfo, position, dataVector, boardIdx, propAngle, propScale, out rendered, propRenderID);
        }


        public static void RenderTextMesh(ushort refID, int boardIdx, int secIdx, BoardInstanceXml descriptor, Matrix4x4 propMatrix, BoardTextDescriptorGeneralXml textDescriptor, MaterialPropertyBlock materialPropertyBlock, DynamicSpriteFont baseFont, Camera targetCamera = null)
        {
            BasicRenderInformation renderInfo = GetTextMesh(baseFont, textDescriptor, refID, boardIdx, secIdx, descriptor);
            if (renderInfo?.m_mesh == null || renderInfo?.m_generatedMaterial == null)
            {
                return;
            }

            float overflowScaleX = 1f;
            float overflowScaleY = 1f;
            float defaultMultiplierX = textDescriptor.m_textScale * SCALING_FACTOR;
            float defaultMultiplierY = textDescriptor.m_textScale * SCALING_FACTOR;
            float realWidth = defaultMultiplierX * renderInfo.m_sizeMetersUnscaled.x;
            float realHeight = defaultMultiplierY * renderInfo.m_sizeMetersUnscaled.y;
            Vector3 targetRelativePosition = textDescriptor.m_textRelativePosition;
            //LogUtils.DoWarnLog($"[{renderInfo},{refID},{boardIdx},{secIdx}] realWidth = {realWidth}; realHeight = {realHeight};");
            if (textDescriptor.m_maxWidthMeters > 0 && textDescriptor.m_maxWidthMeters < realWidth)
            {
                overflowScaleX = textDescriptor.m_maxWidthMeters / realWidth;
                if (textDescriptor.m_applyOverflowResizingOnY)
                {
                    overflowScaleY = overflowScaleX;
                }
            }
            else
            {
                if (textDescriptor.m_maxWidthMeters > 0 && textDescriptor.m_textAlign != UIHorizontalAlignment.Center)
                {
                    float factor = textDescriptor.m_textAlign == UIHorizontalAlignment.Left == (((textDescriptor.m_textRelativeRotation.y % 360) + 810) % 360 > 180) ? 0.5f : -0.5f;
                    targetRelativePosition += new Vector3((textDescriptor.m_maxWidthMeters - realWidth) * factor / descriptor.ScaleX, 0, 0);
                }
            }


            if (textDescriptor.m_verticalAlign != UIVerticalAlignment.Middle)
            {
                float factor = textDescriptor.m_verticalAlign == UIVerticalAlignment.Bottom == (((textDescriptor.m_textRelativeRotation.x % 360) + 810) % 360 > 180) ? -.5f : .5f;
                targetRelativePosition += new Vector3(0, realHeight * factor, 0);
            }

            Matrix4x4 matrix = propMatrix * Matrix4x4.TRS(
                targetRelativePosition,
                Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.x, Vector3.left) * Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.y, Vector3.down) * Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.z, Vector3.back),
                new Vector3(defaultMultiplierX * overflowScaleX / descriptor.ScaleX, defaultMultiplierY * overflowScaleY / descriptor.PropScale.y, 1));

            Color colorToSet = Color.white;
            if (textDescriptor.m_useContrastColor)
            {
                colorToSet = GetContrastColor(refID, boardIdx, secIdx, descriptor);
            }
            else if (textDescriptor.m_defaultColor != null)
            {
                colorToSet = textDescriptor.m_defaultColor;
            }
            materialPropertyBlock.Clear();
            materialPropertyBlock.SetColor(SHADER_PROP_COLOR, colorToSet);
            materialPropertyBlock.SetColor(SHADER_PROP_COLOR0, colorToSet);
            materialPropertyBlock.SetColor(SHADER_PROP_COLOR1, colorToSet);
            materialPropertyBlock.SetColor(SHADER_PROP_COLOR2, colorToSet);
            materialPropertyBlock.SetColor(SHADER_PROP_COLOR3, colorToSet);


            //materialPropertyBlock.SetColor(m_shaderPropEmissive, Color.white * (SimulationManager.instance.m_isNightTime ? textDescriptor.m_nightEmissiveMultiplier : textDescriptor.m_dayEmissiveMultiplier));
            renderInfo.m_generatedMaterial.shader = DTPController.DEFAULT_SHADER_TEXT;
            Graphics.DrawMesh(renderInfo.m_mesh, matrix, renderInfo.m_generatedMaterial, 10, targetCamera, 0, materialPropertyBlock, false);

        }
        public static Color? GetColor(ushort refId, int boardIdx, int textIdx, BoardInstanceXml descriptor)
        {

            if (descriptor is BoardDescriptorRoadNodeXml)
            {
                if (textIdx == 0)
                {
                    return DTPRoadNodesData.Instance.BoardsContainers[refId].m_boardsData[boardIdx]?.m_cachedColor;
                }
                else
                {
                    return DTPRoadNodesData.Instance.BoardsContainers[refId].m_boardsData[boardIdx]?.m_cachedColor2;
                }
            }
            return descriptor.Descriptor.FixedColor;
        }

        public static Color GetContrastColor(ushort refID, int boardIdx, int textIdx, BoardInstanceXml descriptor)
        {
            if (descriptor is BoardDescriptorRoadNodeXml)
            {
                if (textIdx == 0)
                {
                    return DTPRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx]?.m_cachedContrastColor ?? Color.black;
                }
                else
                {
                    return DTPRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx]?.m_cachedContrastColor2 ?? Color.black;
                }
            }
            return KlyteMonoUtils.ContrastColor(descriptor.Descriptor.FixedColor ?? Color.white);
        }

        private static BasicRenderInformation GetTextMesh(DynamicSpriteFont baseFont, BoardTextDescriptorGeneralXml textDescriptor, ushort refID, int boardIdx, int secIdx, BoardInstanceXml descriptor)
        {
            if (descriptor is BoardDescriptorRoadNodeXml)
            {
                switch (textDescriptor.m_textType)
                {
                    case TextType.StreetSuffix:
                        return RenderUtils.GetFromCacheArray((secIdx == 0 ? DTPRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_segmentId1 : DTPRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_segmentId2), textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.SuffixStreetName, baseFont);
                    case TextType.StreetPrefix:
                        return RenderUtils.GetFromCacheArray((secIdx == 0 ? DTPRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_segmentId1 : DTPRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_segmentId2), textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.StreetQualifier, baseFont);
                    case TextType.StreetNameComplete:
                        return RenderUtils.GetFromCacheArray((secIdx == 0 ? DTPRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_segmentId1 : DTPRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_segmentId2), textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.FullStreetName, baseFont);
                    case TextType.District:
                        return RenderUtils.GetFromCacheArray((secIdx == 0 ? DTPRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_districtId1 : DTPRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_districtId2), textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.District, baseFont);
                    case TextType.DistanceFromCenter:
                        int distanceRef = (int)Mathf.Floor(DTPRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_distanceRef / 1000);
                        return RenderUtils.GetTextData(distanceRef.ToString(), textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, baseFont);
                    case TextType.Fixed:
                        return RenderUtils.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, baseFont, textDescriptor.m_overrideFont);
                    default: return null;
                }
            }
            return RenderUtils.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, baseFont, textDescriptor.m_overrideFont);
        }
    }

}
