using ColossalFramework.Math;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.UI;
using Klyte.WriteTheSigns.Xml;
using UnityEngine;
namespace Klyte.WriteTheSigns.Singleton
{
    public class WTSOnNetPropsSingleton : MonoBehaviour
    {
        public WTSOnNetData Data => WTSOnNetData.Instance;

        #region Initialize

        public void Start() => WriteTheSignsMod.Controller.EventOnDistrictChanged += OnDistrictChange;


        private void OnDistrictChange() => Data.ResetDistrictCache();
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
                    if (targetDescriptor?.SimpleProp == null)
                    {
                        continue;
                    }

                    if (targetDescriptor.m_cachedProp?.name != targetDescriptor.m_simplePropName)
                    {
                        targetDescriptor.m_cachedProp = null;
                    }
                }
                else
                {
                    if (targetDescriptor.m_cachedProp?.name != targetDescriptor.Descriptor.m_propName)
                    {
                        targetDescriptor.m_cachedProp = null;
                    }
                }
                if (targetDescriptor.m_cachedPosition == null || targetDescriptor.m_cachedRotation == null)
                {
                    bool segmentInverted = (data.m_flags & NetSegment.Flags.Invert) > 0;
                    float effectiveSegmentPos = segmentInverted ? 1 - targetDescriptor.SegmentPosition : targetDescriptor.SegmentPosition;
                    Vector3 bezierPos = data.GetBezier().Position(effectiveSegmentPos);

                    data.GetClosestPositionAndDirection(bezierPos, out _, out Vector3 dir);
                    float rotation = dir.GetAngleXZ();
                    if (targetDescriptor.InvertSign != segmentInverted)
                    {
                        rotation += 180;
                    }

                    Vector3 rotationVectorX = VectorUtils.X_Y(KlyteMathUtils.DegreeToVector2(rotation - 90));
                    targetDescriptor.m_cachedPosition = (Vector3Xml)(bezierPos + (rotationVectorX * (data.Info.m_halfWidth + targetDescriptor.PropPosition.X)) + (VectorUtils.X_Y(KlyteMathUtils.DegreeToVector2(rotation)) * targetDescriptor.PropPosition.Z));
                    targetDescriptor.m_cachedPosition.Y += targetDescriptor.PropPosition.Y;
                    targetDescriptor.m_cachedRotation = (Vector3Xml)(targetDescriptor.PropRotation + new Vector3(0, rotation + 90));
                }
                RenderSign(cameraInfo, segmentId, i, ref targetDescriptor, ref targetDescriptor.m_cachedProp);
            }

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
                if (targetDescriptor?.Descriptor == null)
                {
                    if (targetDescriptor?.SimpleProp == null)
                    {
                        continue;
                    }

                    if (targetDescriptor.m_cachedProp?.name != targetDescriptor.m_simplePropName)
                    {
                        targetDescriptor.m_cachedProp = null;
                    }
                }
                else
                {
                    if (targetDescriptor.m_cachedProp?.name != targetDescriptor.Descriptor.m_propName)
                    {
                        targetDescriptor.m_cachedProp = null;
                    }
                }
                var isSimple = targetDescriptor.Descriptor == null;
                var propname = isSimple ? targetDescriptor.m_simplePropName : targetDescriptor.Descriptor.m_propName;
                WTSDynamicTextRenderingRules.EnsurePropCache(ref targetDescriptor.m_cachedProp, segmentID, i, 0, ref propname, targetDescriptor.Descriptor, targetDescriptor, out bool rendered);
                (isSimple ? ref targetDescriptor.m_simplePropName : ref targetDescriptor.Descriptor.m_propName) = propname;
                if (rendered)
                {
                    PropInstance.CalculateGroupData(targetDescriptor.m_cachedProp, layer, ref vertexCount, ref triangleCount, ref objectCount, ref vertexArrays);
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
                if (targetDescriptor?.Descriptor == null)
                {
                    if (targetDescriptor?.SimpleProp == null)
                    {
                        continue;
                    }

                    if (targetDescriptor.m_cachedProp?.name != targetDescriptor.m_simplePropName)
                    {
                        targetDescriptor.m_cachedProp = null;
                    }
                }
                else
                {
                    if (targetDescriptor.m_cachedProp?.name != targetDescriptor.Descriptor.m_propName)
                    {
                        targetDescriptor.m_cachedProp = null;
                    }
                }
                if (targetDescriptor.m_cachedProp != null)
                {
                    WTSDynamicTextRenderingRules.PropInstancePopulateGroupData(targetDescriptor.m_cachedProp, layer, new InstanceID { NetSegment = segmentID }, targetDescriptor.m_cachedPosition, targetDescriptor.Scale, targetDescriptor.m_cachedRotation ?? default(Vector3), ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
                }
            }
        }


        private void RenderSign(RenderManager.CameraInfo cameraInfo, ushort segmentId, int boardIdx, ref OnNetInstanceCacheContainerXml targetDescriptor, ref PropInfo cachedProp)
        {
            var position = targetDescriptor.m_cachedPosition ?? Vector3.zero;
            var rotation = targetDescriptor.m_cachedRotation ?? Vector3.zero;
            var isSimple = targetDescriptor.Descriptor == null;

            var propname = isSimple ? targetDescriptor.m_simplePropName : targetDescriptor.Descriptor.m_propName;

            Color parentColor = WTSDynamicTextRenderingRules.RenderPropMesh(ref cachedProp, cameraInfo, segmentId, boardIdx, 0, 0xFFFFFFF, 0, position, Vector4.zero, ref propname, rotation, targetDescriptor.PropScale, targetDescriptor.Descriptor, targetDescriptor, out Matrix4x4 propMatrix, out bool rendered, new InstanceID { NetNode = segmentId });

            (isSimple ? ref targetDescriptor.m_simplePropName : ref targetDescriptor.Descriptor.m_propName) = propname;

            if (rendered && !isSimple)
            {

                for (int j = 0; j < targetDescriptor.Descriptor.TextDescriptors.Length; j++)
                {
                    if (cameraInfo.CheckRenderDistance(position, 200 * targetDescriptor.Descriptor.TextDescriptors[j].m_textScale * (targetDescriptor.Descriptor.TextDescriptors[j].IlluminationConfig.IlluminationType == FontStashSharp.MaterialType.OPAQUE ? 1 : 3)))
                    {
                        MaterialPropertyBlock properties = PropManager.instance.m_materialBlock;
                        properties.Clear();
                        WTSDynamicTextRenderingRules.RenderTextMesh(segmentId, boardIdx, 0, targetDescriptor, propMatrix, targetDescriptor.Descriptor, ref targetDescriptor.Descriptor.TextDescriptors[j], properties, 0, parentColor, cachedProp);
                    }

                }
            }

            if ((WTSOnNetLayoutEditor.Instance?.MainContainer?.isVisible ?? false) && WTSOnNetLayoutEditor.Instance.LockSelection && (WTSOnNetLayoutEditor.Instance?.CurrentSegmentId == segmentId) && WTSOnNetLayoutEditor.Instance.LayoutList.SelectedIndex == boardIdx && !WriteTheSignsMod.Controller.RoadSegmentToolInstance.enabled)
            {
                ToolsModifierControl.cameraController.m_targetPosition = position;
            }
        }
    }
}
