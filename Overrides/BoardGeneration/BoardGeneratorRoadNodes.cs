using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using ICities;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Libraries;
using Klyte.DynamicTextProps.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorRoadNodes;

namespace Klyte.DynamicTextProps.Overrides
{

    public partial class BoardGeneratorRoadNodes : BoardGeneratorParent<BoardGeneratorRoadNodes, BoardBunchContainerStreetPlateXml, CacheControlStreetPlate, BasicRenderInformation, BoardDescriptorStreetSignXml, BoardTextDescriptorSteetSignXml>, ISerializableDataExtension
    {

        private UpdateFlagsSegments[] m_updateDataSegments;
        public bool[] m_updatedStreetPositions;
        public uint[] m_lastFrameUpdate;

        public BasicRenderInformation[] m_cachedDistrictsNames;
        public BasicRenderInformation[] m_cachedNumber;



        public override int ObjArraySize => NetManager.MAX_NODE_COUNT;
        public override UIDynamicFont DrawFont => m_font;
        private UIDynamicFont m_font;

        private readonly Func<RenderManager, uint> m_getCurrentFrame = ReflectionUtils.GetGetFieldDelegate<RenderManager, uint>("m_currentFrame", typeof(RenderManager));

        public static readonly TextType[] AVAILABLE_TEXT_TYPES = new TextType[]
        {
            TextType.OwnName,
            TextType.Fixed,
            TextType.StreetPrefix,
            TextType.StreetSuffix,
            TextType.StreetNameComplete,
            TextType.Custom1,
            TextType.Custom2
        };

        #region Initialize
        public override void Initialize()
        {
            m_updateDataSegments = new UpdateFlagsSegments[NetManager.MAX_SEGMENT_COUNT];
            m_lastFrameUpdate = new uint[NetManager.MAX_NODE_COUNT];
            m_updatedStreetPositions = new bool[ObjArraySize];
            m_cachedDistrictsNames = new BasicRenderInformation[DistrictManager.MAX_DISTRICT_COUNT];
            m_cachedNumber = new BasicRenderInformation[10];

            BuildSurfaceFont(out m_font, LoadedStreetSignDescriptor.FontName);

            NetManagerOverrides.EventNodeChanged += OnNodeChanged;
            DistrictManagerOverrides.EventOnDistrictChanged += OnDistrictChanged;
            NetManagerOverrides.EventSegmentNameChanged += OnNameSeedChanged;

            Type adrEventsType = Type.GetType("Klyte.Addresses.ModShared.AdrEvents, KlyteAddresses");
            if (adrEventsType != null)
            {
                static void RegisterEvent(string eventName, Type adrEventsType, Action action) => adrEventsType.GetEvent(eventName)?.AddEventHandler(null, action);
                RegisterEvent("EventZeroMarkerBuildingChange", adrEventsType, new Action(OnZeroMarkChanged));
                RegisterEvent("EventRoadNamingChange", adrEventsType, new Action(OnZeroMarkChanged));
                RegisterEvent("EventDistrictColorChanged", adrEventsType, new Action(OnZeroMarkChanged));
                RegisterEvent("EventBuildingNameStrategyChanged", adrEventsType, new Action(OnZeroMarkChanged));
            }



            #region Hooks
            System.Reflection.MethodInfo postRenderMeshs = GetType().GetMethod("AfterRenderSegment", RedirectorUtils.allFlags);
            System.Reflection.MethodInfo orig = typeof(NetSegment).GetMethod("RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int) });
            LogUtils.DoLog($"Patching: {orig} => {postRenderMeshs} {postRenderMeshs.IsStatic}");
            RedirectorInstance.AddRedirect(orig, null, postRenderMeshs);
            #endregion
        }

        protected override void OnTextureRebuiltImpl(Font obj)
        {
            if (obj.name == DrawFont.baseFont.name)
            {
                SoftReset();
            }
        }

        protected override void OnChangeFont(string fontName) => LoadedStreetSignDescriptor.FontName = fontName;

        protected void Reset()
        {
            m_boardsContainers = new BoardBunchContainerStreetPlateXml[ObjArraySize];
            m_cachedNumber = new BasicRenderInformation[10];
            m_updateDataSegments = new UpdateFlagsSegments[NetManager.MAX_SEGMENT_COUNT];
            m_updatedStreetPositions = new bool[ObjArraySize];
            m_cachedDistrictsNames = new BasicRenderInformation[DistrictManager.MAX_DISTRICT_COUNT];
            m_testTextInfo = null;
        }

        public void SoftReset()
        {
            m_testTextInfo = null;
            m_cachedNumber = new BasicRenderInformation[10];
            m_updatedStreetPositions = new bool[ObjArraySize];
            m_cachedDistrictsNames = new BasicRenderInformation[DistrictManager.MAX_DISTRICT_COUNT];
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
        //private void OnSegmentChanged(ushort segmentId)
        //{
        //    //doLog("onSegmentChanged");
        //    m_updatedStreetPositions[NetManager.instance.m_segments.m_buffer[segmentId].m_endNode] = false;
        //    m_updatedStreetPositions[NetManager.instance.m_segments.m_buffer[segmentId].m_startNode] = false;
        //    m_updateDataSegments[segmentId] = new UpdateFlagsSegments();
        //}
        private void OnDistrictChanged()
        {
            LogUtils.DoLog("onDistrictChanged");
            m_cachedDistrictsNames = new BasicRenderInformation[DistrictManager.MAX_DISTRICT_COUNT];
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
            if (LoadedStreetSignDescriptor == null)
            {
                LoadedStreetSignDescriptor = new BoardDescriptorStreetSignXml();
            }
            if (LoadedStreetSignDescriptor?.m_propName == null || m_lastFrameUpdate[nodeID] >= m_getCurrentFrame(RenderManager.instance))
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
            if (m_boardsContainers[nodeID] == null)
            {
                m_boardsContainers[nodeID] = new BoardBunchContainerStreetPlateXml();
            }
            //m_streetPlatePrefab
            if (m_boardsContainers[nodeID]?.m_boardsData?.Count() != data.CountSegments() || !m_updatedStreetPositions[nodeID])
            {
                if (m_lastTickUpdate == SimulationManager.instance.m_currentTickIndex)
                {
                    return;
                }

                m_lastTickUpdate = SimulationManager.instance.m_currentTickIndex;

                LogUtils.DoLog($"updatedStreets! {nodeID} {data.CountSegments()}");
                m_boardsContainers[nodeID].m_boardsData = new CacheControlStreetPlate[data.CountSegments()];
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
                                || SegmentUtils.IsSameName(resultOtherSegment, segmentIid, false, false, true, true, true)
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

                            if (m_boardsContainers[nodeID].m_boardsData[controlBoardIdx] == null)
                            {
                                m_boardsContainers[nodeID].m_boardsData[controlBoardIdx] = new CacheControlStreetPlate();
                            }

                            float dir1 = Vector2.zero.GetAngleToPoint(VectorUtils.XZ(segmentIDirection));
                            float dir2 = Vector2.zero.GetAngleToPoint(VectorUtils.XZ(otherSegmentDirection));
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_streetDirection1 = -dir1;
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_streetDirection2 = -dir2;

                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_segmentId1 = segmentIid;
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_segmentId2 = resultOtherSegment;
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_districtId1 = DistrictManager.instance.GetDistrict(NetManager.instance.m_segments.m_buffer[segmentIid].m_middlePosition);
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_districtId2 = DistrictManager.instance.GetDistrict(NetManager.instance.m_segments.m_buffer[resultOtherSegment].m_middlePosition);

                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_platePosition = platePos;
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_renderPlate = true;
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedColor = LoadedStreetSignDescriptor.UseDistrictColor ? DTPHookable.GetDistrictColor(m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_districtId1) : LoadedStreetSignDescriptor.PropColor;
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedColor2 = LoadedStreetSignDescriptor.UseDistrictColor ? DTPHookable.GetDistrictColor(m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_districtId2) : LoadedStreetSignDescriptor.PropColor;
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedContrastColor = KlyteMonoUtils.ContrastColor(m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedColor);
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedContrastColor2 = KlyteMonoUtils.ContrastColor(m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedColor2);
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_distanceRef = Vector2.Distance(VectorUtils.XZ(m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_platePosition), DTPHookable.GetStartPoint());
                            controlBoardIdx++;
                        }
                    }
                }

                m_updatedStreetPositions[nodeID] = true;
            }


            for (int boardIdx = 0; boardIdx < m_boardsContainers[nodeID].m_boardsData.Length; boardIdx++)
            {
                if (m_boardsContainers[nodeID].m_boardsData[boardIdx]?.m_renderPlate ?? false)
                {
                    if (m_boardsContainers[nodeID].m_boardsData[boardIdx]?.m_cachedProp?.name != LoadedStreetSignDescriptor?.m_propName)
                    {
                        m_boardsContainers[nodeID].m_boardsData[boardIdx].m_cachedProp = null;
                    }

                    RenderPropMesh(ref m_boardsContainers[nodeID].m_boardsData[boardIdx].m_cachedProp, cameraInfo, nodeID, boardIdx, 0, 0xFFFFFFF, 0, m_boardsContainers[nodeID].m_boardsData[boardIdx].m_platePosition, Vector4.zero, ref LoadedStreetSignDescriptor.m_propName, new Vector3(0, m_boardsContainers[nodeID].m_boardsData[boardIdx].m_streetDirection1) + LoadedStreetSignDescriptor.m_propRotation, LoadedStreetSignDescriptor.PropScale, ref m_loadedStreetSignDescriptor, out Matrix4x4 propMatrix, out bool rendered);
                    if (rendered)
                    {
                        for (int j = 0; j < LoadedStreetSignDescriptor.m_textDescriptors.Length; j++)
                        {
                            MaterialPropertyBlock properties = PropManager.instance.m_materialBlock;
                            properties.Clear();
                            RenderTextMesh(cameraInfo, nodeID, boardIdx, 0, ref m_loadedStreetSignDescriptor, propMatrix, ref LoadedStreetSignDescriptor.m_textDescriptors[j], ref m_boardsContainers[nodeID].m_boardsData[boardIdx], properties);
                        }
                    }
                    RenderPropMesh(ref m_boardsContainers[nodeID].m_boardsData[boardIdx].m_cachedProp, cameraInfo, nodeID, boardIdx, 1, 0xFFFFFFF, 0, m_boardsContainers[nodeID].m_boardsData[boardIdx].m_platePosition, Vector4.zero, ref LoadedStreetSignDescriptor.m_propName, new Vector3(0, m_boardsContainers[nodeID].m_boardsData[boardIdx].m_streetDirection2) + LoadedStreetSignDescriptor.m_propRotation, LoadedStreetSignDescriptor.PropScale, ref m_loadedStreetSignDescriptor, out propMatrix, out rendered);
                    if (rendered)
                    {

                        for (int j = 0; j < LoadedStreetSignDescriptor.m_textDescriptors.Length; j++)
                        {
                            MaterialPropertyBlock properties = PropManager.instance.m_materialBlock;
                            properties.Clear();
                            RenderTextMesh(cameraInfo, nodeID, boardIdx, 1, ref m_loadedStreetSignDescriptor, propMatrix, ref LoadedStreetSignDescriptor.m_textDescriptors[j], ref m_boardsContainers[nodeID].m_boardsData[boardIdx], properties);
                        }
                    }

                }
            }

        }



        #region Upadate Data

        protected override BasicRenderInformation GetMeshStreetSuffix(ushort idx, int boardIdx, int secIdx, out UIFont font)
        {
            font = DrawFont;
            if (!m_updateDataSegments[idx].m_streetSuffixMesh)
            {
                m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfo = null;
                m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfo2 = null;
                m_updateDataSegments[idx].m_streetSuffixMesh = true;
            }
            if (secIdx == 0)
            {
                if (m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfo == null || lastFontUpdateFrame > m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfoDrawTime)
                {
                    LogUtils.DoLog($"!nameUpdated Node1 {idx}");
                    UpdateMeshStreetSuffix(m_boardsContainers[idx].m_boardsData[boardIdx].m_segmentId1, ref m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfo);
                    m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfoDrawTime = lastFontUpdateFrame;
                }
                return m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfo;
            }
            else
            {
                if (m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfo2 == null || lastFontUpdateFrame > m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfoDrawTime2)
                {
                    LogUtils.DoLog($"!nameUpdated Node2 {idx}");
                    UpdateMeshStreetSuffix(m_boardsContainers[idx].m_boardsData[boardIdx].m_segmentId2, ref m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfo2);
                    m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfoDrawTime2 = lastFontUpdateFrame;
                }
                return m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfo2;
            }
        }

        protected override BasicRenderInformation GetMeshFullStreetName(ushort idx, int boardIdx, int secIdx, out UIFont font)
        {
            font = DrawFont;
            if (!m_updateDataSegments[idx].m_nameMesh)
            {
                m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfo = null;
                m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfo2 = null;
                m_updateDataSegments[idx].m_nameMesh = true;
            }
            if (secIdx == 0)
            {
                if (m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfo == null || lastFontUpdateFrame > m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfoDrawTime)
                {
                    LogUtils.DoLog($"!GetMeshFullStreetName Node1 {idx}");
                    UpdateMeshFullNameStreet(m_boardsContainers[idx].m_boardsData[boardIdx].m_segmentId1, ref m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfo);
                    m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfoDrawTime = lastFontUpdateFrame;
                }
                return m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfo;
            }
            else
            {
                if (m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfo2 == null || lastFontUpdateFrame > m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfoDrawTime2)
                {
                    LogUtils.DoLog($"!GetMeshFullStreetName Node2 {idx}");
                    UpdateMeshFullNameStreet(m_boardsContainers[idx].m_boardsData[boardIdx].m_segmentId2, ref m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfo2);
                    m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfoDrawTime2 = lastFontUpdateFrame;
                }
                return m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfo2;
            }
        }

        protected override BasicRenderInformation GetMeshCustom1(ushort idx, int boardIdx, int secIdx, out UIFont font)
        {
            font = DrawFont;
            byte districtId;
            if (secIdx == 0)
            {
                districtId = m_boardsContainers[idx].m_boardsData[boardIdx].m_districtId1;
            }
            else
            {
                districtId = m_boardsContainers[idx].m_boardsData[boardIdx].m_districtId2;
            }
            if (m_cachedDistrictsNames[districtId] == null)
            {
                LogUtils.DoLog($"!districtName {districtId}");
                string name;
                if (districtId == 0)
                {
                    name = SimulationManager.instance.m_metaData.m_CityName;
                }
                else
                {
                    name = DistrictManager.instance.GetDistrictName(districtId);
                }
                RefreshNameData(ref m_cachedDistrictsNames[districtId], name);
            }
            return m_cachedDistrictsNames[districtId];

        }
        protected override BasicRenderInformation GetMeshCustom2(ushort idx, int boardIdx, int secIdx, out UIFont font)
        {
            font = DrawFont;
            int distanceRef = (int) Mathf.Floor(m_boardsContainers[idx].m_boardsData[boardIdx].m_distanceRef / 1000);
            while (m_cachedNumber.Length <= distanceRef + 1)
            {
                LogUtils.DoLog($"!Length {m_cachedNumber.Length }/{distanceRef}");
                List<BasicRenderInformation> newArray = m_cachedNumber.ToList();
                newArray.Add(null);
                m_cachedNumber = newArray.ToArray();
            }
            if (m_cachedNumber[distanceRef] == null)
            {
                LogUtils.DoLog($"!m_cachedNumber {distanceRef}");
                RefreshNameData(ref m_cachedNumber[distanceRef], distanceRef.ToString());
            }
            return m_cachedNumber[distanceRef];

        }



        #endregion

        public override Color GetColor(ushort buildingID, int idx, int secIdx, BoardDescriptorStreetSignXml descriptor)
        {
            if (secIdx == 0)
            {
                return m_boardsContainers[buildingID].m_boardsData[idx]?.m_cachedColor ?? Color.white;
            }
            else
            {
                return m_boardsContainers[buildingID].m_boardsData[idx]?.m_cachedColor2 ?? Color.white;
            }
        }
        public override Color GetContrastColor(ushort buildingID, int idx, int secIdx, BoardDescriptorStreetSignXml descriptor)
        {
            if (secIdx == 0)
            {
                return m_boardsContainers[buildingID].m_boardsData[idx]?.m_cachedContrastColor ?? Color.black;
            }
            else
            {
                return m_boardsContainers[buildingID].m_boardsData[idx]?.m_cachedContrastColor2 ?? Color.black;
            }
        }



        protected override InstanceID GetPropRenderID(ushort nodeId)
        {
            InstanceID result = default;
            result.NetNode = nodeId;
            return result;
        }

        private struct UpdateFlagsSegments
        {
            public bool m_nameMesh;
            public bool m_streetSuffixMesh;
        }

        private BasicRenderInformation m_testTextInfo = null;
        private long m_testTextInfoTime = 0;
        protected override BasicRenderInformation GetOwnNameMesh(ushort buildingID, int boardIdx, int secIdx, out UIFont font)
        {
            font = DrawFont;
            if (m_testTextInfo == null || m_testTextInfoTime < lastFontUpdateFrame)
            {
                string resultText = "WWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWW";
                UIFont overrideFont = null;
                RefreshNameData(ref m_testTextInfo, resultText, overrideFont);
                m_testTextInfoTime = lastFontUpdateFrame;
            }
            return m_testTextInfo;
        }

        public BoardDescriptorStreetSignXml LoadedStreetSignDescriptor
        {
            get {
                if (m_loadedStreetSignDescriptor == null)
                {
                    m_loadedStreetSignDescriptor = m_loadedXml ?? new BoardDescriptorStreetSignXml();
                    m_loadedXml = null;
                }
                return m_loadedStreetSignDescriptor;
            }

            set => m_loadedStreetSignDescriptor = value;
        }

        private static BoardDescriptorStreetSignXml m_loadedXml = null;

        public void CleanDescriptor() => m_loadedStreetSignDescriptor = new BoardDescriptorStreetSignXml();


        private BoardDescriptorStreetSignXml m_loadedStreetSignDescriptor = null;
        public static void GenerateDefaultSignModelAtLibrary()
        {
            BoardDescriptorStreetSignXml defaultModel = new BoardDescriptorStreetSignXml
            {
                FontName = null,
                m_propName = "1679673551.Street Plate_Data",
                m_propRotation = new Vector3(0, 90, 0),
                UseDistrictColor = true,
                m_textDescriptors = new BoardTextDescriptorSteetSignXml[]{
                    new BoardTextDescriptorSteetSignXml{
                        m_textRelativePosition =new Vector3(0.53f,2.35f,-0.001f) ,
                        m_textRelativeRotation = Vector3.zero,
                        m_maxWidthMeters = 0.92f,
                        m_textScale = .4f,
                        m_useContrastColor = false,
                        m_defaultColor = Color.white,
                        m_textType = TextType.StreetSuffix,
                        m_textAlign = UIHorizontalAlignment.Left,
                        m_verticalAlign = UIVerticalAlignment.Middle,
                        SaveName = "[A] Street Suffix"
                    },
                    new BoardTextDescriptorSteetSignXml{
                        m_textRelativePosition =new Vector3(0.53f,2.19f,-0.001f) ,
                        m_textRelativeRotation = Vector3.zero,
                        m_maxWidthMeters = 0.92f,
                        m_textScale = .15f,
                        m_useContrastColor = false,
                        m_defaultColor = Color.white,
                        m_textType = TextType.StreetNameComplete,
                        m_textAlign = UIHorizontalAlignment.Left,
                        m_verticalAlign = UIVerticalAlignment.Middle,
                        SaveName = "[A] Street Name Complete"
                    },
                    new BoardTextDescriptorSteetSignXml{
                        m_textRelativePosition =new Vector3(0.47f,2.05f,-0.001f) ,
                        m_textRelativeRotation = Vector3.zero,
                        m_maxWidthMeters = 0.8f,
                        m_textScale = .2f,
                        m_useContrastColor = true,
                        m_textType = TextType.Custom1,// District
                        m_verticalAlign =  UIVerticalAlignment.Middle,
                        SaveName = "[A] District"
                    },
                    new BoardTextDescriptorSteetSignXml{
                        m_textRelativePosition =new Vector3(0.94f,2.065f,-0.001f) ,
                        m_textRelativeRotation = Vector3.zero,
                        m_maxWidthMeters = 0.1f,
                        m_textScale = .25f,
                        m_useContrastColor = false,
                        m_defaultColor = Color.black,
                        m_textType = TextType.Custom2, //Distance
                        m_verticalAlign = UIVerticalAlignment.Middle,
                        SaveName = "[A] Distance"
                    },
                    new BoardTextDescriptorSteetSignXml{
                        m_textRelativePosition =new Vector3(0.53f,2.35f,0.001f) ,
                        m_textRelativeRotation = new Vector3(0,180,0),
                        m_maxWidthMeters = 0.92f,
                        m_textScale = .4f,
                        m_useContrastColor = false,
                        m_defaultColor = Color.white,
                        m_textType = TextType.StreetSuffix,
                        m_textAlign = UIHorizontalAlignment.Left,
                        m_verticalAlign = UIVerticalAlignment.Middle,
                        SaveName = "[B] Street Suffix"
                    },
                    new BoardTextDescriptorSteetSignXml{
                        m_textRelativePosition =new Vector3(0.53f,2.19f,0.001f) ,
                        m_textRelativeRotation = new Vector3(0,180,0),
                        m_maxWidthMeters = 0.92f,
                        m_textScale = .15f,
                        m_useContrastColor = false,
                        m_defaultColor = Color.white,
                        m_textType = TextType.StreetNameComplete,
                        m_textAlign = UIHorizontalAlignment.Left,
                        m_verticalAlign = UIVerticalAlignment.Middle,
                        SaveName = "[B] Street Name Complete"
                    },
                    new BoardTextDescriptorSteetSignXml{
                        m_textRelativePosition =new Vector3(0.47f,2.05f,0.001f) ,
                        m_textRelativeRotation = new Vector3(0,180,0),
                        m_maxWidthMeters = 0.8f,
                        m_textScale = .2f,
                        m_useContrastColor = true,
                        m_textType = TextType.Custom1,// District
                        m_verticalAlign =  UIVerticalAlignment.Middle,
                        SaveName = "[B] District"
                    },
                    new BoardTextDescriptorSteetSignXml{
                        m_textRelativePosition =new Vector3(0.94f,2.065f,0.001f) ,
                        m_textRelativeRotation = new Vector3(0,180,0),
                        m_maxWidthMeters = 0.1f,
                        m_textScale = .25f,
                        m_useContrastColor = false,
                        m_defaultColor = Color.black,
                        m_textType = TextType.Custom2, //Distance
                        m_verticalAlign = UIVerticalAlignment.Middle,
                        SaveName = "[B] Distance"
                    }
                }
            };
            DTPLibStreetPropGroup.Instance.Add("<DEFAULT>", defaultModel);
        }


        #region Serialize
        protected override string ID { get; } = "K45_DTP_SS";

        public override void Deserialize(string data)
        {
            LogUtils.DoLog($"{GetType()} STR: \"{data}\"");
            if (data.IsNullOrWhiteSpace())
            {
                return;
            }
            try
            {
                m_loadedXml = XmlUtils.DefaultXmlDeserialize<BoardDescriptorStreetSignXml>(data);
            }
            catch (Exception e)
            {
                LogUtils.DoErrorLog($"Error deserializing: {e.Message}\n{e.StackTrace}");
            }
        }

        public override string Serialize() => XmlUtils.DefaultXmlSerialize(Instance.LoadedStreetSignDescriptor, false);

        #endregion


    }

}
