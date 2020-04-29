using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using SpriteFontPlus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.Utils.SegmentUtils;

namespace Klyte.WriteTheSigns.Overrides
{

    public class BoardGeneratorRoadNodes : Redirector, IRedirectable
    {
        public static readonly int DESTINATIONS_CALC_COUNT = (int)(Enum.GetValues(typeof(DestinationReference)) as DestinationReference[]).Max() + 1;

        public static BoardGeneratorRoadNodes Instance;
        public DynamicSpriteFont DrawFont => FontServer.instance[Data.DefaultFont] ?? FontServer.instance[WTSController.DEFAULT_FONT_KEY];
        public WTSRoadNodesData Data => WTSRoadNodesData.Instance;
        public bool?[] m_updatedStreetPositions;
        public bool?[] m_updatedDestinations;
        public bool?[,,] m_couldReachDestinations;
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
            Instance = this;

            m_lastFrameUpdate = new uint[NetManager.MAX_NODE_COUNT];
            m_updatedStreetPositions = new bool?[Data.ObjArraySize];
            m_updatedDestinations = new bool?[Data.ObjArraySize];
            m_couldReachDestinations = new bool?[Data.ObjArraySize, Data.BoardCount, DESTINATIONS_CALC_COUNT];


            var adrEventsType = Type.GetType("Klyte.Addresses.ModShared.AdrEvents, KlyteAddresses");
            if (adrEventsType != null)
            {
                static void RegisterEvent(string eventName, Type adrEventsType, Action action) => adrEventsType.GetEvent(eventName)?.AddEventHandler(null, action);
                RegisterEvent("EventZeroMarkerBuildingChange", adrEventsType, new Action(OnZeroMarkChanged));
                RegisterEvent("EventRoadNamingChange", adrEventsType, new Action(OnZeroMarkChanged));
                RegisterEvent("EventDistrictColorChanged", adrEventsType, new Action(OnDistrictChanged));
                RegisterEvent("EventBuildingNameStrategyChanged", adrEventsType, new Action(OnZeroMarkChanged));
            }

            #region Hooks
            System.Reflection.MethodInfo postRenderMeshs = GetType().GetMethod("AfterRenderSegment", RedirectorUtils.allFlags);
            System.Reflection.MethodInfo orig = typeof(NetSegment).GetMethod("RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int) });
            LogUtils.DoLog($"Patching: {orig} => {postRenderMeshs} {postRenderMeshs.IsStatic}");
            AddRedirect(orig, null, postRenderMeshs);
            #endregion
        }

        public void Start()
        {
            NetManagerOverrides.EventNodeChanged += OnNodeChanged;
            WTSController.EventOnDistrictChanged += OnDistrictChanged;
            WTSController.EventOnParkChanged += ResetViews;
            NetManagerOverrides.EventSegmentNameChanged += OnNameSeedChanged;
        }

        private void OnNodeChanged(ushort nodeId)
        {
            m_updatedStreetPositions[nodeId] = null;
            InvalidateNodeDestinationPaths(nodeId);
        }

        private void OnNameSeedChanged(ushort segmentId)
        {
            LogUtils.DoLog("onNameSeedChanged");
            m_updatedStreetPositions[NetManager.instance.m_segments.m_buffer[segmentId].m_endNode] = null;
            m_updatedStreetPositions[NetManager.instance.m_segments.m_buffer[segmentId].m_startNode] = null;
            InvalidateNodeDestinationPaths(NetManager.instance.m_segments.m_buffer[segmentId].m_endNode);
            InvalidateNodeDestinationPaths(NetManager.instance.m_segments.m_buffer[segmentId].m_startNode);
        }

        public void OnDistrictChanged() => ResetViews();
        public void OnZeroMarkChanged() => ResetViews();

        public void ResetViews()
        {
            m_updatedStreetPositions = new bool?[Data.ObjArraySize];
            m_updatedDestinations = new bool?[Data.ObjArraySize];
            m_couldReachDestinations = new bool?[Data.ObjArraySize, Data.BoardCount, DESTINATIONS_CALC_COUNT];
            WriteTheSignsMod.Controller?.StopAllCoroutines();
        }
        #endregion

        public static void AfterRenderSegment(RenderManager.CameraInfo cameraInfo, ushort segmentID)
        {
            Instance.AfterRenderInstanceImpl(cameraInfo, NetManager.instance.m_segments.m_buffer[segmentID].m_startNode, ref NetManager.instance.m_nodes.m_buffer[NetManager.instance.m_segments.m_buffer[segmentID].m_startNode]);
            Instance.AfterRenderInstanceImpl(cameraInfo, NetManager.instance.m_segments.m_buffer[segmentID].m_endNode, ref NetManager.instance.m_nodes.m_buffer[NetManager.instance.m_segments.m_buffer[segmentID].m_endNode]);
        }


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
                m_updatedStreetPositions[nodeID] = false;
                WriteTheSignsMod.Controller.StartCoroutine(CalculateSigns(nodeID));
            }

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
            WTSPropRenderingRules.RenderPropMesh(ref cachedProp, cameraInfo, nodeID, boardIdx, secIdx, 0xFFFFFFF, 0, position, Vector4.zero, ref targetDescriptor.Descriptor.m_propName, new Vector3(0, direction) + targetDescriptor.m_propRotation, targetDescriptor.PropScale, targetDescriptor, out Matrix4x4 propMatrix, out bool rendered, GetPropRenderID(nodeID));
            if (rendered)
            {

                for (int j = 0; j < targetDescriptor.Descriptor.m_textDescriptors.Length; j++)
                {
                    if (cameraInfo.CheckRenderDistance(position, 200 * targetDescriptor.Descriptor.m_textDescriptors[j].m_textScale))
                    {
                        if (targetDescriptor.Descriptor.m_textDescriptors[j].m_destinationRelative != DestinationReference.Self)
                        {
                            if (m_updatedDestinations[nodeID] == null)
                            {
                                WriteTheSignsMod.Controller.StartCoroutine(CalculateDestinations(nodeID));
                            }
                        }


                        MaterialPropertyBlock properties = PropManager.instance.m_materialBlock;
                        properties.Clear();
                        WTSPropRenderingRules.RenderTextMesh(nodeID, boardIdx, secIdx, targetDescriptor, propMatrix, ref targetDescriptor.Descriptor.m_textDescriptors[j], properties, DrawFont);
                    }
                }
            }
        }

        #region Calculation destination

        private IEnumerator CalculateDestinations(ushort nodeID)
        {
            m_updatedDestinations[nodeID] = false;
            uint startFrame = SimulationManager.instance.m_currentTickIndex;
            LogUtils.DoLog($" m_updatedDestinations[nodeID] = { m_updatedDestinations[nodeID]}");
            yield return 0;
            LogUtils.DoLog($"Start - Calculate destinations for {nodeID} (m_updatedDestinations[nodeID] = { m_updatedDestinations[nodeID]})");
            if (m_updatedDestinations[nodeID] != false)
            {
                LogUtils.DoLog($"End - aborted!");
                yield break;
            }
            GetIncomingOutcomingTraffic(nodeID, out HashSet<ushort> incomingTraffic, out HashSet<ushort> outcomingTraffic);
            if (outcomingTraffic.Count == 0)
            {
                LogUtils.DoLog($"End - no outcoming for {nodeID}");
                yield break;
            }
            for (int i = 0; i < 8; i++)
            {
                ushort targetSegmentId = NetManager.instance.m_nodes.m_buffer[nodeID].GetSegment(i);
                IEnumerable<ushort> thisIncomingTraffic = incomingTraffic.Where(x => !SegmentUtils.IsSameName(targetSegmentId, x));
                LogUtils.DoLog($"CD[{nodeID}] = Start segment {i} (id: {targetSegmentId})!");
                if (targetSegmentId != 0)
                {
                    if (outcomingTraffic.Contains(targetSegmentId))
                    {
                        LogUtils.DoLog($"CD[{nodeID}/{i}] start tracking - MR1");
                        var cd = CoroutineWithData.From(WriteTheSignsMod.Controller, MapSegmentFlow(nodeID, targetSegmentId, 124000, 2, thisIncomingTraffic));
                        // TupleRef<SegmentMappingObject, HashSet<ushort>, bool> cd = MapSegmentFlow(nodeID, targetSegmentId, 124000, 1, thisIncomingTraffic, 10000);
                        yield return cd.Coroutine;
                        if (m_updatedDestinations[nodeID] != false)
                        {
                            LogUtils.DoWarnLog($"[n{nodeID}] STOPPING B");
                            yield break;
                        }
                        if (cd.result.First.Count >= 1)
                        {
                            LogUtils.DoLog($"CD[{nodeID}/{i}] WRITING - MR1");
                            //SegmentUtils.GetSegmentRoadEdges(endSegment.segmentId, false, false, false, out ComparableRoad startRef, out ComparableRoad endRef, out ushort[] nodesPath);
                            MarkSuccessDestination(nodeID, i, DestinationReference.NextExitMainRoad1, cd.result.First[0].segmentId, cd.result.First[0].nodeList, cd.result.First[0].distanceWalked);
                            if (cd.result.First.Count >= 2)
                            {
                                LogUtils.DoLog($"CD[{nodeID}/{i}] WRITING - MR2");
                                MarkSuccessDestination(nodeID, i, DestinationReference.NextExitMainRoad2, cd.result.First[1].segmentId, cd.result.First[1].nodeList, cd.result.First[1].distanceWalked);
                            }
                            else
                            {
                                LogUtils.DoLog($"CD[{nodeID}/{i}] NOTFOUND - MR2");
                                MarkFailureDestination(nodeID, i, DestinationReference.NextExitMainRoad2, cd.result.Second);
                            }
                        }
                        else
                        {
                            LogUtils.DoLog($"CD[{nodeID}/{i}] NOTFOUND - MR1/2");
                            MarkFailureDestination(nodeID, i, DestinationReference.NextExitMainRoad1, cd.result.Second);
                            MarkFailureDestination(nodeID, i, DestinationReference.NextExitMainRoad2, cd.result.Second);
                        }
                    }
                    else
                    {
                        LogUtils.DoLog($"CD[{nodeID}/{i}] has no outcoming!");
                        m_couldReachDestinations[nodeID, i, (int)DestinationReference.NextExitMainRoad1] = false;
                        m_couldReachDestinations[nodeID, i, (int)DestinationReference.NextExitMainRoad2] = false;
                    }

                }
            }
            LogUtils.DoLog($"END - Calculate destinations for {nodeID} - { SimulationManager.instance.m_currentTickIndex - startFrame} Ticks");
            m_updatedDestinations[nodeID] = true;
            yield break;
        }

        private void MarkSuccessDestination(ushort nodeID, int i, DestinationReference destinationId, ushort targetSegmentId, IEnumerable<ushort> trackPath, float distance)
        {
            m_couldReachDestinations[nodeID, i, (int)destinationId] = true;
            ulong pathHash = RegisterHash(nodeID, (uint)i);
            AddAllToHash(trackPath, pathHash);

            ushort targetNodeId = trackPath.Last();
            string outsideConnectionName = null;
            ref NetNode targetNode = ref NetManager.instance.m_nodes.m_buffer[targetNodeId];
            if ((targetNode.m_flags & NetNode.Flags.Outside) != 0)
            {
                ushort targetBuilding = BuildingManager.instance.FindBuilding(targetNode.m_position, 1000, 0, 0, 0, 0);
                if (targetBuilding > 0)
                {
                    outsideConnectionName = BuildingManager.instance.GetBuildingName(targetBuilding, default);
                }
            }
            m_destinationInfo[nodeID, i, (int)destinationId] = new CacheDestinationRoute
            {
                m_segmentId = targetSegmentId,
                m_distance = distance,
                m_distanceKm = Mathf.RoundToInt(distance / 1000),
                m_distanceMeanString = DistanceToMeanString(distance),
                m_nodeId = targetNodeId,
                m_districtId = DistrictManager.instance.GetDistrict(targetNode.m_position),
                m_parkId = DistrictManager.instance.GetPark(targetNode.m_position),
                m_outsideConnectionName = outsideConnectionName
            };
        }

        private void MarkFailureDestination(ushort nodeID, int i, DestinationReference destinationId, IEnumerable<ushort> trackPath)
        {
            m_couldReachDestinations[nodeID, i, (int)destinationId] = false;
            ulong pathHash = RegisterHash(nodeID, (uint)i);
            AddAllToHash(trackPath, pathHash);
            m_destinationInfo[nodeID, i, (int)destinationId] = null;
        }

        private string DistanceToMeanString(float distance)
        {
            if (distance > 100)
            {
                return "100 m";
            }
            else if (distance > 950)
            {
                return $"{Mathf.RoundToInt(distance / 100)}00 m";
            }
            else if (distance > 10000)
            {
                return $"{(distance / 1000).ToString("D1")} km";
            }
            else
            {
                return $"{Mathf.RoundToInt(distance / 1000)} km";
            }
        }

        private class SegmentMapComparer : IComparer<SegmentMappingObject>
        {
            public int Compare(SegmentMappingObject x, SegmentMappingObject y) => ((-x.pts * 2) + x.iteration).CompareTo((-y.pts * 2) + y.iteration);
        }

        private class SegmentMappingObject
        {
            public readonly ushort segmentId;
            public readonly NetSegment refSegment;
            public readonly int pts;
            public readonly float distanceWalked;
            public readonly ushort[] nodeList;
            public readonly int iteration;
            public SegmentMappingObject(SegmentMappingObject reference, ushort segmentId, ref NetSegment refSegment, int pts, float distanceWalked, ushort nodeToAdd, int iteration)
            {
                this.segmentId = segmentId;
                this.refSegment = refSegment;
                this.pts = pts;
                this.distanceWalked = (reference?.distanceWalked ?? 0) + distanceWalked;
                this.iteration = iteration;
                nodeList = (reference?.nodeList ?? new ushort[0]).Union(new ushort[] { nodeToAdd }).ToArray();
            }
            public override string ToString() => $"[s={segmentId}; d={distanceWalked}; pts = {pts}; lastNode = {nodeList.Last()}; i = {iteration}; ord = {(pts * 2) + iteration}]";
        }

        private IEnumerator<TupleRef<List<SegmentMappingObject>, HashSet<ushort>>> MapSegmentFlow(ushort nodeID, ushort targetSegmentId, int minPoints, int nthSegmentFound, IEnumerable<ushort> incomingSegments, SegmentMappingObject cache = null, int maxDepth = 100)
        {
            var priorityQueue = new PriorityQueue<SegmentMappingObject>(new SegmentMapComparer());
            if (cache != null)
            {
                ushort prevNode = cache.nodeList.Last();
                List<SegmentMappingObject> resultingList = MapNode(cache, incomingSegments, prevNode, -1, true);
                foreach (SegmentMappingObject item in resultingList)
                {
                    LogUtils.DoLog($"[CACHE] ADDING TO PQ: {item}");
                    priorityQueue.Push(item);
                }
            }
            else
            {
                priorityQueue.Push(new SegmentMappingObject(null, targetSegmentId, ref NetManager.instance.m_segments.m_buffer[targetSegmentId], CalculatePoints(ref NetManager.instance.m_segments.m_buffer[targetSegmentId]), NetManager.instance.m_segments.m_buffer[targetSegmentId].m_averageLength, nodeID, -1));
            }
            var nodesPassed = new HashSet<ushort>();
            var resultObjectList = new List<SegmentMappingObject>();
            TupleRef<List<SegmentMappingObject>, HashSet<ushort>> result = Tuple.NewRef(ref resultObjectList, ref nodesPassed);
            var banishedStreets = new List<ushort> { targetSegmentId };
            banishedStreets.AddRange(incomingSegments);
            SegmentMappingObject next;
            int iterations = 0;
            do
            {
                if (m_updatedDestinations[nodeID] != false)
                {
                    LogUtils.DoWarnLog($"[n{nodeID}] STOPPING C");
                    yield break;
                }
                next = priorityQueue.Pop();
                float refDivisor = next.pts / (float)minPoints;
                LogUtils.DoLog($"Starting - {next} (refDivisor = {refDivisor})");
                if (next.nodeList.Length / Mathf.Max(0.01f, next.pts == 0 ? 2f : refDivisor * refDivisor) > maxDepth)
                {
                    LogUtils.DoLog($"OVERFLOW! - skipping");
                    continue;
                }
                if (next.pts >= minPoints && banishedStreets.Where(x => IsSameName(x, next.segmentId)).Count() == 0)
                {
                    LogUtils.DoLog($"PTS MATCH ({minPoints}) - {next}");
                    nthSegmentFound--;
                    result.First.Add(next);
                    banishedStreets.Add(next.segmentId);
                }
                if (nthSegmentFound == 0)
                {
                    LogUtils.DoLog($"BREAKING: SEGMENT FOUND");
                    break;
                }
                List<SegmentMappingObject> resultingList = MapSegment(next, incomingSegments, iterations);
                foreach (SegmentMappingObject item in resultingList)
                {
                    if (result.Second.Contains(item.nodeList.Last()))
                    {
                        LogUtils.DoLog($"Item: {item} - Already mapped - skipping");
                        continue;
                    }
                    LogUtils.DoLog($"ADDING TO PQ: {item}");
                    priorityQueue.Push(item);
                }
                result.Second.UnionWith(next.nodeList);

                LogUtils.DoLog($"NEXT TOP PQ: {priorityQueue.Top}");
                iterations++;
                yield return null;
            } while (priorityQueue.Count > 0);
            yield return result;

        }

        private static List<SegmentMappingObject> MapSegment(SegmentMappingObject prevObj, IEnumerable<ushort> incomingSegments, int iteration)
        {
            ushort nodeID = prevObj.nodeList.Last();
            LogUtils.DoLog($"-<{nodeID}>-- Mapping on segment from {nodeID} ");
            var listReturn = new List<SegmentMappingObject>();

            bool canFlowThruThisSegment = CheckIfFlowIsAllowed(nodeID, prevObj.refSegment);
            LogUtils.DoLog($"-<{nodeID}>-- canFlowThruThisSegment({prevObj.segmentId}) ={canFlowThruThisSegment}");
            if (canFlowThruThisSegment)
            {
                ushort nextNodeId = prevObj.refSegment.GetOtherNode(nodeID);
                if (nextNodeId > 0 && !prevObj.nodeList.Contains(nextNodeId))
                {
                    LogUtils.DoLog($"-<{nodeID}>- nextNodeId ={nextNodeId}");
                    listReturn = MapNode(prevObj, incomingSegments, nextNodeId, iteration);
                }
            }
            return listReturn;
        }

        private static List<SegmentMappingObject> MapNode(SegmentMappingObject prevObj, IEnumerable<ushort> incomingSegments, ushort nextNodeId, int iteration, bool ignoreSegmentCheck = false)
        {
            var listReturn = new List<SegmentMappingObject>();
            ref NetNode nextNode = ref NetManager.instance.m_nodes.m_buffer[nextNodeId];
            for (int i = 0; i < 8; i++)
            {
                ushort currentSegment = nextNode.GetSegment(i);
                LogUtils.DoLog($"----<{nextNodeId}>- currentSegment ={currentSegment} (prev={prevObj.segmentId})");
                if (currentSegment > 0 && (ignoreSegmentCheck || currentSegment != prevObj.segmentId) && !incomingSegments.Any(x => SegmentUtils.IsSameName(currentSegment, x)))
                {
                    int pts = CalculatePoints(ref NetManager.instance.m_segments.m_buffer[currentSegment]);
                    // if (pts >= prevObj.pts * 0.5f || pts == 0)
                    //   {
                    listReturn.Add(new SegmentMappingObject(prevObj, currentSegment, ref NetManager.instance.m_segments.m_buffer[currentSegment], pts, NetManager.instance.m_segments.m_buffer[currentSegment].m_averageLength, nextNodeId, iteration));
                    //  }
                }
            }
            return listReturn;
        }

        private static int CalculatePoints(ref NetSegment segment)
        {
            int basePoints = -1;
            NetInfo info = segment.Info;
            ItemClass.Level level = info.GetClassLevel();
            int lanes = info.m_backwardVehicleLaneCount + info.m_forwardVehicleLaneCount;
            switch (level)
            {
                case ItemClass.Level.Level5:
                    if (info.m_hasBackwardVehicleLanes && info.m_hasForwardVehicleLanes)
                    {
                        basePoints = 75000 * lanes;
                    }
                    else
                    {
                        if (lanes < 2)
                        {
                            basePoints = 0;
                        }
                        else
                        {
                            basePoints = 125000 * lanes;
                        }
                    }
                    break;
                case ItemClass.Level.Level1:
                case ItemClass.Level.Level2:
                case ItemClass.Level.Level3:
                case ItemClass.Level.Level4:
                    int multiplier = 1 + (int)level;
                    basePoints = Mathf.RoundToInt((2000 * info.m_halfWidth) + (multiplier * lanes * (info.m_hasBackwardVehicleLanes && info.m_hasForwardVehicleLanes ? 10000 : 15000)));
                    break;
            }

            return basePoints;
        }

        private static bool CheckIfFlowIsAllowed(ushort nodeID, NetSegment targetSegment)
        {
            bool isSegmentInverted = CheckSegmentInverted(nodeID, ref targetSegment);
            bool canFlowThruThisSegment = (isSegmentInverted && targetSegment.Info.m_hasBackwardVehicleLanes) || (!isSegmentInverted && targetSegment.Info.m_hasForwardVehicleLanes);
            return canFlowThruThisSegment;
        }

        #endregion

        #region calculation basic
        private IEnumerator CalculateSigns(ushort nodeID)
        {
            yield return 0;
            if (m_updatedStreetPositions[nodeID] != false)
            {
                yield break;
            }

            GetIncomingOutcomingTraffic(nodeID, out HashSet<ushort> incomingTraffic, out HashSet<ushort> outcomingTraffic);

            yield return 0;
            if (m_updatedStreetPositions[nodeID] != false)
            {
                yield break;
            }
            //    LogUtils.DoLog($"updatedStreets! {nodeID} {NetManager.instance.m_nodes.m_buffer[nodeID].CountSegments()}");
            int controlBoardIdx = 0;
            for (int i = 0; i < 8; i++)
            {
                ushort segmentIid = NetManager.instance.m_nodes.m_buffer[nodeID].GetSegment(i);
                if (segmentIid != 0)
                {
                    if (Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].Info != null && Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].Info.m_netAI is RoadBaseAI roadAiI)
                    {
                        GetNeighborSegment(nodeID, ref NetManager.instance.m_nodes.m_buffer[nodeID], segmentIid, ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid], out Vector3 segmentIDirection, out Vector3 segmentJDirection, out ushort segmentJid, out bool invertIJ);

                        int j = GetSegmentIndex(ref NetManager.instance.m_nodes.m_buffer[nodeID], segmentJid);

                        ItemClass classI = Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].Info.m_class;
                        ItemClass classJ = Singleton<NetManager>.instance.m_segments.m_buffer[segmentJid].Info.m_class;
                        var matchingDescriptors = new Stack<Tuple<int, BoardInstanceRoadNodeXml>>(Data.DescriptorRulesOrder.Select((x, y) => Tuple.New(y, x))
                            .Where(x => (x.Second.AllowsClass(classI) && (m_updatedDestinations[nodeID] != true || x.Second.Descriptor.m_textDescriptors.All(t => !t.IsTextRelativeToSegment() || m_couldReachDestinations[nodeID, t.m_destinationRelative == DestinationReference.Self ? i : j, (int)t.m_destinationRelative] == true)))
                                     || (x.Second.AllowsClass(classJ) && (m_updatedDestinations[nodeID] != true || x.Second.Descriptor.m_textDescriptors.All(t => !t.IsTextRelativeToSegment() || m_couldReachDestinations[nodeID, t.m_destinationRelative == DestinationReference.Self ? j : i, (int)t.m_destinationRelative] == true)))
                            ).OrderByDescending(x => x.First));
                        if (matchingDescriptors.Count == 0)
                        {
                            continue;
                        }
                        int secondaryIdx = 0;
                        bool hasSpawned = false;
                        BoardInstanceRoadNodeXml targetDescriptor = null;
                        while (matchingDescriptors.Count > 0 && !hasSpawned)
                        {
                            targetDescriptor = matchingDescriptors.Pop()?.Second;
                            hasSpawned = ProcessDescriptor(nodeID, ref NetManager.instance.m_nodes.m_buffer[nodeID], controlBoardIdx, segmentIid, ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid], segmentIDirection, segmentJid, ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentJid], segmentJDirection, invertIJ, targetDescriptor, incomingTraffic, outcomingTraffic, ref secondaryIdx);

                            yield return 0;
                            if (m_updatedStreetPositions[nodeID] != false)
                            {
                                LogUtils.DoWarnLog($"[n{nodeID}] STOPPING X");
                                yield break;
                            }
                        }
                        if (matchingDescriptors.Count > 0 && targetDescriptor.PlaceOnSegmentInsteadOfCorner && targetDescriptor.AllowAnotherRuleForCorner)
                        {
                            targetDescriptor = matchingDescriptors.Where((x) => !x.Second.PlaceOnSegmentInsteadOfCorner).FirstOrDefault()?.Second;
                            if (targetDescriptor != null)
                            {
                                hasSpawned |= ProcessDescriptor(nodeID, ref NetManager.instance.m_nodes.m_buffer[nodeID], controlBoardIdx, segmentIid, ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid], segmentIDirection, segmentJid, ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentJid], segmentJDirection, invertIJ, targetDescriptor, incomingTraffic, outcomingTraffic, ref secondaryIdx);
                            }
                        }
                        if (hasSpawned)
                        {

                            lock (Data.BoardsContainers)
                            {
                                while (secondaryIdx < 4)
                                {
                                    Data.BoardsContainers[nodeID, controlBoardIdx, secondaryIdx++] = null;
                                }
                            };
                            controlBoardIdx++;
                        }

                        yield return 0;
                        if (m_updatedStreetPositions[nodeID] != false)
                        {
                            LogUtils.DoWarnLog($"[n{nodeID}] STOPPING Y");
                            yield break;
                        }
                    }
                }
            }

            lock (Data.BoardsContainers)
            {
                while (controlBoardIdx < 8)
                {

                    Data.BoardsContainers[nodeID, controlBoardIdx, 0] = null;
                    Data.BoardsContainers[nodeID, controlBoardIdx, 1] = null;
                    Data.BoardsContainers[nodeID, controlBoardIdx, 2] = null;
                    Data.BoardsContainers[nodeID, controlBoardIdx, 3] = null;
                    controlBoardIdx++;
                }
            }

            m_updatedStreetPositions[nodeID] = true;
        }

        private static void GetIncomingOutcomingTraffic(ushort nodeID, out HashSet<ushort> incomingTraffic, out HashSet<ushort> outcomingTraffic)
        {
            incomingTraffic = new HashSet<ushort>();
            outcomingTraffic = new HashSet<ushort>();
            for (int i = 0; i < 8; i++)
            {
                ushort segmentIid = NetManager.instance.m_nodes.m_buffer[nodeID].GetSegment(i);
                if (segmentIid != 0)
                {
                    bool invertFlagI = (Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].m_flags & NetSegment.Flags.Invert) != 0;
                    bool isSegmentIinverted = invertFlagI == (Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].m_startNode == nodeID) != (SimulationManager.instance.m_metaData.m_invertTraffic == SimulationMetaData.MetaBool.False);
                    if ((isSegmentIinverted && Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].Info.m_hasBackwardVehicleLanes) || (!isSegmentIinverted && Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].Info.m_hasForwardVehicleLanes))
                    {
                        incomingTraffic.Add(segmentIid);
                    }
                    if ((!isSegmentIinverted && Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].Info.m_hasBackwardVehicleLanes) || (isSegmentIinverted && Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].Info.m_hasForwardVehicleLanes))
                    {
                        outcomingTraffic.Add(segmentIid);
                    }
                }
            }
        }

        private bool ProcessDescriptor(
            ushort nodeID, ref NetNode data, int controlBoardIdx,
            ushort segmentIid, ref NetSegment netSegmentI, Vector3 segmentIDirection,
            ushort segmentJid, ref NetSegment netSegmentJ, Vector3 segmentJDirection, bool invertIJ,
            BoardInstanceRoadNodeXml targetDescriptor, HashSet<ushort> incoming,
            HashSet<ushort> outcoming, ref int subboardOffset)
        {
            if (targetDescriptor?.Descriptor?.m_propName == null)
            {
                return false;
            }
            bool isSegmentIinverted = CheckSegmentInverted(nodeID, ref netSegmentI);
            bool isSegmentJinverted = CheckSegmentInverted(nodeID, ref netSegmentJ);

            if (segmentJid == 0
                || !(Singleton<NetManager>.instance.m_segments.m_buffer[segmentJid].Info.m_netAI is RoadBaseAI)
                || (!targetDescriptor.PlaceOnSegmentInsteadOfCorner
                    && (SegmentUtils.IsSameName(segmentJid, segmentIid, false, false, true, targetDescriptor.PlaceOnDistrictBorder, targetDescriptor.PlaceOnTunnelBridgeStart)
                        || (targetDescriptor.IgnoreEmptyNameRoads
                            && (Singleton<NetManager>.instance.GetSegmentName(segmentIid).IsNullOrWhiteSpace() || Singleton<NetManager>.instance.GetSegmentName(segmentJid).IsNullOrWhiteSpace()))))

                || (new Randomizer(segmentIid | (segmentJid << 16)).UInt32(255) > targetDescriptor.SpawnChance))
            {
                return false;
            }
            Vector3 platePosI;
            Vector3 platePosJ;
            bool renderJ = true;
            bool renderI = true;

            /*
             
                || Singleton<NetManager>.instance.m_segments.m_buffer[segmentJid].Info?.m_halfWidth > targetDescriptor.MaxRoadHalfWidth + 0.002f
                || Singleton<NetManager>.instance.m_segments.m_buffer[segmentJid].Info?.m_halfWidth < targetDescriptor.MinRoadHalfWidth - 0.002f
             */
            if (targetDescriptor.PlaceOnSegmentInsteadOfCorner)
            {
                if (!ProcessRulesPlaceOnSegment(nodeID, segmentIid, ref netSegmentI, ref segmentIDirection, segmentJid, ref netSegmentJ, ref segmentJDirection, targetDescriptor, incoming, outcoming, isSegmentIinverted, isSegmentJinverted, out platePosI, out platePosJ, out renderI, out renderJ))
                {
                    return false;
                }
            }
            else
            {
                CalculateCenterCorner(nodeID, ref data, segmentIid, ref netSegmentI, ref segmentIDirection, segmentJid, ref netSegmentJ, ref segmentJDirection, out platePosI, out platePosJ);
            }
            if (!renderI && !renderJ)
            {
                return false;
            }
            int id1 = subboardOffset++;
            int id2 = subboardOffset++;
            lock (Data.BoardsContainers)
            {
                CacheRoadNodeItem a = FillCacheData(nodeID, controlBoardIdx, id1, segmentIid, platePosI, segmentIDirection, targetDescriptor, ref renderI);
                a.m_otherSegment = FillCacheData(nodeID, controlBoardIdx, id2, segmentJid, platePosJ, segmentJDirection, targetDescriptor, ref renderJ);
                a.m_otherSegment.m_otherSegment = a;
            }
            if (!renderI && !renderJ)
            {
                subboardOffset--;
                subboardOffset--;
                return false;
            }
            return true;
        }

        private static void CalculateCenterCorner(ushort nodeID, ref NetNode data, ushort segmentIid, ref NetSegment netSegmentI, ref Vector3 segmentIDirection, ushort segmentJid, ref NetSegment netSegmentJ, ref Vector3 segmentJDirection, out Vector3 platePosI, out Vector3 platePosJ)
        {
            bool start = netSegmentI.m_startNode == nodeID;
            netSegmentI.CalculateCorner(segmentIid, true, start, false, out Vector3 startPos, out Vector3 startAng, out _);

            start = (netSegmentJ.m_startNode == nodeID);
            netSegmentJ.CalculateCorner(segmentJid, true, start, true, out Vector3 endPos, out Vector3 endAng, out _);

            NetSegment.CalculateMiddlePoints(startPos, -startAng, endPos, -endAng, true, true, out Vector3 rhs, out Vector3 lhs);
            Vector3 relativePos = (((rhs + lhs) * 0.5f) - data.m_position);
            platePosI = platePosJ = relativePos - relativePos.normalized + data.m_position;
            segmentIDirection.y = segmentJDirection.y = 0;
        }

        private static bool ProcessRulesPlaceOnSegment(ushort nodeID, ushort segmentIid, ref NetSegment netSegmentI, ref Vector3 segmentIDirection, ushort segmentJid, ref NetSegment netSegmentJ, ref Vector3 segmentJDirection, BoardInstanceRoadNodeXml targetDescriptor, HashSet<ushort> incoming, HashSet<ushort> outcoming, bool isSegmentIinverted, bool isSegmentJinverted, out Vector3 platePosI, out Vector3 platePosJ, out bool renderJ, out bool renderI)
        {
            float angleIJ = (segmentIDirection.GetAngleXZ() - segmentJDirection.GetAngleXZ()) % 360;
            if (angleIJ > 180)
            {
                angleIJ -= 360;
            }

            if (angleIJ < -180)
            {
                angleIJ += 360;
            }

            bool invertIJ = (angleIJ < 0) == (SimulationManager.instance.m_metaData.m_invertTraffic == SimulationMetaData.MetaBool.False);

            LogUtils.DoLog($"[n{nodeID}]{segmentIid}x{segmentJid} -> dir = {segmentIDirection.GetAngleXZ() }x{ segmentJDirection.GetAngleXZ()} = {angleIJ}; invertIJ = {invertIJ}");

            if (invertIJ)
            {
                return ProcessInternExternRule(nodeID, segmentJid, ref netSegmentJ, ref segmentJDirection, segmentIid, ref netSegmentI, ref segmentIDirection, targetDescriptor, incoming, outcoming, isSegmentJinverted, isSegmentIinverted, out platePosJ, out platePosI, out renderI, out renderJ);
            }
            else
            {
                return ProcessInternExternRule(nodeID, segmentIid, ref netSegmentI, ref segmentIDirection, segmentJid, ref netSegmentJ, ref segmentJDirection, targetDescriptor, incoming, outcoming, isSegmentIinverted, isSegmentJinverted, out platePosI, out platePosJ, out renderJ, out renderI);
            }

        }

        private static bool ProcessInternExternRule(ushort nodeID, ushort segmentIntId, ref NetSegment netSegmentInt, ref Vector3 segmentIntDirection, ushort segmentExtId, ref NetSegment netSegmentExt, ref Vector3 segmentExtDirection, BoardInstanceRoadNodeXml targetDescriptor, HashSet<ushort> incoming, HashSet<ushort> outcoming, bool isSegmentIntInverted, bool isSegmentExtInverted, out Vector3 platePosInt, out Vector3 platePosExt, out bool renderInt, out bool renderExt)
        {
            LogUtils.DoLog($"[n{nodeID}]Int = {segmentIntId};Ext = {segmentExtId}");
            renderInt = !targetDescriptor.EnsureSegmentTypeInAllowedTypes || targetDescriptor.AllowsClass(netSegmentInt.Info.m_class);
            renderExt = !targetDescriptor.EnsureSegmentTypeInAllowedTypes || targetDescriptor.AllowsClass(netSegmentExt.Info.m_class);
            platePosInt = CaculateCenterSegment(nodeID, ref netSegmentInt, ref netSegmentExt, segmentIntId, targetDescriptor, true);
            platePosExt = CaculateCenterSegment(nodeID, ref netSegmentExt, ref netSegmentInt, segmentExtId, targetDescriptor, false);
            segmentIntDirection.y = segmentExtDirection.y = -90;
            if (targetDescriptor.TrafficDirectionRequired == TrafficDirectionRequired.OUTCOMING)
            {
                renderExt &= outcoming.Contains(segmentIntId) && EnsureTrafficLanes(ref netSegmentInt, targetDescriptor, !isSegmentIntInverted) && EnsureRoadWidth(ref netSegmentInt, targetDescriptor);
                renderInt &= outcoming.Contains(segmentIntId) && netSegmentExt.Info.m_hasBackwardVehicleLanes != netSegmentExt.Info.m_hasForwardVehicleLanes && EnsureTrafficLanes(ref netSegmentExt, targetDescriptor, !isSegmentExtInverted) && EnsureRoadWidth(ref netSegmentExt, targetDescriptor);

                if (incoming.Count == 1)
                {
                    renderExt &= (netSegmentExt.Info.m_forwardVehicleLaneCount == 1 && !netSegmentExt.Info.m_hasBackwardVehicleLanes && netSegmentExt.Info.m_netAI is RoadBaseAI aiExt && aiExt.m_highwayRules) || !SegmentUtils.IsSameName(incoming.First(), segmentIntId, false, false, true, false, false);
                    renderInt &= (netSegmentInt.Info.m_forwardVehicleLaneCount == 1 && !netSegmentInt.Info.m_hasBackwardVehicleLanes && netSegmentInt.Info.m_netAI is RoadBaseAI aiInt && aiInt.m_highwayRules) || !SegmentUtils.IsSameName(incoming.First(), segmentExtId, false, false, true, false, false);
                }
            }
            else if (targetDescriptor.TrafficDirectionRequired == TrafficDirectionRequired.INCOMING)
            {
                bool processed = false;
                if (renderExt && incoming.Contains(segmentExtId) && outcoming.Contains(segmentIntId) && (targetDescriptor.ExitSideRequired == ExitSideRequired.NONE || targetDescriptor.ExitSideRequired == ExitSideRequired.OUTSIDE))
                {
                    LogUtils.DoLog($"[n{nodeID}]Inc = {segmentExtId};Out = {segmentIntId} ExitSideRequired.OUTSIDE");
                    processed = ProcessIncomingTrafficSigns(nodeID, segmentExtId, ref netSegmentExt, targetDescriptor, outcoming, isSegmentExtInverted, ref renderExt, ref renderInt);
                }
                else if (renderInt && incoming.Contains(segmentIntId) && outcoming.Contains(segmentExtId) && (targetDescriptor.ExitSideRequired == ExitSideRequired.NONE || targetDescriptor.ExitSideRequired == ExitSideRequired.INSIDE))
                {
                    LogUtils.DoLog($"[n{nodeID}]Inc = {segmentIntId};Out = {segmentExtId} ExitSideRequired.INSIDE");
                    processed = ProcessIncomingTrafficSigns(nodeID, segmentIntId, ref netSegmentInt, targetDescriptor, outcoming, isSegmentIntInverted, ref renderInt, ref renderExt);
                }

                if (!processed)
                {
                    return false;
                }
            }
            else
            {
                renderExt &= isSegmentIntInverted && netSegmentInt.Info.m_hasBackwardVehicleLanes && EnsureRoadWidth(ref netSegmentInt, targetDescriptor);
                renderInt &= isSegmentExtInverted && netSegmentExt.Info.m_hasForwardVehicleLanes && EnsureRoadWidth(ref netSegmentExt, targetDescriptor);
            }
            return true;
        }

        private static bool ProcessIncomingTrafficSigns(ushort nodeID, ushort segmentIncId, ref NetSegment netSegmentInc, BoardInstanceRoadNodeXml targetDescriptor, HashSet<ushort> outcoming, bool isSegmentIncInverted, ref bool renderInc, ref bool renderOut)
        {
            int outcomingCount = outcoming.Where(x => x != segmentIncId).Count();
            if (outcomingCount < targetDescriptor.MinNodeOutcomingSegments || outcomingCount > targetDescriptor.MaxNodeOutcomingSegments)
            {
                LogUtils.DoLog($"[n{nodeID}]outcomingCount out of range ({outcomingCount}) [{targetDescriptor.MinNodeOutcomingSegments},{targetDescriptor.MaxNodeOutcomingSegments}]");
                return false;
            }
            renderOut = false;
            renderInc &= EnsureTrafficLanes(ref netSegmentInc, targetDescriptor, !isSegmentIncInverted) && EnsureRoadWidth(ref netSegmentInc, targetDescriptor);
            LogUtils.DoLog($"[n{nodeID}]segmentIncId = {segmentIncId}; renderOut = {renderOut};renderInc = {renderInc}({EnsureTrafficLanes(ref netSegmentInc, targetDescriptor, !isSegmentIncInverted) } && {EnsureRoadWidth(ref netSegmentInc, targetDescriptor)})");
            if (outcoming.Count == 1)
            {
                renderInc &= (netSegmentInc.Info.m_forwardVehicleLaneCount == 1 && !netSegmentInc.Info.m_hasBackwardVehicleLanes && netSegmentInc.Info.m_netAI is RoadBaseAI aiExt && aiExt.m_highwayRules) || !SegmentUtils.IsSameName(outcoming.First(), segmentIncId, false, false, true, false, false);
            }
            return true;
        }

        private static bool CheckSegmentInverted(ushort nodeID, ref NetSegment netSegmentJ)
        {
            bool invertFlagJ = (netSegmentJ.m_flags & NetSegment.Flags.Invert) != 0;
            bool isSegmentJinverted = invertFlagJ == (netSegmentJ.m_startNode == nodeID) == (SimulationManager.instance.m_metaData.m_invertTraffic == SimulationMetaData.MetaBool.False);
            return isSegmentJinverted;
        }

        private static bool EnsureTrafficLanes(ref NetSegment netSegment, BoardInstanceRoadNodeXml targetDescriptor, bool isSegmentInverted)
        {
            return (isSegmentInverted && netSegment.Info.m_backwardVehicleLaneCount >= targetDescriptor.MinDirectionTrafficLanes && netSegment.Info.m_backwardVehicleLaneCount <= targetDescriptor.MaxDirectionTrafflcLanes)
               || (!isSegmentInverted && netSegment.Info.m_forwardVehicleLaneCount >= targetDescriptor.MinDirectionTrafficLanes && netSegment.Info.m_forwardVehicleLaneCount <= targetDescriptor.MaxDirectionTrafflcLanes);
        }

        private static bool EnsureRoadWidth(ref NetSegment netSegment, BoardInstanceRoadNodeXml targetDescriptor) => netSegment.Info?.m_halfWidth < targetDescriptor.MaxRoadHalfWidth + 0.002f && netSegment.Info?.m_halfWidth > targetDescriptor.MinRoadHalfWidth - 0.002f;

        private static Vector3 CaculateCenterSegment(int nodeID, ref NetSegment segmentI, ref NetSegment segmentJ, ushort segmentId, BoardInstanceRoadNodeXml descriptor, bool invertSide)
        {
            bool comingFromNode = (segmentI.m_startNode == nodeID);
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
                segmentI.CalculateCorner(segmentId, true, comingFromNode, !invertSide, out bezierPos, out Vector3 angleVec, out _);
                targetOffsetX = descriptor.m_propPosition.x * (segmentI.Info.m_pavementWidth + segmentJ.Info.m_pavementWidth) / (invertSide ? -2 : 2);
                angle = angleVec.GetAngleXZ();

                invertedSegment ^= angleVec.GetAngleXZ() < 0;

                LogUtils.DoLog($"[n{nodeID}]{segmentId} angleVec = ({angleVec.GetAngleXZ()}) (invertSide = {invertSide})");
            }


            segmentI.GetClosestPositionAndDirection(bezierPos, out _, out Vector3 dir);
            int rotationOffsetSide = invertedSegment ? 90 : -90;
            float rotation = dir.GetAngleXZ();

            Vector3 rotationVectorSide = VectorUtils.X_Y(KlyteMathUtils.DegreeToVector2(rotation + rotationOffsetSide - angle));
            platePosI = bezierPos + (descriptor.PropPositionZ < 1 ? (rotationVectorSide * (segmentI.Info.m_halfWidth - segmentI.Info.m_pavementWidth)) : default) + (rotationVectorSide * targetOffsetX);
            platePosI.y += descriptor.m_propPosition.y;

            return platePosI;
        }

        private ref CacheRoadNodeItem FillCacheData(ushort nodeID, int controlBoardIdx, int boardId, ushort segmentIid, Vector3 platePosI, Vector3 segmentIDirection, BoardInstanceRoadNodeXml targetDescriptor, ref bool render)
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



        private static void GetNeighborSegment(ushort nodeID, ref NetNode data, ushort segmentIid, ref NetSegment netSegmentI, out Vector3 segmentIDirection, out Vector3 otherSegmentDirection, out ushort resultOtherSegment, out bool invertIJ)
        {
            segmentIDirection = (nodeID != netSegmentI.m_startNode) ? netSegmentI.m_endDirection : netSegmentI.m_startDirection;
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
                        Vector3 segmentJDirection = (nodeID != netSegmentCand.m_startNode) ? netSegmentCand.m_endDirection : netSegmentCand.m_startDirection;
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

        protected InstanceID GetPropRenderID(ushort nodeId)
        {
            return new InstanceID
            {
                NetNode = nodeId
            };

        }
        #endregion

        #region routing data cache
        private readonly HashSet<ulong> m_validHashes = new HashSet<ulong>();

        private readonly HashSet<ulong>[] m_passingHashes = new HashSet<ulong>[NetManager.MAX_NODE_COUNT];

        private readonly CacheDestinationRoute[,,] m_destinationInfo = new CacheDestinationRoute[NetManager.MAX_NODE_COUNT, 8, DESTINATIONS_CALC_COUNT];

        public CacheDestinationRoute GetTargetDestination(ulong hash, int destination) => m_destinationInfo[hash & (NetManager.MAX_NODE_COUNT - 1), (hash & ((NetManager.MAX_NODE_COUNT << 3) - 1)) >> 15, destination];
        public ref CacheDestinationRoute GetTargetDestination(ushort nodeId, int segmentIdx, int destination) => ref m_destinationInfo[nodeId, segmentIdx, destination];

        private void InvalidateNodeDestinationPaths(ushort nodeId)
        {
            LogUtils.DoLog($"InvalidateNodeDestinationPaths Invalidating node: {nodeId} ({m_passingHashes[nodeId]?.Count} hashes)");
            m_updatedDestinations[nodeId] = null;
            m_updatedStreetPositions[nodeId] = null;
            if (m_passingHashes[nodeId] != null)
            {
                FilterValid(ref m_passingHashes[nodeId]);
                m_passingHashes[nodeId]?.ForEach(x =>
                {
                    ulong targetNodeId = x & (NetManager.MAX_NODE_COUNT - 1);
                    LogUtils.DoLog($"InvalidateNodeDestinationPaths Marking null: targetNodeId={targetNodeId} (hash = {x.ToString("X16")})");
                    m_updatedDestinations[targetNodeId] = null;
                    m_updatedStreetPositions[targetNodeId] = null;
                });
                InvalidateHashes(ref m_passingHashes[nodeId]);
            }
        }

        private ulong RegisterHash(ushort nodeId, uint segmentIdx)
        {
            m_nextHash += 0x100000;
            ulong hash = m_nextHash | (nodeId & 0x7FFFFu) | ((segmentIdx & 7u) << 15);
            m_validHashes.Add(hash);
            LogUtils.DoLog($"RegisterHash Registered hash for node: {nodeId}/{segmentIdx}({hash.ToString("X16")})");
            return hash;
        }

        private void AddAllToHash(IEnumerable<ushort> nodeList, ulong hash)
        {
            LogUtils.DoLog($"AddAllToHash Adding hash ({hash.ToString("X16")}) to nodes: [{string.Join(",", nodeList.Select(x => x.ToString()).ToArray())}]");
            foreach (ushort node in nodeList)
            {
                AddToPassingList(ref m_passingHashes[node], hash);
            }
        }

        private void AddToPassingList(ref HashSet<ulong> passing, ulong hash) => (passing ??= new HashSet<ulong>()).Add(hash);
        private void InvalidateHashes(ref HashSet<ulong> targets)
        {
            m_validHashes.ExceptWith(targets);
            targets = null;
        }

        private void FilterValid(ref HashSet<ulong> source) => source.IntersectWith(m_validHashes);

        private ulong m_nextHash = 0;

        #endregion
    }

    public class CacheDestinationRoute
    {
        public ushort m_nodeId;
        public ushort m_segmentId;
        public byte m_districtId;
        public byte m_parkId;
        public float m_distance;
        public int m_distanceKm;
        public string m_distanceMeanString;
        public string m_outsideConnectionName;
    }

}
