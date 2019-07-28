using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.Utils.SegmentUtils;

namespace Klyte.DynamicTextProps.Overrides
{

    public partial class BoardGeneratorHighwayMileage : BoardGeneratorParent<BoardGeneratorHighwayMileage, IBoardBunchContainer<CacheControl, BasicRenderInformation>, CacheControl, BasicRenderInformation, BoardDescriptorXml, BoardTextDescriptorXml, BoardGeneratorHighwayMileage.RoadIdentifier>
    {

        public Dictionary<RoadIdentifier, List<MileagePlateDescriptor>> m_highwayMarksObjects;
        public RoadIdentifier[] m_segmentToHighway;
        public BasicRenderInformation[] m_cachedKilometerMeshes;
        public BasicRenderInformation[] m_cachedDirectionMeshes;
        public List<RoadIdentifier> m_destroyQueue = new List<RoadIdentifier>();

        private UIDynamicFont m_font;


        public override int ObjArraySize => 0;
        public override UIDynamicFont DrawFont => m_font;


        private BoardDescriptorXml m_baseDescriptorMileagePlate = new BoardDescriptorXml
        {
            m_propName = "1679681061.Mileage Marker_Data",
            m_textDescriptors = new BoardTextDescriptorXml[]{
                new BoardTextDescriptorXml{
                    m_textRelativePosition =new Vector3(0f,1.78f,0.066f) ,
                    m_maxWidthMeters = 0.45f,
                    m_textScale = .375f,
                    m_useContrastColor = false,
                    m_textRelativeRotation = new Vector3(0,180,0),
                    m_textType = TextType.Custom1
                },
                new BoardTextDescriptorXml{
                    m_textRelativePosition =new Vector3(0f,1.33f,0.066f) ,
                    m_textRelativeRotation = new Vector3(0,180,0),
                    m_maxWidthMeters = 0.45f,
                    m_textScale = .5f,
                    m_useContrastColor = false,
                    m_textType = TextType.BuildingNumber
                },
            }
        };
        private PropInfo m_cachedDefaultInfo;

        #region Initialize
        public override void Initialize()
        {
            m_highwayMarksObjects = new Dictionary<RoadIdentifier, List<MileagePlateDescriptor>>();
            m_segmentToHighway = new RoadIdentifier[NetManager.MAX_SEGMENT_COUNT];
            m_cachedKilometerMeshes = new BasicRenderInformation[100];
            m_cachedDirectionMeshes = new BasicRenderInformation[9];

            NetManagerOverrides.EventNodeChanged += OnNodeChanged;
            NetManagerOverrides.EventSegmentChanged += OnSegmentChanged;
            NetManagerOverrides.EventSegmentNameChanged += OnNameSeedChanged;
            DistrictManagerOverrides.EventOnDistrictChanged += OnDistrictChanged;

            BuildSurfaceFont(out m_font, "Highway Gothic");

            #region Hooks
            System.Reflection.MethodInfo postRenderMeshs = GetType().GetMethod("AfterRenderNode", RedirectorUtils.allFlags);
            System.Reflection.MethodInfo afterRenderSegment = GetType().GetMethod("AfterRenderSegment", RedirectorUtils.allFlags);
            LogUtils.DoLog($"Patching=> {postRenderMeshs} {postRenderMeshs.IsStatic}");
            RedirectorInstance.AddRedirect(typeof(RoadBaseAI).GetMethod("RenderNode", RedirectorUtils.allFlags), null, postRenderMeshs);
            RedirectorInstance.AddRedirect(typeof(NetSegment).GetMethod("RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int) }), null, afterRenderSegment);
            #endregion
        }



        protected override void OnTextureRebuiltImpl(Font obj)
        {
        }

        private void OnNodeChanged(ushort nodeId)
        {
            //doLog($"onNodeChanged { System.Environment.StackTrace }");
            for (int i = 0; i < 8; i++)
            {
                ushort segmentId = NetManager.instance.m_nodes.m_buffer[nodeId].GetSegment(i);
                //if (NetManager.instance.m_segments.m_buffer[segmentId].Info.m_netAI is RoadBaseAI)
                //{
                //    onDistrictChanged();
                //    return;
                //}
                if (segmentId > 0)
                {
                    OnSegmentChanged(segmentId);
                }
            }
        }
        private void OnNameSeedChanged(ushort segmentId)
        {
            //doLog("onNameSeedChanged");
            OnSegmentChanged(segmentId);
        }
        private void OnSegmentChanged(ushort segmentId)
        {
            //doLog("onSegmentChanged");
            //if (NetManager.instance.m_segments.m_buffer[segmentId].Info.m_netAI is RoadBaseAI)
            //{
            //    onDistrictChanged();
            //}

            if (m_segmentToHighway[segmentId] != default)
            {
                RoadIdentifier target = m_segmentToHighway[segmentId];
                if (!m_destroyQueue.Contains(target))
                {
                    m_destroyQueue.Add(target);
                }
            }
        }
        private void OnDistrictChanged()
        {
            //doLog("onDistrictChanged");
            m_segmentToHighway = new RoadIdentifier[NetManager.MAX_SEGMENT_COUNT];
            m_highwayMarksObjects = new Dictionary<RoadIdentifier, List<MileagePlateDescriptor>>();
        }
        #endregion

        public static void AfterRenderNode(RenderManager.CameraInfo cameraInfo, ushort nodeID)
        {

            Instance.AfterRenderInstanceImpl(cameraInfo, nodeID);

        }
        public static void AfterRenderSegment(RenderManager.CameraInfo cameraInfo, ushort segmentID, int layerMask)
        {

            Instance.AfterRenderSegmentImpl(cameraInfo, segmentID, layerMask);

        }
        public void AfterRenderInstanceImpl(RenderManager.CameraInfo cameraInfo, ushort nodeID)
        {
            if (cameraInfo == null)
            {
                throw new ArgumentNullException(nameof(cameraInfo));
            }

            for (int i = 0; i < 8; i++)
            {
                ushort segmentId = NetManager.instance.m_nodes.m_buffer[nodeID].GetSegment(i);
                if (segmentId == 0)
                {
                    continue;
                }

                if (!(NetManager.instance.m_segments.m_buffer[segmentId].Info.m_netAI is RoadBaseAI baseAI) || !baseAI.m_highwayRules)
                {
                    continue;
                }
                if (m_segmentToHighway[segmentId] == default || m_destroyQueue.Contains(m_segmentToHighway[segmentId]))
                {
                    RoadIdentifier removeTarget = m_segmentToHighway[segmentId];
                    if (removeTarget.segments != null)
                    {
                        foreach (ushort id in removeTarget.segments)
                        {
                            if (m_segmentToHighway[id] == removeTarget)
                            {
                                m_segmentToHighway[id] = default;
                            }
                        }
                    }
                    m_highwayMarksObjects.Remove(removeTarget);
                    m_destroyQueue.Remove(removeTarget);

                    IEnumerable<Tuple<ushort, float>> segments = SegmentUtils.GetSegmentRoadEdges(segmentId, false, false, false, out ComparableRoad start, out ComparableRoad end);
                    if (segments == null)
                    {
                        RoadIdentifier tuple = new RoadIdentifier(default, default, new ushort[] { segmentId });
                        m_segmentToHighway[segmentId] = tuple;
                    }
                    else
                    {
                        RoadIdentifier tupleIdentifier = new RoadIdentifier(start, end, segments.Select(x => x.First).ToArray());
                        bool roadInverted = tupleIdentifier.start.nodeReference == NetManager.instance.m_segments.m_buffer[tupleIdentifier.segments[0]].m_endNode == ((NetManager.instance.m_segments.m_buffer[tupleIdentifier.segments[0]].m_flags & NetSegment.Flags.Invert) == 0);
                        List<MileagePlateDescriptor> entry = new List<MileagePlateDescriptor>();
                        float meterCount = 0f;
                        foreach (Tuple<ushort, float> segmentRef in segments)
                        {
                            m_segmentToHighway[segmentRef.First] = tupleIdentifier;
                            int oldMeterKm = (int) meterCount / 1000;
                            meterCount += segmentRef.Second;
                            if (oldMeterKm != (int) meterCount / 1000)
                            {
                                NetSegment segmentObj = NetManager.instance.m_segments.m_buffer[segmentRef.First];
                                bool invert = (NetManager.instance.m_segments.m_buffer[segmentRef.First].m_flags & NetSegment.Flags.Invert) > 0;
                                NetManager.instance.m_segments.m_buffer[segmentRef.First].GetClosestPositionAndDirection(NetManager.instance.m_segments.m_buffer[segmentRef.First].m_middlePosition, out Vector3 pos, out Vector3 dir);
                                float rotation = dir.GetAngleXZ();
                                if (invert)
                                {
                                    rotation += 180;
                                }

                                byte cardinalDirection = SegmentUtils.GetCardinalDirection(start, end);
                                if (roadInverted)
                                {
                                    cardinalDirection = (byte) ((cardinalDirection + 4) % 8);
                                }
                                entry.Add(new MileagePlateDescriptor
                                {
                                    segmentId = segmentRef.First,
                                    kilometer = oldMeterKm + 1,
                                    position = segmentObj.m_middlePosition + (VectorUtils.X_Y(KlyteMathUtils.DegreeToVector2(rotation - 90)) * (segmentObj.Info.m_halfWidth - 1)),
                                    cardinalDirection8 = cardinalDirection,
                                    rotation = rotation + 90

                                });
                                if (segmentObj.Info.m_backwardVehicleLaneCount + segmentObj.Info.m_forwardVehicleLaneCount >= 2)
                                {
                                    entry.Add(new MileagePlateDescriptor
                                    {
                                        segmentId = segmentRef.First,
                                        kilometer = oldMeterKm + 1,
                                        position = segmentObj.m_middlePosition + (VectorUtils.X_Y(KlyteMathUtils.DegreeToVector2(rotation + 90)) * (segmentObj.Info.m_halfWidth - 1)),
                                        cardinalDirection8 = (byte) ((cardinalDirection + (segmentObj.Info.m_hasBackwardVehicleLanes && segmentObj.Info.m_hasForwardVehicleLanes ? 4 : 0)) % 8),
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
            IEnumerable<Tuple<RoadIdentifier, int, MileagePlateDescriptor>> renderQueue = m_highwayMarksObjects.SelectMany(x => x.Value.Where(y => y.segmentId == segmentID).Select((y, j) => Tuple.New(x.Key, j, y)));

            foreach (Tuple<RoadIdentifier, int, MileagePlateDescriptor> plate in renderQueue)
            {
                RenderPropMesh(ref m_cachedDefaultInfo, cameraInfo, segmentID, plate.Second, plate.Third.kilometer, layerMask, 0, plate.Third.position, Vector4.zero, ref m_baseDescriptorMileagePlate.m_propName, new Vector3(0, plate.Third.rotation) + m_baseDescriptorMileagePlate.m_propRotation, m_baseDescriptorMileagePlate.PropScale, ref m_baseDescriptorMileagePlate, out Matrix4x4 propMatrix, out bool rendered);
                if (rendered)
                {
                    for (int j = 0; j < m_baseDescriptorMileagePlate.m_textDescriptors.Length; j++)
                    {
                        CacheControl c = null;
                        MaterialPropertyBlock block = NetManager.instance.m_materialBlock;
                        block.Clear();
                        RenderTextMesh(cameraInfo, plate.First, plate.Second, plate.Third.kilometer, ref m_baseDescriptorMileagePlate, propMatrix, ref m_baseDescriptorMileagePlate.m_textDescriptors[j], ref c, block);
                    }
                }

            }

        }



        #region Upadate Data
        protected override BasicRenderInformation GetMeshCustom1(RoadIdentifier id, int boardIdx, int kilometers, out UIFont font)
        {
            font = DrawFont;
            //doLog($"GetOwnNameMesh segmentId {id} (boardIdx {boardIdx}|kilometers {kilometers})");
            byte direction = m_highwayMarksObjects[id][boardIdx].cardinalDirection8;
            if (m_cachedDirectionMeshes[direction] == null || lastFontUpdateFrame > m_cachedDirectionMeshes[direction].m_frameDrawTime)
            {
                LogUtils.DoLog($"!nameUpdated Node1 {kilometers} ({direction})");
                RefreshNameData(ref m_cachedDirectionMeshes[direction], Locale.Get("K45_CARDINAL_POINT_LONG", direction.ToString()).ToUpper());

            }
            return m_cachedDirectionMeshes[direction];

        }
        protected override BasicRenderInformation GetMeshCurrentNumber(RoadIdentifier id, int boardIdx, int kilometers, out UIFont font)
        {
            font = DrawFont;
            if (m_cachedKilometerMeshes.Length <= kilometers + 1)
            {
                m_cachedKilometerMeshes = new BasicRenderInformation[kilometers + 1];
            }
            if (m_cachedKilometerMeshes[kilometers] == null || lastFontUpdateFrame > m_cachedKilometerMeshes[kilometers].m_frameDrawTime)
            {
                LogUtils.DoLog($"!nameUpdated Node1 {kilometers}");
                RefreshNameData(ref m_cachedKilometerMeshes[kilometers], $"km\n{kilometers}");
            }
            return m_cachedKilometerMeshes[kilometers];

        }
        #endregion

        public override Color GetColor(ushort buildingID, int idx, int secIdx, BoardDescriptorXml descriptor) => Color.white;

        protected override InstanceID GetPropRenderID(ushort nodeId)
        {
            InstanceID result = default;
            result.NetNode = nodeId;
            return result;
        }

        public override Color GetContrastColor(RoadIdentifier refID, int boardIdx, int secIdx, BoardDescriptorXml descriptor) => Color.black;

        #region Serialize
        public override void Deserialize(string data) { }
        public override string Serialize() => null;
        protected override string ID => "";

        #endregion

    }
}
