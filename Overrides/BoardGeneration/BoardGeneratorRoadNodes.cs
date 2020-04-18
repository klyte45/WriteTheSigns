using ColossalFramework;
using ColossalFramework.Math;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Data;
using Klyte.DynamicTextProps.Rendering;
using Klyte.DynamicTextProps.Utils;
using Klyte.DynamicTextProps.Xml;
using SpriteFontPlus;
using System;
using System.Linq;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{

    public class BoardGeneratorRoadNodes : Redirector, IRedirectable
    {
        public static BoardGeneratorRoadNodes Instance;
        public DynamicSpriteFont DrawFont => FontServer.instance[Data.DefaultFont] ?? FontServer.instance[DTPController.DEFAULT_FONT_KEY];
        public DTPRoadNodesData Data => DTPRoadNodesData.Instance;
        public bool[] m_updatedStreetPositions;
        public uint[] m_lastFrameUpdate;

        private readonly Func<RenderManager, uint> m_getCurrentFrame = ReflectionUtils.GetGetFieldDelegate<RenderManager, uint>("m_currentFrame", typeof(RenderManager));

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
            if (Data.CurrentDescriptor == null)
            {
                Data.CurrentDescriptor = new BoardDescriptorRoadNodeXml();
            }
            if (Data.CurrentDescriptor?.Descriptor.m_propName == null || m_lastFrameUpdate[nodeID] >= m_getCurrentFrame(RenderManager.instance))
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
            if (Data.BoardsContainers[nodeID] == null)
            {
                Data.BoardsContainers[nodeID] = new IBoardBunchContainer<CacheControlRoadNode>();
            }
            //m_streetPlatePrefab
            if (Data.BoardsContainers[nodeID]?.m_boardsData?.Count() != data.CountSegments() || !m_updatedStreetPositions[nodeID])
            {
                if (m_lastTickUpdate == SimulationManager.instance.m_currentTickIndex)
                {
                    return;
                }

                m_lastTickUpdate = SimulationManager.instance.m_currentTickIndex;

                LogUtils.DoLog($"updatedStreets! {nodeID} {data.CountSegments()}");
                Data.BoardsContainers[nodeID].m_boardsData = new CacheControlRoadNode[data.CountSegments()];
                int controlBoardIdx = 0;
                for (int i = 0; i < 8; i++)
                {
                    ushort segmentIid = data.GetSegment(i);
                    if (segmentIid != 0)
                    {
                        NetSegment netSegmentI = Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid];
                        if (netSegmentI.Info != null && netSegmentI.Info.m_netAI is RoadBaseAI roadAiI)
                        {
                            Vector3 segmentIDirection = (nodeID != netSegmentI.m_startNode) ? netSegmentI.m_endDirection : netSegmentI.m_startDirection;
                            Vector3 otherSegmentDirection = Vector3.zero;
                            float resultAngle = -4f;
                            ushort resultOtherSegment = 0;
                            for (int j = 0; j < 8; j++)
                            {
                                ushort segmentJid = data.GetSegment(j);
                                if (segmentJid != 0 && segmentJid != segmentIid)
                                {
                                    NetSegment netSegmentCand = Singleton<NetManager>.instance.m_segments.m_buffer[segmentJid];
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
                            if (resultOtherSegment == 0
                                || !(Singleton<NetManager>.instance.m_segments.m_buffer[resultOtherSegment].Info.m_netAI is RoadBaseAI roadAiJ)
                                || SegmentUtils.IsSameName(resultOtherSegment, segmentIid, false, false, true, Data.CurrentDescriptor.PlaceOnDistrictBorder, true)
                                || (roadAiJ.m_highwayRules && roadAiI.m_highwayRules)
                                || roadAiI.GenerateName(segmentIid, ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid]).IsNullOrWhiteSpace()
                                || roadAiJ.GenerateName(resultOtherSegment, ref Singleton<NetManager>.instance.m_segments.m_buffer[resultOtherSegment]).IsNullOrWhiteSpace())
                            {
                                continue;
                            }
                            bool start = netSegmentI.m_startNode == nodeID;

                            netSegmentI.CalculateCorner(segmentIid, true, start, false, out Vector3 startPos, out Vector3 startAng, out _);
                            NetSegment netSegmentJ = Singleton<NetManager>.instance.m_segments.m_buffer[resultOtherSegment];
                            start = (netSegmentJ.m_startNode == nodeID);

                            netSegmentJ.CalculateCorner(resultOtherSegment, true, start, true, out Vector3 endPos, out Vector3 endAng, out bool flag);

                            NetSegment.CalculateMiddlePoints(startPos, -startAng, endPos, -endAng, true, true, out Vector3 rhs, out Vector3 lhs);
                            Vector3 relativePos = (((rhs + lhs) * 0.5f) - data.m_position);
                            Vector3 platePos = relativePos - relativePos.normalized + data.m_position;

                            if (Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx] == null)
                            {
                                Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx] = new CacheControlRoadNode();
                            }

                            float dir1 = Vector2.zero.GetAngleToPoint(VectorUtils.XZ(segmentIDirection));
                            float dir2 = Vector2.zero.GetAngleToPoint(VectorUtils.XZ(otherSegmentDirection));
                            Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_streetDirection1 = -dir1;
                            Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_streetDirection2 = -dir2;

                            Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_segmentId1 = segmentIid;
                            Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_segmentId2 = resultOtherSegment;
                            Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_districtId1 = DistrictManager.instance.GetDistrict(NetManager.instance.m_segments.m_buffer[segmentIid].m_middlePosition);
                            Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_districtId2 = DistrictManager.instance.GetDistrict(NetManager.instance.m_segments.m_buffer[resultOtherSegment].m_middlePosition);

                            Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_platePosition = platePos;
                            Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_renderPlate = true;
                            Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedColor = Data.CurrentDescriptor.UseDistrictColor ? DTPHookable.GetDistrictColor(Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_districtId1) : Data.CurrentDescriptor.Descriptor.FixedColor ?? Color.white;
                            Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedColor2 = Data.CurrentDescriptor.UseDistrictColor ? DTPHookable.GetDistrictColor(Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_districtId2) : Data.CurrentDescriptor.Descriptor.FixedColor ?? Color.white;
                            Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedContrastColor = KlyteMonoUtils.ContrastColor(Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedColor);
                            Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedContrastColor2 = KlyteMonoUtils.ContrastColor(Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedColor2);
                            Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_distanceRef = Vector2.Distance(VectorUtils.XZ(Data.BoardsContainers[nodeID].m_boardsData[controlBoardIdx].m_platePosition), DTPHookable.GetStartPoint());
                            controlBoardIdx++;
                        }
                    }
                }

                m_updatedStreetPositions[nodeID] = true;
            }


            for (int boardIdx = 0; boardIdx < Data.BoardsContainers[nodeID].m_boardsData.Length; boardIdx++)
            {
                if (Data.BoardsContainers[nodeID].m_boardsData[boardIdx]?.m_renderPlate ?? false)
                {
                    if (Data.BoardsContainers[nodeID].m_boardsData[boardIdx]?.m_cachedProp?.name != Data.CurrentDescriptor?.Descriptor.m_propName)
                    {
                        Data.BoardsContainers[nodeID].m_boardsData[boardIdx].m_cachedProp = null;
                    }

                    DTPPropRenderingRules.RenderPropMesh(ref Data.BoardsContainers[nodeID].m_boardsData[boardIdx].m_cachedProp, cameraInfo, nodeID, boardIdx, 0, 0xFFFFFFF, 0, Data.BoardsContainers[nodeID].m_boardsData[boardIdx].m_platePosition, Vector4.zero, ref Data.CurrentDescriptor.Descriptor.m_propName, new Vector3(0, Data.BoardsContainers[nodeID].m_boardsData[boardIdx].m_streetDirection1) + Data.CurrentDescriptor.m_propRotation, Data.CurrentDescriptor.PropScale, Data.CurrentDescriptor, out Matrix4x4 propMatrix, out bool rendered, GetPropRenderID(nodeID));
                    if (rendered)
                    {
                        for (int j = 0; j < Data.CurrentDescriptor.Descriptor.m_textDescriptors.Length; j++)
                        {
                            MaterialPropertyBlock properties = PropManager.instance.m_materialBlock;
                            properties.Clear();
                            DTPPropRenderingRules.RenderTextMesh(nodeID, boardIdx, 0, Data.CurrentDescriptor, propMatrix, Data.CurrentDescriptor.Descriptor.m_textDescriptors[j], properties, DrawFont);
                        }
                    }
                    DTPPropRenderingRules.RenderPropMesh(ref Data.BoardsContainers[nodeID].m_boardsData[boardIdx].m_cachedProp, cameraInfo, nodeID, boardIdx, 1, 0xFFFFFFF, 0, Data.BoardsContainers[nodeID].m_boardsData[boardIdx].m_platePosition, Vector4.zero, ref Data.CurrentDescriptor.Descriptor.m_propName, new Vector3(0, Data.BoardsContainers[nodeID].m_boardsData[boardIdx].m_streetDirection2) + Data.CurrentDescriptor.m_propRotation, Data.CurrentDescriptor.PropScale, Data.CurrentDescriptor, out propMatrix, out rendered, GetPropRenderID(nodeID));
                    if (rendered)
                    {

                        for (int j = 0; j < Data.CurrentDescriptor.Descriptor.m_textDescriptors.Length; j++)
                        {
                            MaterialPropertyBlock properties = PropManager.instance.m_materialBlock;
                            properties.Clear();
                            DTPPropRenderingRules.RenderTextMesh(nodeID, boardIdx, 1, Data.CurrentDescriptor, propMatrix, Data.CurrentDescriptor.Descriptor.m_textDescriptors[j], properties, DrawFont);
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
        public void CleanDescriptor() => Data.CurrentDescriptor = new BoardDescriptorRoadNodeXml();
    }

}
