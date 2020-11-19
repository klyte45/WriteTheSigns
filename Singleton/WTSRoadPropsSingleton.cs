using ColossalFramework;
using ColossalFramework.Math;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Overrides;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.WriteTheSigns.Singleton
{
    public class WTSRoadPropsSingleton : MonoBehaviour
    {
        public WTSRoadNodesData Data => WTSRoadNodesData.Instance;
        public bool?[] m_updatedStreetPositions;
        public uint[] m_lastFrameUpdate;

        private readonly Func<RenderManager, uint> m_getCurrentFrame = ReflectionUtils.GetGetFieldDelegate<RenderManager, uint>("m_currentFrame", typeof(RenderManager));

        private Dictionary<ItemClass, List<NetInfo>> m_allClasses;

        public Dictionary<ItemClass, List<NetInfo>> AllClasses
        {
            get {
                if (m_allClasses == null)
                {
                    m_allClasses = ((FastList<PrefabCollection<NetInfo>.PrefabData>)typeof(PrefabCollection<NetInfo>).GetField("m_scenePrefabs", RedirectorUtils.allFlags).GetValue(null)).m_buffer
                        .Select(x => x.m_prefab)
                        .Where(x => x?.m_class != null && x.m_class.m_service == ItemClass.Service.Road)
                        .GroupBy(x => x.m_class.name)
                        .ToDictionary(x => x.First().m_class, x => x.ToList());
                }
                return m_allClasses;
            }
        }

        #region Initialize
        public void Awake()
        {

            m_lastFrameUpdate = new uint[NetManager.MAX_NODE_COUNT];
            m_updatedStreetPositions = new bool?[Data.ObjArraySize];
        }

        public void Start()
        {
            NetManagerOverrides.EventNodeChanged += OnNodeChanged;
            WriteTheSignsMod.Controller.EventOnParkChanged += ResetViews;
            NetManagerOverrides.EventSegmentNameChanged += OnNameSeedChanged;
            WriteTheSignsMod.Controller.EventOnDistrictChanged += ResetViews;
            WriteTheSignsMod.Controller.EventOnZeroMarkerChanged += ResetViews;
            WriteTheSignsMod.Controller.EventOnPostalCodeChanged += ResetViews;
        }

        private void OnNodeChanged(ushort nodeId) => m_updatedStreetPositions[nodeId] = null;

        private void OnNameSeedChanged(ushort segmentId)
        {
            LogUtils.DoLog("onNameSeedChanged");
            m_updatedStreetPositions[NetManager.instance.m_segments.m_buffer[segmentId].m_endNode] = null;
            m_updatedStreetPositions[NetManager.instance.m_segments.m_buffer[segmentId].m_startNode] = null;
        }

        public void ResetViews() => m_updatedStreetPositions = new bool?[Data.ObjArraySize];
        #endregion



        public void AfterRenderInstanceImpl(RenderManager.CameraInfo cameraInfo, ushort nodeID, ref NetNode data)
        {
            if (Data.DescriptorRulesOrder == null)
            {
                Data.DescriptorRulesOrder = new BoardInstanceRoadNodeXml[0];
            }
            if (Data.DescriptorRulesOrder.Length == 0 || m_lastFrameUpdate[nodeID] == m_getCurrentFrame(RenderManager.instance))
            {
                return;
            }

            m_lastFrameUpdate[nodeID] = m_getCurrentFrame(RenderManager.instance);
            if (data.CountSegments() < 2)
            {
                return;
            }
            if (m_updatedStreetPositions[nodeID] == null)
            {
                LogUtils.DoLog($"m_updatedStreetPositions[{nodeID}] == null!");
                m_updatedStreetPositions[nodeID] = false;
                WriteTheSignsMod.Controller.StartCoroutine(CalculateSigns(nodeID));
            }

            RenderNodeSigns(cameraInfo, nodeID);
        }


        internal bool CalculateGroupData(ushort nodeID, int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays)
        {
            var result = false;
            for (int y = 0; y < Data.BoardsContainers.GetLength(1); y++)
            {
                for (int z = 0; z < Data.BoardsContainers.GetLength(2); z++)
                {
                    ref CacheRoadNodeItem item = ref Data.BoardsContainers[nodeID, y, z];
                    if (item?.m_renderPlate ?? false)
                    {
                        ref BoardInstanceRoadNodeXml targetDescriptor = ref item.m_currentDescriptor;
                        if (targetDescriptor?.Descriptor?.m_propName == null)
                        {
                            continue;
                        }

                        if (item.m_cachedProp?.name != targetDescriptor?.Descriptor?.m_propName)
                        {
                            item.m_cachedProp = null;
                        }
                        WTSDynamicTextRenderingRules.EnsurePropCache(ref item.m_cachedProp, nodeID, y, z, ref targetDescriptor.Descriptor.m_propName, targetDescriptor.Descriptor, targetDescriptor, out bool rendered);
                        if (rendered)
                        {
                            result = PropInstance.CalculateGroupData(item.m_cachedProp, layer, ref vertexCount, ref triangleCount, ref objectCount, ref vertexArrays);
                        }
                    }
                }
            }
            return result;
        }
        internal void PopulateGroupData(ushort nodeID, int layer, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance)
        {
            for (int y = 0; y < Data.BoardsContainers.GetLength(1); y++)
            {
                for (int z = 0; z < Data.BoardsContainers.GetLength(2); z++)
                {
                    ref CacheRoadNodeItem item = ref Data.BoardsContainers[nodeID, y, z];
                    if (item?.m_renderPlate ?? false)
                    {
                        BoardInstanceRoadNodeXml targetDescriptor = item.m_currentDescriptor;
                        if (targetDescriptor?.Descriptor?.m_propName == null || item.m_cachedProp == null)
                        {
                            continue;
                        }
                        WTSDynamicTextRenderingRules.PropInstancePopulateGroupData(item.m_cachedProp, layer, new InstanceID { NetNode = nodeID }, item.m_platePosition, targetDescriptor.Scale, new Vector3(0, item.m_streetDirection, 0), ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
                    }
                }
            }
        }

        private void RenderNodeSigns(RenderManager.CameraInfo cameraInfo, ushort nodeID)
        {
            for (int y = 0; y < Data.BoardsContainers.GetLength(1); y++)
            {
                for (int z = 0; z < Data.BoardsContainers.GetLength(2); z++)
                {
                    ref CacheRoadNodeItem item = ref Data.BoardsContainers[nodeID, y, z];
                    if (item?.m_renderPlate ?? false)
                    {
                        ref BoardInstanceRoadNodeXml targetDescriptor = ref item.m_currentDescriptor;
                        if (targetDescriptor?.Descriptor?.m_propName == null)
                        {
                            continue;
                        }

                        if (item.m_cachedProp?.name != targetDescriptor?.Descriptor?.m_propName)
                        {
                            item.m_cachedProp = null;
                        }
                        RenderSign(cameraInfo, nodeID, y, z, item.m_platePosition, item.m_streetDirection, ref targetDescriptor, ref item.m_cachedProp);
                    }
                }
            }
        }

        private void RenderSign(RenderManager.CameraInfo cameraInfo, ushort nodeID, int boardIdx, int secIdx, Vector3 position, float direction, ref BoardInstanceRoadNodeXml targetDescriptor, ref PropInfo cachedProp)
        {
            Color parentColor = WTSDynamicTextRenderingRules.RenderPropMesh(ref cachedProp, cameraInfo, nodeID, boardIdx, secIdx, 0xFFFFFFF, 0, position, Vector4.zero, ref targetDescriptor.Descriptor.m_propName, new Vector3(0, direction) + targetDescriptor.PropRotation, targetDescriptor.PropScale, targetDescriptor.Descriptor, targetDescriptor, out Matrix4x4 propMatrix, out bool rendered, new InstanceID { NetNode = nodeID });
            if (rendered)
            {

                for (int j = 0; j < targetDescriptor.Descriptor.TextDescriptors.Length; j++)
                {
                    if (cameraInfo.CheckRenderDistance(position, 200 * targetDescriptor.Descriptor.TextDescriptors[j].m_textScale * (targetDescriptor.Descriptor.TextDescriptors[j].IlluminationConfig.IlluminationType == FontStashSharp.MaterialType.OPAQUE ? 1 : 3)))
                    {
                        MaterialPropertyBlock properties = PropManager.instance.m_materialBlock;
                        properties.Clear();
                        WTSDynamicTextRenderingRules.RenderTextMesh(nodeID, boardIdx, secIdx, targetDescriptor, propMatrix, targetDescriptor.Descriptor, ref targetDescriptor.Descriptor.TextDescriptors[j], properties, 0, parentColor, cachedProp);
                    }
                }
            }
        }

        private IEnumerator CalculateSigns(ushort nodeID)
        {
            yield return 0;
            if (m_updatedStreetPositions[nodeID] != false)
            {
                yield break;
            }

            WTSRoadNodeCommons.GetIncomingOutcomingTraffic(nodeID, out HashSet<ushort> incomingTraffic, out HashSet<ushort> outcomingTraffic, out int[] rotationOrder, out int[] rotationAnglesOrder);

            yield return 0;
            if (m_updatedStreetPositions[nodeID] != false)
            {
                yield break;
            }
            for (int i = 0; i < 8; i++)
            {
                ushort segmentIid = NetManager.instance.m_nodes.m_buffer[nodeID].GetSegment(i);
                if (segmentIid != 0)
                {
                    if (Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].Info != null && Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].Info.m_netAI is RoadBaseAI)
                    {
                        LogUtils.DoLog($"[n{nodeID}/{i}] pre ProcessCorners");
                        yield return StartCoroutine(ProcessCorners(nodeID, rotationOrder, rotationAnglesOrder, i, segmentIid));
                        if (m_updatedStreetPositions[nodeID] != false)
                        {
                            yield break;
                        }
                    }
                }
            }

            m_updatedStreetPositions[nodeID] = true;
            LogUtils.DoLog($"[n{nodeID}/] END PROCESS!");
        }

        #region Corner process


        private bool ProcessDescriptorCorner(
          ushort nodeID, ref NetNode data, int controlBoardIdx,
            ushort segmentIid, ref NetSegment netSegmentI, Vector3 segmentIDirection,
            ushort segmentJid, ref NetSegment netSegmentJ, Vector3 segmentJDirection,
            BoardInstanceRoadNodeXml targetDescriptor, int[] nodeRotationOrder, int[] anglesRotationOrder,
            ref int subboardOffset)
        {
            if (targetDescriptor?.Descriptor?.m_propName == null)
            {
                return false;
            }

            if (segmentJid == 0
                || !(Singleton<NetManager>.instance.m_segments.m_buffer[segmentJid].Info.m_netAI is RoadBaseAI)
                || SegmentUtils.IsSameName(segmentJid, segmentIid, false, false, true, targetDescriptor.PlaceOnDistrictBorder, targetDescriptor.PlaceOnTunnelBridgeStart)
                || (targetDescriptor.IgnoreEmptyNameRoads && (Singleton<NetManager>.instance.GetSegmentName(segmentIid).IsNullOrWhiteSpace() || Singleton<NetManager>.instance.GetSegmentName(segmentJid).IsNullOrWhiteSpace()))
                || new Randomizer(segmentIid | (segmentJid << 16)).UInt32(255) > targetDescriptor.SpawnChance)
            {
                return false;
            }
            bool renderJ = true;
            bool renderI = true;
            CalculateCenterCorner(nodeID, ref data, segmentIid, ref netSegmentI, ref segmentIDirection, segmentJid, ref netSegmentJ, ref segmentJDirection, out Vector3 platePosI, out Vector3 platePosJ);

            if (!renderI && !renderJ)
            {
                return false;
            }
            int id1 = subboardOffset++;
            int id2 = subboardOffset++;
            lock (Data.BoardsContainers)
            {
                FillCacheData(nodeID, controlBoardIdx, id1, segmentIid, platePosI, segmentIDirection, targetDescriptor, nodeRotationOrder, anglesRotationOrder, ref renderI);
                FillCacheData(nodeID, controlBoardIdx, id2, segmentJid, platePosJ, segmentJDirection, targetDescriptor, nodeRotationOrder, anglesRotationOrder, ref renderJ);
            }
            if (!renderI && !renderJ)
            {
                subboardOffset--;
                subboardOffset--;
                return false;
            }
            return true;
        }

        private IEnumerator ProcessCorners(ushort nodeID, int[] nodeRotationOrder, int[] anglesRotationOrder, int i, ushort segmentIid)
        {
            yield return 0;
            if (m_updatedStreetPositions[nodeID] != false)
            {
                LogUtils.DoLog($"[n{nodeID}/{i}] BREAK J");
                yield break;
            }
            GetNeighborSegment(nodeID, ref NetManager.instance.m_nodes.m_buffer[nodeID], segmentIid, ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid], out Vector3 segmentIDirection, out Vector3 segmentJDirection, out ushort segmentJid, out bool invertIJ);
            int j = GetSegmentIndex(ref NetManager.instance.m_nodes.m_buffer[nodeID], segmentJid);

            ItemClass classI = Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].Info.m_class;
            ItemClass classJ = Singleton<NetManager>.instance.m_segments.m_buffer[segmentJid].Info.m_class;
            var matchingDescriptors = new Stack<Tuple<int, BoardInstanceRoadNodeXml>>(Data.DescriptorRulesOrder.Select((x, y) => Tuple.New(y, x))
                .Where(x => (x.Second.AllowsClass(classI) || x.Second.AllowsClass(classJ))
                ).OrderByDescending(x => x.First));
            int secondaryIdx = 0;
            bool hasSpawned = false;
            BoardInstanceRoadNodeXml targetDescriptor = null;
            while (matchingDescriptors.Count > 0 && !hasSpawned)
            {
                targetDescriptor = matchingDescriptors.Pop()?.Second;
                hasSpawned = ProcessDescriptorCorner(nodeID, ref NetManager.instance.m_nodes.m_buffer[nodeID], i, segmentIid, ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid], segmentIDirection, segmentJid, ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentJid], segmentJDirection, targetDescriptor, nodeRotationOrder, anglesRotationOrder, ref secondaryIdx);

                yield return 0;
                if (m_updatedStreetPositions[nodeID] != false)
                {
                    LogUtils.DoLog($"[n{nodeID}/{i}] BREAK K");
                    yield break;
                }
            }
            lock (Data.BoardsContainers)
            {
                while (secondaryIdx < 2)
                {
                    Data.BoardsContainers[nodeID, i, secondaryIdx++] = null;
                }
            };
        }

        private static void GetNeighborSegment(ushort nodeID, ref NetNode data, ushort segmentIid, ref NetSegment netSegmentI, out Vector3 segmentIDirection, out Vector3 otherSegmentDirection, out ushort resultOtherSegment, out bool invertIJ)
        {
            segmentIDirection = nodeID != netSegmentI.m_startNode ? netSegmentI.m_endDirection : netSegmentI.m_startDirection;
            otherSegmentDirection = Vector3.zero;
            float resultAngle = -4f;
            resultOtherSegment = 0;
            for (int j = 0; j < 8; j++)
            {
                ushort segmentJid = data.GetSegment(j);
                if (segmentJid != 0 && segmentJid != segmentIid)
                {
                    ref NetSegment netSegmentCand = ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentJid];
                    if (netSegmentCand.Info != null)
                    {
                        Vector3 segmentJDirection = nodeID != netSegmentCand.m_startNode ? netSegmentCand.m_endDirection : netSegmentCand.m_startDirection;
                        float angle = (segmentIDirection.x * segmentJDirection.x) + (segmentIDirection.z * segmentJDirection.z);
                        if ((segmentJDirection.z * segmentIDirection.x) - (segmentJDirection.x * segmentIDirection.z) < 0f)
                        {
                            if (angle > resultAngle)
                            {
                                resultAngle = angle;
                                resultOtherSegment = segmentJid;
                                otherSegmentDirection = segmentJDirection;
                            }
                        }
                        else
                        {
                            angle = -2f - angle;
                            if (angle > resultAngle)
                            {
                                resultAngle = angle;
                                resultOtherSegment = segmentJid;
                                otherSegmentDirection = segmentJDirection;
                            }
                        }
                    }
                }
            }
            invertIJ = resultAngle < -2f;
        }
        private static void CalculateCenterCorner(ushort nodeID, ref NetNode data, ushort segmentIid, ref NetSegment netSegmentI, ref Vector3 segmentIDirection, ushort segmentJid, ref NetSegment netSegmentJ, ref Vector3 segmentJDirection, out Vector3 platePosI, out Vector3 platePosJ)
        {
            bool start = netSegmentI.m_startNode == nodeID;
            netSegmentI.CalculateCorner(segmentIid, true, start, false, out Vector3 startPos, out Vector3 startAng, out _);

            start = netSegmentJ.m_startNode == nodeID;
            netSegmentJ.CalculateCorner(segmentJid, true, start, true, out Vector3 endPos, out Vector3 endAng, out _);

            NetSegment.CalculateMiddlePoints(startPos, -startAng, endPos, -endAng, true, true, out Vector3 rhs, out Vector3 lhs);
            Vector3 relativePos = ((rhs + lhs) * 0.5f) - data.m_position;
            platePosI = platePosJ = relativePos - relativePos.normalized + data.m_position;
            segmentIDirection.y = segmentJDirection.y = 0;
        }
        #endregion

        #region Cache write
        private ref CacheRoadNodeItem FillCacheData(ushort nodeID, int controlBoardIdx, int boardId, ushort segmentIid, Vector3 platePosI, Vector3 segmentIDirection, BoardInstanceRoadNodeXml targetDescriptor, int[] rotationOrder, int[] rotationAnglesOrder, ref bool render)
        {

            Data.BoardsContainers[nodeID, controlBoardIdx, boardId] = new CacheRoadNodeItem();
            ref CacheRoadNodeItem refBoard = ref Data.BoardsContainers[nodeID, controlBoardIdx, boardId];

            float dir = Vector2.zero.GetAngleToPoint(VectorUtils.XZ(segmentIDirection));

            refBoard.m_districtId = DistrictManager.instance.GetDistrict(NetManager.instance.m_segments.m_buffer[segmentIid].m_middlePosition);
            refBoard.m_districtParkId = DistrictManager.instance.GetPark(NetManager.instance.m_segments.m_buffer[segmentIid].m_middlePosition);
            render = refBoard.m_renderPlate = render && targetDescriptor.Allows(refBoard.m_districtParkId, refBoard.m_districtId);
            refBoard.m_platePosition = platePosI;
            refBoard.m_streetDirection = -dir + 90 + segmentIDirection.y;
            refBoard.m_segmentId = segmentIid;
            refBoard.m_cachedColor = targetDescriptor.UseDistrictColor ? WriteTheSignsMod.Controller.ConnectorADR.GetDistrictColor(refBoard.m_districtId) : targetDescriptor.Descriptor.FixedColor ?? Color.white;
            refBoard.m_cachedContrastColor = KlyteMonoUtils.ContrastColor(refBoard.m_cachedColor);
            refBoard.m_distanceRef = Vector2.Distance(VectorUtils.XZ(refBoard.m_platePosition), WriteTheSignsMod.Controller.ConnectorADR.GetStartPoint());
            refBoard.m_distanceRefKm = Mathf.RoundToInt(refBoard.m_distanceRef / 1000);
            refBoard.m_currentDescriptor = targetDescriptor;
            refBoard.m_segnentIndex = GetSegmentIndex(ref NetManager.instance.m_nodes.m_buffer[nodeID], segmentIid);
            refBoard.m_nodesOrder = rotationOrder;
            refBoard.m_nodesRotation = rotationAnglesOrder;
            return ref Data.BoardsContainers[nodeID, controlBoardIdx, boardId];
        }

        private int GetSegmentIndex(ref NetNode node, int segmentId)
        {
            if (segmentId == node.m_segment0)
            {
                return 0;
            }

            if (segmentId == node.m_segment1)
            {
                return 1;
            }

            if (segmentId == node.m_segment2)
            {
                return 2;
            }

            if (segmentId == node.m_segment3)
            {
                return 3;
            }

            if (segmentId == node.m_segment4)
            {
                return 4;
            }

            if (segmentId == node.m_segment5)
            {
                return 5;
            }

            if (segmentId == node.m_segment6)
            {
                return 6;
            }

            if (segmentId == node.m_segment7)
            {
                return 7;
            }

            return -1;
        }
        #endregion




    }
}
