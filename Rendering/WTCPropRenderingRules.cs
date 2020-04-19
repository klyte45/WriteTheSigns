using ColossalFramework;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using Klyte.WriteTheCity.Data;
using Klyte.WriteTheCity.Utils;
using Klyte.WriteTheCity.Xml;
using SpriteFontPlus;
using SpriteFontPlus.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace Klyte.WriteTheCity.Rendering
{
    internal static class WTCPropRenderingRules
    {
        public const float SCALING_FACTOR = 0.005f;
        public static readonly int SHADER_PROP_COLOR = Shader.PropertyToID("_Color");
        public static readonly int SHADER_PROP_COLOR0 = Shader.PropertyToID("_ColorV0");
        public static readonly int SHADER_PROP_COLOR1 = Shader.PropertyToID("_ColorV1");
        public static readonly int SHADER_PROP_COLOR2 = Shader.PropertyToID("_ColorV2");
        public static readonly int SHADER_PROP_COLOR3 = Shader.PropertyToID("_ColorV3");
        public static readonly int SHADER_PROP_SPECULAR = Shader.PropertyToID("_SpecColor");

        public static readonly Dictionary<TextRenderingClass, TextType[]> ALLOWED_TYPES_PER_RENDERING_CLASS = new Dictionary<TextRenderingClass, TextType[]>
        {
            [TextRenderingClass.RoadNodes] = new TextType[]
            {
                TextType.Fixed,
                TextType.StreetPrefix,
                TextType.StreetSuffix,
                TextType.StreetNameComplete,
                TextType.District,
                TextType.DistanceFromCenter,
            },
            [TextRenderingClass.MileageMarker] = new TextType[]
            {
                TextType.Fixed,
                TextType.Direction,
                TextType.Mileage,
                TextType.StreetCode,
                TextType.StreetPrefix,
                TextType.StreetSuffix,
                TextType.StreetNameComplete,
            },
            [TextRenderingClass.Buildings] = new TextType[]
            {
               TextType.Fixed,
               TextType.OwnName,
               TextType.StreetPrefix,
               TextType.StreetSuffix,
               TextType.StreetNameComplete,
               TextType.LinesSymbols,
               TextType.NextStopLine, // Next Station Line 1
               TextType.PrevStopLine, // Previous Station Line 2
               TextType.LastStopLine, // Line Destination (Last stop before get back) 3
            },
            [TextRenderingClass.PlaceOnNet] = new TextType[]
            {
               TextType.Fixed,

               TextType.StreetPrefix,
               TextType.StreetSuffix,
               TextType.StreetNameComplete,

               TextType.NextExitText,
               TextType.NextExitDestination1,
               TextType.NextExitDestination2,
               TextType.NextExitDestination3,
               TextType.NextExitDestination4,
               TextType.NextExitDistance,

               TextType.Next2ExitText,
               TextType.Next2ExitDestination1,
               TextType.Next2ExitDestination2,
               TextType.Next2ExitDestination3,
               TextType.Next2ExitDestination4,
               TextType.Next2ExitDistance,

            },
        };


        public static void RenderPropMesh(ref PropInfo propInfo, RenderManager.CameraInfo cameraInfo, ushort refId, int boardIdx, int secIdx, int layerMask, float refAngleRad, Vector3 position, Vector4 dataVector, ref string propName, Vector3 propAngle, Vector3 propScale, BoardInstanceXml descriptor, out Matrix4x4 propMatrix, out bool rendered, InstanceID propRenderID)
        {
            Color? propColor = WTCPropRenderingRules.GetColor(refId, boardIdx, secIdx, descriptor);
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
                    if (propInfo.m_material.shader == WTCController.DISALLOWED_SHADER_PROP)
                    {
                        propInfo.m_material.shader = WTCController.DEFAULT_SHADER_TEXT;
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

            List<Matrix4x4> textMatrixes = CalculateTextMatrix(descriptor, textDescriptor, renderInfo);
            foreach (Matrix4x4 textMatrix in textMatrixes)
            {
                Matrix4x4 matrix = propMatrix * textMatrix;

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
                renderInfo.m_generatedMaterial.shader = WTCController.DEFAULT_SHADER_TEXT;
                Graphics.DrawMesh(renderInfo.m_mesh, matrix, renderInfo.m_generatedMaterial, 10, targetCamera, 0, materialPropertyBlock, false);
            }
        }

        internal static List<Matrix4x4> CalculateTextMatrix(BoardInstanceXml instance, BoardTextDescriptorGeneralXml textDescriptor, BasicRenderInformation renderInfo, bool centerReference = false)
        {
            var result = new List<Matrix4x4>();

            Matrix4x4 textMatrix = ApplyTextAdjustments(textDescriptor.m_textRelativePosition, textDescriptor.m_textRelativeRotation, renderInfo, instance.PropScale, textDescriptor.m_textScale, textDescriptor.m_textAlign, textDescriptor.m_verticalAlign, textDescriptor.m_maxWidthMeters, textDescriptor.m_applyOverflowResizingOnY, centerReference);

            result.Add(textMatrix);

            if (textDescriptor.m_create180degYClone)
            {
                UIHorizontalAlignment targetTextAlignment = textDescriptor.m_textAlign;
                if (textDescriptor.m_invertYCloneHorizontalAlign)
                {
                    targetTextAlignment = 2 - targetTextAlignment;
                }
                result.Add(ApplyTextAdjustments(new Vector3(textDescriptor.m_textRelativePosition.x, textDescriptor.m_textRelativePosition.y, -textDescriptor.m_textRelativePosition.z), textDescriptor.m_textRelativeRotation + new Vector3(0, 180), renderInfo, instance.PropScale, textDescriptor.m_textScale, targetTextAlignment, textDescriptor.m_verticalAlign, textDescriptor.m_maxWidthMeters, textDescriptor.m_applyOverflowResizingOnY, centerReference));
            }

            return result;
        }

        private static Matrix4x4 ApplyTextAdjustments(Vector3 textPosition, Vector3 textRotation, BasicRenderInformation renderInfo, Vector3 propScale, float textScale, UIHorizontalAlignment horizontalAlignment, UIVerticalAlignment verticalAlignment, float maxWidth, bool applyResizeOverflowOnY, bool centerReference)
        {
            float overflowScaleX = 1f;
            float overflowScaleY = 1f;
            float defaultMultiplierX = textScale * SCALING_FACTOR;
            float defaultMultiplierY = textScale * SCALING_FACTOR;
            float realWidth = defaultMultiplierX * renderInfo.m_sizeMetersUnscaled.x;
            float realHeight = defaultMultiplierY * renderInfo.m_sizeMetersUnscaled.y;
            Vector3 targetRelativePosition = textPosition;
            //LogUtils.DoWarnLog($"[{renderInfo},{refID},{boardIdx},{secIdx}] realWidth = {realWidth}; realHeight = {realHeight};");
            if (maxWidth > 0 && maxWidth < realWidth)
            {
                overflowScaleX = maxWidth / realWidth;
                if (applyResizeOverflowOnY)
                {
                    overflowScaleY = overflowScaleX;
                }
            }
            else
            {
                if (maxWidth > 0 && horizontalAlignment != UIHorizontalAlignment.Center)
                {
                    float factor = horizontalAlignment == UIHorizontalAlignment.Left == (((textRotation.y % 360) + 810) % 360 > 180) ? 0.5f : -0.5f;
                    targetRelativePosition += new Vector3((maxWidth - realWidth) * factor / propScale.x, 0, 0);
                }
            }


            if (verticalAlignment != UIVerticalAlignment.Middle)
            {
                float factor = verticalAlignment == UIVerticalAlignment.Bottom == (((textRotation.x % 360) + 810) % 360 > 180) ? -.5f : .5f;
                targetRelativePosition += new Vector3(0, realHeight * factor, 0);
            }

            var textMatrix = Matrix4x4.TRS(
                targetRelativePosition,
               Quaternion.AngleAxis(textRotation.x, Vector3.left) * Quaternion.AngleAxis(textRotation.y, Vector3.down) * Quaternion.AngleAxis(textRotation.z, Vector3.back),
           centerReference ? new Vector3(SCALING_FACTOR, SCALING_FACTOR, SCALING_FACTOR) : new Vector3(defaultMultiplierX * overflowScaleX / propScale.x, defaultMultiplierY * overflowScaleY / propScale.y, 1));
            return textMatrix;
        }

        public static Color? GetColor(ushort refId, int boardIdx, int textIdx, BoardInstanceXml instance)
        {

            if (instance is BoardInstanceRoadNodeXml)
            {
                if (textIdx == 0)
                {
                    return WTCRoadNodesData.Instance.BoardsContainers[refId].m_boardsData[boardIdx]?.m_cachedColor;
                }
                else
                {
                    return WTCRoadNodesData.Instance.BoardsContainers[refId].m_boardsData[boardIdx]?.m_cachedColor2;
                }
            }
            return instance.Descriptor.FixedColor ?? GetCurrentSimulationColor();

        }

        private static Color[] m_spectreSteps = new Color[]
        {
            new Color32(170,170,170,255),
            Color.white,
            Color.red,
            Color.yellow ,
            Color.green  ,
            Color.cyan   ,
            Color.blue   ,
            Color.magenta,
            new Color32(128,0,0,255),
            new Color32(128,128,0,255),
            new Color32(0,128,0,255),
            new Color32(0,128,128,255),
            new Color32(0,0,128,255),
            new Color32(128,0,128,255),
            Color.black,
            new Color32(85,85,85,255),
        };
        private static Color GetCurrentSimulationColor()
        {
            if (SimulationManager.exists)
            {
                uint frame = SimulationManager.instance.m_currentTickIndex & 0x3FFF;
                byte currColor = (byte)(frame >> 10);
                byte nextColor = (byte)((currColor + 1) & 0xf);

                return Color.Lerp(m_spectreSteps[currColor], m_spectreSteps[nextColor], (frame & 0x3FF) / 1024f);

            }
            return Color.gray;
        }

        public static Color GetContrastColor(ushort refID, int boardIdx, int textIdx, BoardInstanceXml instance)
        {
            if (instance is BoardInstanceRoadNodeXml)
            {
                if (textIdx == 0)
                {
                    return WTCRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx]?.m_cachedContrastColor ?? Color.black;
                }
                else
                {
                    return WTCRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx]?.m_cachedContrastColor2 ?? Color.black;
                }
            }
            return KlyteMonoUtils.ContrastColor(instance.Descriptor.FixedColor ?? GetCurrentSimulationColor());
        }

        internal static BasicRenderInformation GetTextMesh(DynamicSpriteFont baseFont, BoardTextDescriptorGeneralXml textDescriptor, ushort refID, int boardIdx, int secIdx, BoardInstanceXml instance)
        {
            if (instance is BoardPreviewInstanceXml preview)
            {
                if (!preview.m_overrideText.IsNullOrWhiteSpace())
                {
                    return RenderUtils.GetTextData(preview.m_overrideText, "", "", false, baseFont, textDescriptor.m_overrideFont);
                }
                return textDescriptor.m_textType switch
                {
                    TextType.Fixed => RenderUtils.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, baseFont, textDescriptor.m_overrideFont),
                    _ => RenderUtils.GetTextData($"{textDescriptor.m_textType}: {preview.m_currentText}", textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, baseFont, textDescriptor.m_overrideFont),
                };
            }
            else if (instance is BoardInstanceRoadNodeXml)
            {
                switch (textDescriptor.m_textType)
                {
                    case TextType.StreetSuffix:
                        return RenderUtils.GetFromCacheArray((secIdx == 0 ? WTCRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_segmentId1 : WTCRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_segmentId2), textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.SuffixStreetName, baseFont);
                    case TextType.StreetPrefix:
                        return RenderUtils.GetFromCacheArray((secIdx == 0 ? WTCRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_segmentId1 : WTCRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_segmentId2), textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.StreetQualifier, baseFont);
                    case TextType.StreetNameComplete:
                        return RenderUtils.GetFromCacheArray((secIdx == 0 ? WTCRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_segmentId1 : WTCRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_segmentId2), textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.FullStreetName, baseFont);
                    case TextType.District:
                        return RenderUtils.GetFromCacheArray((secIdx == 0 ? WTCRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_districtId1 : WTCRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_districtId2), textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.District, baseFont);
                    case TextType.DistanceFromCenter:
                        int distanceRef = (int)Mathf.Floor(WTCRoadNodesData.Instance.BoardsContainers[refID].m_boardsData[boardIdx].m_distanceRef / 1000);
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
