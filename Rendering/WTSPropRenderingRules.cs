﻿using ColossalFramework;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Singleton;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using SpriteFontPlus;
using SpriteFontPlus.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Klyte.WriteTheSigns.Xml.BoardGeneratorBuildings;

namespace Klyte.WriteTheSigns.Rendering
{
    internal static class WTSPropRenderingRules
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
                TextType.DistanceFromReference,
                TextType.District,
                TextType.Park,
                TextType.DistrictOrPark,
                TextType.ParkOrDistrict,
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
               TextType.PlatformNumber,
            },
            //[TextRenderingClass.PlaceOnNet] = new TextType[]
            //{
            //   TextType.Fixed,

            //   TextType.StreetPrefix,
            //   TextType.StreetSuffix,
            //   TextType.StreetNameComplete,

            //   //TextType.NextExitText,
            //   //TextType.NextExitDestination1,
            //   //TextType.NextExitDestination2,
            //   //TextType.NextExitDestination3,
            //   //TextType.NextExitDestination4,
            //   //TextType.NextExitDistance,

            //   //TextType.Next2ExitText,
            //   //TextType.Next2ExitDestination1,
            //   //TextType.Next2ExitDestination2,
            //   //TextType.Next2ExitDestination3,
            //   //TextType.Next2ExitDestination4,
            //   //TextType.Next2ExitDistance,

            //},
        };


        public static void RenderPropMesh(ref PropInfo propInfo, RenderManager.CameraInfo cameraInfo, ushort refId, int boardIdx, int secIdx, int layerMask, float refAngleRad, Vector3 position, Vector4 dataVector, ref string propName, Vector3 propAngle, Vector3 propScale, BoardDescriptorGeneralXml propLayout, BoardInstanceXml descriptor, out Matrix4x4 propMatrix, out bool rendered, InstanceID propRenderID)
        {
            Color? propColor = WTSPropRenderingRules.GetColor(refId, boardIdx, secIdx, descriptor, propLayout);
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
                    if (propInfo.m_material.shader == WTSController.DISALLOWED_SHADER_PROP)
                    {
                        propInfo.m_material.shader = WTSController.DEFAULT_SHADER_TEXT;
                    }
                }
            }
            else
            {
                propInfo = null;
            }
            propMatrix = RenderUtils.RenderProp(refId, refAngleRad, cameraInfo, propInfo, propColor ?? Color.white, position, dataVector, boardIdx, propAngle, propScale, layerMask, out rendered, propRenderID);
        }


        public static void RenderTextMesh(ushort refID, int boardIdx, int secIdx, BoardInstanceXml descriptor, Matrix4x4 propMatrix, BoardDescriptorGeneralXml propLayout, ref BoardTextDescriptorGeneralXml textDescriptor, MaterialPropertyBlock materialPropertyBlock, DynamicSpriteFont baseFont, Camera targetCamera = null)
        {
            BasicRenderInformation renderInfo = GetTextMesh(baseFont, textDescriptor, refID, boardIdx, secIdx, descriptor, propLayout);
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
                    colorToSet = GetContrastColor(refID, boardIdx, secIdx, descriptor, propLayout);
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
                Material targetMaterial;
                switch (textDescriptor.MaterialType)
                {
                    default:
                    case FontStashSharp.MaterialType.OPAQUE:
                        targetMaterial = renderInfo.m_generatedMaterial;
                        break;
                    case FontStashSharp.MaterialType.DAYNIGHT:
                        targetMaterial = renderInfo.m_generatedMaterialDayNight;
                        break;
                    case FontStashSharp.MaterialType.BRIGHT:
                        targetMaterial = renderInfo.m_generatedMaterialBright;
                        break;
                }
                targetMaterial.shader = WTSController.DEFAULT_SHADER_TEXT;
                Graphics.DrawMesh(renderInfo.m_mesh, matrix, targetMaterial, 10, targetCamera, 0, materialPropertyBlock, false);
            }
        }

        internal static List<Matrix4x4> CalculateTextMatrix(BoardInstanceXml instance, BoardTextDescriptorGeneralXml textDescriptor, BasicRenderInformation renderInfo, bool centerReference = false)
        {
            var result = new List<Matrix4x4>();

            Matrix4x4 textMatrix = ApplyTextAdjustments(textDescriptor.m_textRelativePosition, textDescriptor.m_textRelativeRotation, renderInfo, instance.PropScale, textDescriptor.m_textScale, textDescriptor.m_textAlign, textDescriptor.m_maxWidthMeters, textDescriptor.m_applyOverflowResizingOnY, centerReference);

            result.Add(textMatrix);

            if (textDescriptor.m_create180degYClone)
            {
                UIHorizontalAlignment targetTextAlignment = textDescriptor.m_textAlign;
                if (textDescriptor.m_invertYCloneHorizontalAlign)
                {
                    targetTextAlignment = 2 - targetTextAlignment;
                }
                result.Add(ApplyTextAdjustments(new Vector3(textDescriptor.m_textRelativePosition.x, textDescriptor.m_textRelativePosition.y, -textDescriptor.m_textRelativePosition.z), textDescriptor.m_textRelativeRotation + new Vector3(0, 180), renderInfo, instance.PropScale, textDescriptor.m_textScale, targetTextAlignment, textDescriptor.m_maxWidthMeters, textDescriptor.m_applyOverflowResizingOnY, centerReference));
            }

            return result;
        }

        private static Matrix4x4 ApplyTextAdjustments(Vector3 textPosition, Vector3 textRotation, BasicRenderInformation renderInfo, Vector3 propScale, float textScale, UIHorizontalAlignment horizontalAlignment, float maxWidth, bool applyResizeOverflowOnY, bool centerReference)
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
            targetRelativePosition.y -= (renderInfo.m_YAxisOverflows.min + renderInfo.m_YAxisOverflows.max) / 2 * defaultMultiplierY * overflowScaleY;

            var textMatrix = Matrix4x4.TRS(
                targetRelativePosition,
               Quaternion.AngleAxis(textRotation.x, Vector3.left) * Quaternion.AngleAxis(textRotation.y, Vector3.down) * Quaternion.AngleAxis(textRotation.z, Vector3.back),
           centerReference ? new Vector3(SCALING_FACTOR, SCALING_FACTOR, SCALING_FACTOR) : new Vector3(defaultMultiplierX * overflowScaleX / propScale.x, defaultMultiplierY * overflowScaleY / propScale.y, 1));
            return textMatrix;
        }

        public static Color? GetColor(ushort refId, int boardIdx, int secIdx, BoardInstanceXml instance, BoardDescriptorGeneralXml propLayout)
        {

            if (instance is BoardInstanceRoadNodeXml)
            {
                return WTSRoadNodesData.Instance.BoardsContainers[refId, boardIdx, secIdx]?.m_cachedColor;
            }
            else if (instance is BoardInstanceBuildingXml buildingDescriptor)
            {
                switch (buildingDescriptor.ColorModeProp)
                {
                    case ColoringMode.Fixed:
                        return propLayout.FixedColor;
                    case ColoringMode.ByPlatform:
                        StopInformation stop = GetTargetStopInfo(buildingDescriptor, ref WTSBuildingsData.Instance.BoardsContainers[refId, 0, 0][boardIdx]);
                        if (stop.m_lineId != 0)
                        {
                            return TransportManager.instance.GetLineColor(stop.m_lineId);
                        }
                        if (!buildingDescriptor.m_showIfNoLine)
                        {
                            return null;
                        }
                        return Color.white;
                    case ColoringMode.ByDistrict:
                        byte districtId = DistrictManager.instance.GetDistrict(BuildingManager.instance.m_buildings.m_buffer[refId].m_position);
                        return WTSHookable.GetDistrictColor(districtId);
                    case ColoringMode.FromBuilding:
                        return BuildingManager.instance.m_buildings.m_buffer[refId].Info.m_buildingAI.GetColor(refId, ref BuildingManager.instance.m_buildings.m_buffer[refId], InfoManager.InfoMode.None);
                }
            }
            else if (instance is BoardPreviewInstanceXml preview)
            {
                return preview?.Descriptor?.FixedColor ?? GetCurrentSimulationColor();
            }
            return null;
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

        public static Color GetContrastColor(ushort refID, int boardIdx, int secIdx, BoardInstanceXml instance, BoardDescriptorGeneralXml propLayout)
        {
            if (instance is BoardInstanceRoadNodeXml)
            {
                return WTSRoadNodesData.Instance.BoardsContainers[refID, boardIdx, secIdx]?.m_cachedContrastColor ?? Color.black;
            }
            else if (instance is BoardPreviewInstanceXml preview)
            {
                return KlyteMonoUtils.ContrastColor(preview?.Descriptor?.FixedColor ?? GetCurrentSimulationColor());
            }
            return KlyteMonoUtils.ContrastColor(GetColor(refID, boardIdx, secIdx, instance, propLayout) ?? Color.white);
        }

        internal static BasicRenderInformation GetTextMesh(DynamicSpriteFont baseFont, BoardTextDescriptorGeneralXml textDescriptor, ushort refID, int boardIdx, int secIdx, BoardInstanceXml instance, BoardDescriptorGeneralXml propLayout)
        {
            if (instance is BoardPreviewInstanceXml preview)
            {
                if (!preview.m_overrideText.IsNullOrWhiteSpace())
                {
                    return RenderUtils.GetTextData(preview.m_overrideText, "", "", baseFont, textDescriptor.m_overrideFont ?? preview?.Descriptor?.FontName);
                }
                string otherText = "";
                if (textDescriptor.IsTextRelativeToSegment())
                {
                    otherText = $"({textDescriptor.m_destinationRelative}) ";
                }
                switch (textDescriptor.m_textType)
                {
                    case TextType.Fixed: return RenderUtils.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? preview?.Descriptor?.FontName);
                    case TextType.DistanceFromReference: return RenderUtils.GetTextData("00", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? preview?.Descriptor?.FontName);
                    case TextType.StreetSuffix: return RenderUtils.GetTextData($"{otherText}Suffix", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? preview?.Descriptor?.FontName);
                    case TextType.StreetPrefix: return RenderUtils.GetTextData($"{otherText}Pre.", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? preview?.Descriptor?.FontName);
                    case TextType.StreetNameComplete: return RenderUtils.GetTextData($"{otherText}Full road name", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? preview?.Descriptor?.FontName);
                    case TextType.District: return RenderUtils.GetTextData($"{otherText}District", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? preview?.Descriptor?.FontName);
                    case TextType.DistrictOrPark: return RenderUtils.GetTextData($"{otherText}District or Area", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? preview?.Descriptor?.FontName);
                    case TextType.ParkOrDistrict: return RenderUtils.GetTextData($"{otherText}Area or District", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? preview?.Descriptor?.FontName);
                    case TextType.Park: return RenderUtils.GetTextData($"{otherText}Area", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? preview?.Descriptor?.FontName);
                    case TextType.PlatformNumber: return RenderUtils.GetTextData("00", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? preview?.Descriptor?.FontName);

                    default:
                        string text = $"{textDescriptor.m_textType}: {preview.m_currentText}";
                        if (textDescriptor.m_allCaps)
                        {
                            text = text.ToUpper();
                        }
                        return RenderUtils.GetTextData(text, textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? preview?.Descriptor?.FontName);
                };
            }
            else if (instance is BoardInstanceBuildingXml buildingDescritpor)
            {
                ref BoardGeneratorBuildings.BoardBunchContainerBuilding data = ref WTSBuildingsData.Instance.BoardsContainers[refID, 0, 0][boardIdx];
                if (data == null)
                {
                    return null;
                }
                TextType targetType = textDescriptor.m_textType;
                switch (targetType)
                {
                    case TextType.Fixed: return RenderUtils.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? propLayout.FontName);
                    case TextType.OwnName: return RenderUtils.GetFromCacheArray(refID, textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.BuildingName, baseFont, textDescriptor.m_overrideFont ?? propLayout.FontName);
                    case TextType.NextStopLine: return RenderUtils.GetFromCacheArray(GetTargetStopInfo(buildingDescritpor, ref data).NextStopBuildingId, textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.BuildingName, baseFont, textDescriptor.m_overrideFont ?? propLayout.FontName);
                    case TextType.PrevStopLine: return RenderUtils.GetFromCacheArray(GetTargetStopInfo(buildingDescritpor, ref data).PrevStopBuildingId, textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.BuildingName, baseFont, textDescriptor.m_overrideFont ?? propLayout.FontName);
                    case TextType.LastStopLine: return RenderUtils.GetFromCacheArray(GetTargetStopInfo(buildingDescritpor, ref data).DestinationBuildingId, textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.BuildingName, baseFont, textDescriptor.m_overrideFont ?? propLayout.FontName);
                    case TextType.StreetPrefix: return RenderUtils.GetFromCacheArray(WTSBuildingDataCaches.GetBuildingMainAccessSegment(refID), textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.SuffixStreetNameAbbreviation, baseFont, textDescriptor.m_overrideFont ?? propLayout.FontName);
                    case TextType.StreetSuffix: return RenderUtils.GetFromCacheArray(WTSBuildingDataCaches.GetBuildingMainAccessSegment(refID), textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.StreetQualifier, baseFont, textDescriptor.m_overrideFont ?? propLayout.FontName);
                    case TextType.StreetNameComplete: return RenderUtils.GetFromCacheArray(WTSBuildingDataCaches.GetBuildingMainAccessSegment(refID), textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.FullStreetNameAbbreviation, baseFont, textDescriptor.m_overrideFont ?? propLayout.FontName);
                    case TextType.PlatformNumber: return RenderUtils.GetTextData((buildingDescritpor.m_platforms.FirstOrDefault() + 1).ToString(), textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? propLayout.FontName);
                    case TextType.LinesSymbols:
                    default:
                        return null;
                }
            }
            else if (instance is BoardInstanceRoadNodeXml roadDescritpor)
            {
                CacheRoadNodeItem data = WTSRoadNodesData.Instance.BoardsContainers[refID, boardIdx, secIdx];
                if (data == null)
                {
                    return null;
                }

                TextType targetType = textDescriptor.m_textType;
                if (textDescriptor.IsTextRelativeToSegment() && textDescriptor.m_destinationRelative >= 0)
                {
                    CacheDestinationRoute destination = null;
                    int segmentIdx = data.m_nodesOrder[(data.m_nodesOrder.Length + textDescriptor.m_targetNodeRelative) % data.m_nodesOrder.Length];
                    switch (textDescriptor.m_destinationRelative)
                    {
                        case DestinationReference.NextExitMainRoad1:
                        case DestinationReference.NextExitMainRoad2:
                            destination = WTSDestinationSingleton.instance.GetTargetDestination(refID, segmentIdx, (int)textDescriptor.m_destinationRelative);
                            break;
                        case DestinationReference.Next2ExitMainRoad1:
                        case DestinationReference.Next2ExitMainRoad2:
                        case DestinationReference.RoadEnd:
                        case DestinationReference.Next2Exit:
                        case DestinationReference.NextExit:
                            return null;
                        case DestinationReference.Self:
                            break;
                    }

                    if (WTSDestinationSingleton.instance.m_updatedDestinations[refID] == true && WTSDestinationSingleton.instance.m_couldReachDestinations[refID, segmentIdx, (int)textDescriptor.m_destinationRelative] == false)
                    {
                        LogUtils.DoWarnLog($"[n{refID}/{boardIdx}] REMOVING UNAVAILABLE {WTSRoadPropsSingleton.instance.Data.BoardsContainers[refID, boardIdx, secIdx] } (T={refID}, {segmentIdx}, {textDescriptor.m_destinationRelative} - rotationOrder = {string.Join(",", data.m_nodesOrder.Select(x => x.ToString()).ToArray())} - offset ={ textDescriptor.m_targetNodeRelative} )");

                        WTSRoadPropsSingleton.instance.Data.BoardsContainers[refID, boardIdx, secIdx] = null;
                        WTSRoadPropsSingleton.instance.m_updatedStreetPositions[refID] = null;
                        return null;
                    }
                    if (WTSDestinationSingleton.instance.m_updatedDestinations[refID] == false)
                    {
                        return RenderUtils.GetTextData("Loading" + new string('.', ((int)(SimulationManager.instance.m_currentTickIndex & 0x3F) >> 4) + 1), textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? roadDescritpor.Descriptor.FontName);
                    }
                    if (destination != null)
                    {
                        switch (targetType)
                        {
                            case TextType.ParkOrDistrict: targetType = destination.m_parkId > 0 ? TextType.Park : TextType.District; break;
                            case TextType.DistrictOrPark: targetType = destination.m_districtId == 0 && destination.m_parkId > 0 ? TextType.Park : TextType.District; break;
                            case TextType.Park:
                                if (destination.m_parkId == 0)
                                {
                                    return null;
                                }
                                break;
                        }
                        return targetType switch
                        {
                            TextType.DistanceFromReference => RenderUtils.GetTextData(destination.m_distanceMeanString, textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? roadDescritpor.Descriptor.FontName),
                            TextType.StreetSuffix => RenderUtils.GetFromCacheArray(destination.m_segmentId, textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, roadDescritpor.ApplyAbreviationsOnFullName ? RenderUtils.CacheArrayTypes.SuffixStreetNameAbbreviation : RenderUtils.CacheArrayTypes.SuffixStreetName, baseFont, textDescriptor.m_overrideFont ?? roadDescritpor.Descriptor.FontName),
                            TextType.StreetPrefix => RenderUtils.GetFromCacheArray(destination.m_segmentId, textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.StreetQualifier, baseFont, textDescriptor.m_overrideFont ?? roadDescritpor.Descriptor.FontName),
                            TextType.StreetNameComplete => RenderUtils.GetFromCacheArray(destination.m_segmentId, textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, roadDescritpor.ApplyAbreviationsOnFullName ? RenderUtils.CacheArrayTypes.FullStreetNameAbbreviation : RenderUtils.CacheArrayTypes.FullStreetName, baseFont, textDescriptor.m_overrideFont ?? roadDescritpor.Descriptor.FontName),
                            TextType.District => RenderUtils.GetFromCacheArray(destination.m_districtId, textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.Districts, baseFont, textDescriptor.m_overrideFont ?? roadDescritpor.Descriptor.FontName),
                            TextType.Park => RenderUtils.GetFromCacheArray(destination.m_parkId, textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.Parks, baseFont, textDescriptor.m_overrideFont ?? roadDescritpor.Descriptor.FontName),


                            _ => null,
                        };

                    }
                }
                switch (targetType)
                {
                    case TextType.ParkOrDistrict: targetType = data.m_districtParkId > 0 ? TextType.Park : TextType.District; break;
                    case TextType.DistrictOrPark: targetType = data.m_districtId == 0 && data.m_districtParkId > 0 ? TextType.Park : TextType.District; break;
                    case TextType.Park:
                        if (data.m_districtParkId == 0)
                        {
                            return null;
                        }
                        break;
                }
                return targetType switch
                {
                    TextType.DistanceFromReference => RenderUtils.GetTextData($"{data.m_distanceRefKm}", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? roadDescritpor.Descriptor.FontName),
                    TextType.Fixed => RenderUtils.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? roadDescritpor.Descriptor.FontName),
                    TextType.StreetSuffix => RenderUtils.GetFromCacheArray(data.m_segmentId, textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, roadDescritpor.ApplyAbreviationsOnFullName ? RenderUtils.CacheArrayTypes.SuffixStreetNameAbbreviation : RenderUtils.CacheArrayTypes.SuffixStreetName, baseFont, textDescriptor.m_overrideFont ?? roadDescritpor.Descriptor.FontName),
                    TextType.StreetPrefix => RenderUtils.GetFromCacheArray(data.m_segmentId, textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.StreetQualifier, baseFont, textDescriptor.m_overrideFont ?? roadDescritpor.Descriptor.FontName),
                    TextType.StreetNameComplete => RenderUtils.GetFromCacheArray(data.m_segmentId, textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, roadDescritpor.ApplyAbreviationsOnFullName ? RenderUtils.CacheArrayTypes.FullStreetNameAbbreviation : RenderUtils.CacheArrayTypes.FullStreetName, baseFont, textDescriptor.m_overrideFont ?? roadDescritpor.Descriptor.FontName),
                    TextType.District => RenderUtils.GetFromCacheArray(data.m_districtId, textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.Districts, baseFont, textDescriptor.m_overrideFont ?? roadDescritpor.Descriptor.FontName),
                    TextType.Park => RenderUtils.GetFromCacheArray(data.m_districtParkId, textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, RenderUtils.CacheArrayTypes.Parks, baseFont, textDescriptor.m_overrideFont ?? roadDescritpor.Descriptor.FontName),


                    _ => null,
                };
            }
            return RenderUtils.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont ?? WTSController.DEFAULT_FONT_KEY);
        }


        private static StopInformation GetTargetStopInfo(BoardInstanceBuildingXml descriptor, ref BoardBunchContainerBuilding cache)
        {
            foreach (int platform in descriptor.m_platforms)
            {
                if (cache.m_platformToLine != null && cache.m_platformToLine.ElementAtOrDefault(platform) != null)
                {
                    StopInformation line = cache.m_platformToLine[platform].ElementAtOrDefault(0);
                    if (line.m_lineId != 0)
                    {
                        return line;
                    }
                }
            }
            return default;
        }
    }

}