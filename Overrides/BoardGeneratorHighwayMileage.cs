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
using static Klyte.Commons.Utils.KlyteUtils;

namespace Klyte.DynamicTextBoards.Overrides
{

    public class BoardGeneratorHighwayMileage : BoardGeneratorParent<BoardGeneratorHighwayMileage, IBoardBunchContainer<CacheControl, BasicRenderInformation>, CacheControl, BasicRenderInformation, BoardDescriptor, BoardTextDescriptor, BoardGeneratorHighwayMileage.RoadIdentifier>
    {

        public Dictionary<RoadIdentifier, List<MileagePlateDescriptor>> m_highwayMarksObjects;
        public RoadIdentifier[] m_segmentToHighway;
        public BasicRenderInformation[] m_cachedKilometerMeshes;
        public BasicRenderInformation[] m_cachedDirectionMeshes;
        public List<RoadIdentifier> m_destroyQueue = new List<RoadIdentifier>();

        private UIDynamicFont m_font;


        public override int ObjArraySize => 0;
        public override UIDynamicFont DrawFont => m_font;

        private BoardDescriptor m_baseDescriptorMileagePlate = new BoardDescriptor
        {
            m_propName = "1679681061.Mileage Marker_Data",
            m_textDescriptors = new BoardTextDescriptor[]{
                new BoardTextDescriptor{
                    m_textRelativePosition =new Vector3(0f,1.78f,0.066f) ,
                    m_maxWidthMeters = 0.45f,
                    m_textScale = .375f,
                    m_useContrastColor = false,
                    m_textRelativeRotation = new Vector3(0,180,0)
                },
                new BoardTextDescriptor{
                    m_textRelativePosition =new Vector3(0f,1.33f,0.066f) ,
                    m_textRelativeRotation = new Vector3(0,180,0),
                    m_maxWidthMeters = 0.45f,
                    m_textScale = .5f,
                    m_useContrastColor = false,
                    m_textType = TextType.BuildingNumber
                },
            }
        };
        private PropInfo cachedDefaultInfo;

        #region Initialize
        public override void Initialize()
        {
            m_highwayMarksObjects = new Dictionary<RoadIdentifier, List<MileagePlateDescriptor>>();
            m_segmentToHighway = new RoadIdentifier[NetManager.MAX_SEGMENT_COUNT];
            m_cachedKilometerMeshes = new BasicRenderInformation[100];
            m_cachedDirectionMeshes = new BasicRenderInformation[9];

            NetManagerOverrides.eventNodeChanged += onNodeChanged;
            NetManagerOverrides.eventSegmentChanged += onSegmentChanged;
            NetManagerOverrides.eventSegmentNameChanged += onNameSeedChanged;
            DistrictManagerOverrides.eventOnDistrictChanged += onDistrictChanged;

            BuildSurfaceFont(out m_font, "Highway Gothic");

            #region Hooks
            var postRenderMeshs = GetType().GetMethod("AfterRenderNode", allFlags);
            var afterRenderSegment = GetType().GetMethod("AfterRenderSegment", allFlags);
            doLog($"Patching=> {postRenderMeshs} {postRenderMeshs.IsStatic}");
            AddRedirect(typeof(RoadBaseAI).GetMethod("RenderNode", allFlags), null, postRenderMeshs);
            AddRedirect(typeof(NetSegment).GetMethod("RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int) }), null, afterRenderSegment);
            #endregion
        }



        private void Font_textureRebuilt(Font obj)
        {
        }

        protected override void OnTextureRebuilt()
        {
        }

        private void onNodeChanged(ushort nodeId)
        {
            //doLog($"onNodeChanged { System.Environment.StackTrace }");
            for (int i = 0; i < 8; i++)
            {
                var segmentId = NetManager.instance.m_nodes.m_buffer[nodeId].GetSegment(i);
                //if (NetManager.instance.m_segments.m_buffer[segmentId].Info.m_netAI is RoadBaseAI)
                //{
                //    onDistrictChanged();
                //    return;
                //}
                if (segmentId > 0)
                {
                    onSegmentChanged(segmentId);
                }
            }
        }
        private void onNameSeedChanged(ushort segmentId)
        {
            //doLog("onNameSeedChanged");
            onSegmentChanged(segmentId);
        }
        private void onSegmentChanged(ushort segmentId)
        {
            //doLog("onSegmentChanged");
            //if (NetManager.instance.m_segments.m_buffer[segmentId].Info.m_netAI is RoadBaseAI)
            //{
            //    onDistrictChanged();
            //}

            if ((m_segmentToHighway[segmentId]) != default(RoadIdentifier))
            {
                var target = m_segmentToHighway[segmentId];
                if (!m_destroyQueue.Contains(target)) m_destroyQueue.Add(target);
            }
        }
        private void onDistrictChanged()
        {
            //doLog("onDistrictChanged");
            m_segmentToHighway = new RoadIdentifier[NetManager.MAX_SEGMENT_COUNT];
            m_highwayMarksObjects = new Dictionary<RoadIdentifier, List<MileagePlateDescriptor>>();
        }
        #endregion

        public static void AfterRenderNode(RenderManager.CameraInfo cameraInfo, ushort nodeID, ref NetNode nodeData)
        {

            instance.AfterRenderInstanceImpl(cameraInfo, nodeID, ref nodeData);

        }
        public static void AfterRenderSegment(RenderManager.CameraInfo cameraInfo, ushort segmentID, int layerMask)
        {

            instance.AfterRenderSegmentImpl(cameraInfo, segmentID, layerMask);

        }
        public void AfterRenderInstanceImpl(RenderManager.CameraInfo cameraInfo, ushort nodeID, ref NetNode data)
        {


            for (int i = 0; i < 8; i++)
            {
                var segmentId = NetManager.instance.m_nodes.m_buffer[nodeID].GetSegment(i);
                if (segmentId == 0) continue;
                if (!(NetManager.instance.m_segments.m_buffer[segmentId].Info.m_netAI is RoadBaseAI baseAI) || !baseAI.m_highwayRules)
                {
                    continue;
                }
                if (m_segmentToHighway[segmentId] == default(RoadIdentifier) || m_destroyQueue.Contains(m_segmentToHighway[segmentId]))
                {
                    var removeTarget = m_segmentToHighway[segmentId];
                    if (removeTarget.segments != null)
                    {
                        foreach (var id in removeTarget.segments)
                        {
                            if (m_segmentToHighway[id] == removeTarget) m_segmentToHighway[id] = default(RoadIdentifier);
                        }
                    }
                    m_highwayMarksObjects.Remove(removeTarget);
                    m_destroyQueue.Remove(removeTarget);

                    var segments = DTBUtils.GetSegmentRoadEdges(segmentId, false, false, false, out ComparableRoad start, out ComparableRoad end);
                    if (segments == null)
                    {
                        RoadIdentifier tuple = new RoadIdentifier(default(ComparableRoad), default(ComparableRoad), new ushort[] { segmentId });
                        m_segmentToHighway[segmentId] = tuple;
                    }
                    else
                    {
                        var tupleIdentifier = new RoadIdentifier(start, end, segments.Select(x => x.First).ToArray());
                        var roadInverted = (tupleIdentifier.start.nodeReference == NetManager.instance.m_segments.m_buffer[tupleIdentifier.segments[0]].m_endNode) == ((NetManager.instance.m_segments.m_buffer[tupleIdentifier.segments[0]].m_flags & NetSegment.Flags.Invert) == 0);
                        var entry = new List<MileagePlateDescriptor>();
                        var meterCount = 0f;
                        foreach (var segmentRef in segments)
                        {
                            m_segmentToHighway[segmentRef.First] = tupleIdentifier;
                            int oldMeterKm = (int)meterCount / 1000;
                            meterCount += segmentRef.Second;
                            if (oldMeterKm != (int)meterCount / 1000)
                            {
                                var segmentObj = NetManager.instance.m_segments.m_buffer[segmentRef.First];
                                bool invert = (NetManager.instance.m_segments.m_buffer[segmentRef.First].m_flags & NetSegment.Flags.Invert) > 0;
                                NetManager.instance.m_segments.m_buffer[segmentRef.First].GetClosestPositionAndDirection(NetManager.instance.m_segments.m_buffer[segmentRef.First].m_middlePosition, out Vector3 pos, out Vector3 dir);
                                var rotation = dir.GetAngleXZ();
                                if (invert) rotation += 180;
                                var cardinalDirection = KlyteUtils.GetCardinalDirection(start, end);
                                if (roadInverted)
                                {
                                    cardinalDirection = (byte)((cardinalDirection + 4) % 8);
                                }
                                entry.Add(new MileagePlateDescriptor
                                {
                                    segmentId = segmentRef.First,
                                    kilometer = oldMeterKm + 1,
                                    position = segmentObj.m_middlePosition + VectorUtils.X_Y(DTBUtils.DegreeToVector2(rotation - 90)) * (segmentObj.Info.m_halfWidth - 1),
                                    cardinalDirection8 = cardinalDirection,
                                    rotation = rotation + 90

                                });
                                if (segmentObj.Info.m_backwardVehicleLaneCount + segmentObj.Info.m_forwardVehicleLaneCount >= 2)
                                {
                                    entry.Add(new MileagePlateDescriptor
                                    {
                                        segmentId = segmentRef.First,
                                        kilometer = oldMeterKm + 1,
                                        position = segmentObj.m_middlePosition + VectorUtils.X_Y(DTBUtils.DegreeToVector2(rotation + 90)) * (segmentObj.Info.m_halfWidth - 1),
                                        cardinalDirection8 = (byte)((cardinalDirection + (segmentObj.Info.m_hasBackwardVehicleLanes && segmentObj.Info.m_hasForwardVehicleLanes ? 4 : 0)) % 8),
                                        rotation = segmentObj.Info.m_hasBackwardVehicleLanes && segmentObj.Info.m_hasForwardVehicleLanes ? rotation - 90 : rotation + 90
                                    });
                                }

                            }
                        }
                        if (entry.Count > 0)
                        {
                            m_destroyQueue.AddRange(m_highwayMarksObjects.Where(x => x.Value.Intersect(entry).Count() > 0).Select(x => x.Key));
                            m_highwayMarksObjects[tupleIdentifier] = (entry);
                        }
                    }
                }
            }

        }
        public void AfterRenderSegmentImpl(RenderManager.CameraInfo cameraInfo, ushort segmentID, int layerMask)
        {
            var renderQueue = m_highwayMarksObjects.SelectMany(x => x.Value.Where(y => y.segmentId == segmentID).Select((y, j) => Tuple.New(x.Key, j, y)));

            foreach (var plate in renderQueue)
            {
                RenderPropMesh(ref cachedDefaultInfo, cameraInfo, segmentID, plate.Second, plate.Third.kilometer, layerMask, 0, plate.Third.position, Vector4.zero, ref m_baseDescriptorMileagePlate.m_propName, new Vector3(0, plate.Third.rotation) + m_baseDescriptorMileagePlate.m_propRotation, m_baseDescriptorMileagePlate.PropScale, ref m_baseDescriptorMileagePlate, out Matrix4x4 propMatrix, out bool rendered);
                if (rendered)
                {
                    for (int j = 0; j < m_baseDescriptorMileagePlate.m_textDescriptors.Length; j++)
                    {
                        CacheControl c = null;
                        var block = NetManager.instance.m_materialBlock;
                        block.Clear();
                        RenderTextMesh(cameraInfo, plate.First, plate.Second, plate.Third.kilometer, ref m_baseDescriptorMileagePlate, propMatrix, ref m_baseDescriptorMileagePlate.m_textDescriptors[j], ref c, block);
                    }
                }

            }

        }



        #region Upadate Data
        protected override BasicRenderInformation GetOwnNameMesh(RoadIdentifier id, int boardIdx, int kilometers)
        {
            //doLog($"GetOwnNameMesh segmentId {id} (boardIdx {boardIdx}|kilometers {kilometers})");
            var direction = m_highwayMarksObjects[id][boardIdx].cardinalDirection8;
            if (m_cachedDirectionMeshes[direction] == null || lastFontUpdateFrame > m_cachedDirectionMeshes[direction].m_frameDrawTime)
            {
                doLog($"!nameUpdated Node1 {kilometers} ({direction})");
                RefreshNameData(ref m_cachedDirectionMeshes[direction], Locale.Get("KCM_CARDINAL_POINT_LONG", direction.ToString()).ToUpper());

            }
            return m_cachedDirectionMeshes[direction];

        }
        protected override BasicRenderInformation GetMeshCurrentNumber(RoadIdentifier id, int boardIdx, int kilometers)
        {
            if (m_cachedKilometerMeshes.Length <= kilometers + 1)
            {
                m_cachedKilometerMeshes = new BasicRenderInformation[kilometers + 1];
            }
            if (m_cachedKilometerMeshes[kilometers] == null || lastFontUpdateFrame > m_cachedKilometerMeshes[kilometers].m_frameDrawTime)
            {
                doLog($"!nameUpdated Node1 {kilometers}");
                RefreshNameData(ref m_cachedKilometerMeshes[kilometers], $"km\n{kilometers}");
            }
            return m_cachedKilometerMeshes[kilometers];

        }
        #endregion

        public override Color GetColor(ushort buildingID, int idx, int secIdx, BoardDescriptor descriptor)
        {
            return Color.white;

        }

        protected override InstanceID GetPropRenderID(ushort nodeId)
        {
            InstanceID result = default(InstanceID);
            result.NetNode = nodeId;
            return result;
        }

        public override Color GetContrastColor(RoadIdentifier refID, int boardIdx, int secIdx, BoardDescriptor descriptor)
        {
            return Color.black;
        }

        private static Func<ushort, Color> GetDistrictColor = (ushort districtId) => Color.gray;


        public struct MileagePlateDescriptor
        {
            public ushort segmentId;
            public int kilometer;
            public Vector3 position;
            public float rotation;
            public byte cardinalDirection8;
        }

        public struct RoadIdentifier
        {
            public RoadIdentifier(ComparableRoad start, ComparableRoad end, ushort[] segments)
            {
                this.start = start;
                this.end = end;
                this.segments = segments;
            }

            public ComparableRoad start;
            public ComparableRoad end;
            public ushort[] segments;

            public static bool operator ==(RoadIdentifier id, RoadIdentifier other)
            {
                return (other.start.ToString() == id.start.ToString() && other.end.ToString() == id.end.ToString()) || (other.end.ToString() == id.start.ToString() && other.start.ToString() == id.end.ToString());
            }
            public static bool operator !=(RoadIdentifier id, RoadIdentifier other)
            {
                return !(id == other);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is RoadIdentifier))
                {
                    return false;
                }

                var identifier = (RoadIdentifier)obj;
                return EqualityComparer<ComparableRoad>.Default.Equals(start, identifier.start) &&
                       EqualityComparer<ComparableRoad>.Default.Equals(end, identifier.end);
            }

            public override int GetHashCode()
            {
                var hashCode = 1075529825;
                hashCode = hashCode * -1521134295 + base.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<ComparableRoad>.Default.GetHashCode(start);
                hashCode = hashCode * -1521134295 + EqualityComparer<ComparableRoad>.Default.GetHashCode(end);
                return hashCode;
            }
        }

    }
}
