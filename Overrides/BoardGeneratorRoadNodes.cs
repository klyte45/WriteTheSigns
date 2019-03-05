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
using System.Xml.Serialization;
using UnityEngine;
using static BuildingInfo;

namespace Klyte.DynamicTextBoards.Overrides
{

    public class BoardGeneratorRoadNodes : BoardGeneratorParent<BoardGeneratorRoadNodes, BoardBunchContainerStreetPlate, CacheControlStreetPlate, BasicRenderInformation, BoardDescriptor, BoardTextDescriptor, ushort>
    {

        private UpdateFlagsSegments[] m_updateDataSegments;
        public bool[] m_updatedStreetPositions;

        public override int ObjArraySize => NetManager.MAX_NODE_COUNT;
        public override UIFont DrawFont => Singleton<DistrictManager>.instance.m_properties.m_areaNameFont;

        private BoardDescriptor m_baseDescriptorStreetPlate = new BoardDescriptor
        {
            m_propName = "StreetPlateSP.Street Plate_Data",
            m_propRotation = 90,
            m_textDescriptors = new BoardTextDescriptor[]{
                new BoardTextDescriptor{
                    m_textRelativePosition =new Vector3(0.55f,2.15f,-0.05f) ,
                    m_textRelativeRotation = Vector3.zero,
                    m_maxWidthMeters = 0.9f,
                    m_textScale = .45f,
                    m_useContrastColor = false
                },
                new BoardTextDescriptor{
                    m_textRelativePosition =new Vector3(0.55f,2.15f,0.05f) ,
                    m_textRelativeRotation = new Vector3(0,180,0),
                    m_maxWidthMeters = 0.9f,
                    m_textScale = .45f,
                    m_useContrastColor = false
                },
            },
            m_targetVehicle = VehicleInfo.VehicleType.Train
        };

        #region Initialize
        public override void Initialize()
        {
            m_updateDataSegments = new UpdateFlagsSegments[NetManager.MAX_SEGMENT_COUNT];
            m_updatedStreetPositions = new bool[ObjArraySize];


            NetManagerOverrides.eventNodeChanged += onNodeChanged;
            DistrictManagerOverrides.eventOnDistrictChanged += onDistrictChanged;

            #region Hooks
            //var postRenderMeshs = GetType().GetMethod("AfterRenderNode", allFlags);
            //doLog($"Patching=> {postRenderMeshs} {postRenderMeshs.IsStatic}");
            //AddRedirect(typeof(RoadBaseAI).GetMethod("RenderNode", allFlags), null, postRenderMeshs);
            #endregion
        }


        protected override void OnTextureRebuilt()
        {
            m_updateDataSegments = new UpdateFlagsSegments[NetManager.MAX_SEGMENT_COUNT];
        }

        protected void Reset()
        {
            m_boardsContainers = new BoardBunchContainerStreetPlate[ObjArraySize];
            m_updateDataSegments = new UpdateFlagsSegments[NetManager.MAX_SEGMENT_COUNT];
            m_updatedStreetPositions = new bool[ObjArraySize];
        }

        private void onNodeChanged(ushort nodeId)
        {
            doLog("onNodeChanged");
            m_updatedStreetPositions[nodeId] = false;
        }
        private void onSegmentChanged(ushort segmentId)
        {
            doLog("onSegmentChanged");
            m_updateDataSegments[segmentId] = new UpdateFlagsSegments();
        }
        private void onDistrictChanged()
        {
            doLog("onDistrictChanged");
            m_updatedStreetPositions = new bool[BuildingManager.MAX_BUILDING_COUNT];
        }
        #endregion

        public static void AfterRenderNode(RenderManager.CameraInfo cameraInfo, ushort nodeID, ref NetNode nodeData)
        {

            instance.AfterRenderInstanceImpl(cameraInfo, nodeID, ref nodeData);

        }
        public void AfterRenderInstanceImpl(RenderManager.CameraInfo cameraInfo, ushort nodeID, ref NetNode data)
        {
            if (data.CountSegments() <= 2)
            {
                return;
            }
            if (m_boardsContainers[nodeID] == null)
            {
                m_boardsContainers[nodeID] = new BoardBunchContainerStreetPlate();
            }
            //m_streetPlatePrefab
            if (m_boardsContainers[nodeID]?.m_boardsData?.Count() != data.CountSegments() * 2)
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
                //for (int i = 0; i < 8; i++)
                //{
                //    currentId = data.GetSegment(i);
                //    if (currentId == 0) continue;
                //    if (NetManager.instance.m_segments.m_buffer[currentId].Info.m_netAI is RoadBaseAI)
                //    {
                //        validSegments.Add(Tuple.New(currentId, VectorUtils.XZ(data.m_position).GetAngleToPoint(VectorUtils.XZ(NetManager.instance.m_segments.m_buffer[currentId].m_middlePosition))));
                //    }
                //}
                var controlBoardIdx = 0;
                for (int i = 0; i < 8; i++)
                {
                    ushort segmentIid = data.GetSegment(i);
                    if (segmentIid != 0)
                    {
                        NetSegment netSegmentI = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segmentIid];
                        if (netSegmentI.Info != null && netSegmentI.Info.m_netAI is RoadBaseAI roadAiI && !roadAiI.m_highwayRules)
                        {
                            Vector3 startPos = Vector3.zero;
                            Vector3 startAng = Vector3.zero;
                            Vector3 endPos = Vector3.zero;
                            Vector3 endAng = Vector3.zero;
                            Vector3 segmentIDirection = (nodeID != netSegmentI.m_startNode) ? netSegmentI.m_endDirection : netSegmentI.m_startDirection;
                            Vector3 otherSegmentDirection = (nodeID == netSegmentI.m_startNode) ? netSegmentI.m_endDirection : netSegmentI.m_startDirection;
                            float resultAngle = -4f;
                            ushort resultOtherSegment = 0;
                            for (int j = 0; j < 8; j++)
                            {
                                ushort segmentJid = data.GetSegment(j);
                                if (segmentJid != 0 && segmentJid != segmentIid)
                                {
                                    NetSegment netSegmentJ = Singleton<NetManager>.instance.m_segments.m_buffer[(int)segmentJid];
                                    if (netSegmentJ.Info != null && netSegmentJ.Info.m_netAI is RoadBaseAI roadAiJ && !roadAiJ.m_highwayRules)
                                    {
                                        Vector3 segmentJDirection = (nodeID != netSegmentJ.m_startNode) ? netSegmentJ.m_endDirection : netSegmentJ.m_startDirection;
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
                            bool start = netSegmentI.m_startNode == nodeID;
                            netSegmentI.CalculateCorner(segmentIid, true, start, false, out startPos, out startAng, out bool flag);
                            if (resultOtherSegment != 0)
                            {
                                NetSegment netSegment3 = Singleton<NetManager>.instance.m_segments.m_buffer[(int)resultOtherSegment];
                                start = (netSegment3.m_startNode == nodeID);
                                netSegment3.CalculateCorner(resultOtherSegment, true, start, true, out endPos, out endAng, out flag);
                            }
                            NetSegment.CalculateMiddlePoints(startPos, -startAng, endPos, -endAng, true, true, out Vector3 rhs, out Vector3 lhs);
                            if (m_boardsContainers[nodeID].m_boardsData[controlBoardIdx] == null) m_boardsContainers[nodeID].m_boardsData[controlBoardIdx] = new CacheControlStreetPlate();
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_streetDirection1 = -Vector2.zero.GetAngleToPoint(VectorUtils.XZ(segmentIDirection));
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_streetDirection2 = -Vector2.zero.GetAngleToPoint(VectorUtils.XZ(otherSegmentDirection));
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_segmentId1 = segmentIid;
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_segmentId2 = resultOtherSegment;
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_platePosition = (rhs + lhs) / 2;
                            m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_renderPlate = true;
                            doLog($@"
endAng = {endAng}
startAng = {startAng} 
 m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_streetDirection1 = { m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_streetDirection1 }
 m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_streetDirection2  = { m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_streetDirection2 }
 m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_platePosition = { m_boardsContainers[nodeID].m_boardsData[controlBoardIdx].m_platePosition}
 node pos = { data.m_position }
");
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

                    RenderPropMesh(ref m_boardsContainers[nodeID].m_boardsData[boardIdx].m_cachedProp, cameraInfo, nodeID, boardIdx, 0, 0xFFFFFFF, 0, m_boardsContainers[nodeID].m_boardsData[boardIdx].m_platePosition, Vector4.zero, ref descriptor.m_propName, m_boardsContainers[nodeID].m_boardsData[boardIdx].m_streetDirection2 + descriptor.m_propRotation, out Matrix4x4 propMatrix, out bool rendered);
                    if (rendered)
                    {
                        for (int j = 0; j < descriptor.m_textDescriptors.Length; j++)
                        {
                            RenderTextMesh(cameraInfo, nodeID, boardIdx, 0, ref descriptor, propMatrix, ref descriptor.m_textDescriptors[j], ref m_boardsContainers[nodeID].m_boardsData[boardIdx]);
                        }
                    }
                    RenderPropMesh(ref m_boardsContainers[nodeID].m_boardsData[boardIdx].m_cachedProp, cameraInfo, nodeID, boardIdx, 1, 0xFFFFFFF, 0, m_boardsContainers[nodeID].m_boardsData[boardIdx].m_platePosition, Vector4.zero, ref descriptor.m_propName, m_boardsContainers[nodeID].m_boardsData[boardIdx].m_streetDirection2 + descriptor.m_propRotation, out propMatrix, out rendered);
                    if (rendered)
                    {

                        for (int j = 0; j < descriptor.m_textDescriptors.Length; j++)
                        {
                            RenderTextMesh(cameraInfo, nodeID, boardIdx, 1, ref descriptor, propMatrix, ref descriptor.m_textDescriptors[j], ref m_boardsContainers[nodeID].m_boardsData[boardIdx]);
                        }
                    }

                }
            }

        }



        #region Upadate Data
        protected override BasicRenderInformation GetOwnNameMesh(ushort idx, int boardIdx, int secIdx)
        {
            if (!m_updateDataSegments[idx].m_nameMesh)
            {
                m_boardsContainers[idx].m_nameSubInfo = null;
                m_boardsContainers[idx].m_nameSubInfo2 = null;
                m_updateDataSegments[idx].m_nameMesh = true;
            }
            if (secIdx == 0)
            {
                if (m_boardsContainers[idx].m_nameSubInfo == null)
                {
                    doLog($"!nameUpdated Node1 {idx}");
                    RefreshNameData(ref m_boardsContainers[idx].m_nameSubInfo, "DUMMY!!");
                }
                return m_boardsContainers[idx].m_nameSubInfo;
            }
            else
            {
                if (m_boardsContainers[idx].m_nameSubInfo2 == null)
                {
                    doLog($"!nameUpdated Node2 {idx}");
                    RefreshNameData(ref m_boardsContainers[idx].m_nameSubInfo, "DUMMY!!!!!");
                }
                return m_boardsContainers[idx].m_nameSubInfo;
            }

        }
        private Color[] randomColors = { Color.black, Color.gray, Color.white, Color.red, new Color32(0xFF, 0x88, 0, 0xFf), Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta };

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

        public override Color GetColor(ushort buildingID, int idx, int secIdx)
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


        protected override InstanceID GetPropRenderID(ushort nodeId)
        {
            InstanceID result = default(InstanceID);
            result.NetNode = nodeId;
            return result;
        }

        private static Func<ushort, Color> GetDistrictColor = (ushort districtId) => Color.gray;


        private struct UpdateFlagsSegments
        {
            public bool m_nameMesh;
            public bool m_streetPrefixMesh;
            public bool m_streetSuffixMesh;
            public bool m_streetNumberMesh;
        }

    }
    public class CacheControlStreetPlate : CacheControl
    {
        public Vector3 m_platePosition;
        public float m_streetDirection1;
        public float m_streetDirection2;
        public ushort m_segmentId1;
        public ushort m_segmentId2;
        public bool m_renderPlate;
        public Color m_cachedColor2 = Color.white;
    }
    public class BoardBunchContainerStreetPlate : IBoardBunchContainer<CacheControlStreetPlate, BasicRenderInformation>
    {
        internal BasicRenderInformation m_nameSubInfo2;
    }

}
