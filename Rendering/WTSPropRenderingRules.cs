using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Singleton;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using SpriteFontPlus;
using SpriteFontPlus.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
                TextType.PostalCode,
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
            Color propColor = WTSPropRenderingRules.GetColor(refId, boardIdx, secIdx, descriptor, propLayout, out bool colorFound);
            if (!colorFound)
            {
                rendered = false;
                propMatrix = new Matrix4x4();
                return;
            }
            propColor.a = 1;

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
                        propInfo.m_material.shader = WTSController.REPLACEMENT_SHADER_PROP;
                    }
                }
            }
            else
            {
                propInfo = null;
            }
            propMatrix = RenderUtils.RenderProp(refId, refAngleRad, cameraInfo, propInfo, propColor, position, dataVector, boardIdx, propAngle, propScale, layerMask, out rendered, propRenderID);
        }


        public static void RenderTextMesh(ushort refID, int boardIdx, int secIdx, BoardInstanceXml descriptor, Matrix4x4 propMatrix, BoardDescriptorGeneralXml propLayout, ref BoardTextDescriptorGeneralXml textDescriptor, MaterialPropertyBlock materialPropertyBlock, Camera targetCamera = null, Shader overrideShader = null)
        {
            BasicRenderInformation renderInfo = GetTextMesh(textDescriptor, refID, boardIdx, secIdx, descriptor, propLayout, out IEnumerable<BasicRenderInformation> multipleOutput);
            if (renderInfo == null)
            {
                if (multipleOutput != null && multipleOutput.Count() > 0)
                {
                    BasicRenderInformation[] resultArray = multipleOutput.ToArray();
                    if (resultArray.Length == 1)
                    {
                        renderInfo = multipleOutput.First();
                    }
                    else
                    {
                        BoardTextDescriptorGeneralXml.SubItemSettings settings = textDescriptor.MultiItemSettings;
                        int targetCount = Math.Min(settings.SubItemsPerRow * settings.SubItemsPerColumn, resultArray.Length);
                        int maxItemsInARow, maxItemsInAColumn, lastRowOrColumnItemCount;

                        if (textDescriptor.MultiItemSettings.VerticalFirst)
                        {
                            maxItemsInAColumn = Math.Min(targetCount, settings.SubItemsPerColumn);
                            maxItemsInARow = Mathf.CeilToInt((float)targetCount / settings.SubItemsPerColumn);
                            lastRowOrColumnItemCount = targetCount % settings.SubItemsPerColumn;
                        }
                        else
                        {
                            maxItemsInARow = Math.Min(targetCount, settings.SubItemsPerRow);
                            maxItemsInAColumn = Mathf.CeilToInt((float)targetCount / settings.SubItemsPerRow);
                            lastRowOrColumnItemCount = targetCount % settings.SubItemsPerRow;
                        }
                        float unscaledColumnWidth = resultArray.Max(x => x.m_sizeMetersUnscaled.x);
                        float rowHeight = resultArray.Max(x => x.m_sizeMetersUnscaled.y) * textDescriptor.m_textScale * SCALING_FACTOR + textDescriptor.MultiItemSettings.SubItemSpacing.Y;
                        float columnWidth = (textDescriptor.m_maxWidthMeters > 0 ? Mathf.Min(textDescriptor.m_maxWidthMeters / maxItemsInARow, unscaledColumnWidth * SCALING_FACTOR) : unscaledColumnWidth * SCALING_FACTOR) * textDescriptor.m_textScale + textDescriptor.MultiItemSettings.SubItemSpacing.X;


                        var startPoint = new Vector3(textDescriptor.PlacingConfig.Position.X + columnWidth * (maxItemsInARow - 1) / 2f, textDescriptor.PlacingConfig.Position.Y, textDescriptor.PlacingConfig.Position.Z);
                        Vector3 lastRowOrColumnStartPoint;
                        if (textDescriptor.MultiItemSettings.VerticalFirst)
                        {
                            lastRowOrColumnStartPoint = new Vector3(startPoint.x, textDescriptor.PlacingConfig.Position.Y - (settings.SubItemsPerColumn - lastRowOrColumnItemCount) / 2f * rowHeight, startPoint.z);
                        }
                        else
                        {
                            lastRowOrColumnStartPoint = new Vector3(textDescriptor.PlacingConfig.Position.X + columnWidth * (lastRowOrColumnItemCount - 1) / 2f, startPoint.y, startPoint.z);
                        }
                        Color colorToSet = GetTargetColor(refID, boardIdx, secIdx, descriptor, propLayout, textDescriptor);
                        //LogUtils.DoWarnLog($"sz = {resultArray.Length};targetCount = {targetCount}; origPos = {textDescriptor.PlacingConfig.Position}; maxItemsInAColumn = {maxItemsInAColumn}; maxItemsInARow = {maxItemsInARow};columnWidth={columnWidth};rowHeight={rowHeight}");
                        int firstItemIdxLastRowOrColumn = targetCount - lastRowOrColumnItemCount;
                        for (int i = 0; i < targetCount; i++)
                        {
                            int x, y;
                            if (textDescriptor.MultiItemSettings.VerticalFirst)
                            {
                                y = i % settings.SubItemsPerColumn;
                                x = i / settings.SubItemsPerColumn;
                            }
                            else
                            {
                                x = i % settings.SubItemsPerRow;
                                y = i / settings.SubItemsPerRow;
                            }

                            BasicRenderInformation currentItem = resultArray[i];
                            Vector3 targetPosA = (i >= firstItemIdxLastRowOrColumn ? lastRowOrColumnStartPoint : startPoint) - new Vector3(columnWidth * x, rowHeight * y);
                            DrawTextBri(refID, boardIdx, secIdx, propMatrix, textDescriptor, materialPropertyBlock, currentItem, colorToSet, targetPosA, textDescriptor.PlacingConfig.Rotation, descriptor.PropScale, false, targetCamera, overrideShader);
                            if (textDescriptor.PlacingConfig.m_create180degYClone)
                            {
                                targetPosA = startPoint - new Vector3(columnWidth * (maxItemsInARow - x), rowHeight * y);
                                targetPosA.z *= -1;
                                DrawTextBri(refID, boardIdx, secIdx, propMatrix, textDescriptor, materialPropertyBlock, currentItem, colorToSet, targetPosA, textDescriptor.PlacingConfig.Rotation + new Vector3(0, 180), descriptor.PropScale, false, targetCamera, overrideShader);
                            }
                        }
                    }
                }
                else
                {
                    return;
                }
            }
            if (renderInfo?.m_mesh == null || renderInfo?.m_generatedMaterial == null)
            {
                return;
            }

            Vector3 targetPos = textDescriptor.PlacingConfig.Position;


            DrawTextBri(refID, boardIdx, secIdx, propMatrix, textDescriptor, materialPropertyBlock, renderInfo, GetTargetColor(refID, boardIdx, secIdx, descriptor, propLayout, textDescriptor), targetPos, textDescriptor.PlacingConfig.Rotation, descriptor.PropScale, textDescriptor.PlacingConfig.m_create180degYClone, targetCamera, overrideShader);
        }

        private static Color GetTargetColor(ushort refID, int boardIdx, int secIdx, BoardInstanceXml descriptor, BoardDescriptorGeneralXml propLayout, BoardTextDescriptorGeneralXml textDescriptor)
        {
            if (textDescriptor.ColoringConfig.m_useContrastColor)
            {
                return GetContrastColor(refID, boardIdx, secIdx, descriptor, propLayout);
            }
            else if (textDescriptor.ColoringConfig.m_defaultColor != null)
            {
                return textDescriptor.ColoringConfig.m_defaultColor;
            }
            return Color.white;
        }

        public static void DrawTextBri(ushort refID, int boardIdx, int secIdx, Matrix4x4 propMatrix, BoardTextDescriptorGeneralXml textDescriptor,
            MaterialPropertyBlock materialPropertyBlock, BasicRenderInformation renderInfo, Color colorToSet, Vector3 taregetPos, Vector3 taregetRotation,
            Vector3 baseScale, bool placeClone180Y, Camera targetCamera = null, Shader overrideShader = null)
        {
            List<Matrix4x4> textMatrixes = CalculateTextMatrix(taregetPos, taregetRotation, baseScale, textDescriptor, renderInfo, placeClone180Y);

            foreach (Matrix4x4 textMatrix in textMatrixes)
            {
                Matrix4x4 matrix = propMatrix * textMatrix;

                materialPropertyBlock.Clear();
                materialPropertyBlock.SetColor(SHADER_PROP_COLOR, colorToSet);
                materialPropertyBlock.SetColor(SHADER_PROP_COLOR0, colorToSet);
                materialPropertyBlock.SetColor(SHADER_PROP_COLOR1, colorToSet);
                materialPropertyBlock.SetColor(SHADER_PROP_COLOR2, colorToSet);
                materialPropertyBlock.SetColor(SHADER_PROP_COLOR3, colorToSet);

                var objectIndex = new Vector4();

                Material targetMaterial = renderInfo.m_generatedMaterial;
                var randomizer = new Randomizer((refID << 8) + (boardIdx << 2) + secIdx);
                switch (textDescriptor.ColoringConfig.MaterialType)
                {
                    default:
                    case FontStashSharp.MaterialType.OPAQUE:
                        objectIndex.z = 0;
                        break;
                    case FontStashSharp.MaterialType.DAYNIGHT:
                        float num = m_daynightOffTime + (randomizer.Int32(100000u) * 1E-05f);
                        objectIndex.z = MathUtils.SmoothStep(num + 0.01f, num - 0.01f, Singleton<RenderManager>.instance.lightSystem.DayLightIntensity) * textDescriptor.ColoringConfig.IlluminationStrength;
                        break;
                    case FontStashSharp.MaterialType.BRIGHT:
                        objectIndex.z = textDescriptor.ColoringConfig.IlluminationStrength;
                        break;
                }

                if (objectIndex.z > 0 && textDescriptor.ColoringConfig.BlinkType != BlinkType.None)
                {
                    float num = m_daynightOffTime + (randomizer.Int32(100000u) * 1E-05f);
                    Vector4 blinkVector;
                    if (textDescriptor.ColoringConfig.BlinkType == BlinkType.Custom)
                    {
                        blinkVector = textDescriptor.ColoringConfig.CustomBlink;
                    }
                    else
                    {
                        blinkVector = LightEffect.GetBlinkVector((LightEffect.BlinkType)textDescriptor.ColoringConfig.BlinkType);
                    }
                    float num2 = num * 3.71f + Singleton<SimulationManager>.instance.m_simulationTimer / blinkVector.w;
                    num2 = (num2 - Mathf.Floor(num2)) * blinkVector.w;
                    float num3 = MathUtils.SmoothStep(blinkVector.x, blinkVector.y, num2);
                    float num4 = MathUtils.SmoothStep(blinkVector.w, blinkVector.z, num2);
                    objectIndex.z *= 1f - (num3 * num4);
                }

                materialPropertyBlock.SetVector(PropManager.instance.ID_ObjectIndex, objectIndex);
                targetMaterial.shader = overrideShader ?? WTSController.DEFAULT_SHADER_TEXT;
                Graphics.DrawMesh(renderInfo.m_mesh, matrix, targetMaterial, 10, targetCamera, 0, materialPropertyBlock, false);

            }
        }

        private static readonly float m_daynightOffTime = 6 * Convert.ToSingle(Math.Pow(Convert.ToDouble((6 - (15 / 2.5)) / 6), Convert.ToDouble(1 / 1.09)));

        internal static List<Matrix4x4> CalculateTextMatrix(Vector3 targetPosition, Vector3 targetRotation, Vector3 baseScale, BoardTextDescriptorGeneralXml textDescriptor, BasicRenderInformation renderInfo, bool placeClone180Y, bool centerReference = false)
        {
            var result = new List<Matrix4x4>();
            if (renderInfo == null)
            {
                return result;
            }

            Matrix4x4 textMatrix = ApplyTextAdjustments(targetPosition, targetRotation, renderInfo, baseScale, textDescriptor.m_textScale, textDescriptor.m_textAlign, textDescriptor.m_maxWidthMeters, textDescriptor.m_applyOverflowResizingOnY, centerReference);

            result.Add(textMatrix);

            if (placeClone180Y)
            {
                UIHorizontalAlignment targetTextAlignment = textDescriptor.m_textAlign;
                if (textDescriptor.PlacingConfig.m_invertYCloneHorizontalAlign)
                {
                    targetTextAlignment = 2 - targetTextAlignment;
                }
                result.Add(ApplyTextAdjustments(new Vector3(targetPosition.x, targetPosition.y, -targetPosition.z), targetRotation + new Vector3(0, 180), renderInfo, baseScale, textDescriptor.m_textScale, targetTextAlignment, textDescriptor.m_maxWidthMeters, textDescriptor.m_applyOverflowResizingOnY, centerReference));
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
            Vector3 targetRelativePosition = textPosition;
            //LogUtils.DoWarnLog($"[{renderInfo},{refID},{boardIdx},{secIdx}] realWidth = {realWidth}; realHeight = {realHeight};");
            var rotationMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(textRotation.x, Vector3.left) * Quaternion.AngleAxis(textRotation.y, Vector3.down) * Quaternion.AngleAxis(textRotation.z, Vector3.back), Vector3.one);
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
                    float factor = horizontalAlignment == UIHorizontalAlignment.Left ? 0.5f : -0.5f;
                    targetRelativePosition += rotationMatrix.MultiplyPoint(new Vector3((maxWidth - realWidth) * factor, 0, 0));
                }
            }
            targetRelativePosition += rotationMatrix.MultiplyPoint(new Vector3(0, -(renderInfo.m_YAxisOverflows.min + renderInfo.m_YAxisOverflows.max) / 2 * defaultMultiplierY * overflowScaleY));

            Matrix4x4 textMatrix =
                Matrix4x4.Translate(targetRelativePosition) *
                rotationMatrix *
                Matrix4x4.Scale(centerReference ? new Vector3(SCALING_FACTOR, SCALING_FACTOR, SCALING_FACTOR) : new Vector3(defaultMultiplierX * overflowScaleX / propScale.x, defaultMultiplierY * overflowScaleY / propScale.y, 1)) * Matrix4x4.Scale(propScale)
               ;
            return textMatrix;
        }

        public static Color GetColor(ushort refId, int boardIdx, int secIdx, BoardInstanceXml instance, BoardDescriptorGeneralXml propLayout, out bool found)
        {

            if (instance is BoardInstanceRoadNodeXml)
            {
                found = WTSRoadNodesData.Instance.BoardsContainers[refId, boardIdx, secIdx] != null;
                return WTSRoadNodesData.Instance.BoardsContainers[refId, boardIdx, secIdx]?.m_cachedColor ?? default;
            }
            else if (instance is BoardInstanceBuildingXml buildingDescriptor)
            {
                found = true;
                switch (buildingDescriptor.ColorModeProp)
                {
                    case ColoringMode.Fixed:
                        return propLayout.FixedColor ?? default;
                    case ColoringMode.ByPlatform:
                        StopInformation stop = GetTargetStopInfo(buildingDescriptor, refId).FirstOrDefault();
                        if (stop.m_lineId != 0)
                        {
                            return TransportManager.instance.GetLineColor(stop.m_lineId);
                        }
                        if (!buildingDescriptor.m_showIfNoLine)
                        {
                            found = false;
                            return default;
                        }
                        return Color.white;
                    case ColoringMode.ByDistrict:
                        byte districtId = DistrictManager.instance.GetDistrict(BuildingManager.instance.m_buildings.m_buffer[refId].m_position);
                        return WriteTheSignsMod.Controller.ConnectorADR.GetDistrictColor(districtId);
                    case ColoringMode.FromBuilding:
                        return BuildingManager.instance.m_buildings.m_buffer[refId].Info.m_buildingAI.GetColor(refId, ref BuildingManager.instance.m_buildings.m_buffer[refId], InfoManager.InfoMode.None);
                }
            }
            else if (instance is BoardPreviewInstanceXml preview)
            {
                found = true;
                return preview?.Descriptor?.FixedColor ?? GetCurrentSimulationColor();
            }
            found = false;
            return default;
        }

        private static readonly Color[] m_spectreSteps = new Color[]
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
            var targetColor = GetColor(refID, boardIdx, secIdx, instance, propLayout, out bool colorFound);
            return KlyteMonoUtils.ContrastColor(colorFound ? targetColor : Color.white);
        }

        internal static BasicRenderInformation GetTextMesh(BoardTextDescriptorGeneralXml textDescriptor, ushort refID, int boardIdx, int secIdx, BoardInstanceXml instance, BoardDescriptorGeneralXml propLayout, out IEnumerable<BasicRenderInformation> multipleOutput)
        {
            multipleOutput = null;
            DynamicSpriteFont baseFont = FontServer.instance[propLayout.FontName];
            if (instance is BoardPreviewInstanceXml preview)
            {
                if (!preview.m_overrideText.IsNullOrWhiteSpace())
                {
                    return RenderUtils.GetTextData(preview.m_overrideText, "", "", FontServer.instance[preview?.Descriptor?.FontName], textDescriptor.m_overrideFont);
                }
                string otherText = "";
                if (textDescriptor.IsTextRelativeToSegment())
                {
                    otherText = $"({textDescriptor.m_destinationRelative}) ";
                }

                switch (propLayout.m_allowedRenderClass)
                {
                    case TextRenderingClass.RoadNodes:
                        baseFont ??= FontServer.instance[WTSRoadNodesData.Instance.DefaultFont];
                        break;
                    case TextRenderingClass.MileageMarker:
                        baseFont ??= FontServer.instance[WTSRoadNodesData.Instance.DefaultFont];
                        break;
                    case TextRenderingClass.Buildings:
                        baseFont ??= FontServer.instance[WTSBuildingsData.Instance.DefaultFont];
                        break;
                }

                switch (textDescriptor.m_textType)
                {
                    case TextType.Fixed: return RenderUtils.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                    case TextType.DistanceFromReference: return RenderUtils.GetTextData("00", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                    case TextType.PostalCode: return RenderUtils.GetTextData("00000", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                    case TextType.StreetSuffix: return RenderUtils.GetTextData($"{otherText}Suffix", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                    case TextType.StreetPrefix: return RenderUtils.GetTextData($"{otherText}Pre.", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                    case TextType.StreetNameComplete: return RenderUtils.GetTextData($"{otherText}Full road name", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                    case TextType.District: return RenderUtils.GetTextData($"{otherText}District", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                    case TextType.DistrictOrPark: return RenderUtils.GetTextData($"{otherText}District or Area", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                    case TextType.ParkOrDistrict: return RenderUtils.GetTextData($"{otherText}Area or District", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                    case TextType.Park: return RenderUtils.GetTextData($"{otherText}Area", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                    case TextType.PlatformNumber: return RenderUtils.GetTextData("00", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                    case TextType.LinesSymbols:
                        multipleOutput = WriteTheSignsMod.Controller.TransportLineRenderingRules.DrawLineFormats(new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, Vector3.one);

                        return null;
                    default:
                        string text = $"{textDescriptor.m_textType}: {preview.m_currentText}";
                        if (textDescriptor.m_allCaps)
                        {
                            text = text.ToUpper();
                        }
                        return RenderUtils.GetTextData(text, textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                };
            }
            else if (instance is BoardInstanceBuildingXml buildingDescritpor)
            {
                ref BoardBunchContainerBuilding data = ref WTSBuildingsData.Instance.BoardsContainers[refID, 0, 0][boardIdx];
                if (data == null)
                {
                    return null;
                }
                baseFont ??= FontServer.instance[WTSBuildingsData.Instance.DefaultFont];
                TextType targetType = textDescriptor.m_textType;
                switch (targetType)
                {
                    case TextType.Fixed: return RenderUtils.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                    case TextType.OwnName: return GetFromCacheArray(refID, textDescriptor, RenderUtils.CacheArrayTypes.BuildingName, baseFont);
                    case TextType.NextStopLine: return GetFromCacheArray(GetTargetStopInfo(buildingDescritpor, refID).FirstOrDefault().NextStopBuildingId, textDescriptor, RenderUtils.CacheArrayTypes.BuildingName, baseFont);
                    case TextType.PrevStopLine: return GetFromCacheArray(GetTargetStopInfo(buildingDescritpor, refID).FirstOrDefault().PrevStopBuildingId, textDescriptor, RenderUtils.CacheArrayTypes.BuildingName, baseFont);
                    case TextType.LastStopLine: return GetFromCacheArray(GetTargetStopInfo(buildingDescritpor, refID).FirstOrDefault().DestinationBuildingId, textDescriptor, RenderUtils.CacheArrayTypes.BuildingName, baseFont);
                    case TextType.StreetPrefix: return GetFromCacheArray(WTSBuildingDataCaches.GetBuildingMainAccessSegment(refID), textDescriptor, RenderUtils.CacheArrayTypes.StreetQualifier, baseFont);
                    case TextType.StreetSuffix: return GetFromCacheArray(WTSBuildingDataCaches.GetBuildingMainAccessSegment(refID), textDescriptor, RenderUtils.CacheArrayTypes.SuffixStreetName, baseFont);
                    case TextType.StreetNameComplete: return GetFromCacheArray(WTSBuildingDataCaches.GetBuildingMainAccessSegment(refID), textDescriptor, RenderUtils.CacheArrayTypes.FullStreetName, baseFont);
                    case TextType.PlatformNumber: return RenderUtils.GetTextData((buildingDescritpor.m_platforms.FirstOrDefault() + 1).ToString(), textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                    case TextType.LinesSymbols:
                        multipleOutput = WriteTheSignsMod.Controller.TransportLineRenderingRules.DrawLineFormats(GetAllTargetStopInfo(buildingDescritpor, refID).GroupBy(x => x.m_lineId).Select(x => x.First()).Select(x => x.m_lineId), Vector3.one);
                        return null;
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

                baseFont ??= FontServer.instance[WTSRoadNodesData.Instance.DefaultFont];

                TextType targetType = textDescriptor.m_textType;
                if (textDescriptor.IsTextRelativeToSegment() && textDescriptor.m_destinationRelative >= 0)
                {
                    CacheDestinationRoute destination = null;
                    int segmentIdx = data.m_nodesOrder[(data.m_nodesOrder.Length + textDescriptor.m_targetNodeRelative) % data.m_nodesOrder.Length];
                    switch (textDescriptor.m_destinationRelative)
                    {
                        case DestinationReference.NextExitMainRoad1:
                        case DestinationReference.NextExitMainRoad2:
                            destination = WriteTheSignsMod.Controller.DestinationSingleton.GetTargetDestination(refID, segmentIdx, (int)textDescriptor.m_destinationRelative);
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

                    if (WriteTheSignsMod.Controller.DestinationSingleton.m_updatedDestinations[refID] == true && WriteTheSignsMod.Controller.DestinationSingleton.m_couldReachDestinations[refID, segmentIdx, (int)textDescriptor.m_destinationRelative] == false)
                    {
                        LogUtils.DoWarnLog($"[n{refID}/{boardIdx}] REMOVING UNAVAILABLE {WriteTheSignsMod.Controller.RoadPropsSingleton.Data.BoardsContainers[refID, boardIdx, secIdx] } (T={refID}, {segmentIdx}, {textDescriptor.m_destinationRelative} - rotationOrder = {string.Join(",", data.m_nodesOrder.Select(x => x.ToString()).ToArray())} - offset ={ textDescriptor.m_targetNodeRelative} )");

                        WriteTheSignsMod.Controller.RoadPropsSingleton.Data.BoardsContainers[refID, boardIdx, secIdx] = null;
                        WriteTheSignsMod.Controller.RoadPropsSingleton.m_updatedStreetPositions[refID] = null;
                        return null;
                    }
                    if (WriteTheSignsMod.Controller.DestinationSingleton.m_updatedDestinations[refID] == false)
                    {
                        return RenderUtils.GetTextData("Loading" + new string('.', ((int)(SimulationManager.instance.m_currentTickIndex & 0x3F) >> 4) + 1), textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
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
                            TextType.DistanceFromReference => RenderUtils.GetTextData(destination.m_distanceMeanString, textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont),
                            TextType.StreetSuffix => GetFromCacheArray(destination.m_segmentId, textDescriptor, RenderUtils.CacheArrayTypes.SuffixStreetName, baseFont),
                            TextType.StreetPrefix => GetFromCacheArray(destination.m_segmentId, textDescriptor, RenderUtils.CacheArrayTypes.StreetQualifier, baseFont),
                            TextType.PostalCode => GetFromCacheArray(destination.m_segmentId, textDescriptor, RenderUtils.CacheArrayTypes.PostalCode, baseFont),
                            TextType.StreetNameComplete => GetFromCacheArray(destination.m_segmentId, textDescriptor, RenderUtils.CacheArrayTypes.FullStreetName, baseFont),
                            TextType.District => GetFromCacheArray(destination.m_districtId, textDescriptor, RenderUtils.CacheArrayTypes.Districts, baseFont),
                            TextType.Park => GetFromCacheArray(destination.m_parkId, textDescriptor, RenderUtils.CacheArrayTypes.Parks, baseFont),


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
                    TextType.DistanceFromReference => RenderUtils.GetTextData($"{data.m_distanceRefKm}", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont),
                    TextType.Fixed => RenderUtils.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont),
                    TextType.StreetSuffix => GetFromCacheArray(data.m_segmentId, textDescriptor, RenderUtils.CacheArrayTypes.SuffixStreetName, baseFont),
                    TextType.StreetNameComplete => GetFromCacheArray(data.m_segmentId, textDescriptor, RenderUtils.CacheArrayTypes.FullStreetName, baseFont),
                    TextType.StreetPrefix => GetFromCacheArray(data.m_segmentId, textDescriptor, RenderUtils.CacheArrayTypes.StreetQualifier, baseFont),
                    TextType.District => GetFromCacheArray(data.m_districtId, textDescriptor, RenderUtils.CacheArrayTypes.Districts, baseFont),
                    TextType.Park => GetFromCacheArray(data.m_districtParkId, textDescriptor, RenderUtils.CacheArrayTypes.Parks, baseFont),
                    TextType.PostalCode => GetFromCacheArray(data.m_segmentId, textDescriptor, RenderUtils.CacheArrayTypes.PostalCode, baseFont),

                    _ => null,
                };
            }
            return RenderUtils.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, null, textDescriptor.m_overrideFont);
        }


        private static StopInformation[] GetTargetStopInfo(BoardInstanceBuildingXml descriptor, ushort buildingId)
        {
            foreach (int platform in descriptor.m_platforms)
            {
                if (WriteTheSignsMod.Controller.BuildingPropsSingleton.m_platformToLine[buildingId] != null && WriteTheSignsMod.Controller.BuildingPropsSingleton.m_platformToLine[buildingId].ElementAtOrDefault(platform)?.Length > 0)
                {
                    StopInformation[] line = WriteTheSignsMod.Controller.BuildingPropsSingleton.m_platformToLine[buildingId][platform];
                    return line;
                }
            }
            return m_emptyInfo;
        }
        private static StopInformation[] GetAllTargetStopInfo(BoardInstanceBuildingXml descriptor, ushort buildingId)
        {
            return descriptor?.m_platforms?.SelectMany(platform =>
            {
                if (WriteTheSignsMod.Controller.BuildingPropsSingleton.m_platformToLine[buildingId] != null && WriteTheSignsMod.Controller.BuildingPropsSingleton.m_platformToLine[buildingId]?.ElementAtOrDefault(platform)?.Length > 0)
                {
                    StopInformation[] line = WriteTheSignsMod.Controller.BuildingPropsSingleton.m_platformToLine[buildingId][platform];
                    return line;
                }
                return new StopInformation[0];
            }).ToArray();
        }

        private static readonly StopInformation[] m_emptyInfo = new StopInformation[0];


        private static BasicRenderInformation GetFromCacheArray(ushort reference, BoardTextDescriptorGeneralXml textDescriptor, RenderUtils.CacheArrayTypes cacheType, DynamicSpriteFont baseFont)
            => RenderUtils.GetFromCacheArray2(reference, textDescriptor.m_prefix, textDescriptor.m_suffix, textDescriptor.m_allCaps, textDescriptor.m_applyAbbreviations, cacheType, baseFont, textDescriptor.m_overrideFont);


    }



}
