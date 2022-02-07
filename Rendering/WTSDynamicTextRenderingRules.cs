﻿using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons.UI.Sprites;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Singleton;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using SpriteFontPlus.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.WriteTheSigns.Rendering
{
    internal static class WTSDynamicTextRenderingRules
    {
        public const float SCALING_FACTOR = 0.005f;
        public static readonly int SHADER_PROP_COLOR = Shader.PropertyToID("_Color");
        private static readonly float m_daynightOffTime = 6 * Convert.ToSingle(Math.Pow(Convert.ToDouble((6 - (15 / 2.5)) / 6), Convert.ToDouble(1 / 1.09)));

        public static readonly TextType[] ALLOWED_TYPES_VEHICLE = new TextType[]
        {
            TextType.Fixed,
            TextType.GameSprite,
            TextType.OwnName,
            TextType.LinesSymbols,
            TextType.LineIdentifier,
            TextType.NextStopLine,
            TextType.PrevStopLine,
            TextType.LastStopLine,
            TextType.LineFullName,
            TextType.CityName,
        };
        public static readonly TextType[] ALLOWED_TYPES_HIGHWAY_SHIELDS = new TextType[]
        {
            TextType.Fixed,
            TextType.GameSprite,
            TextType.CityName,
            TextType.HwCodeShort,
            TextType.HwCodeLong,
            TextType.HwDettachedPrefix,
            TextType.HwIdentifierSuffix,
         };

        internal static Material m_rotorMaterial;
        internal static Material m_outsideMaterial;

        private static Mesh m_genMesh;
        private static Mesh m_genMeshInv;
        private static Mesh m_genMeshGlass;


        public static readonly Dictionary<TextRenderingClass, TextType[]> ALLOWED_TYPES_PER_RENDERING_CLASS = new Dictionary<TextRenderingClass, TextType[]>
        {
            [TextRenderingClass.RoadNodes] = new TextType[]
            {
                TextType.Fixed,
                TextType.GameSprite,
                TextType.StreetPrefix,
                TextType.StreetSuffix,
                TextType.StreetNameComplete,
                TextType.DistanceFromReference,
                TextType.PostalCode,
                TextType.District,
                TextType.Park,
                TextType.DistrictOrPark,
                TextType.ParkOrDistrict,
                TextType.CityName
            },
            [TextRenderingClass.Buildings] = new TextType[]
            {
                TextType.Fixed,
                TextType.GameSprite,
                TextType.ParameterizedGameSprite,
                TextType.ParameterizedGameSpriteIndexed,
                TextType.ParameterizedText,
                TextType.OwnName,
                TextType.LinesSymbols,
                TextType.LineFullName,
                TextType.NextStopLine, // Next Station Line 1
                TextType.PrevStopLine, // Previous Station Line 2
                TextType.LastStopLine, // Line Destination (Last stop before get back) 3
                TextType.PlatformNumber,
                TextType.TimeTemperature,
                TextType.CityName
            },
            [TextRenderingClass.PlaceOnNet] = new TextType[]
            {
                TextType.Fixed,
                TextType.GameSprite,
                TextType.ParameterizedGameSprite,
                TextType.ParameterizedGameSpriteIndexed,
                TextType.ParameterizedText,
                TextType.HwShield,
                TextType.StreetPrefix,
                TextType.StreetSuffix,
                TextType.StreetNameComplete,
                TextType.PostalCode,
                TextType.District,
                TextType.Park,
                TextType.DistrictOrPark,
                TextType.ParkOrDistrict,
                TextType.TimeTemperature,
                TextType.CityName
            },
        };

        #region Main flow
        public static void PropInstancePopulateGroupData(PropInfo info, int layer, InstanceID id, Vector3 position, Vector3 scale, Vector3 angle, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance)
        {
            LightSystem lightSystem = RenderManager.instance.lightSystem;
            if (info.m_prefabDataLayer == layer)
            {
                float y = info.m_generatedInfo.m_size.y * scale.y;
                float num = Mathf.Max(info.m_generatedInfo.m_size.x, info.m_generatedInfo.m_size.z) * scale.y * 0.5f;
                min = Vector3.Min(min, position - new Vector3(num, 0f, num));
                max = Vector3.Max(max, position + new Vector3(num, y, num));
                maxRenderDistance = Mathf.Max(maxRenderDistance, info.m_maxRenderDistance);
                maxInstanceDistance = Mathf.Max(maxInstanceDistance, info.m_maxRenderDistance);
            }
            else if (info.m_effectLayer == layer || (info.m_effectLayer == lightSystem.m_lightLayer && layer == lightSystem.m_lightLayerFloating))
            {
                Matrix4x4 matrix4x = default;
                matrix4x.SetTRS(position, Quaternion.AngleAxis(angle.x, Vector3.left) * Quaternion.AngleAxis(angle.y, Vector3.down) * Quaternion.AngleAxis(angle.z, Vector3.back), scale);
                for (int i = 0; i < info.m_effects.Length; i++)
                {
                    Vector3 pos = matrix4x.MultiplyPoint(info.m_effects[i].m_position);
                    Vector3 dir = matrix4x.MultiplyVector(info.m_effects[i].m_direction);
                    info.m_effects[i].m_effect.PopulateGroupData(layer, id, pos, dir, ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
                }
            }
        }

        public static Color RenderPropMesh<DESC>(PropInfo propInfo, RenderManager.CameraInfo cameraInfo, ushort refId, int boardIdx, int secIdx,
            int layerMask, float refAngleRad, Vector3 position, Vector4 dataVector, Vector3 propAngle, Vector3 propScale, BoardDescriptorGeneralXml propLayout,
            DESC descriptor, out Matrix4x4 propMatrix,
            out bool rendered, InstanceID propRenderID) where DESC : BoardInstanceXml
        {
            Color propColor = GetColorForRule(refId, boardIdx, secIdx, propLayout, descriptor, out rendered);
            if (!rendered)
            {
                propMatrix = new Matrix4x4();
                return propColor;
            }
            propMatrix = RenderProp(refId, refAngleRad, cameraInfo, propInfo, propColor, position, dataVector, boardIdx, propAngle, propScale, layerMask, out rendered, propRenderID);
            return propColor;
        }

        private static Matrix4x4 RenderProp(ushort refId, float refAngleRad, RenderManager.CameraInfo cameraInfo,
                                    PropInfo propInfo, Color propColor, Vector3 position, Vector4 dataVector, int idx,
                                    Vector3 rotation, Vector3 scale, int layerMask, out bool rendered, InstanceID propRenderID2)
        {
            rendered = false;
            var randomizer = new Randomizer((refId << 6) | (idx + 32));
            Matrix4x4 matrix = default;
            matrix.SetTRS(position, Quaternion.AngleAxis(rotation.y + (refAngleRad * Mathf.Rad2Deg), Vector3.down) * Quaternion.AngleAxis(rotation.x, Vector3.left) * Quaternion.AngleAxis(rotation.z, Vector3.back), scale);
            if (propInfo != null)
            {
                propInfo = propInfo.GetVariation(ref randomizer);
                if (cameraInfo.CheckRenderDistance(position, propInfo.m_maxRenderDistance * scale.sqrMagnitude))
                {
                    int oldLayerMask = cameraInfo.m_layerMask;
                    float oldRenderDist = propInfo.m_lodRenderDistance;
                    propInfo.m_lodRenderDistance *= scale.sqrMagnitude;
                    cameraInfo.m_layerMask = 0x7FFFFFFF;
                    try
                    {
                        PropInstance.RenderInstance(cameraInfo, propInfo, propRenderID2, matrix, position, scale.y, refAngleRad + (rotation.y * Mathf.Deg2Rad), propColor, dataVector, true);
                    }
                    finally
                    {
                        propInfo.m_lodRenderDistance = oldRenderDist;
                        cameraInfo.m_layerMask = oldLayerMask;
                    }
                    rendered = true;
                }
            }
            return matrix;
        }

        public static Color GetColorForRule<DESC>(ushort refId, int boardIdx, int secIdx, BoardDescriptorGeneralXml propLayout, DESC descriptor, out bool rendered) where DESC : BoardInstanceXml
        {
            Color propColor = WTSDynamicTextRenderingRules.GetPropColor(refId, boardIdx, secIdx, descriptor, propLayout, out bool colorFound);
            if (!colorFound)
            {
                rendered = false;
                return propColor;
            }
            propColor.a = 1;
            rendered = true;
            return propColor;
        }

        public static Color EnsurePropCache<DESC>(ushort refId, int boardIdx, int secIdx, BoardDescriptorGeneralXml propLayout, DESC descriptor, out bool rendered) where DESC : BoardInstanceXml
        {
            Color propColor = WTSDynamicTextRenderingRules.GetPropColor(refId, boardIdx, secIdx, descriptor, propLayout, out bool colorFound);
            if (!colorFound)
            {
                rendered = false;
                return propColor;
            }
            propColor.a = 1;

            rendered = true;
            return propColor;
        }

        public static void RenderTextMesh(ushort refID, int boardIdx, int secIdx, BoardInstanceXml descriptor, Matrix4x4 propMatrix,
            BoardDescriptorGeneralXml propLayout, ref BoardTextDescriptorGeneralXml textDescriptor, MaterialPropertyBlock materialPropertyBlock,
            int instanceFlags, Color parentColor, PrefabInfo srcInfo, ref int defaultCallsCounter, Camera targetCamera = null)
        {
            BasicRenderInformation renderInfo = WTSTextMeshProcess.GetTextMesh(textDescriptor, refID, boardIdx, secIdx, descriptor, propLayout, out IEnumerable<BasicRenderInformation> multipleOutput, propLayout?.CachedProp);
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
                        float unscaledColumnWidth = resultArray.Max(x => x?.m_sizeMetersUnscaled.x ?? 0) * SCALING_FACTOR;
                        float rowHeight = resultArray.Max(x => x?.m_sizeMetersUnscaled.y ?? 0) * SCALING_FACTOR * textDescriptor.m_textScale + textDescriptor.MultiItemSettings.SubItemSpacing.Y;
                        float columnWidth = (unscaledColumnWidth * textDescriptor.m_textScale) + textDescriptor.MultiItemSettings.SubItemSpacing.X;
                        var maxWidth = textDescriptor.m_maxWidthMeters;


                        float regularOffsetX = CalculateOffsetXMultiItem(textDescriptor.m_textAlign, maxItemsInARow, columnWidth, maxWidth);
                        float cloneOffsetX = textDescriptor.PlacingConfig.m_invertYCloneHorizontalAlign ? CalculateOffsetXMultiItem(2 - textDescriptor.m_textAlign, maxItemsInARow, columnWidth, maxWidth) : regularOffsetX;
                        float regularOffsetY = CalculateOffsetYMultiItem(textDescriptor.MultiItemSettings.VerticalAlign, maxItemsInAColumn, rowHeight);

                        var startPoint = new Vector3(textDescriptor.PlacingConfig.Position.X + regularOffsetX, textDescriptor.PlacingConfig.Position.Y - regularOffsetY, textDescriptor.PlacingConfig.Position.Z);
                        var startPointClone = new Vector3(textDescriptor.PlacingConfig.Position.X + cloneOffsetX, textDescriptor.PlacingConfig.Position.Y - regularOffsetY, textDescriptor.PlacingConfig.Position.Z);

                        Vector3 lastRowOrColumnStartPoint = textDescriptor.MultiItemSettings.VerticalFirst
                            ? new Vector3(startPoint.x, textDescriptor.PlacingConfig.Position.Y - CalculateOffsetYMultiItem(textDescriptor.MultiItemSettings.VerticalAlign, lastRowOrColumnItemCount, rowHeight), startPoint.z)
                            : new Vector3(textDescriptor.PlacingConfig.Position.X + CalculateOffsetXMultiItem(textDescriptor.m_textAlign, lastRowOrColumnItemCount, columnWidth, maxWidth), startPoint.y, startPoint.z);
                        Color colorToSet = GetTextColor(refID, boardIdx, secIdx, descriptor, propLayout, textDescriptor);
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
                            DrawTextBri(refID, boardIdx, secIdx, propMatrix, textDescriptor, materialPropertyBlock, currentItem, colorToSet, targetPosA, textDescriptor.PlacingConfig.Rotation, descriptor.PropScale, false, textDescriptor.m_textAlign, 0, instanceFlags, parentColor, srcInfo, ref defaultCallsCounter, targetCamera);
                            if (textDescriptor.PlacingConfig.m_create180degYClone)
                            {
                                targetPosA = startPointClone - new Vector3(columnWidth * (maxItemsInARow - x - 1), rowHeight * y);
                                targetPosA.z *= -1;
                                DrawTextBri(refID, boardIdx, secIdx, propMatrix, textDescriptor, materialPropertyBlock, currentItem, colorToSet, targetPosA, textDescriptor.PlacingConfig.Rotation + new Vector3(0, 180), descriptor.PropScale, false, textDescriptor.m_textAlign, 0, instanceFlags, parentColor, srcInfo, ref defaultCallsCounter, targetCamera);
                            }
                        }
                    }
                }
                else
                {
                    return;
                }
            }
            if (renderInfo?.m_mesh is null || renderInfo?.m_generatedMaterial is null)
            {
                return;
            }

            Vector3 targetPos = textDescriptor.PlacingConfig.Position;


            DrawTextBri(refID, boardIdx, secIdx, propMatrix, textDescriptor, materialPropertyBlock, renderInfo, GetTextColor(refID, boardIdx, secIdx, descriptor, propLayout, textDescriptor), targetPos, textDescriptor.PlacingConfig.Rotation, descriptor.PropScale, textDescriptor.PlacingConfig.m_create180degYClone, textDescriptor.m_textAlign, textDescriptor.m_maxWidthMeters, instanceFlags, parentColor, srcInfo, ref defaultCallsCounter, targetCamera);
        }


        private static void DrawTextBri(ushort refID, int boardIdx, int secIdx, Matrix4x4 propMatrix, BoardTextDescriptorGeneralXml textDescriptor,
            MaterialPropertyBlock materialPropertyBlock, BasicRenderInformation renderInfo, Color colorToSet, Vector3 targetPos, Vector3 targetRotation,
            Vector3 baseScale, bool placeClone180Y, UIHorizontalAlignment targetTextAlignment, float maxWidth, int instanceFlags, Color parentColor,
            PrefabInfo srcInfo, ref int defaultCallsCounter, Camera targetCamera = null)
        {

            var textMatrixes = CalculateTextMatrix(targetPos, targetRotation, baseScale, targetTextAlignment, maxWidth, textDescriptor, renderInfo, placeClone180Y);

            foreach (var textMatrixTuple in textMatrixes)
            {
                Matrix4x4 matrix = propMatrix * textMatrixTuple.First;

                materialPropertyBlock.Clear();

                Material targetMaterial = renderInfo.m_generatedMaterial;
                PropManager instance = CalculateIllumination(refID, boardIdx, secIdx, textDescriptor, materialPropertyBlock, ref colorToSet, instanceFlags);


                defaultCallsCounter++;
                Graphics.DrawMesh(renderInfo.m_mesh, matrix, targetMaterial, 10, targetCamera, 0, materialPropertyBlock, false);

                if (((Vector2)textDescriptor.BackgroundMeshSettings.Size).sqrMagnitude != 0)
                {
                    BasicRenderInformation bgBri = WriteTheSignsMod.Controller.AtlasesLibrary.GetFromLocalAtlases(null, KlyteResourceLoader.GetDefaultSpriteNameFor(LineIconSpriteNames.K45_SquareIcon));
                    if (bgBri != null)
                    {
                        Matrix4x4 containerMatrix = DrawBgMesh(ref propMatrix, textDescriptor, materialPropertyBlock, ref targetPos, ref targetRotation, ref baseScale, targetTextAlignment, targetCamera, textMatrixTuple, instance, bgBri, ref defaultCallsCounter);
                        if (textDescriptor.BackgroundMeshSettings.UseFrame)
                        {
                            DrawTextFrame(textDescriptor, materialPropertyBlock, ref targetPos, ref targetRotation, ref baseScale, ref parentColor, srcInfo, targetCamera, ref containerMatrix, ref defaultCallsCounter);
                        }
                    }
                }
            }
        }


        private static Matrix4x4 DrawBgMesh(ref Matrix4x4 propMatrix, BoardTextDescriptorGeneralXml textDescriptor, MaterialPropertyBlock materialPropertyBlock, ref Vector3 targetPos,
            ref Vector3 targetRotation, ref Vector3 baseScale, UIHorizontalAlignment targetTextAlignment, Camera targetCamera, Tuple<Matrix4x4, Tuple<Matrix4x4, Matrix4x4, Matrix4x4, Matrix4x4>> textMatrixTuple,
            PropManager instance, BasicRenderInformation bgBri, ref int defaultCallsCounter)
        {
            materialPropertyBlock.SetColor(WTSDynamicTextRenderingRules.SHADER_PROP_COLOR, textDescriptor.BackgroundMeshSettings.BackgroundColor * new Color(1, 1, 1, 0));
            materialPropertyBlock.SetVector(instance.ID_ObjectIndex, new Vector4());
            var bgBriMatrix = ApplyTextAdjustments(targetPos, targetRotation, bgBri, baseScale, textDescriptor.BackgroundMeshSettings.Size.Y, targetTextAlignment, textDescriptor.BackgroundMeshSettings.Size.X, false, false, false);

            var lineAdjustmentVector = new Vector3(0, (-textDescriptor.BackgroundMeshSettings.Size.Y / 2) + (32 * SCALING_FACTOR * textDescriptor.m_textScale) - (bgBri.m_YAxisOverflows.min + bgBri.m_YAxisOverflows.max) * SCALING_FACTOR * textDescriptor.m_textScale / 2, -0.001f);
            var containerMatrix = propMatrix
                * Matrix4x4.Translate(targetPos)
                * textMatrixTuple.Second.Second
                * Matrix4x4.Translate(lineAdjustmentVector)
                * textMatrixTuple.Second.Fourth
                ;
            var bgMatrix = propMatrix
                * Matrix4x4.Translate(targetPos)
                * textMatrixTuple.Second.Second
                * Matrix4x4.Translate(lineAdjustmentVector)
                * Matrix4x4.Scale(new Vector3(textDescriptor.BackgroundMeshSettings.Size.X / bgBri.m_mesh.bounds.size.x, textDescriptor.BackgroundMeshSettings.Size.Y / bgBri.m_mesh.bounds.size.y, 1))
                * textMatrixTuple.Second.Fourth;
            defaultCallsCounter++;
            Graphics.DrawMesh(bgBri.m_mesh, bgMatrix, bgBri.m_generatedMaterial, 10, targetCamera, 0, materialPropertyBlock, false);
            return containerMatrix;
        }
        private static void DrawTextFrame(BoardTextDescriptorGeneralXml textDescriptor, MaterialPropertyBlock materialPropertyBlock, ref Vector3 targetPos, ref Vector3 targetRotation, ref Vector3 baseScale, ref Color parentColor, PrefabInfo srcInfo, Camera targetCamera, ref Matrix4x4 containerMatrix, ref int defaultCallsCounter)
        {
            var frameConfig = textDescriptor.BackgroundMeshSettings.FrameMeshSettings;

            if (m_genMesh is null)
            {
                WTSDisplayContainerMeshUtils.GenerateDisplayContainer(new Vector2(1, 1), new Vector2(1, 1), new Vector2(), 0.05f, 0.3f, 0.1f, out Vector3[] points, out Vector4[] tangents);
                m_genMesh = new Mesh
                {
                    vertices = points,
                    triangles = WTSDisplayContainerMeshUtils.m_triangles,
                    uv = points.Select((x, i) => new Vector2(i / 4f % 1, i / 2f % 1)).ToArray(),
                    colors = points.Select(x => Color.blue).ToArray(),
                    tangents = tangents
                };

                m_genMeshInv = new Mesh
                {
                    vertices = points,
                    triangles = WTSDisplayContainerMeshUtils.m_triangles.Select((x, i) => WTSDisplayContainerMeshUtils.m_triangles[(((i / 3) << 0) * 3) + 2 - (i % 3)]).ToArray(),
                    uv = points.Select((x, i) => new Vector2(i / 4f % 1, i / 2f % 1)).ToArray(),
                    colors = points.Select(x => Color.blue).ToArray(),
                    tangents = tangents
                };

                m_genMeshGlass = new Mesh
                {
                    vertices = points.Take(4).ToArray(),
                    colors32 = points.Take(4).Select(x => new Color32(165, 0, 0, 0)).ToArray(),
                    triangles = WTSDisplayContainerMeshUtils.m_trianglesGlass,
                    uv = points.Take(4).Select(x => new Vector2(0.5f, .5f)).ToArray(),
                    tangents = tangents
                };
            }
            var instance2 = Singleton<VehicleManager>.instance;
            if (m_rotorMaterial == null)
            {
                m_rotorMaterial = new Material(Shader.Find("Custom/Vehicles/Vehicle/Rotors"))
                {
                    mainTexture = Texture2D.whiteTexture
                };
                var targetTexture = new Texture2D(1, 1);
                targetTexture.SetPixels(targetTexture.GetPixels().Select(x => new Color(.5f, .5f, 0.7f, 1)).ToArray());
                targetTexture.Apply();
                m_rotorMaterial.SetTexture("_XYSMap", targetTexture);
                m_rotorMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive | MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
            if (m_outsideMaterial == null)
            {
                var targetTexture = new Texture2D(1, 1);
                targetTexture.SetPixels(targetTexture.GetPixels().Select(x => new Color(.5f, .5f, .5f, 1)).ToArray());
                targetTexture.Apply();
                m_outsideMaterial = new Material(Shader.Find("Custom/Vehicles/Vehicle/Default"))
                {
                    mainTexture = targetTexture
                };
                targetTexture = new Texture2D(1, 1);
                targetTexture.SetPixels(targetTexture.GetPixels().Select(x => new Color(186 / 255f, 186 / 255f, 229 / 255f, 1)).ToArray());
                targetTexture.Apply();
                m_outsideMaterial.SetTexture(instance2.ID_XYSMap, targetTexture);
                targetTexture = new Texture2D(1, 1);
                targetTexture.SetPixels(targetTexture.GetPixels().Select(x => new Color(0, 0f, .5f, 0)).ToArray());
                targetTexture.Apply();
                m_outsideMaterial.SetTexture(instance2.ID_ACIMap, targetTexture);

                m_outsideMaterial.SetVector(instance2.ID_LightState, Vector3.zero);
                m_outsideMaterial.SetVector(instance2.ID_TyrePosition, Vector3.zero);
                m_outsideMaterial.SetMatrix(instance2.ID_TyreMatrix, Matrix4x4.identity);
                m_outsideMaterial.SetColor(WTSDynamicTextRenderingRules.SHADER_PROP_COLOR, Color.black * new Color(1, 1, 1, 0));
                m_outsideMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive | MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }

            if (frameConfig.cachedFrameArray == null)
            {
                WTSDisplayContainerMeshUtils.GenerateDisplayContainer(textDescriptor.BackgroundMeshSettings.Size,
                    frameConfig.BackSize,
                    frameConfig.BackOffset,
                    frameConfig.FrontDepth,
                    frameConfig.BackDepth,
                    frameConfig.FrontBorderThickness,
                    out frameConfig.cachedFrameArray,
                    out Vector4[] tangents);

                frameConfig.meshInnerContainer = new Mesh()
                {
                    vertices = frameConfig.cachedFrameArray,
                    triangles = m_genMeshInv.triangles,
                    uv = m_genMeshInv.uv,
                    normals = m_genMeshInv.normals,
                    colors = m_genMeshInv.colors,
                    tangents = tangents
                };
                frameConfig.meshOuterContainer = new Mesh()
                {
                    vertices = frameConfig.cachedFrameArray,
                    triangles = m_genMesh.triangles,
                    uv = m_genMesh.uv,
                    normals = m_genMesh.normals,
                    colors = m_genMesh.colors,
                    tangents = tangents
                };
                frameConfig.meshGlass = new Mesh()
                {
                    vertices = frameConfig.cachedFrameArray.Take(4).ToArray(),
                    triangles = m_genMeshGlass.triangles,
                    uv = m_genMeshGlass.uv,
                    normals = m_genMeshGlass.normals,
                    colors = frameConfig.cachedFrameArray.Take(4).Select(x => new Color(1 - frameConfig.GlassTransparency, 0, 0, 0)).ToArray(),
                    tangents = tangents.Take(4).ToArray()
                };
                foreach (var k in new Mesh[] { frameConfig.meshOuterContainer, frameConfig.meshInnerContainer, frameConfig.meshGlass })
                {
                    k.RecalculateNormals();
                    k.RecalculateTangents();
                }

                if (frameConfig.cachedGlassMain is null)
                {
                    frameConfig.cachedGlassMain = new Texture2D(1, 1);
                }

                if (frameConfig.cachedGlassXYS is null)
                {
                    frameConfig.cachedGlassXYS = new Texture2D(1, 1);
                }

                if (frameConfig.cachedOuterXYS is null)
                {
                    frameConfig.cachedOuterXYS = new Texture2D(1, 1);
                }

                frameConfig.cachedGlassMain.SetPixels(new Color[] { frameConfig.GlassColor });
                frameConfig.cachedGlassXYS.SetPixels(new Color[] { new Color(.5f, .5f, 1 - frameConfig.GlassSpecularLevel, 1) });
                frameConfig.cachedOuterXYS.SetPixels(new Color[] { new Color(.5f, .5f, 1 - frameConfig.OuterSpecularLevel, 1) });
                frameConfig.cachedGlassMain.Apply();
                frameConfig.cachedGlassXYS.Apply();
                frameConfig.cachedOuterXYS.Apply();


            }
            Matrix4x4 value;
            if (srcInfo is VehicleInfo vi)
            {
                var idt = Matrix4x4.identity;
                var qtr = Quaternion.Euler(targetRotation);
                value = vi.m_vehicleAI.CalculateTyreMatrix(Vehicle.Flags.Created | Vehicle.Flags.Spawned | Vehicle.Flags.TransferToTarget, ref targetPos, ref qtr, ref baseScale, ref idt);
            }
            else
            {
                return;
            }

            materialPropertyBlock.Clear();
            materialPropertyBlock.SetTexture(instance2.ID_XYSMap, frameConfig.cachedGlassXYS);
            materialPropertyBlock.SetTexture(instance2.ID_MainTex, frameConfig.cachedGlassMain);
            defaultCallsCounter++;
            Graphics.DrawMesh(frameConfig.meshGlass, containerMatrix, m_rotorMaterial, srcInfo.m_prefabDataIndex, targetCamera, 0, materialPropertyBlock);

            materialPropertyBlock.Clear();
            materialPropertyBlock.SetVectorArray(instance2.ID_TyreLocation, vi.m_generatedInfo.m_tyres);
            defaultCallsCounter++;
            Graphics.DrawMesh(frameConfig.meshInnerContainer, containerMatrix, m_outsideMaterial, srcInfo.m_prefabDataIndex, targetCamera, 0, materialPropertyBlock, true, true);
            var color = frameConfig.InheritColor ? parentColor : frameConfig.OutsideColor;
            materialPropertyBlock.SetColor(WTSDynamicTextRenderingRules.SHADER_PROP_COLOR, color * new Color(1, 1, 1, 0));
            materialPropertyBlock.SetTexture(instance2.ID_XYSMap, frameConfig.cachedOuterXYS);
            defaultCallsCounter++;
            Graphics.DrawMesh(frameConfig.meshOuterContainer, containerMatrix, m_outsideMaterial, srcInfo.m_prefabDataIndex, targetCamera, 0, materialPropertyBlock, true, true);
        }
        #endregion
        #region Illumination handling
        private static PropManager CalculateIllumination(ushort refID, int boardIdx, int secIdx, BoardTextDescriptorGeneralXml textDescriptor, MaterialPropertyBlock materialPropertyBlock, ref Color colorToSet, int instanceFlags)
        {
            Vector4 objectIndex = default;
            var randomizer = new Randomizer((refID << 8) + (boardIdx << 2) + secIdx);
            switch (textDescriptor.IlluminationConfig.IlluminationType)
            {
                default:
                case FontStashSharp.MaterialType.OPAQUE:
                    objectIndex.z = 0;
                    break;
                case FontStashSharp.MaterialType.DAYNIGHT:
                    float num = m_daynightOffTime + (randomizer.Int32(100000u) * 1E-05f);
                    objectIndex.z = MathUtils.SmoothStep(num + 0.01f, num - 0.01f, Singleton<RenderManager>.instance.lightSystem.DayLightIntensity) * textDescriptor.IlluminationConfig.IlluminationStrength;
                    break;
                case FontStashSharp.MaterialType.FLAGS:
                    objectIndex.z = ((instanceFlags & textDescriptor.IlluminationConfig.m_requiredFlags) == textDescriptor.IlluminationConfig.m_requiredFlags) && ((instanceFlags & textDescriptor.IlluminationConfig.m_forbiddenFlags) == 0) ? textDescriptor.IlluminationConfig.IlluminationStrength : 0;
                    break;
                case FontStashSharp.MaterialType.BRIGHT:
                    objectIndex.z = textDescriptor.IlluminationConfig.IlluminationStrength;
                    break;
            }
            colorToSet *= Color.Lerp(new Color32(200, 200, 200, 255), Color.white, objectIndex.z);
            materialPropertyBlock.SetColor(SHADER_PROP_COLOR, colorToSet);


            if (objectIndex.z > 0 && textDescriptor.IlluminationConfig.BlinkType != BlinkType.None)
            {
                CalculateBlinkEffect(textDescriptor, ref objectIndex, ref randomizer);
            }

            objectIndex.x = textDescriptor.IlluminationConfig.IlluminationDepth;

            PropManager instance = Singleton<PropManager>.instance;
            materialPropertyBlock.SetVector(instance.ID_ObjectIndex, objectIndex);
            return instance;
        }
        private static void CalculateBlinkEffect(BoardTextDescriptorGeneralXml textDescriptor, ref Vector4 objectIndex, ref Randomizer randomizer)
        {
            float num = m_daynightOffTime + (randomizer.Int32(100000u) * 1E-05f);
            Vector4 blinkVector;
            if (textDescriptor.IlluminationConfig.BlinkType == BlinkType.Custom)
            {
                blinkVector = textDescriptor.IlluminationConfig.CustomBlink;
            }
            else
            {
                blinkVector = LightEffect.GetBlinkVector((LightEffect.BlinkType)textDescriptor.IlluminationConfig.BlinkType);
            }
            float num2 = num * 3.71f + Singleton<SimulationManager>.instance.m_simulationTimer / blinkVector.w;
            num2 = (num2 - Mathf.Floor(num2)) * blinkVector.w;
            float num3 = MathUtils.SmoothStep(blinkVector.x, blinkVector.y, num2);
            float num4 = MathUtils.SmoothStep(blinkVector.w, blinkVector.z, num2);
            objectIndex.z *= 1f - (num3 * num4);
        }
        #endregion
        #region Text handling
        private static float CalculateOffsetXMultiItem(UIHorizontalAlignment alignment, int itemCount, float columnWidth, float maxWidth)
        {
            var offsetX = (columnWidth * (itemCount - 1) / 2);
            if (maxWidth > 0)
            {
                if (alignment == UIHorizontalAlignment.Right)
                {
                    offsetX = -((maxWidth - columnWidth) / 2) + (columnWidth * (itemCount - 1));
                }
                else if (alignment == UIHorizontalAlignment.Left)
                {
                    offsetX = ((maxWidth - columnWidth) / 2);
                }
            }

            return offsetX;
        }

        private static float CalculateOffsetYMultiItem(UIVerticalAlignment alignment, int itemCount, float rowHeight)
        {
            var offsetY = 0f;
            if (alignment == UIVerticalAlignment.Middle)
            {
                offsetY = -rowHeight * (itemCount - 1) / 2f;
            }
            else if (alignment == UIVerticalAlignment.Bottom)
            {
                offsetY = -(rowHeight * (itemCount - 1));
            }


            return offsetY;
        }

        internal static List<Tuple<Matrix4x4, Tuple<Matrix4x4, Matrix4x4, Matrix4x4, Matrix4x4>>> CalculateTextMatrix(Vector3 targetPosition, Vector3 targetRotation, Vector3 baseScale, UIHorizontalAlignment targetTextAlignment, float maxWidth, BoardTextDescriptorGeneralXml textDescriptor, BasicRenderInformation renderInfo, bool placeClone180Y, bool centerReference = false)
        {
            var result = new List<Tuple<Matrix4x4, Tuple<Matrix4x4, Matrix4x4, Matrix4x4, Matrix4x4>>>();
            if (renderInfo == null)
            {
                return result;
            }

            var textMatrix = ApplyTextAdjustments(targetPosition, targetRotation, renderInfo, baseScale, textDescriptor.m_textScale, targetTextAlignment, maxWidth, textDescriptor.m_applyOverflowResizingOnY, centerReference, textDescriptor.PlacingConfig.m_mirrored);

            result.Add(textMatrix);

            if (placeClone180Y)
            {
                if (textDescriptor.PlacingConfig.m_invertYCloneHorizontalAlign)
                {
                    targetTextAlignment = 2 - targetTextAlignment;
                }
                result.Add(ApplyTextAdjustments(new Vector3(targetPosition.x, targetPosition.y, -targetPosition.z), targetRotation + new Vector3(0, 180), renderInfo, baseScale, textDescriptor.m_textScale, targetTextAlignment, maxWidth, textDescriptor.m_applyOverflowResizingOnY, centerReference, textDescriptor.PlacingConfig.m_mirrored));
            }

            return result;
        }
        internal static Tuple<Matrix4x4, Tuple<Matrix4x4, Matrix4x4, Matrix4x4, Matrix4x4>> ApplyTextAdjustments(Vector3 textPosition, Vector3 textRotation, BasicRenderInformation renderInfo, Vector3 propScale, float textScale, UIHorizontalAlignment horizontalAlignment, float maxWidth, bool applyResizeOverflowOnY, bool centerReference, bool mirrored)
        {
            float overflowScaleX = 1f;
            float overflowScaleY = 1f;
            float defaultMultiplierX = textScale * SCALING_FACTOR;
            float defaultMultiplierY = textScale * SCALING_FACTOR;
            float realWidth = defaultMultiplierX * renderInfo.m_sizeMetersUnscaled.x;
            Vector3 targetRelativePosition = textPosition;
            //LogUtils.DoWarnLog($"[{renderInfo},{refID},{boardIdx},{secIdx}] realWidth = {realWidth}; realHeight = {realHeight};");
            var rotationMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(textRotation), Vector3.one);
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

            var scaleVector = centerReference ? new Vector3(SCALING_FACTOR, SCALING_FACTOR, SCALING_FACTOR) : new Vector3(defaultMultiplierX * overflowScaleX / propScale.x, defaultMultiplierY * overflowScaleY / propScale.y, mirrored ? -1 : 1);
            Matrix4x4 textMatrix =
                Matrix4x4.Translate(targetRelativePosition) *
                rotationMatrix *
                Matrix4x4.Scale(scaleVector) * Matrix4x4.Scale(propScale)
               ;
            return Tuple.New(textMatrix, Tuple.New(Matrix4x4.Translate(targetRelativePosition), rotationMatrix, Matrix4x4.Scale(scaleVector), Matrix4x4.Scale(propScale)));
        }
        #endregion
        #region Color rules

        private static Randomizer rand = new Randomizer(0);
        public static Color GetPropColor(ushort refId, int boardIdx, int secIdx, BoardInstanceXml instance, BoardDescriptorGeneralXml propLayout, out bool found)
        {

            if (instance is BoardInstanceRoadNodeXml)
            {
                found = WTSRoadNodesData.Instance.BoardsContainers[refId, boardIdx, secIdx] != null;
                return WTSRoadNodesData.Instance.BoardsContainers[refId, boardIdx, secIdx]?.m_cachedColor ?? default;
            }
            else if (instance is LayoutDescriptorVehicleXml vehicleDescriptor)
            {
                found = true;
                ref Vehicle targetVehicle = ref VehicleManager.instance.m_vehicles.m_buffer[refId];
                return targetVehicle.Info.m_vehicleAI.GetColor(refId, ref targetVehicle, InfoManager.InfoMode.None);
            }
            else if (instance is BoardInstanceBuildingXml buildingDescriptor)
            {
                found = true;
                switch (buildingDescriptor.ColorModeProp)
                {
                    case ColoringMode.Fixed:
                        rand.seed = refId * (1u + (uint)boardIdx);
                        return propLayout?.FixedColor ?? buildingDescriptor.CachedSimpleProp?.GetColor(ref rand) ?? Color.white;
                    case ColoringMode.ByPlatform:
                        var stops = WTSStopUtils.GetAllTargetStopInfo(buildingDescriptor, refId).Where(x => x.m_lineId != 0);
                        if (buildingDescriptor.UseFixedIfMultiline && stops.GroupBy(x => x.m_lineId).Count() > 1)
                        {
                            rand.seed = refId * (1u + (uint)boardIdx);
                            return propLayout?.FixedColor ?? buildingDescriptor.CachedSimpleProp?.GetColor(ref rand) ?? Color.white;
                        }
                        if (stops.Count() != 0)
                        {
                            var line = new WTSLine(stops.FirstOrDefault());
                            if (!line.ZeroLine)
                            {
                                return WriteTheSignsMod.Controller.ConnectorTLM.GetLineColor(line);
                            }
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
            else if (instance is BoardInstanceOnNetXml n)
            {
                found = n.Descriptor != null || n.SimpleProp != null;
                return n.Descriptor?.FixedColor ?? n.SimpleProp?.m_color0 ?? Color.white;
            }
            found = false;
            return default;
        }
        private static Color GetTextColor(ushort refID, int boardIdx, int secIdx, BoardInstanceXml descriptor, BoardDescriptorGeneralXml propLayout, BoardTextDescriptorGeneralXml textDescriptor)
        {
            if (textDescriptor.ColoringConfig.UseContrastColor)
            {
                return GetContrastColor(refID, boardIdx, secIdx, descriptor, propLayout);
            }
            else if (textDescriptor.ColoringConfig.m_cachedColor != null)
            {
                return textDescriptor.ColoringConfig.m_cachedColor;
            }
            return Color.white;
        }

        internal static readonly Color[] m_spectreSteps = new Color[]
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
                return (preview?.Descriptor?.FixedColor ?? GetCurrentSimulationColor()).ContrastColor();
            }
            var targetColor = GetPropColor(refID, boardIdx, secIdx, instance, propLayout, out bool colorFound);
            return (colorFound ? targetColor : Color.white).ContrastColor();
        }
        #endregion



    }

}
