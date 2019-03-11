using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Overrides;
using Klyte.Commons.Utils;
using Klyte.DynamicTextBoards.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using UnityEngine;
using static BuildingInfo;

namespace Klyte.DynamicTextBoards.Overrides
{

    public class BoardGeneratorRoadNodes : BoardGeneratorParent<BoardGeneratorRoadNodes, BoardBunchContainerStreetPlate, CacheControlStreetPlate, BasicRenderInformation, BoardDescriptor, BoardTextDescriptor, ushort>
    {

        private UpdateFlagsSegments[] m_updateDataSegments;
        public bool[] m_updatedStreetPositions;

        public BasicRenderInformation[] m_cachedDistrictsNames;
        public BasicRenderInformation[] m_cachedNumber;

        public override int ObjArraySize => NetManager.MAX_NODE_COUNT;
        public override UIDynamicFont DrawFont => m_font;
        private UIDynamicFont m_font;


        #region Initialize
        public override void Initialize()
        {
            m_updateDataSegments = new UpdateFlagsSegments[NetManager.MAX_SEGMENT_COUNT];
            m_updatedStreetPositions = new bool[ObjArraySize];
            m_cachedDistrictsNames = new BasicRenderInformation[DistrictManager.MAX_DISTRICT_COUNT];
            m_cachedNumber = new BasicRenderInformation[10];

            BuildSurfaceFont(out m_font, "Svaector Str8");

            NetManagerOverrides.eventNodeChanged += onNodeChanged;
            DistrictManagerOverrides.eventOnDistrictChanged += onDistrictChanged;
            NetManagerOverrides.eventSegmentNameChanged += onNameSeedChanged;

            #region Hooks
            var postRenderMeshs = GetType().GetMethod("AfterRenderNode", allFlags);
            doLog($"Patching=> {postRenderMeshs} {postRenderMeshs.IsStatic}");
            AddRedirect(typeof(RoadBaseAI).GetMethod("RenderNode", allFlags), null, postRenderMeshs);
            #endregion
        }


        protected override void OnTextureRebuilt()
        {
            m_cachedNumber = new BasicRenderInformation[10];
            m_cachedDistrictsNames = new BasicRenderInformation[DistrictManager.MAX_DISTRICT_COUNT];
        }

        protected void Reset()
        {
            m_boardsContainers = new BoardBunchContainerStreetPlate[ObjArraySize];
            m_updateDataSegments = new UpdateFlagsSegments[NetManager.MAX_SEGMENT_COUNT];
            m_updatedStreetPositions = new bool[ObjArraySize];
            m_cachedDistrictsNames = new BasicRenderInformation[DistrictManager.MAX_DISTRICT_COUNT];
        }

        private void onNodeChanged(ushort nodeId)
        {
            //doLog($"onNodeChanged { System.Environment.StackTrace }");
            m_updatedStreetPositions[nodeId] = false;
        }
        private void onNameSeedChanged(ushort segmentId)
        {
            doLog("onNameSeedChanged");
            m_updatedStreetPositions[NetManager.instance.m_segments.m_buffer[segmentId].m_endNode] = false;
            m_updatedStreetPositions[NetManager.instance.m_segments.m_buffer[segmentId].m_startNode] = false;
        }
        private void onSegmentChanged(ushort segmentId)
        {
            //doLog("onSegmentChanged");
            m_updatedStreetPositions[NetManager.instance.m_segments.m_buffer[segmentId].m_endNode] = false;
            m_updatedStreetPositions[NetManager.instance.m_segments.m_buffer[segmentId].m_startNode] = false;
            m_updateDataSegments[segmentId] = new UpdateFlagsSegments();
        }
        private void onDistrictChanged()
        {
            doLog("onDistrictChanged");
            m_cachedDistrictsNames = new BasicRenderInformation[DistrictManager.MAX_DISTRICT_COUNT];
        }
        #endregion

        public static void AfterRenderNode(RenderManager.CameraInfo cameraInfo, ushort nodeID, ref NetNode nodeData)
        {

            instance.AfterRenderInstanceImpl(cameraInfo, nodeID, ref nodeData);

        }
        public void AfterRenderInstanceImpl(RenderManager.CameraInfo cameraInfo, ushort nodeID, ref NetNode data)
        {

            if (!cameraInfo.CheckRenderDistance(data.m_position, 400))
            {
                return;
            }
            if (data.CountSegments() <= 2)
            {
                return;
            }
            if (m_boardsContainers[nodeID] == null)
            {
                m_boardsContainers[nodeID] = new BoardBunchContainerStreetPlate();
            }
            //m_streetPlatePrefab
            if (m_boardsContainers[nodeID]?.m_boardsData?.Count() != data.CountSegments())
            {
                m_updatedStreetPositions[nodeID] = false;
            }
            var descriptor = m_baseDescriptorStreetPlate;
            var updatedStreets = m_updatedStreetPositions[nodeID];
            if (!updatedStreets)
            {
                doLog($"updatedStreets! {nodeID}");
                m_boardsContainers[nodeID].m_boardsData = new CacheControlStreetPlate[data.CountSegments()];
                var firstId = data.GetSegment(0);
                var currentId = firstId;
                var validSegments = new List<Tuple<ushort, float>>();
                var controlBoardIdx = 0;
                for (int i = 0; i < 8; i++)
                {
                    ushort segmentIid = data.GetSegment(i);
                    if (segmentIid != 0)
                    {
                        NetSegment netSegmentI = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segmentIid];
                        if (netSegmentI.Info != null && netSegmentI.Info.m_netAI is RoadBaseAI roadAiI)
                        {
                            Vector3 startPos = Vector3.zero;
                            Vector3 startAng = Vector3.zero;
                            Vector3 endPos = Vector3.zero;
                            Vector3 endAng = Vector3.zero;
                            Vector3 segmentIDirection = (nodeID != netSegmentI.m_startNode) ? netSegmentI.m_endDirection : netSegmentI.m_startDirection;
                            Vector3 otherSegmentDirection = Vector3.zero;
                            float resultAngle = -4f;
                            ushort resultOtherSegment = 0;
                            for (int j = 0; j < 8; j++)
                            {
                                ushort segmentJid = data.GetSegment(j);
                                if (segmentJid != 0 && segmentJid != segmentIid)
                                {
                                    NetSegment netSegmentCand = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segmentJid];
                                    if (netSegmentCand.Info != null)
                                    {
                                        Vector3 segmentJDirection = (nodeID != netSegmentCand.m_startNode) ? netSegmentCand.m_endDirection : netSegmentCand.m_startDirection;
                                        float angle = segmentIDirection.x * segmentJDirection.x + segmentIDirection.z * segmentJDirection.z;
                                        if (segmentJDirection.z * segmentIDirection.x - segmentJDirection.x * segmentIDirection.z < 0f)
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
                                || !(Singleton<NetManager>.instance.m_segments.m_buffer[(int)resultOtherSegment].Info.m_netAI is RoadBaseAI roadAiJ)
                                || DTBUtils.IsSameName(resultOtherSegment, segmentIid)
                                || (roadAiJ.m_highwayRules && roadAiI.m_highwayRules)
                                || roadAiI.GenerateName(segmentIid, ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid]).IsNullOrWhiteSpace()
                                || roadAiJ.GenerateName(resultOtherSegment, ref Singleton<NetManager>.instance.m_segments.m_buffer[resultOtherSegment]).IsNullOrWhiteSpace())
                            {
                                continue;
                            }
                            bool start = netSegmentI.m_startNode == nodeID;
                            netSegmentI.CalculateCorner(segmentIid, true, start, false, out startPos, out startAng, out bool flag);
                            NetSegment netSegmentJ = Singleton<NetManager>.instance.m_segments.m_buffer[(int)resultOtherSegment];
                            start = (netSegmentJ.m_startNode == nodeID);
                            netSegmentJ.CalculateCorner(resultOtherSegment, true, start, true, out endPos, out endAng, out flag);

                            NetSegment.CalculateMiddlePoints(startPos, -startAng, endPos, -endAng, true, true, out Vector3 rhs, out Vector3 lhs);
                            var relativePos = ((rhs + lhs) * 0.5f - data.m_position);
                            var platePos = (relativePos - relativePos.normalized) + data.m_position;

                            if (m_boardsContainers[nodeID].m_boardsData[controlBoardIdx] == null) m_boardsContainers[nodeID].m_boardsData[controlBoardIdx] = new CacheControlStreetPlate();

                            var dir1 = Vector2.zero.GetAngleToPoint(VectorUtils.XZ(segmentIDirection));
                            var dir2 = Vector2.zero.GetAngleToPoint(VectorUtils.XZ(otherSegmentDirection));
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_streetDirection1 = -dir1;
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_streetDirection2 = -dir2;

                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_segmentId1 = segmentIid;
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_segmentId2 = resultOtherSegment;
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_districtId1 = DistrictManager.instance.GetDistrict(NetManager.instance.m_segments.m_buffer[segmentIid].m_middlePosition);
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_districtId2 = DistrictManager.instance.GetDistrict(NetManager.instance.m_segments.m_buffer[resultOtherSegment].m_middlePosition);

                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_platePosition = platePos;
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_renderPlate = true;
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedColor = GetDistrictColor(m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_districtId1);
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedColor2 = GetDistrictColor(m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_districtId2);
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedContrastColor = KlyteUtils.contrastColor(m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedColor);
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedContrastColor2 = KlyteUtils.contrastColor(m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_cachedColor2);
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_distanceRef = Vector2.Distance(VectorUtils.XZ(m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_platePosition), GetStartPoint());
                            //doLog($@" ({ NetManager.instance.GetDefaultSegmentName(segmentIid)}) x  ({ NetManager.instance.GetDefaultSegmentName(resultOtherSegment)})
                            //endAng = {endAng}
                            //startAng = {startAng} 
                            // midpos1= { netSegmentI.m_middlePosition} ({segmentIid}) ({ nodeXZ.GetAngleToPoint(VectorUtils.XZ(netSegmentI.m_middlePosition))})
                            // midpos2 = { netSegmentJ.m_middlePosition } ({resultOtherSegment})  ({nodeXZ.GetAngleToPoint(VectorUtils.XZ(netSegmentJ.m_middlePosition))})
                            // m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_streetDirection1 = { m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_streetDirection1 } 
                            // m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_streetDirection2  = { m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_streetDirection2 }
                            // m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_platePosition = { m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_platePosition}
                            // inverted = {inverted}
                            // relativePos = {relativePos} ({Vector2.zero.GetAngleToPoint(VectorUtils.XZ(relativePos))})
                            // node pos = { data.m_position } ({nodeXZ})
                            //");
                            controlBoardIdx++;
                        }
                    }
                }

                //if (validSegments.Count > 2 || (validSegments.Count == 2 && !KlyteUtils.IsSameName(validSegments[0].First, validSegments[1].First)))
                //{
                //    validSegments.Sort((a, b) => a.Second.CompareTo(b.Second));
                //    for (int i = 0; i < validSegments.Count; i++)
                //    {
                //        UpdateSubparams(ref m_boardsContainers[nodeID].m_boardsData[i * 2], nodeID, ref data, validSegments[i].First, validSegments[(i + 1) % validSegments.Count].First);
                //        UpdateSubparams(ref m_boardsContainers[nodeID].m_boardsData[i * 2 + 1], nodeID, ref data, validSegments[i].First, validSegments[(i - 1 + validSegments.Count) % validSegments.Count].First);
                //    }
                //}

                m_updatedStreetPositions[nodeID] = true;
            }


            for (int boardIdx = 0; boardIdx < m_boardsContainers[nodeID].m_boardsData.Length; boardIdx++)
            {
                if (m_boardsContainers[nodeID].m_boardsData[boardIdx]?.m_renderPlate ?? false)
                {

                    RenderPropMesh(ref m_boardsContainers[nodeID].m_boardsData[boardIdx].m_cachedProp, cameraInfo, nodeID, boardIdx, 0, 0xFFFFFFF, 0, m_boardsContainers[nodeID].m_boardsData[boardIdx].m_platePosition, Vector4.zero, ref descriptor.m_propName, new Vector3(0, m_boardsContainers[nodeID].m_boardsData[boardIdx].m_streetDirection1) + descriptor.m_propRotation, descriptor.PropScale, ref descriptor, out Matrix4x4 propMatrix, out bool rendered);
                    if (rendered)
                    {
                        for (int j = 0; j < descriptor.m_textDescriptors.Length; j++)
                        {
                            var properties = PropManager.instance.m_materialBlock;
                            properties.Clear();
                            RenderTextMesh(cameraInfo, nodeID, boardIdx, 0, ref descriptor, propMatrix, ref descriptor.m_textDescriptors[j], ref m_boardsContainers[nodeID].m_boardsData[boardIdx], properties);
                        }
                    }
                    RenderPropMesh(ref m_boardsContainers[nodeID].m_boardsData[boardIdx].m_cachedProp, cameraInfo, nodeID, boardIdx, 1, 0xFFFFFFF, 0, m_boardsContainers[nodeID].m_boardsData[boardIdx].m_platePosition, Vector4.zero, ref descriptor.m_propName, new Vector3(0, m_boardsContainers[nodeID].m_boardsData[boardIdx].m_streetDirection2) + descriptor.m_propRotation, descriptor.PropScale, ref descriptor, out propMatrix, out rendered);
                    if (rendered)
                    {

                        for (int j = 0; j < descriptor.m_textDescriptors.Length; j++)
                        {
                            var properties = PropManager.instance.m_materialBlock;
                            properties.Clear();
                            RenderTextMesh(cameraInfo, nodeID, boardIdx, 1, ref descriptor, propMatrix, ref descriptor.m_textDescriptors[j], ref m_boardsContainers[nodeID].m_boardsData[boardIdx], properties);
                        }
                    }

                }
            }

        }



        #region Upadate Data

        protected override BasicRenderInformation GetMeshStreetSuffix(ushort idx, int boardIdx, int secIdx)
        {
            if (!m_updateDataSegments[idx].m_nameMesh)
            {
                m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfo = null;
                m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfo2 = null;
                m_updateDataSegments[idx].m_nameMesh = true;
            }
            if (secIdx == 0)
            {
                if (m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfo == null || lastFontUpdateFrame > m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfoDrawTime)
                {
                    doLog($"!nameUpdated Node1 {idx}");
                    UpdateMeshStreetSuffix(m_boardsContainers[idx].m_boardsData[boardIdx].m_segmentId1, ref m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfo);
                    m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfoDrawTime = lastFontUpdateFrame;
                }
                return m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfo;
            }
            else
            {
                if (m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfo2 == null || lastFontUpdateFrame > m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfoDrawTime2)
                {
                    doLog($"!nameUpdated Node2 {idx}");
                    UpdateMeshStreetSuffix(m_boardsContainers[idx].m_boardsData[boardIdx].m_segmentId2, ref m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfo2);
                    m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfoDrawTime2 = lastFontUpdateFrame;
                }
                return m_boardsContainers[idx].m_boardsData[boardIdx].m_nameSubInfo2;
            }
        }

        protected override BasicRenderInformation GetMeshFullStreetName(ushort idx, int boardIdx, int secIdx)
        {
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
                    doLog($"!GetMeshFullStreetName Node1 {idx}");
                    UpdateMeshFullNameStreet(m_boardsContainers[idx].m_boardsData[boardIdx].m_segmentId1, ref m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfo);
                    m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfoDrawTime = lastFontUpdateFrame;
                }
                return m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfo;
            }
            else
            {
                if (m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfo2 == null || lastFontUpdateFrame > m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfoDrawTime2)
                {
                    doLog($"!GetMeshFullStreetName Node2 {idx}");
                    UpdateMeshFullNameStreet(m_boardsContainers[idx].m_boardsData[boardIdx].m_segmentId2, ref m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfo2);
                    m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfoDrawTime2 = lastFontUpdateFrame;
                }
                return m_boardsContainers[idx].m_boardsData[boardIdx].m_fullNameSubInfo2;
            }
        }

        protected override BasicRenderInformation GetMeshCustom1(ushort idx, int boardIdx, int secIdx)
        {
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
                doLog($"!districtName {districtId}");
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
        protected override BasicRenderInformation GetMeshCustom2(ushort idx, int boardIdx, int secIdx)
        {
            int distanceRef = (int)Mathf.Floor(m_boardsContainers[idx].m_boardsData[boardIdx].m_distanceRef / 1000);
            if (m_cachedNumber.Length <= distanceRef + 1)
            {
                doLog($"!Length {m_cachedNumber.Length }/{distanceRef}");
                var newArray = new BasicRenderInformation[distanceRef + 1];
                for (int i = 0; i < m_cachedNumber.Length; i++)
                {
                    newArray[i] = m_cachedNumber[i];
                }
                m_cachedNumber = newArray;
            }
            if (m_cachedNumber[distanceRef] == null)
            {
                doLog($"!m_cachedNumber {distanceRef}");
                RefreshNameData(ref m_cachedNumber[distanceRef], distanceRef.ToString());
            }
            return m_cachedNumber[distanceRef];

        }



        private static Color[] randomColors = { Color.black, Color.gray, Color.white, Color.red, new Color32(0xFF, 0x88, 0, 0xFf), Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta };

        protected void UpdateSubparams(ref CacheControlStreetPlate ctrl, ushort nodeId, ref NetNode data, ushort mainSegmentId, ushort otherSegmentId)
        {
            //            if (ctrl == null) ctrl = new CacheControlStreetPlate();
            //            doLog($"!colorUpdated Node {nodeId}");
            //            ctrl.m_cachedColor = randomColors[mainSegmentId % randomColors.Length];//GetDistrictColor(DistrictManager.instance.GetDistrict(data.m_position));
            //            ctrl.m_cachedContrastColor = DTBUtils.contrastColor(ctrl.m_cachedColor);

            //            var main = NetManager.instance.m_segments.m_buffer[mainSegmentId];
            //            var other = NetManager.instance.m_segments.m_buffer[otherSegmentId];
            //            ctrl.m_renderPlate = true;
            //            var ang = VectorUtils.XZ(data.m_position).GetAngleToPoint(VectorUtils.XZ(main.m_middlePosition));
            //            ctrl.m_streetDirection = (main.m_startNode == nodeId ? main.m_cornerAngleStart : main.m_cornerAngleEnd);
            //            var otherDirection = other.m_startNode == nodeId ? other.m_cornerAngleStart : other.m_cornerAngleEnd;
            //            var mediumDirection = (Mathf.Max(otherDirection, ctrl.m_streetDirection) - (otherDirection - ctrl.m_streetDirection) / 2) * 2;
            //            var mediumHalfWidth = (main.Info.m_halfWidth + other.Info.m_halfWidth) / 2;
            //            ctrl.m_platePosition = DTBUtils.DegreeToVector2(mediumDirection) * mediumHalfWidth * 1.4142f;
            //            doLog($@"
            //N{nodeId} S{mainSegmentId} O{otherSegmentId} NXZ{VectorUtils.XZ(data.m_position)} SXZ{VectorUtils.XZ(main.m_middlePosition)}
            //Cang Sse Ose: {main.m_cornerAngleStart}|{main.m_cornerAngleEnd} {other.m_cornerAngleStart}|{other.m_cornerAngleEnd}
            //Dir Sse Ose: {main.m_startDirection}|{main.m_endDirection} {other.m_startDirection}|{other.m_endDirection}
            //Ang NS: {ang}
            //ctrl.m_streetDirection: {ctrl.m_streetDirection}
            //Points N-S: { data.m_position}-{main.m_middlePosition}
            //Directions S-O-M: { ctrl.m_streetDirection}|{otherDirection}|{mediumDirection}");
            //            return;
        }
        #endregion

        public override Color GetColor(ushort buildingID, int idx, int secIdx, BoardDescriptor descriptor)
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
        public override Color GetContrastColor(ushort buildingID, int idx, int secIdx, BoardDescriptor descriptor)
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
            InstanceID result = default(InstanceID);
            result.NetNode = nodeId;
            return result;
        }

        private static Func<ushort, Color> GetDistrictColor = (ushort districtId) => randomColors[districtId % randomColors.Length];
        private static Func<Vector2> GetStartPoint = () =>
        {
            if (m_cachedPos == null)
            {
                GameAreaManager.instance.GetStartTile(out int x, out int y);
                m_cachedPos = new Vector2((x - 2) * 1920, (y - 2) * 1920);
            }
            return m_cachedPos.GetValueOrDefault();
        };

        private static Vector2? m_cachedPos;

        private struct UpdateFlagsSegments
        {
            public bool m_nameMesh;
            public bool m_streetSuffixMesh;
        }


        private BoardDescriptor m_baseDescriptorStreetPlate = new BoardDescriptor
        {
            m_propName = "1679673551.Street Plate_Data",
            m_propRotation = new Vector3(0, 90, 0),
            m_textDescriptors = new BoardTextDescriptor[]{
                new BoardTextDescriptor{
                    m_textRelativePosition =new Vector3(0.53f,2.25f,-0.001f) ,
                    m_textRelativeRotation = Vector3.zero,
                    m_maxWidthMeters = 0.92f,
                    m_textScale = .5f,
                    m_useContrastColor = false,
                    m_defaultColor = Color.white,
                    m_textType = TextType.StreetSuffix,
                    m_textAlign = UIHorizontalAlignment.Left,
                    m_verticalAlign = UIVerticalAlignment.Bottom
                },
                new BoardTextDescriptor{
                    m_textRelativePosition =new Vector3(0.53f,2.25f,-0.001f) ,
                    m_textRelativeRotation = Vector3.zero,
                    m_maxWidthMeters = 0.92f,
                    m_textScale = .2f,
                    m_useContrastColor = false,
                    m_defaultColor = Color.white,
                    m_textType = TextType.StreetNameComplete,
                    m_textAlign = UIHorizontalAlignment.Left,
                    m_verticalAlign = UIVerticalAlignment.Top
                },
                new BoardTextDescriptor{
                    m_textRelativePosition =new Vector3(0.47f,2.05f,-0.001f) ,
                    m_textRelativeRotation = Vector3.zero,
                    m_maxWidthMeters = 0.8f,
                    m_textScale = .2f,
                    m_useContrastColor = true,
                    m_textType = TextType.Custom1,// District
                    m_verticalAlign =  UIVerticalAlignment.Middle
                },
                new BoardTextDescriptor{
                    m_textRelativePosition =new Vector3(0.94f,2.06f,-0.001f) ,
                    m_textRelativeRotation = Vector3.zero,
                    m_maxWidthMeters = 0.1f,
                    m_textScale = .25f,
                    m_useContrastColor = false,
                    m_defaultColor = Color.black,
                    m_textType = TextType.Custom2, //Distance
                    m_verticalAlign = UIVerticalAlignment.Middle
                },
                new BoardTextDescriptor{
                    m_textRelativePosition =new Vector3(0.53f,2.25f,0.001f) ,
                    m_textRelativeRotation = new Vector3(0,180,0),
                    m_maxWidthMeters = 0.92f,
                    m_textScale = .5f,
                    m_useContrastColor = false,
                    m_defaultColor = Color.white,
                    m_textType = TextType.StreetSuffix,
                    m_textAlign = UIHorizontalAlignment.Left,
                    m_verticalAlign = UIVerticalAlignment.Bottom
                },
                new BoardTextDescriptor{
                    m_textRelativePosition =new Vector3(0.53f,2.25f,0.001f) ,
                    m_textRelativeRotation = new Vector3(0,180,0),
                    m_maxWidthMeters = 0.92f,
                    m_textScale = .2f,
                    m_useContrastColor = false,
                    m_defaultColor = Color.white,
                    m_textType = TextType.StreetNameComplete,
                    m_textAlign = UIHorizontalAlignment.Left,
                    m_verticalAlign = UIVerticalAlignment.Top
                },
                new BoardTextDescriptor{
                    m_textRelativePosition =new Vector3(0.47f,2.05f,0.001f) ,
                    m_textRelativeRotation = new Vector3(0,180,0),
                    m_maxWidthMeters = 0.8f,
                    m_textScale = .2f,
                    m_useContrastColor = true,
                    m_textType = TextType.Custom1,// District
                    m_verticalAlign =  UIVerticalAlignment.Middle
                },
                new BoardTextDescriptor{
                    m_textRelativePosition =new Vector3(0.94f,2.06f,0.001f) ,
                    m_textRelativeRotation = new Vector3(0,180,0),
                    m_maxWidthMeters = 0.1f,
                    m_textScale = .25f,
                    m_useContrastColor = false,
                    m_defaultColor = Color.black,
                    m_textType = TextType.Custom2, //Distance
                    m_verticalAlign = UIVerticalAlignment.Middle
                }
            }
        };

    }
    public class CacheControlStreetPlate : CacheControl
    {
        public Vector3 m_platePosition;
        public float m_streetDirection1;
        public float m_streetDirection2;
        public ushort m_segmentId1;
        public ushort m_segmentId2;
        public byte m_districtId1;
        public byte m_districtId2;
        public float m_distanceRef;
        public bool m_renderPlate;
        public Color m_cachedColor = Color.white;
        public Color m_cachedColor2 = Color.white;
        internal uint m_fullNameSubInfoDrawTime2;
        internal uint m_fullNameSubInfoDrawTime;
        internal uint m_nameSubInfoDrawTime2;
        internal uint m_nameSubInfoDrawTime;
        internal Color m_cachedContrastColor;
        internal Color m_cachedContrastColor2;
        internal BasicRenderInformation m_nameSubInfo;
        internal BasicRenderInformation m_nameSubInfo2;
        internal BasicRenderInformation m_fullNameSubInfo;
        internal BasicRenderInformation m_fullNameSubInfo2;
    }
    public class BoardBunchContainerStreetPlate : IBoardBunchContainer<CacheControlStreetPlate, BasicRenderInformation> { }

}
