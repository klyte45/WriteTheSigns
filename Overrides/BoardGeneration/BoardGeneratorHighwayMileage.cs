using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Libraries;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.Utils.SegmentUtils;

namespace Klyte.DynamicTextProps.Overrides
{

    public partial class BoardGeneratorHighwayMileage : BoardGeneratorParent<BoardGeneratorHighwayMileage, IBoardBunchContainer<CacheControl, BasicRenderInformation>, CacheControl, BasicRenderInformation, BoardDescriptorMileageMarkerXml, BoardTextDescriptorMileageMarkerXml>
    {

        public BasicRenderInformation[] m_cachedKilometerMeshes;
        public BasicRenderInformation[] m_cachedDirectionMeshes;
        public Tuple<RoadIdentifier, MileageMarkerDescriptor, MileageMarkerDescriptor>[] m_segmentCachedInfo;

        private UIDynamicFont m_font;

        public static readonly TextType[] AVAILABLE_TEXT_TYPES = new TextType[]
          {
                    TextType.OwnName,
                    TextType.Fixed,
                    TextType.Custom1,
                    TextType.BuildingNumber
          };
        public override int ObjArraySize => 0;
        public override UIDynamicFont DrawFont => m_font;


        private static BoardDescriptorMileageMarkerXml m_loadedDescriptor = new BoardDescriptorMileageMarkerXml();

        public static BoardDescriptorMileageMarkerXml LoadedMileageMarkerModel
        {
            get {
                if (m_loadedDescriptor == null)
                {
                    m_loadedDescriptor = new BoardDescriptorMileageMarkerXml();
                }
                return m_loadedDescriptor;
            }
            set => m_loadedDescriptor = value;
        }

        internal static void GenerateDefaultSignModelAtLibrary()
        {
            BoardDescriptorMileageMarkerXml defaultModel = new BoardDescriptorMileageMarkerXml
            {
                m_propName = "1679681061.Mileage Marker_Data",
                m_textDescriptors = new BoardTextDescriptorMileageMarkerXml[]{
                    new BoardTextDescriptorMileageMarkerXml{
                        SaveName = "Direction",
                        m_textRelativePosition =new Vector3(0f,1.78f,0.066f) ,
                        m_maxWidthMeters = 0.45f,
                        m_textScale = .375f,
                        m_useContrastColor = false,
                        m_textRelativeRotation = new Vector3(0,180,0),
                        m_textType = TextType.Custom1
                    },
                    new BoardTextDescriptorMileageMarkerXml{
                        SaveName = "Kilometer Label",
                        m_textRelativePosition =new Vector3(0f,1.48f,0.066f) ,
                        m_textRelativeRotation = new Vector3(0,180,0),
                        m_maxWidthMeters = 0.45f,
                        m_textScale = .5f,
                        m_useContrastColor = false,
                        m_textType = TextType.Fixed,
                        m_fixedText = "km"
                    },
                    new BoardTextDescriptorMileageMarkerXml{
                        SaveName = "Kilometer",
                        m_textRelativePosition =new Vector3(0f,1.18f,0.066f) ,
                        m_textRelativeRotation = new Vector3(0,180,0),
                        m_maxWidthMeters = 0.45f,
                        m_textScale = .5f,
                        m_useContrastColor = false,
                        m_textType = TextType.BuildingNumber
                    },
                }
            };


            DTPLibMileageMarkerGroup.Instance.Add("<DEFAULT>", defaultModel);
        }
        #region Initialize
        public override void Initialize()
        {
            m_segmentCachedInfo = new Tuple<RoadIdentifier, MileageMarkerDescriptor, MileageMarkerDescriptor>[NetManager.MAX_SEGMENT_COUNT];
            m_cachedKilometerMeshes = new BasicRenderInformation[100];
            m_cachedDirectionMeshes = new BasicRenderInformation[9];

            NetManagerOverrides.EventNodeChanged += OnNodeChanged;
            NetManagerOverrides.EventSegmentChanged += OnSegmentChanged;

            BuildSurfaceFont(out m_font, LoadedMileageMarkerModel.FontName);

            #region Hooks
            System.Reflection.MethodInfo postRenderMeshs = GetType().GetMethod("AfterRenderNode", RedirectorUtils.allFlags);
            System.Reflection.MethodInfo afterRenderSegment = GetType().GetMethod("AfterRenderSegment", RedirectorUtils.allFlags);
            System.Reflection.MethodInfo onApplyModificationTool = GetType().GetMethod("OnApplyModificationTool", RedirectorUtils.allFlags);
            LogUtils.DoLog($"Patching=> {postRenderMeshs} {postRenderMeshs.IsStatic}");
            RedirectorInstance.AddRedirect(typeof(RoadBaseAI).GetMethod("RenderNode", RedirectorUtils.allFlags), null, postRenderMeshs);
            RedirectorInstance.AddRedirect(typeof(NetSegment).GetMethod("RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int) }), null, afterRenderSegment);
            RedirectorInstance.AddRedirect(typeof(NetAdjust).GetMethod("ApplyModification", new Type[] { typeof(int) }), null, onApplyModificationTool);
            #endregion
        }



        protected override void OnTextureRebuiltImpl(Font obj)
        {
            if (obj.name == DrawFont.baseFont.name)
            {
                SoftReset();
            }
        }

        public void SoftReset()
        {
            m_testTextInfo = null;
            m_cachedDirectionMeshes = new BasicRenderInformation[9];
            m_cachedKilometerMeshes = new BasicRenderInformation[100];
            m_segmentCachedInfo = new Tuple<RoadIdentifier, MileageMarkerDescriptor, MileageMarkerDescriptor>[NetManager.MAX_SEGMENT_COUNT];
            m_cachedPropInfo = null;
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

        private static readonly Func<NetAdjust, InstanceID> m_getLastInstanceAdjust = ReflectionUtils.GetGetFieldDelegate<NetAdjust, InstanceID>("m_lastInstance", typeof(NetAdjust));

#pragma warning disable IDE0051 // Remover membros privados não utilizados
        private static void OnApplyModificationTool(ref NetAdjust __instance) => Instance.OnSegmentChanged(m_getLastInstanceAdjust(__instance).NetSegment);
#pragma warning restore IDE0051 // Remover membros privados não utilizados

        private void OnSegmentChanged(ushort segmentId) => m_segmentCachedInfo = new Tuple<RoadIdentifier, MileageMarkerDescriptor, MileageMarkerDescriptor>[NetManager.MAX_SEGMENT_COUNT];
        #endregion
        protected override void OnChangeFont(string fontName) => LoadedMileageMarkerModel.FontName = fontName;

        public static void AfterRenderNode(ushort nodeID)
        {

            Instance.ComputeNode(nodeID);

        }
        public static void AfterRenderSegment(RenderManager.CameraInfo cameraInfo, ushort segmentID, int layerMask)
        {

            Instance.AfterRenderSegmentImpl(cameraInfo, segmentID, layerMask);

        }
        public void ComputeNode(ushort nodeID)
        {

            for (int i = 0; i < 8; i++)
            {
                ushort segmentId = NetManager.instance.m_nodes.m_buffer[nodeID].GetSegment(i);
                if (segmentId == 0)
                {
                    continue;
                }

                if (!(NetManager.instance.m_segments.m_buffer[segmentId].Info.m_netAI is RoadBaseAI baseAI) || !baseAI.m_highwayRules)
                {
                    m_segmentCachedInfo[segmentId] = Tuple.New<RoadIdentifier, MileageMarkerDescriptor, MileageMarkerDescriptor>(default, default, default);
                    continue;
                }
                if (m_segmentCachedInfo[segmentId] == null)
                {
                    IEnumerable<Tuple<ushort, float>> segments = SegmentUtils.GetSegmentRoadEdges(segmentId, false, false, false, out ComparableRoad start, out ComparableRoad end);
                    if (segments == null)
                    {
                        MileageMarkerDescriptor defaultMM = default;
                        RoadIdentifier identifier = new RoadIdentifier(default, default, new ushort[] { segmentId });
                        m_segmentCachedInfo[segmentId] = Tuple.NewRef(ref identifier, ref defaultMM, ref defaultMM);
                    }
                    else
                    {
                        RoadIdentifier tupleIdentifier = new RoadIdentifier(start, end, segments.Select(x => x.First).ToArray());
                        bool roadInverted = tupleIdentifier.start.nodeReference == NetManager.instance.m_segments.m_buffer[tupleIdentifier.segments[0]].m_endNode == ((NetManager.instance.m_segments.m_buffer[tupleIdentifier.segments[0]].m_flags & NetSegment.Flags.Invert) == 0);
                        List<MileageMarkerDescriptor> entry = new List<MileageMarkerDescriptor>();
                        float meterCount = 0f;
                        foreach (Tuple<ushort, float> segmentRef in segments)
                        {
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
                                MileageMarkerDescriptor marker1 = new MileageMarkerDescriptor
                                {
                                    segmentId = segmentRef.First,
                                    kilometer = oldMeterKm + 1,
                                    position = segmentObj.m_middlePosition + (VectorUtils.X_Y(KlyteMathUtils.DegreeToVector2(rotation - 90)) * (segmentObj.Info.m_halfWidth - 1)),
                                    cardinalDirection8 = cardinalDirection,
                                    rotation = rotation + 90

                                };
                                if (segmentObj.Info.m_backwardVehicleLaneCount + segmentObj.Info.m_forwardVehicleLaneCount >= 2)
                                {
                                    MileageMarkerDescriptor marker2 = new MileageMarkerDescriptor
                                    {
                                        segmentId = segmentRef.First,
                                        kilometer = oldMeterKm + 1,
                                        position = segmentObj.m_middlePosition + (VectorUtils.X_Y(KlyteMathUtils.DegreeToVector2(rotation + 90)) * (segmentObj.Info.m_halfWidth - 1)),
                                        cardinalDirection8 = (byte) ((cardinalDirection + (segmentObj.Info.m_hasBackwardVehicleLanes && segmentObj.Info.m_hasForwardVehicleLanes ? 4 : 0)) % 8),
                                        rotation = segmentObj.Info.m_hasBackwardVehicleLanes && segmentObj.Info.m_hasForwardVehicleLanes ? rotation - 90 : rotation + 90
                                    };

                                    m_segmentCachedInfo[segmentRef.First] = Tuple.NewRef(ref tupleIdentifier, ref marker1, ref marker2);
                                }
                                else
                                {
                                    MileageMarkerDescriptor defaultMM = default;

                                    m_segmentCachedInfo[segmentRef.First] = Tuple.NewRef(ref tupleIdentifier, ref marker1, ref defaultMM);
                                }
                            }
                            else
                            {
                                MileageMarkerDescriptor defaultMM = default;
                                m_segmentCachedInfo[segmentRef.First] = Tuple.NewRef(ref tupleIdentifier, ref defaultMM, ref defaultMM);
                            }
                        }





                    }
                }
            }

        }
        public void AfterRenderSegmentImpl(RenderManager.CameraInfo cameraInfo, ushort segmentID, int layerMask)
        {

            if (m_segmentCachedInfo[segmentID] == null)
            {
                if (segmentID == 0)
                {
                    return;
                }

                ComputeNode(NetManager.instance.m_segments.m_buffer[segmentID].m_startNode);
            }

            MileageMarkerDescriptor[] markers = new MileageMarkerDescriptor[] { m_segmentCachedInfo[segmentID].Second, m_segmentCachedInfo[segmentID].Third };
            for (int i = 0; i < markers.Length; i++)
            {
                MileageMarkerDescriptor marker = markers[i];
                if (marker.segmentId == default)
                {
                    continue;
                }

                RenderPropMesh(ref m_cachedPropInfo, cameraInfo, segmentID, i, marker.kilometer, layerMask, 0, marker.position, Vector4.zero, ref LoadedMileageMarkerModel.m_propName, new Vector3(0, marker.rotation) + LoadedMileageMarkerModel.m_propRotation, LoadedMileageMarkerModel.PropScale, ref m_loadedDescriptor, out Matrix4x4 propMatrix, out bool rendered);
                if (rendered)
                {
                    for (int j = 0; j < LoadedMileageMarkerModel.m_textDescriptors.Length; j++)
                    {
                        CacheControl c = null;
                        MaterialPropertyBlock block = NetManager.instance.m_materialBlock;
                        block.Clear();
                        RenderTextMesh(cameraInfo, segmentID, i, marker.kilometer, ref m_loadedDescriptor, propMatrix, ref LoadedMileageMarkerModel.m_textDescriptors[j], ref c, block);
                    }
                }
            }

        }



        #region Upadate Data
        protected override BasicRenderInformation GetMeshCustom1(ushort id, int boardIdx, int kilometers, out UIFont font)
        {
            font = DrawFont;
            byte direction = (boardIdx == 1 ? m_segmentCachedInfo[id].Third : m_segmentCachedInfo[id].Second).cardinalDirection8;
            if (m_cachedDirectionMeshes[direction] == null || lastFontUpdateFrame > m_cachedDirectionMeshes[direction].m_frameDrawTime)
            {
                LogUtils.DoLog($"!nameUpdated Node1 {kilometers} ({direction})");
                RefreshNameData(ref m_cachedDirectionMeshes[direction], Locale.Get("K45_CARDINAL_POINT_LONG", direction.ToString()).ToUpper());

            }
            return m_cachedDirectionMeshes[direction];

        }
        protected override BasicRenderInformation GetMeshCurrentNumber(ushort id, int boardIdx, int kilometers, out UIFont font)
        {
            font = DrawFont;
            if (m_cachedKilometerMeshes.Length <= kilometers + 1)
            {
                m_cachedKilometerMeshes = new BasicRenderInformation[kilometers + 1];
            }
            if (m_cachedKilometerMeshes[kilometers] == null || lastFontUpdateFrame > m_cachedKilometerMeshes[kilometers].m_frameDrawTime)
            {
                LogUtils.DoLog($"!nameUpdated Node1 {kilometers}");
                RefreshNameData(ref m_cachedKilometerMeshes[kilometers], $"{kilometers}");
            }
            return m_cachedKilometerMeshes[kilometers];

        }

        private BasicRenderInformation m_testTextInfo = null;
        private long m_testTextInfoTime = 0;
        private PropInfo m_cachedPropInfo;

        protected override BasicRenderInformation GetOwnNameMesh(ushort id, int boardIdx, int secIdx, out UIFont font)
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
        #endregion

        public override Color? GetColor(ushort buildingID, int idx, int secIdx, BoardDescriptorMileageMarkerXml descriptor) => descriptor.PropColor;

        protected override InstanceID GetPropRenderID(ushort nodeId)
        {
            InstanceID result = default;
            result.NetNode = nodeId;
            return result;
        }

        public override Color GetContrastColor(ushort refID, int boardIdx, int secIdx, BoardDescriptorMileageMarkerXml descriptor) => KlyteMonoUtils.ContrastColor(descriptor.PropColor);

        #region Serialize
        public override void Deserialize(string data)
        {
            LogUtils.DoLog($"{GetType()} STR: \"{data}\"");
            if (data.IsNullOrWhiteSpace())
            {
                return;
            }
            try
            {
                LoadedMileageMarkerModel = XmlUtils.DefaultXmlDeserialize<BoardDescriptorMileageMarkerXml>(data);
            }
            catch (Exception e)
            {
                LogUtils.DoErrorLog($"Error deserializing: {e.Message}\n{e.StackTrace}");
            }
        }
        public override string Serialize() => XmlUtils.DefaultXmlSerialize(LoadedMileageMarkerModel, false);
        protected override string ID { get; } = "K45_DTP_MM";

        internal void CleanDescriptor()
        {
            LoadedMileageMarkerModel = new BoardDescriptorMileageMarkerXml();
            SoftReset();
        }

        #endregion

    }
}
