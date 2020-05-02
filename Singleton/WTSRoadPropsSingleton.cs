using ColossalFramework;
using ColossalFramework.Math;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Overrides;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using SpriteFontPlus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.WriteTheSigns.Singleton
{
    public class WTSRoadPropsSingleton : Singleton<WTSRoadPropsSingleton>
    {
        public DynamicSpriteFont DrawFont => FontServer.instance[Data.DefaultFont] ?? FontServer.instance[WTSController.DEFAULT_FONT_KEY];
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
            WTSController.EventOnDistrictChanged += OnDistrictChanged;
            WTSController.EventOnParkChanged += ResetViews;
            NetManagerOverrides.EventSegmentNameChanged += OnNameSeedChanged;
        }

        private void OnNodeChanged(ushort nodeId) => m_updatedStreetPositions[nodeId] = null;

        private void OnNameSeedChanged(ushort segmentId)
        {
            LogUtils.DoLog("onNameSeedChanged");
            m_updatedStreetPositions[NetManager.instance.m_segments.m_buffer[segmentId].m_endNode] = null;
            m_updatedStreetPositions[NetManager.instance.m_segments.m_buffer[segmentId].m_startNode] = null;
        }

        public static void OnDistrictChanged() => instance.ResetViews();
        public static void OnZeroMarkChanged() => instance.ResetViews();
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
            WTSPropRenderingRules.RenderPropMesh(ref cachedProp, cameraInfo, nodeID, boardIdx, secIdx, 0xFFFFFFF, 0, position, Vector4.zero, ref targetDescriptor.Descriptor.m_propName, new Vector3(0, direction) + targetDescriptor.m_propRotation, targetDescriptor.PropScale, targetDescriptor, out Matrix4x4 propMatrix, out bool rendered, new InstanceID { NetNode = nodeID });
            if (rendered)
            {

                for (int j = 0; j < targetDescriptor.Descriptor.m_textDescriptors.Length; j++)
                {
                    if (cameraInfo.CheckRenderDistance(position, 200 * targetDescriptor.Descriptor.m_textDescriptors[j].m_textScale))
                    {
                        if (targetDescriptor.Descriptor.m_textDescriptors[j].m_destinationRelative != DestinationReference.Self)
                        {
                            if (WTSDestinationSingleton.instance.m_updatedDestinations[nodeID] == null)
                            {
                                WriteTheSignsMod.Controller.StartCoroutine(WTSDestinationSingleton.instance.CalculateDestinations(nodeID));
                            }
                        }


                        MaterialPropertyBlock properties = PropManager.instance.m_materialBlock;
                        properties.Clear();
                        WTSPropRenderingRules.RenderTextMesh(nodeID, boardIdx, secIdx, targetDescriptor, propMatrix, ref targetDescriptor.Descriptor.m_textDescriptors[j], properties, DrawFont);
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

                        LogUtils.DoLog($"[n{nodeID}/{i}] pre ProcessRoadSigns");
                        yield return StartCoroutine(ProcessRoadSigns(nodeID, incomingTraffic, outcomingTraffic, rotationOrder, rotationAnglesOrder, i, segmentIid));
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

        #region roadSigns process
        private IEnumerator ProcessRoadSigns(ushort nodeID, HashSet<ushort> incomingTraffic, HashSet<ushort> outcomingTraffic, int[] originalNodeRotation, int[] originalRotationAnglesOrder, int i, ushort segmentIid)
        {
            yield return 0;
            if (m_updatedStreetPositions[nodeID] != false)
            {
                LogUtils.DoLog($"[n{nodeID}/{i}] BREAK D");
                yield break;
            }
            ItemClass classI = Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].Info.m_class;

            int[] rotationOrder, rotationAnglesOrder;
            int idx = Array.IndexOf(originalNodeRotation, i);
            rotationOrder = new int[originalNodeRotation.Length];
            rotationAnglesOrder = new int[originalNodeRotation.Length];
            int itemsAfter = originalNodeRotation.Length - idx;
            Array.Copy(originalNodeRotation, idx, rotationOrder, 0, itemsAfter);
            Array.Copy(originalRotationAnglesOrder, idx, rotationAnglesOrder, 0, itemsAfter);
            if (idx != 0)
            {
                Array.Copy(originalNodeRotation, 0, rotationOrder, itemsAfter, originalNodeRotation.Length - itemsAfter);
                Array.Copy(originalRotationAnglesOrder, 0, rotationAnglesOrder, itemsAfter, originalNodeRotation.Length - itemsAfter);
            }
            int rot0 = rotationAnglesOrder[0];
            rotationAnglesOrder = rotationAnglesOrder.Select(x => (((x - rot0) % 360) + 360) % 360).ToArray();

            int rotationOrderPosition = Array.IndexOf(rotationOrder, i);
            int leftSegmentIdx = rotationOrder[(rotationOrderPosition + rotationOrder.Length - 1) % rotationOrder.Length];
            int rightSegmentIdx = rotationOrder[(rotationOrderPosition + 1) % rotationOrder.Length];
            bool freeSlotLeft = true;
            bool freeSlotRight = true;

            //LogUtils.DoLog($"[n{nodeID}/{i}] rotationOrderPosition = {rotationOrderPosition}; rotationOrder = {string.Join(",", rotationOrder.Select(x => x.ToString()).ToArray())}; originalNodeRotation = {string.Join(",", originalNodeRotation.Select(x => x.ToString()).ToArray())}; rotationAnglesOrder = {string.Join(",", rotationAnglesOrder.Select(x => x.ToString()).ToArray())}; originalNodeRotation = {string.Join(",", originalNodeRotation.Select(x => x.ToString()).ToArray())}");

            var matchingDescriptors = new Stack<Tuple<int, BoardInstanceRoadNodeXml>>(Data.DescriptorRulesOrder.Select((x, y) => Tuple.New(y, x))
                .Where(x => x.Second.PlaceOnSegmentInsteadOfCorner
                && x.Second.AllowsClass(classI)
                && x.Second.Descriptor.m_textDescriptors.All(
                        t =>
                        {
                            if (!t.IsTextRelativeToSegment() || t.m_targetNodeRelative == 0)
                            {
                                return true;
                            }
                            int nodeIdx = (rotationOrder.Length + t.m_targetNodeRelative) % rotationOrder.Length;
                            return rotationAnglesOrder[nodeIdx] > 40 && rotationAnglesOrder[nodeIdx] < 320;
                        })
                && (WTSDestinationSingleton.instance.m_updatedDestinations[nodeID] != true
                    || x.Second.Descriptor.m_textDescriptors.All(
                        t =>
                        {
                            if (!t.IsTextRelativeToSegment())
                            {
                                return true;
                            }
                            if (Math.Abs(t.m_targetNodeRelative) >= rotationOrder.Length)
                            {
                                return false;
                            }
                            int nodeIdx = (rotationOrder.Length + t.m_targetNodeRelative) % rotationOrder.Length;
                            int segmentIdx = rotationOrder[nodeIdx];
                            return WTSDestinationSingleton.instance.m_couldReachDestinations[nodeID, segmentIdx, (int)t.m_destinationRelative] == true;
                        }))

                ).OrderByDescending(x => x.First));
            int secondaryIdx = 2;
            //LogUtils.DoLog($"[n{nodeID}/{i}] matchingDescriptors = {matchingDescriptors.Count} [{string.Join(",", matchingDescriptors.Select(x => x.Second.SaveName).ToArray())}] ");
            while (matchingDescriptors.Count > 0 && (freeSlotLeft || freeSlotRight))
            {
                BoardInstanceRoadNodeXml targetDescriptor = matchingDescriptors.Pop()?.Second;
                if ((freeSlotLeft || targetDescriptor.RoadSide == RoadSide.RIGHT) && (freeSlotRight || targetDescriptor.RoadSide == RoadSide.LEFT))
                {
                    ProcessDescriptorRoadSign(nodeID, ref NetManager.instance.m_nodes.m_buffer[nodeID], i, segmentIid, ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid], leftSegmentIdx, rightSegmentIdx, targetDescriptor, incomingTraffic, outcomingTraffic, ref secondaryIdx, ref freeSlotLeft, ref freeSlotRight, rotationOrder, rotationAnglesOrder);
                }
                yield return 0;
                //LogUtils.DoLog($"[n{nodeID}/{i}] ENDLOOP matchingDescriptors = {matchingDescriptors.Count} [{string.Join(",", matchingDescriptors.Select(x => x.Second.SaveName).ToArray())}] ");
                if (m_updatedStreetPositions[nodeID] != false)
                {
                    //LogUtils.DoLog($"[n{nodeID}/{i}] BREAK matchingDescriptors = {matchingDescriptors.Count} [{string.Join(",", matchingDescriptors.Select(x => x.Second.SaveName).ToArray())}] ");
                    break;
                }
            }
            lock (Data.BoardsContainers)
            {
                while (secondaryIdx < 4)
                {
                    Data.BoardsContainers[nodeID, i, secondaryIdx++] = null;
                }
            };
        }



        private void ProcessDescriptorRoadSign(
            ushort nodeID, ref NetNode data, int controlBoardIdx,
            ushort segmentIid, ref NetSegment netSegmentI,
            int leftSegmentIdx, int rightSegmentIdx,
            BoardInstanceRoadNodeXml targetDescriptor, HashSet<ushort> incoming,
            HashSet<ushort> outcoming, ref int subboardOffset,
            ref bool leftSlot, ref bool rightSlot,
            int[] nodeRotationOrder, int[] rotationAnglesOrder)
        {
            bool isSegmentIinverted = WTSRoadNodeCommons.CheckSegmentInverted(nodeID, ref netSegmentI);

            if (targetDescriptor?.Descriptor?.m_propName == null
                || new Randomizer(segmentIid).UInt32(255) > targetDescriptor.SpawnChance
                || !CalculatePositionRoadSign(nodeID, ref data, segmentIid, ref netSegmentI, leftSegmentIdx, rightSegmentIdx, targetDescriptor, incoming, outcoming, isSegmentIinverted, out Vector3 platePosI))
            {
                return;
            }
            bool render = true;
            lock (Data.BoardsContainers)
            {
                CacheRoadNodeItem a = FillCacheData(nodeID, controlBoardIdx, subboardOffset++, segmentIid, platePosI, new Vector3(0, (netSegmentI.m_startNode == nodeID ? netSegmentI.m_startDirection : netSegmentI.m_endDirection).GetAngleXZ() - 90, 0), targetDescriptor, nodeRotationOrder, rotationAnglesOrder, ref render);
            }
            if (render)
            {
                LogUtils.DoLog($"[n{nodeID}] renderTrue");
                leftSlot &= targetDescriptor.RoadSide == RoadSide.RIGHT;
                rightSlot &= targetDescriptor.RoadSide == RoadSide.LEFT;
            }
            else
            {
                LogUtils.DoLog($"[n{nodeID}] renderFalse");
                subboardOffset--;
            }
        }

        private static bool CalculatePositionRoadSign(ushort nodeID, ref NetNode data, ushort segmentExtId, ref NetSegment netSegmentExt, int leftSegmentIdx, int rightSegmentIdx, BoardInstanceRoadNodeXml targetDescriptor, HashSet<ushort> incoming, HashSet<ushort> outcoming, bool isSegmentExtInverted, out Vector3 platePosExt)
        {
            LogUtils.DoLog($"[n{nodeID}]Int = ;Ext = {segmentExtId}");
            if (!targetDescriptor.AllowsClass(netSegmentExt.Info.m_class))
            {
                LogUtils.DoLog($"[n{nodeID}]NOT ALLOWED");
                platePosExt = default;
                return false;
            }
            ushort leftSegmentId = data.GetSegment(leftSegmentIdx);
            ushort rightSegmentId = data.GetSegment(rightSegmentIdx);
            ref NetSegment leftSegment = ref NetManager.instance.m_segments.m_buffer[leftSegmentId];
            ref NetSegment rightSegment = ref NetManager.instance.m_segments.m_buffer[rightSegmentId];

            LogUtils.DoLog($"[n{nodeID}]leftSegmentId = {leftSegmentId}; rightSegmentId = {rightSegmentId}");

            if (targetDescriptor.RoadSide == RoadSide.LEFT)
            {
                platePosExt = CaculateCenterSegment(nodeID, ref netSegmentExt, ref leftSegment, segmentExtId, targetDescriptor, targetDescriptor.RoadSide);
            }
            else
            {
                platePosExt = CaculateCenterSegment(nodeID, ref netSegmentExt, ref rightSegment, segmentExtId, targetDescriptor, targetDescriptor.RoadSide);
            }

            if (targetDescriptor.TrafficDirectionRequired == TrafficDirectionRequired.OUTCOMING)
            {
                return outcoming.Contains(segmentExtId)
                    && EnsureTrafficLanes(ref netSegmentExt, targetDescriptor, !isSegmentExtInverted)
                    && EnsureRoadWidth(ref netSegmentExt, targetDescriptor)
                    //&& ((incoming.Where(x => x != segmentExtId).Count() > 1 && netSegmentExt.Info.m_forwardVehicleLaneCount == 1 && !netSegmentExt.Info.m_hasBackwardVehicleLanes && netSegmentExt.Info.m_netAI is RoadBaseAI aiExt && aiExt.m_highwayRules)
                    && (incoming.Where(x => x != segmentExtId).Count() > 1 || !SegmentUtils.IsSameName(incoming.First(), segmentExtId, false, false, true, false, false))
                        ;
            }
            else if (targetDescriptor.TrafficDirectionRequired == TrafficDirectionRequired.INCOMING)
            {
                return incoming.Contains(segmentExtId)
                    && EnsureTrafficLanes(ref netSegmentExt, targetDescriptor, !isSegmentExtInverted)
                    && EnsureRoadWidth(ref netSegmentExt, targetDescriptor)
                   //&& ((outcoming.Where(x => x != segmentExtId).Count() > 1 && netSegmentExt.Info.m_forwardVehicleLaneCount == 1 && !netSegmentExt.Info.m_hasBackwardVehicleLanes && netSegmentExt.Info.m_netAI is RoadBaseAI aiExt && aiExt.m_highwayRules)
                   && outcoming.Where(x => x != segmentExtId).Count() > 1
                    ;
            }
            else
            {
                return EnsureTrafficLanes(ref netSegmentExt, targetDescriptor, !isSegmentExtInverted) && EnsureRoadWidth(ref netSegmentExt, targetDescriptor) && (!SegmentUtils.IsSameName(outcoming.First(), segmentExtId, false, false, true, false, false));
            }

        }
        private static bool EnsureTrafficLanes(ref NetSegment netSegment, BoardInstanceRoadNodeXml targetDescriptor, bool isSegmentInverted)
        {
            return (isSegmentInverted && netSegment.Info.m_backwardVehicleLaneCount >= targetDescriptor.MinDirectionTrafficLanes && netSegment.Info.m_backwardVehicleLaneCount <= targetDescriptor.MaxDirectionTrafflcLanes)
               || (!isSegmentInverted && netSegment.Info.m_forwardVehicleLaneCount >= targetDescriptor.MinDirectionTrafficLanes && netSegment.Info.m_forwardVehicleLaneCount <= targetDescriptor.MaxDirectionTrafflcLanes);
        }

        private static bool EnsureRoadWidth(ref NetSegment netSegment, BoardInstanceRoadNodeXml targetDescriptor) => netSegment.Info?.m_halfWidth < targetDescriptor.MaxRoadHalfWidth + 0.002f && netSegment.Info?.m_halfWidth > targetDescriptor.MinRoadHalfWidth - 0.002f;
        #endregion

        #region Corner process
        private static Vector3 CaculateCenterSegment(int nodeID, ref NetSegment segmentI, ref NetSegment segmentJ, ushort segmentId, BoardInstanceRoadNodeXml descriptor, RoadSide roadSide)
        {
            bool comingFromNode = segmentI.m_startNode == nodeID;
            bool invertedSegment = (segmentI.m_flags & NetSegment.Flags.Invert) != 0 != comingFromNode != (SimulationManager.instance.m_metaData.m_invertTraffic == SimulationMetaData.MetaBool.False);
            Vector3 platePosI;
            Vector3 bezierPos;
            float angle;
            float targetOffsetX;
            if (descriptor.PropPositionZ < 1)
            {
                bezierPos = segmentI.GetBezier().Position(Mathf.Max(0, Mathf.Min(1, (descriptor.m_propPosition.z / (invertedSegment ? 2 : -2)) + 0.5f)));
                angle = default;
                targetOffsetX = descriptor.m_propPosition.x;
            }
            else
            {
                segmentI.CalculateCorner(segmentId, true, comingFromNode, roadSide == RoadSide.LEFT, out bezierPos, out Vector3 angleVec, out _);
                if (roadSide == RoadSide.CENTER)
                {
                    segmentI.CalculateCorner(segmentId, true, comingFromNode, true, out Vector3 bezierPos2, out Vector3 angleVec2, out _);
                    bezierPos = (bezierPos + bezierPos2) / 2;
                    angleVec = (angleVec + angleVec2) / 2;
                }
                targetOffsetX = descriptor.m_propPosition.x * (segmentI.Info.m_pavementWidth + segmentJ.Info.m_pavementWidth) / (roadSide == RoadSide.LEFT ? 2 : -2);
                angle = angleVec.GetAngleXZ();

                invertedSegment ^= angleVec.GetAngleXZ() < 0;

                LogUtils.DoLog($"[n{nodeID}]{segmentId} angleVec = ({angleVec.GetAngleXZ()}) (roadSide = {roadSide})");
            }


            segmentI.GetClosestPositionAndDirection(bezierPos, out _, out Vector3 dir);
            int rotationOffsetSide = invertedSegment ? 90 : -90;
            float rotation = dir.GetAngleXZ();

            Vector3 rotationVectorSide = VectorUtils.X_Y(KlyteMathUtils.DegreeToVector2(rotation + rotationOffsetSide - angle));
            platePosI = bezierPos + (descriptor.PropPositionZ < 1 ? roadSide == RoadSide.CENTER ? default : rotationVectorSide * (segmentI.Info.m_halfWidth - segmentI.Info.m_pavementWidth) : default) + (rotationVectorSide * targetOffsetX);
            platePosI.y += descriptor.m_propPosition.y;

            return platePosI;
        }

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
                .Where(x => !x.Second.PlaceOnSegmentInsteadOfCorner && (x.Second.AllowsClass(classI) || x.Second.AllowsClass(classJ))
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
            refBoard.m_cachedColor = targetDescriptor.UseDistrictColor ? WTSHookable.GetDistrictColor(refBoard.m_districtId) : targetDescriptor.Descriptor.FixedColor ?? Color.white;
            refBoard.m_cachedContrastColor = KlyteMonoUtils.ContrastColor(refBoard.m_cachedColor);
            refBoard.m_distanceRef = Vector2.Distance(VectorUtils.XZ(refBoard.m_platePosition), WTSHookable.GetStartPoint());
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
