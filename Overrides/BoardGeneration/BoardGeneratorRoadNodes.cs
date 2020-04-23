using ColossalFramework;
using ColossalFramework.Math;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using SpriteFontPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.WriteTheSigns.Overrides
{

    public class BoardGeneratorRoadNodes : Redirector, IRedirectable
    {
        public static BoardGeneratorRoadNodes Instance;
        public DynamicSpriteFont DrawFont => FontServer.instance[Data.DefaultFont] ?? FontServer.instance[WTSController.DEFAULT_FONT_KEY];
        public WTSRoadNodesData Data => WTSRoadNodesData.Instance;
        public bool[] m_updatedStreetPositions;
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
            m_updatedStreetPositions = new bool[Data.ObjArraySize];

            NetManagerOverrides.EventNodeChanged += OnNodeChanged;
            DistrictManagerOverrides.EventOnDistrictChanged += OnDistrictChanged;
            NetManagerOverrides.EventSegmentNameChanged += OnNameSeedChanged;

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

        private void OnNodeChanged(ushort nodeId)
        {
            //doLog($"onNodeChanged { System.Environment.StackTrace }");
            m_updatedStreetPositions[nodeId] = false;
        }
        private void OnNameSeedChanged(ushort segmentId)
        {
            LogUtils.DoLog("onNameSeedChanged");
            m_updatedStreetPositions[NetManager.instance.m_segments.m_buffer[segmentId].m_endNode] = false;
            m_updatedStreetPositions[NetManager.instance.m_segments.m_buffer[segmentId].m_startNode] = false;
        }

        private void OnDistrictChanged()
        {
            LogUtils.DoLog("onDistrictChanged");
            m_updatedStreetPositions = new bool[NetManager.MAX_NODE_COUNT];
        }
        private void OnZeroMarkChanged()
        {
            LogUtils.DoLog("onZeroMarkChanged");
            m_updatedStreetPositions = new bool[NetManager.MAX_NODE_COUNT];
        }
        #endregion

        public static void AfterRenderSegment(RenderManager.CameraInfo cameraInfo, ushort segmentID)
        {
            Instance.AfterRenderInstanceImpl(cameraInfo, NetManager.instance.m_segments.m_buffer[segmentID].m_startNode, ref NetManager.instance.m_nodes.m_buffer[NetManager.instance.m_segments.m_buffer[segmentID].m_startNode]);
            Instance.AfterRenderInstanceImpl(cameraInfo, NetManager.instance.m_segments.m_buffer[segmentID].m_endNode, ref NetManager.instance.m_nodes.m_buffer[NetManager.instance.m_segments.m_buffer[segmentID].m_endNode]);
        }

        private uint m_lastTickUpdate = 0;

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
            if (!cameraInfo.CheckRenderDistance(data.m_position, 400))
            {
                return;
            }
            if (data.CountSegments() < 2)
            {
                return;
            }
            //m_streetPlatePrefab
            if (!m_updatedStreetPositions[nodeID] || Data.BoardsContainers[nodeID, 0, 0] == null)
            {
                if (m_lastTickUpdate == SimulationManager.instance.m_currentTickIndex)
                {
                    return;
                }
                CalculateSigns(nodeID, ref data);
            }

            for (int y = 0; y < Data.BoardsContainers.GetLength(1); y++)
            {
                for (int z = 0; z < Data.BoardsContainers.GetLength(2); z++)
                {
                    ref CacheRoadNodeItem item = ref Data.BoardsContainers[nodeID, y, z];
                    if (item?.m_renderPlate ?? false)
                    {
                        ref BoardInstanceRoadNodeXml targetDescriptor = ref item.m_currentDescriptor;

                        if (item.m_cachedProp?.name != targetDescriptor.Descriptor.m_propName)
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
                        MaterialPropertyBlock properties = PropManager.instance.m_materialBlock;
                        properties.Clear();
                        WTSPropRenderingRules.RenderTextMesh(nodeID, boardIdx, secIdx, targetDescriptor, propMatrix, targetDescriptor.Descriptor.m_textDescriptors[j], properties, DrawFont);
                    }
                }
            }
        }

        private void CalculateSigns(ushort nodeID, ref NetNode data)
        {
            m_lastTickUpdate = SimulationManager.instance.m_currentTickIndex;

            LogUtils.DoLog($"updatedStreets! {nodeID} {data.CountSegments()}");
            int controlBoardIdx = 0;
            for (int i = 0; i < 8; i++)
            {
                ushort segmentIid = data.GetSegment(i);
                if (segmentIid != 0)
                {
                    ref NetSegment netSegmentI = ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid];
                    if (netSegmentI.Info != null && netSegmentI.Info.m_netAI is RoadBaseAI roadAiI)
                    {
                        GetNeigborSegment(nodeID, ref data, segmentIid, ref netSegmentI, out Vector3 segmentIDirection, out Vector3 segmentJDirection, out ushort segmentJid);

                        ref NetSegment netSegmentJ = ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentJid];
                        ItemClass classI = netSegmentI.Info.m_class;
                        ItemClass classJ = netSegmentJ.Info.m_class;
                        BoardInstanceRoadNodeXml targetDescriptor = Data.DescriptorRulesOrder.FirstOrDefault(x => x.AllowsClass(classI) || x.AllowsClass(classJ));
                        if (targetDescriptor == null || targetDescriptor.Descriptor == null)
                        {
                            continue;
                        }
                        if (segmentJid == 0
                            || !(Singleton<NetManager>.instance.m_segments.m_buffer[segmentJid].Info.m_netAI is RoadBaseAI roadAiJ)
                            || roadAiI.GenerateName(segmentIid, ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid]).IsNullOrWhiteSpace()
                            || roadAiJ.GenerateName(segmentJid, ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentJid]).IsNullOrWhiteSpace()
                            || SegmentUtils.IsSameName(segmentJid, segmentIid, false, false, true, targetDescriptor.PlaceOnDistrictBorder, true)
                            || (new Randomizer(segmentIid | (segmentJid << 16)).UInt32(255) > targetDescriptor.SpawnChance))
                        {
                            continue;
                        }
                        Vector3 platePosI;
                        Vector3 platePosJ = default;
                        bool renderJ = true;
                        bool renderI = true;
                        if (targetDescriptor.PlaceOnSegmentInsteadOfCorner)
                        {
                            platePosI = CaculateCenterSegment(ref netSegmentI, targetDescriptor, out segmentIDirection.y, out renderI);
                            platePosJ = CaculateCenterSegment(ref netSegmentJ, targetDescriptor, out segmentJDirection.y, out renderJ, true);
                        }
                        else
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
                        FillCacheData(nodeID, controlBoardIdx, 0, segmentIid, platePosI, segmentIDirection, targetDescriptor, renderI);
                        FillCacheData(nodeID, controlBoardIdx, 1, segmentJid, platePosJ, segmentJDirection, targetDescriptor, renderJ);
                        controlBoardIdx++;
                    }
                }
            }
            if (controlBoardIdx == 0)
            {
                Data.BoardsContainers[nodeID, 0, 0] = new CacheRoadNodeItem();
                Data.BoardsContainers[nodeID, 0, 1] = null;
                controlBoardIdx++;
            }
            while (controlBoardIdx < 8)
            {

                Data.BoardsContainers[nodeID, controlBoardIdx, 0] = null;
                Data.BoardsContainers[nodeID, controlBoardIdx, 1] = null;
                controlBoardIdx++;
            }

            m_updatedStreetPositions[nodeID] = true;
        }

        private static Vector3 CaculateCenterSegment(ref NetSegment segmentI, BoardInstanceRoadNodeXml descriptor, out float rotationOffset, out bool render, bool invertSide = false)
        {
            bool invertedSegment = (segmentI.m_flags & NetSegment.Flags.Invert) != 0;
            Vector3 platePosI;
            Vector3 bezierPos = segmentI.GetBezier().Position(Mathf.Max(0, Mathf.Min(1, (descriptor.m_propPosition.x / 2) + 0.5f)));

            segmentI.GetClosestPositionAndDirection(bezierPos, out _, out Vector3 dir);
            rotationOffset = invertedSegment != invertSide ? 90 : -90;
            float rotation = dir.GetAngleXZ() + rotationOffset;
            //if (sign.descriptor.m_invertSign != segmentInverted)
            //{
            //    rotation += 180;
            //}

            render = (segmentI.Info.m_hasBackwardVehicleLanes && invertedSegment != invertSide) || (segmentI.Info.m_hasForwardVehicleLanes && invertedSegment == invertSide);

            Vector3 rotationVectorX = VectorUtils.X_Y(KlyteMathUtils.DegreeToVector2(rotation));
            platePosI = bezierPos + ((rotationVectorX * segmentI.Info.m_halfWidth) + (VectorUtils.X_Y(KlyteMathUtils.DegreeToVector2(rotation)) * descriptor.m_propPosition.z));
            platePosI.y += descriptor.m_propPosition.y;

            return platePosI;
        }

        private void FillCacheData(ushort nodeID, int controlBoardIdx, int boardId, ushort segmentIid, Vector3 platePosI, Vector3 segmentIDirection, BoardInstanceRoadNodeXml targetDescriptor, bool render)
        {

            Data.BoardsContainers[nodeID, controlBoardIdx, boardId] = new CacheRoadNodeItem();
            ref CacheRoadNodeItem refBoard = ref Data.BoardsContainers[nodeID, controlBoardIdx, boardId];

            float dir = Vector2.zero.GetAngleToPoint(VectorUtils.XZ(segmentIDirection));

            refBoard.m_streetDirection = -dir + 90 + segmentIDirection.y;
            refBoard.m_segmentId = segmentIid;
            refBoard.m_districtId = DistrictManager.instance.GetDistrict(NetManager.instance.m_segments.m_buffer[segmentIid].m_middlePosition);
            refBoard.m_platePosition = platePosI;
            refBoard.m_renderPlate = render;
            refBoard.m_cachedColor = targetDescriptor.UseDistrictColor ? WTSHookable.GetDistrictColor(refBoard.m_districtId) : targetDescriptor.Descriptor.FixedColor ?? Color.white;
            refBoard.m_cachedContrastColor = KlyteMonoUtils.ContrastColor(refBoard.m_cachedColor);
            refBoard.m_distanceRef = Vector2.Distance(VectorUtils.XZ(refBoard.m_platePosition), WTSHookable.GetStartPoint());
            refBoard.m_currentDescriptor = targetDescriptor;
        }

        private static void GetNeigborSegment(ushort nodeID, ref NetNode data, ushort segmentIid, ref NetSegment netSegmentI, out Vector3 segmentIDirection, out Vector3 otherSegmentDirection, out ushort resultOtherSegment)
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
        }

        protected InstanceID GetPropRenderID(ushort nodeId)
        {
            return new InstanceID
            {
                NetNode = nodeId
            };

        }
    }

}
