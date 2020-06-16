using ColossalFramework.UI;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Overrides;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.Utils.SegmentUtils;

namespace Klyte.WriteTheSigns.Singleton
{
    public class WTSDestinationSingleton : MonoBehaviour
    {
        internal IEnumerator CalculateDestinations(ushort nodeID)
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
            WTSRoadNodeCommons.GetIncomingOutcomingTraffic(nodeID, out HashSet<ushort> incomingTraffic, out HashSet<ushort> outcomingTraffic, out int[] rotationOrder, out _);
            if (outcomingTraffic.Count == 0)
            {
                LogUtils.DoLog($"End - no outcoming for {nodeID}");
                yield break;
            }
            for (int i = 0; i < 8; i++)
            {
                ushort targetSegmentId = NetManager.instance.m_nodes.m_buffer[nodeID].GetSegment(i);
                IEnumerable<ushort> thisIncomingTraffic = incomingTraffic.Where(x => !IsSameName(targetSegmentId, x));
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
            public int Compare(SegmentMappingObject x, SegmentMappingObject y) => (-x.pts * 2 + x.iteration).CompareTo(-y.pts * 2 + y.iteration);
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
            public override string ToString() => $"[s={segmentId}; d={distanceWalked}; pts = {pts}; lastNode = {nodeList.Last()}; i = {iteration}; ord = {pts * 2 + iteration}]";
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
                if (currentSegment > 0 && (ignoreSegmentCheck || currentSegment != prevObj.segmentId) && !incomingSegments.Any(x => IsSameName(currentSegment, x)))
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
                    basePoints = Mathf.RoundToInt(2000 * info.m_halfWidth + multiplier * lanes * (info.m_hasBackwardVehicleLanes && info.m_hasForwardVehicleLanes ? 10000 : 15000));
                    break;
            }

            return basePoints;
        }

        private static bool CheckIfFlowIsAllowed(ushort nodeID, NetSegment targetSegment)
        {
            bool isSegmentInverted = WTSRoadNodeCommons.CheckSegmentInverted(nodeID, ref targetSegment);
            bool canFlowThruThisSegment = isSegmentInverted && targetSegment.Info.m_hasBackwardVehicleLanes || !isSegmentInverted && targetSegment.Info.m_hasForwardVehicleLanes;
            return canFlowThruThisSegment;
        }


        #region routing data cache
        private readonly HashSet<ulong> m_validHashes = new HashSet<ulong>();

        private readonly HashSet<ulong>[] m_passingHashes = new HashSet<ulong>[NetManager.MAX_NODE_COUNT];

        private readonly CacheDestinationRoute[,,] m_destinationInfo = new CacheDestinationRoute[NetManager.MAX_NODE_COUNT, 8, DESTINATIONS_CALC_COUNT];

        public ref CacheDestinationRoute GetTargetDestination(ushort nodeId, int segmentIdx, int destination) => ref m_destinationInfo[nodeId, segmentIdx, destination];

        internal void InvalidateNodeDestinationPaths(ushort nodeId)
        {
            LogUtils.DoLog($"InvalidateNodeDestinationPaths Invalidating node: {nodeId} ({m_passingHashes[nodeId]?.Count} hashes)");
            m_updatedDestinations[nodeId] = null;
            WriteTheSignsMod.Controller.RoadPropsSingleton.m_updatedStreetPositions[nodeId] = null;
            if (m_passingHashes[nodeId] != null)
            {
                FilterValid(ref m_passingHashes[nodeId]);
                m_passingHashes[nodeId]?.ForEach(x =>
                {
                    ulong targetNodeId = x & NetManager.MAX_NODE_COUNT - 1;
                    LogUtils.DoLog($"InvalidateNodeDestinationPaths Marking null: targetNodeId={targetNodeId} (hash = {x.ToString("X16")})");
                    m_updatedDestinations[targetNodeId] = null;
                    WriteTheSignsMod.Controller.RoadPropsSingleton.m_updatedStreetPositions[targetNodeId] = null;
                });
                InvalidateHashes(ref m_passingHashes[nodeId]);
            }
        }

        private ulong RegisterHash(ushort nodeId, uint segmentIdx)
        {
            m_nextHash += 0x100000;
            ulong hash = m_nextHash | nodeId & 0x7FFFFu | (segmentIdx & 7u) << 15;
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

        public static readonly int DESTINATIONS_CALC_COUNT = (int)(Enum.GetValues(typeof(DestinationReference)) as DestinationReference[]).Max() + 1;

        public bool?[] m_updatedDestinations;
        public bool?[,,] m_couldReachDestinations;

        public void Awake() => ResetViews();

        public void ResetViews()
        {
            m_updatedDestinations = new bool?[NetManager.MAX_NODE_COUNT];
            m_couldReachDestinations = new bool?[NetManager.MAX_NODE_COUNT, 8, DESTINATIONS_CALC_COUNT];
        }
        public void Start()
        {
            NetManagerOverrides.EventNodeChanged += OnNodeChanged;
            WriteTheSignsMod.Controller.EventOnDistrictChanged += ResetViews;
            WriteTheSignsMod.Controller.EventOnParkChanged += ResetViews;
            NetManagerOverrides.EventSegmentNameChanged += OnNameSeedChanged;
            WriteTheSignsMod.Controller.EventOnZeroMarkerChanged += ResetViews;
        }

        private void OnNodeChanged(ushort nodeId) => InvalidateNodeDestinationPaths(nodeId);

        private void OnNameSeedChanged(ushort segmentId)
        {
            LogUtils.DoLog("onNameSeedChanged");
            InvalidateNodeDestinationPaths(NetManager.instance.m_segments.m_buffer[segmentId].m_endNode);
            InvalidateNodeDestinationPaths(NetManager.instance.m_segments.m_buffer[segmentId].m_startNode);
        }


    }
}
