﻿using ColossalFramework.Math;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.UI;
using Klyte.WriteTheSigns.Xml;
using System.Collections.Generic;
using UnityEngine;
namespace Klyte.WriteTheSigns.Singleton
{
    public class WTSOnNetPropsSingleton : MonoBehaviour
    {
        public WTSOnNetData Data => WTSOnNetData.Instance;

        #region Initialize

        public void Start() => WriteTheSignsMod.Controller.EventOnDistrictChanged += OnDistrictChange;


        private void OnDistrictChange() => WTSCacheSingleton.ClearCacheDistrictArea();
        #endregion



        public void AfterRenderInstanceImpl(RenderManager.CameraInfo cameraInfo, ushort segmentId, ref NetSegment data)
        {

            ref OnNetGroupDescriptorXml itemGroup = ref Data.m_boardsContainers[segmentId];
            if (itemGroup == null || !itemGroup.HasAnyBoard())
            {
                return;
            }
            for (var i = 0; i < itemGroup.BoardsData.Length; i++)
            {
                var targetDescriptor = itemGroup.BoardsData[i];
                if (targetDescriptor?.Descriptor == null)
                {
                    if (targetDescriptor?.SimpleProp is null)
                    {
                        continue;
                    }
                }
                if (targetDescriptor.m_cachedPositions == null || targetDescriptor.m_cachedRotations == null)
                {
                    bool segmentInverted = (data.m_flags & NetSegment.Flags.Invert) > 0;

                    targetDescriptor.m_cachedPositions = new List<Vector3Xml>();
                    targetDescriptor.m_cachedRotations = new List<Vector3Xml>();
                    if (targetDescriptor.SegmentPositionRepeating)
                    {
                        if (targetDescriptor.SegmentPositionRepeatCount == 1)
                        {
                            var segPos = (targetDescriptor.SegmentPositionStart + targetDescriptor.SegmentPositionEnd) * .5f;
                            CreateSegmentRenderInstance(ref data, targetDescriptor, segmentInverted, segPos);
                        }
                        else if (targetDescriptor.SegmentPositionRepeatCount > 0)
                        {
                            var step = (targetDescriptor.SegmentPositionEnd - targetDescriptor.SegmentPositionStart) / (targetDescriptor.SegmentPositionRepeatCount - 1);
                            for (int k = 0; k < targetDescriptor.SegmentPositionRepeatCount; k++)
                            {
                                CreateSegmentRenderInstance(ref data, targetDescriptor, segmentInverted, targetDescriptor.SegmentPositionStart + (step * k));
                            }
                        }
                    }
                    else
                    {
                        var segPos = targetDescriptor.SegmentPosition;
                        CreateSegmentRenderInstance(ref data, targetDescriptor, segmentInverted, segPos);
                    }
                }
                RenderSign(cameraInfo, segmentId, i, ref targetDescriptor, targetDescriptor?.Descriptor?.CachedProp ?? targetDescriptor.m_simpleCachedProp);
            }

        }

        private static void CreateSegmentRenderInstance(ref NetSegment data, OnNetInstanceCacheContainerXml targetDescriptor, bool segmentInverted, float segPos)
        {
            Vector3Xml cachedPosition;
            Vector3Xml cachedRotation;
            float effectiveSegmentPos = segmentInverted ? 1 - segPos : segPos;
            Vector3 bezierPos = data.GetBezier().Position(effectiveSegmentPos);

            data.GetClosestPositionAndDirection(bezierPos, out _, out Vector3 dir);
            float rotation = dir.GetAngleXZ();
            if (targetDescriptor.InvertSign != segmentInverted)
            {
                rotation += 180;
            }

            Vector3 rotationVectorX = VectorUtils.X_Y(KlyteMathUtils.DegreeToVector2(rotation - 90));
            cachedPosition = (Vector3Xml)(bezierPos + (rotationVectorX * (data.Info.m_halfWidth + targetDescriptor.PropPosition.X)) + (VectorUtils.X_Y(KlyteMathUtils.DegreeToVector2(rotation)) * targetDescriptor.PropPosition.Z));
            cachedPosition.Y += targetDescriptor.PropPosition.Y;
            cachedRotation = (Vector3Xml)(targetDescriptor.PropRotation + new Vector3(0, rotation + 90));

            targetDescriptor.m_cachedRotations.Add(cachedRotation);
            targetDescriptor.m_cachedPositions.Add(cachedPosition);
        }

        internal void CalculateGroupData(ushort segmentID, int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays)
        {
            ref OnNetGroupDescriptorXml itemGroup = ref Data.m_boardsContainers[segmentID];
            if (itemGroup == null || !itemGroup.HasAnyBoard())
            {
                return;
            }
            for (var i = 0; i < itemGroup.BoardsData.Length; i++)
            {
                var targetDescriptor = itemGroup.BoardsData[i];
                bool rendered;
                var isSimple = targetDescriptor.Descriptor == null;
                if (isSimple)
                {
                    if (targetDescriptor?.SimpleProp == null)
                    {
                        continue;
                    }
                    WTSDynamicTextRenderingRules.EnsurePropCache(segmentID, i, 0, targetDescriptor.Descriptor, targetDescriptor, out rendered);
                }
                else
                {
                    WTSDynamicTextRenderingRules.GetColorForRule(segmentID, i, 0, targetDescriptor.Descriptor, targetDescriptor, out rendered);
                }
                if (rendered)
                {
                    int deltaVertexCount = 0;
                    int deltaTriangleCount = 0;
                    int deltaObjectCount = 0;
                    PropInstance.CalculateGroupData(isSimple ? targetDescriptor.m_simpleCachedProp : targetDescriptor.Descriptor.CachedProp, layer, ref deltaVertexCount, ref deltaTriangleCount, ref deltaObjectCount, ref vertexArrays);

                    int multiplier = 1;

                    if (targetDescriptor.SegmentPositionRepeating)
                    {
                        multiplier = targetDescriptor.SegmentPositionRepeatCount;
                    }

                    vertexCount += multiplier * deltaVertexCount;
                    triangleCount += multiplier * deltaTriangleCount;
                    objectCount += multiplier * deltaTriangleCount;
                }
            }
        }

        internal void PopulateGroupData(ushort segmentID, int layer, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance)
        {
            ref OnNetGroupDescriptorXml itemGroup = ref Data.m_boardsContainers[segmentID];
            if (itemGroup == null || !itemGroup.HasAnyBoard())
            {
                return;
            }
            for (var i = 0; i < itemGroup.BoardsData.Length; i++)
            {
                var targetDescriptor = itemGroup.BoardsData[i];
                if (targetDescriptor is null)
                {
                    continue;
                }

                if (targetDescriptor?.Descriptor == null)
                {
                    if (targetDescriptor?.SimpleProp is null)
                    {
                        continue;
                    }
                }
                if (!(targetDescriptor.m_cachedRotations is null) && !(targetDescriptor.m_cachedPositions is null))
                {
                    if (!(targetDescriptor.Descriptor?.CachedProp is null))
                    {
                        for (int k = 0; k < targetDescriptor.m_cachedPositions.Count; k++)
                        {
                            WTSDynamicTextRenderingRules.PropInstancePopulateGroupData(targetDescriptor.Descriptor.CachedProp, layer, new InstanceID { NetSegment = segmentID }, targetDescriptor.m_cachedPositions[k], targetDescriptor.Scale, targetDescriptor.m_cachedRotations[k], ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
                        }
                    }
                    if (!(targetDescriptor.m_simpleCachedProp is null))
                    {
                        for (int k = 0; k < targetDescriptor.m_cachedPositions.Count; k++)
                        {
                            WTSDynamicTextRenderingRules.PropInstancePopulateGroupData(targetDescriptor.m_simpleCachedProp, layer, new InstanceID { NetSegment = segmentID }, targetDescriptor.m_cachedPositions[k], targetDescriptor.Scale, targetDescriptor.m_cachedRotations[k], ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
                        }
                    }
                }
            }
        }


        private void RenderSign(RenderManager.CameraInfo cameraInfo, ushort segmentId, int boardIdx, ref OnNetInstanceCacheContainerXml targetDescriptor, PropInfo cachedProp)
        {
            if (targetDescriptor.m_cachedPositions is null || targetDescriptor.m_cachedRotations is null)
            {
                return;
            }
            for (int i = 0; i < targetDescriptor.m_cachedPositions.Count; i++)
            {
                var position = targetDescriptor.m_cachedPositions[i];
                var rotation = targetDescriptor.m_cachedRotations[i];
                var isSimple = targetDescriptor.Descriptor == null;

                var propname = isSimple ? targetDescriptor.m_simplePropName : targetDescriptor.Descriptor?.PropName;
                if (propname is null)
                {
                    return;
                }

                Color parentColor = WTSDynamicTextRenderingRules.RenderPropMesh(cachedProp, cameraInfo, segmentId, boardIdx, 0, 0xFFFFFFF, 0, position, Vector4.zero, rotation, targetDescriptor.PropScale, targetDescriptor.Descriptor, targetDescriptor, out Matrix4x4 propMatrix, out bool rendered, new InstanceID { NetNode = segmentId });


                if (rendered && !isSimple)
                {

                    for (int j = 0; j < targetDescriptor.Descriptor.TextDescriptors.Length; j++)
                    {
                        if (cameraInfo.CheckRenderDistance(position, 200 * targetDescriptor.Descriptor.TextDescriptors[j].m_textScale * (targetDescriptor.Descriptor.TextDescriptors[j].IlluminationConfig.IlluminationType == FontStashSharp.MaterialType.OPAQUE ? 1 : 3)))
                        {
                            MaterialPropertyBlock properties = PropManager.instance.m_materialBlock;
                            properties.Clear();
                            WTSDynamicTextRenderingRules.RenderTextMesh(segmentId, boardIdx, i, targetDescriptor, propMatrix, targetDescriptor.Descriptor, ref targetDescriptor.Descriptor.TextDescriptors[j], properties, 0, parentColor, cachedProp, ref NetManager.instance.m_drawCallData.m_batchedCalls);
                        }

                    }
                }

                if ((i == 0) && WTSOnNetLiteUI.LockSelection && WTSOnNetLiteUI.Instance.Visible && (WTSOnNetLiteUI.Instance.CurrentSegmentId == segmentId) && WTSOnNetLiteUI.Instance.ListSel == boardIdx && !WriteTheSignsMod.Controller.RoadSegmentToolInstance.enabled)
                {
                    ToolsModifierControl.cameraController.m_targetPosition = position;
                }
            }
        }
    }
}
