using ColossalFramework.Math;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Xml;
using UnityEngine;
namespace Klyte.WriteTheSigns.Singleton
{
    public class WTSOnNetPropsSingleton : MonoBehaviour
    {
        public WTSOnNetData Data => WTSOnNetData.Instance;

        #region Initialize

        public void Start() => WriteTheSignsMod.Controller.EventOnDistrictChanged += OnDistrictChange;


        private void OnDistrictChange() => Data.m_lastUpdateDistrict++;
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
                    continue;
                }

                if (targetDescriptor.m_cachedProp?.name != targetDescriptor.Descriptor.m_propName)
                {
                    targetDescriptor.m_cachedProp = null;
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
                RenderSign(cameraInfo, segmentId, i, ref targetDescriptor, ref targetDescriptor.m_cachedProp, ref data);
            }

        }

        private void RenderSign(RenderManager.CameraInfo cameraInfo, ushort nodeID, int boardIdx, ref OnNetInstanceCacheContainerXml targetDescriptor, ref PropInfo cachedProp, ref NetSegment data)
        {
            var position = targetDescriptor.m_cachedPosition ?? Vector3.zero;
            var rotation = targetDescriptor.m_cachedRotation ?? Vector3.zero;
            Color parentColor = WTSDynamicTextRenderingRules.RenderPropMesh(ref cachedProp, cameraInfo, nodeID, boardIdx, 0, 0xFFFFFFF, 0, position, Vector4.zero, ref targetDescriptor.Descriptor.m_propName, rotation, targetDescriptor.PropScale, targetDescriptor.Descriptor, targetDescriptor, out Matrix4x4 propMatrix, out bool rendered, new InstanceID { NetNode = nodeID });
            if (rendered)
            {

                for (int j = 0; j < targetDescriptor.Descriptor.TextDescriptors.Length; j++)
                {
                    if (cameraInfo.CheckRenderDistance(position, 200 * targetDescriptor.Descriptor.TextDescriptors[j].m_textScale * (targetDescriptor.Descriptor.TextDescriptors[j].IlluminationConfig.IlluminationType == FontStashSharp.MaterialType.OPAQUE ? 1 : 3)))
                    {
                        if (targetDescriptor.Descriptor.TextDescriptors[j].m_destinationRelative != DestinationReference.Self)
                        {
                            if (WriteTheSignsMod.Controller.DestinationSingleton.m_updatedDestinations[nodeID] == null)
                            {
                                WriteTheSignsMod.Controller.StartCoroutine(WriteTheSignsMod.Controller.DestinationSingleton.CalculateDestinations(nodeID));
                            }
                        }


                        MaterialPropertyBlock properties = PropManager.instance.m_materialBlock;
                        properties.Clear();
                        WTSDynamicTextRenderingRules.RenderTextMesh(nodeID, boardIdx, 0, targetDescriptor, propMatrix, targetDescriptor.Descriptor, ref targetDescriptor.Descriptor.TextDescriptors[j], properties, 0, parentColor, cachedProp);
                    }
                }
            }
        }
    }
}
