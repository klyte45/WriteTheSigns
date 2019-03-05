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

    public class BoardGeneratorHighwayMileage : BoardGeneratorParent<BoardGeneratorHighwayMileage, BoardBunchContainer, CacheControl, BasicRenderInformation, BoardDescriptor, BoardTextDescriptor, BoardGeneratorHighwayMileage.RoadIdentifier>
    {

        public Dictionary<RoadIdentifier, List<MileagePlateDescriptor>> m_highwayMarksObjects;
        public RoadIdentifier[] m_segmentToHighway;
        public BasicRenderInformation[] m_cachedKilometerMeshes;
        public BasicRenderInformation[] m_cachedDirectionMeshes;
        public List<RoadIdentifier> m_destroyQueue = new List<RoadIdentifier>();

        private UIDynamicFont font = new UIDynamicFont();

        public override int ObjArraySize => 0;
        public override UIFont DrawFont => font;

        private BoardDescriptor m_baseDescriptorMileagePlate = new BoardDescriptor
        {
            m_propName = "Mileage Marker.Mileage Marker_Data",
            m_propRotation = 0,
            m_textDescriptors = new BoardTextDescriptor[]{
                new BoardTextDescriptor{
                    m_textRelativePosition =new Vector3(0f,1.65f,0.066f) ,
                    m_maxWidthMeters = 0.45f,
                    m_textScale = .375f,
                    m_useContrastColor = false,
                    m_textRelativeRotation = new Vector3(0,180,0),
                },
                new BoardTextDescriptor{
                    m_textRelativePosition =new Vector3(0f,0.95f,0.066f) ,
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

            font.shader = Shader.Find("UI/Dynamic Font Shader");
            font.material = new Material(Singleton<DistrictManager>.instance.m_properties.m_areaNameFont.material);
            font.baseline = (Singleton<DistrictManager>.instance.m_properties.m_areaNameFont as UIDynamicFont).baseline;
            font.size = (Singleton<DistrictManager>.instance.m_properties.m_areaNameFont as UIDynamicFont).size;
            font.lineHeight = (Singleton<DistrictManager>.instance.m_properties.m_areaNameFont as UIDynamicFont).lineHeight;
            font.baseFont = Font.CreateDynamicFontFromOSFont("Highway Gothic", 16);

            #region Hooks
            var postRenderMeshs = GetType().GetMethod("AfterRenderNode", allFlags);
            var afterRenderSegment = GetType().GetMethod("AfterRenderSegment", allFlags);
            doLog($"Patching=> {postRenderMeshs} {postRenderMeshs.IsStatic}");
            AddRedirect(typeof(RoadBaseAI).GetMethod("RenderNode", allFlags), null, postRenderMeshs);
            AddRedirect(typeof(NetSegment).GetMethod("RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int) }), null, afterRenderSegment);
            #endregion
        }

        protected override void OnTextureRebuilt()
        {
            doLog("onTextureRebuilt");
            m_cachedKilometerMeshes = new BasicRenderInformation[m_cachedKilometerMeshes.Length];
            m_cachedDirectionMeshes = new BasicRenderInformation[9];
        }

        private void onNodeChanged(ushort nodeId)
        {
            //doLog("onNodeChanged");
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
                            m_segmentToHighway[id] = default(RoadIdentifier);
                        }
                    }
                    m_highwayMarksObjects.Remove(removeTarget);
                    m_destroyQueue.Remove(removeTarget);

                    var segments = DTBUtils.GetSegmentRoadEdges(segmentId, true, false, false, out ComparableRoad start, out ComparableRoad end);
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
                RenderPropMesh(ref cachedDefaultInfo, cameraInfo, segmentID, plate.Second, plate.Third.kilometer, layerMask, 0, plate.Third.position, Vector4.zero, ref m_baseDescriptorMileagePlate.m_propName, plate.Third.rotation + m_baseDescriptorMileagePlate.m_propRotation, out Matrix4x4 propMatrix, out bool rendered);
                if (rendered)
                {
                    for (int j = 0; j < m_baseDescriptorMileagePlate.m_textDescriptors.Length; j++)
                    {
                        CacheControl c = null;
                        RenderTextMesh(cameraInfo, plate.First, plate.Second, plate.Third.kilometer, ref m_baseDescriptorMileagePlate, propMatrix, ref m_baseDescriptorMileagePlate.m_textDescriptors[j], ref c);
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
            if (m_cachedKilometerMeshes.Length <= kilometers)
            {
                m_cachedKilometerMeshes = new BasicRenderInformation[kilometers];
            }
            if (m_cachedKilometerMeshes[kilometers] == null || lastFontUpdateFrame > m_cachedKilometerMeshes[kilometers].m_frameDrawTime)
            {
                doLog($"!nameUpdated Node1 {kilometers}");
                RefreshNameData(ref m_cachedKilometerMeshes[kilometers], $"km\n{kilometers}");
            }
            return m_cachedKilometerMeshes[kilometers];

        }
        #endregion

        public override Color GetColor(ushort buildingID, int idx, int secIdx)
        {
            return Color.white;

        }


        protected override InstanceID GetPropRenderID(ushort nodeId)
        {
            InstanceID result = default(InstanceID);
            result.NetNode = nodeId;
            return result;
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

        private static Shader[] m_shaderList = new Shader[]
        {
Shader.Find("Custom/Buildings/Building/AnimUV"                 ),
Shader.Find("Custom/Buildings/Building/Basement"               ),
Shader.Find("Custom/Buildings/Building/Construction"           ),
Shader.Find("Custom/Buildings/Building/Default"                ),
Shader.Find("Custom/Buildings/Building/Fence"                  ),
Shader.Find("Custom/Buildings/Building/Floating"               ),
Shader.Find("Custom/Buildings/Building/NoBase"                 ),
Shader.Find("Custom/Buildings/Building/Water"                  ),
Shader.Find("Custom/Buildings/Building/WaterFlow"              ),
Shader.Find("Custom/Buildings/Building/WindTurbine"            ),
Shader.Find("Custom/Buildings/Group/Default"                   ),
Shader.Find("Custom/Buildings/Marker/Default"                  ),
Shader.Find("Custom/Citizens/Citizen/Default"                  ),
Shader.Find("Custom/Citizens/Citizen/Underground"              ),
Shader.Find("Custom/DayNight/Clouds"                           ),
Shader.Find("Custom/DayNight/DynamicClouds"                    ),
Shader.Find("Custom/DayNight/SaraBorealis"                     ),
Shader.Find("Custom/Decoration/Default"                        ),
Shader.Find("Custom/Decoration/Render"                         ),
Shader.Find("Custom/Effects/Lightning"                         ),
Shader.Find("Custom/Lights/FloatingGroup"                      ),
Shader.Find("Custom/Lights/FloatingGroupVolume"                ),
Shader.Find("Custom/Lights/Group"                              ),
Shader.Find("Custom/Lights/GroupVolume"                        ),
Shader.Find("Custom/Loading/AlphaBlend"                        ),
Shader.Find("Custom/Net/Electricity"                           ),
Shader.Find("Custom/Net/Fence"                                 ),
Shader.Find("Custom/Net/Group/Electricity"                     ),
Shader.Find("Custom/Net/Group/Metro"                           ),
Shader.Find("Custom/Net/Group/Road"                            ),
Shader.Find("Custom/Net/Group/Water"                           ),
Shader.Find("Custom/Net/Marker"                                ),
Shader.Find("Custom/Net/Metro"                                 ),
Shader.Find("Custom/Net/Road"                                  ),
Shader.Find("Custom/Net/RoadBridge"                            ),
Shader.Find("Custom/Net/TrainBridge"                           ),
Shader.Find("Custom/Net/Water"                                 ),
Shader.Find("Custom/Overlay/AreaBorder"                        ),
Shader.Find("Custom/Overlay/Brush"                             ),
Shader.Find("Custom/Overlay/BuildingHighlight"                 ),
Shader.Find("Custom/Overlay/DecorationArea"                    ),
Shader.Find("Custom/Overlay/DirectionArrow"                    ),
Shader.Find("Custom/Overlay/DistrictAreas"                     ),
Shader.Find("Custom/Overlay/DistrictIcon"                      ),
Shader.Find("Custom/Overlay/DistrictName"                      ),
Shader.Find("Custom/Overlay/GameAreas"                         ),
Shader.Find("Custom/Overlay/Notification"                      ),
Shader.Find("Custom/Overlay/RoadName"                          ),
Shader.Find("Custom/Overlay/Shape"                             ),
Shader.Find("Custom/Overlay/ShapeBlend"                        ),
Shader.Find("Custom/Overlay/SurfaceLine"                       ),
Shader.Find("Custom/Overlay/Topography"                        ),
Shader.Find("Custom/Overlay/TransportConnection"               ),
Shader.Find("Custom/Overlay/TransportLine"                     ),
Shader.Find("Custom/Overlay/TransportPath"                     ),
Shader.Find("Custom/Overlay/UndergroundLine"                   ),
Shader.Find("Custom/Overlay/ZonedArea"                         ),
Shader.Find("Custom/Particles/Additive (Soft)"                 ),
Shader.Find("Custom/Particles/Alpha Blended"                   ),
Shader.Find("Custom/Particles/Alpha Tested"                    ),
Shader.Find("Custom/PostProcess/Fog"                           ),
Shader.Find("Custom/PostProcess/Overlay"                       ),
Shader.Find("Custom/Props/Decal/Blend"                         ),
Shader.Find("Custom/Props/Decal/Solid"                         ),
Shader.Find("Custom/Props/Marker/Default"                      ),
Shader.Find("Custom/Props/Prop/AnimUV"                         ),
Shader.Find("Custom/Props/Prop/Default"                        ),
Shader.Find("Custom/Props/Prop/Fence"                          ),
Shader.Find("Custom/Props/Prop/Flag"                           ),
Shader.Find("Custom/Props/Prop/Floating"                       ),
Shader.Find("Custom/Props/Prop/Rotating"                       ),
Shader.Find("Custom/Props/Prop/TrafficLight"                   ),
Shader.Find("Custom/Rain"                                      ),
Shader.Find("Custom/RainParticle"                              ),
Shader.Find("Custom/SMAAshader"                                ),
Shader.Find("Custom/Terrain/Default"                           ),
Shader.Find("Custom/Tools/DisasterMarker"                      ),
Shader.Find("Custom/Trees/Default"                             ),
Shader.Find("Custom/Trees/Group"                               ),
Shader.Find("Custom/Trees/Render"                              ),
Shader.Find("Custom/Vehicles/Vehicle/Aircraft"                 ),
Shader.Find("Custom/Vehicles/Vehicle/Billboard"                ),
Shader.Find("Custom/Vehicles/Vehicle/Default"                  ),
Shader.Find("Custom/Vehicles/Vehicle/Helicopter"               ),
Shader.Find("Custom/Vehicles/Vehicle/Rotors"                   ),
Shader.Find("Custom/Vehicles/Vehicle/Ship"                     ),
Shader.Find("Custom/Vehicles/Vehicle/Train"                    ),
Shader.Find("Custom/Vehicles/Vehicle/Underground"              ),
Shader.Find("Custom/Vehicles/Vehicle/Vortex"                   ),
Shader.Find("Custom/Water/Default"                             ),
Shader.Find("Custom/Water/Transparent"                         ),
Shader.Find("GUI/Text Shader"                                  ),
Shader.Find("Hidden/3DLUTColorCorrection"                      ),
Shader.Find("Hidden/BlendForBloom"                             ),
Shader.Find("Hidden/BlitCopy"                                  ),
Shader.Find("Hidden/BlitCopyDepth"                             ),
Shader.Find("Hidden/BlurAndFlares"                             ),
Shader.Find("Hidden/BrightPassFilter2"                         ),
Shader.Find("Hidden/ConvertTexture"                            ),
Shader.Find("Hidden/CubeBlend"                                 ),
Shader.Find("Hidden/CubeBlur"                                  ),
Shader.Find("Hidden/CubeCopy"                                  ),
Shader.Find("Hidden/DayNight/Fog"                              ),
Shader.Find("Hidden/DayNight/Skybox"                           ),
Shader.Find("Hidden/DayNight/Stars"                            ),
Shader.Find("Hidden/Dof/DepthOfFieldHdr"                       ),
Shader.Find("Hidden/Dof/DX11Dof"                               ),
Shader.Find("Hidden/Dof/TiltShiftHdrLensBlur"                  ),
Shader.Find("Hidden/Fade Effect"                               ),
Shader.Find("Hidden/FilmGrainEffect"                           ),
Shader.Find("Hidden/Internal-CombineDepthNormals"              ),
Shader.Find("Hidden/Internal-DeferredReflections"              ),
Shader.Find("Hidden/Internal-DeferredShading"                  ),
Shader.Find("Hidden/Internal-DepthNormalsTexture"              ),
Shader.Find("Hidden/InternalErrorShader"                       ),
Shader.Find("Hidden/Internal-Flare"                            ),
Shader.Find("Hidden/Internal-GUITexture"                       ),
Shader.Find("Hidden/Internal-GUITextureBlit"                   ),
Shader.Find("Hidden/Internal-GUITextureClip"                   ),
Shader.Find("Hidden/Internal-GUITextureClipText"               ),
Shader.Find("Hidden/Internal-Halo"                             ),
Shader.Find("Hidden/Internal-MotionVectors"                    ),
Shader.Find("Hidden/Internal-PrePassLighting"                  ),
Shader.Find("Hidden/Internal-ScreenSpaceShadows"               ),
Shader.Find("Hidden/Internal-StencilWrite"                     ),
Shader.Find("Hidden/LensFlareCreate"                           ),
Shader.Find("Hidden/ToneMapping"                               ),
Shader.Find("Hidden/VideoDecode"                               ),
Shader.Find("Legacy Shaders/Diffuse"                           ),
Shader.Find("Legacy Shaders/VertexLit"                         ),
Shader.Find("Sprites/Default"                                  ),
Shader.Find("UI/ColorPicker HSB"                               ),
Shader.Find("UI/ColorPicker Hue"                               ),
Shader.Find("UI/Default"                                       ),
Shader.Find("UI/Default Font"                                  ),
Shader.Find("UI/Default UI Shader"                             ),
Shader.Find("UI/Dynamic Font Shader"                           ),
Shader.Find("UI/LegendGradient"                                ),
Shader.Find("UI/LegendStepGradient"                            ),
Shader.Find("UI/LegendStepGradient3"                           ),
Shader.Find("UI/ModalEffect"                                   ),
Shader.Find("UI/ParticlesAdditive"                             ),
Shader.Find("Unlit/Transparent"                                ),

        };

    }
}
