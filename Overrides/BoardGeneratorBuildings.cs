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
using static Klyte.DynamicTextBoards.Overrides.BoardGeneratorBuildings;

namespace Klyte.DynamicTextBoards.Overrides
{

    public class BoardGeneratorBuildings : BoardGeneratorParent<BoardGeneratorBuildings, BoardBunchContainerBuilding, CacheControlTransportBuilding, BasicRenderInformation, BoardDescriptor, BoardTextDescriptor, ushort>
    {

        private Dictionary<String, List<BoardDescriptor>> loadedDescriptors;

        private LineDescriptor[] m_linesDescriptors;
        private UpdateFlagsBuildings[] m_updateData;
        private Dictionary<string, StopPointDescriptorLanes[]> m_buildingStopsDescriptor = new Dictionary<string, StopPointDescriptorLanes[]>();

        public override int ObjArraySize => BuildingManager.MAX_BUILDING_COUNT;

        private UIDynamicFont m_font;

        public override UIDynamicFont DrawFont => m_font;

        #region Initialize
        public override void Initialize()
        {
            m_updateData = new UpdateFlagsBuildings[BuildingManager.MAX_BUILDING_COUNT];
            m_linesDescriptors = new LineDescriptor[TransportManager.MAX_LINE_COUNT];
            loadedDescriptors = GenerateDefaultDictionary();

            BuildSurfaceFont(out m_font, "Arial");

            TransportManagerOverrides.eventOnLineUpdated += onLineUpdated;
            NetManagerOverrides.eventNodeChanged += onNodeChanged;
            TransportManager.instance.eventLineColorChanged += onLineUpdated;
            BuildingManagerOverrides.eventOnBuildingRenamed += onBuildingNameChanged;

            #region Hooks
            var postRenderMeshs = GetType().GetMethod("AfterRenderMeshes", allFlags);
            doLog($"Patching=> {postRenderMeshs}");
            AddRedirect(typeof(BuildingAI).GetMethod("RenderMeshes", allFlags), null, postRenderMeshs);
            #endregion
        }

        private void onNodeChanged(ushort id)
        {
            var buildingId = NetNode.FindOwnerBuilding(id, 56f);
            if (buildingId > 0 && m_boardsContainers[buildingId] != null)
            {
                m_boardsContainers[buildingId].m_linesUpdateFrame = 0;
            }
        }

        protected override void OnTextureRebuilt()
        {
            m_updateData = new UpdateFlagsBuildings[BuildingManager.MAX_BUILDING_COUNT];
        }


        private void onLineUpdated(ushort lineId)
        {
            //doLog("onLineUpdated");
            m_linesDescriptors[lineId] = default(LineDescriptor);
        }
        private void onBuildingNameChanged(ushort id)
        {
            //doLog("onBuildingNameChanged");
            m_updateData[id].m_nameMesh = false;
        }
        #endregion


        public static void AfterRenderMeshes(BuildingAI __instance, RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance)
        {
            BoardGeneratorBuildings.instance.AfterRenderMeshesImpl(cameraInfo, buildingID, ref data, layerMask, ref instance, __instance);
        }

        public void AfterRenderMeshesImpl(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance renderInstance, BuildingAI __instance)
        {
            if (!loadedDescriptors.ContainsKey(data.Info.name) || loadedDescriptors[data.Info.name].Count == 0)
            {
                return;
            }
            if (m_boardsContainers[buildingID] == null)
            {
                m_boardsContainers[buildingID] = new BoardBunchContainerBuilding();
            }
            if (m_boardsContainers[buildingID]?.m_boardsData?.Count() != loadedDescriptors[data.Info.name].Count)
            {
                m_boardsContainers[buildingID].m_boardsData = new CacheControlTransportBuilding[loadedDescriptors[data.Info.name].Count];
                m_updateData[buildingID].m_nameMesh = false;
            }

            UpdateLinesBuilding(buildingID, ref data, m_boardsContainers[buildingID]);
            for (var i = 0; i < loadedDescriptors[data.Info.name].Count; i++)
            {
                var descriptor = loadedDescriptors[data.Info.name][i];
                if (m_boardsContainers[buildingID].m_boardsData[i] == null) m_boardsContainers[buildingID].m_boardsData[i] = new CacheControlTransportBuilding();
                RenderPropMesh(ref m_boardsContainers[buildingID].m_boardsData[i].m_cachedProp, cameraInfo, buildingID, i, 0, layerMask, data.m_angle, renderInstance.m_dataMatrix1.MultiplyPoint(descriptor.m_propPosition), renderInstance.m_dataVector3, ref descriptor.m_propName, descriptor.m_propRotation, ref descriptor, out Matrix4x4 propMatrix, out bool rendered);
                if (rendered)
                {
                    for (int j = 0; j < descriptor.m_textDescriptors.Length; j++)
                    {
                        MaterialPropertyBlock materialBlock = Singleton<PropManager>.instance.m_materialBlock;
                        materialBlock.Clear();

                        RenderTextMesh(cameraInfo, buildingID, i, j, ref descriptor, propMatrix, ref descriptor.m_textDescriptors[j], ref m_boardsContainers[buildingID].m_boardsData[i], materialBlock);
                    }
                }
            }
        }



        #region Upadate Data
        protected override BasicRenderInformation GetOwnNameMesh(ushort buildingID, int boardIdx, int secIdx)
        {
            if (m_boardsContainers[buildingID].m_nameSubInfo == null || !m_updateData[buildingID].m_nameMesh)
            {
                RefreshNameData(ref m_boardsContainers[buildingID].m_nameSubInfo, BuildingManager.instance.GetBuildingName(buildingID, new InstanceID()) ?? "DUMMY!!!!!");
                m_updateData[buildingID].m_nameMesh = true;
            }
            return m_boardsContainers[buildingID].m_nameSubInfo;

        }
        protected void UpdateLinesBuilding(ushort buildingID, ref Building data, BoardBunchContainerBuilding bbcb)
        {
            if (bbcb.m_platformToLine == null || (bbcb.m_ordenedLines?.Length > 0 && bbcb.m_linesUpdateFrame < bbcb.m_ordenedLines.Select((x) => m_linesDescriptors[x]?.m_lastUpdate ?? 0).Max()))
            {
                if (!m_buildingStopsDescriptor.ContainsKey(data.Info.name))
                {
                    m_buildingStopsDescriptor[data.Info.name] = MapStopPoints(data.Info);
                    //m_buildingStopsDescriptor[data.Info.name + "PLAT"] = GetAllPlatforms(data.Info.m_buildingAI);
                }

                var platforms = m_buildingStopsDescriptor[data.Info.name].Select((v, i) => new { Key = i, Value = v }).ToDictionary(o => o.Key, o => o.Value);
                //var platformsPlat = m_buildingStopsDescriptor[data.Info.name + "PLAT"].Select((v, i) => new { Key = i, Value = v }).ToDictionary(o => o.Key, o => o.Value);
                if (platforms.Count == 0)
                {
                    bbcb.m_ordenedLines = new ushort[0];
                    bbcb.m_platformToLine = new ushort[0][];
                }
                else
                {

                    List<Quad2> boundaries = new List<Quad2>();
                    var subBuilding = buildingID;
                    var allnodes = new List<ushort>();
                    while (subBuilding > 0)
                    {
                        boundaries.Add(GetBounds(ref BuildingManager.instance.m_buildings.m_buffer[subBuilding]));
                        var node = BuildingManager.instance.m_buildings.m_buffer[subBuilding].m_netNode;
                        while (node > 0)
                        {
                            allnodes.Add(node);
                            node = NetManager.instance.m_nodes.m_buffer[node].m_nextBuildingNode;
                        }
                        subBuilding = BuildingManager.instance.m_buildings.m_buffer[subBuilding].m_subBuilding;
                    }
                    foreach (ushort node in allnodes)
                    {
                        if (!boundaries.Any(x => x.Intersect(NetManager.instance.m_nodes.m_buffer[node].m_position)))
                        {
                            for (var segIdx = 0; segIdx < 8; segIdx++)
                            {
                                var segmentId = NetManager.instance.m_nodes.m_buffer[node].GetSegment(segIdx);
                                if (segmentId != 0 && allnodes.Contains(NetManager.instance.m_segments.m_buffer[segmentId].GetOtherNode(node)))
                                {


                                    boundaries.Add(GetBounds(
                                        NetManager.instance.m_nodes.m_buffer[NetManager.instance.m_segments.m_buffer[segmentId].m_startNode].m_position,
                                        NetManager.instance.m_nodes.m_buffer[NetManager.instance.m_segments.m_buffer[segmentId].m_endNode].m_position,
                                        NetManager.instance.m_segments.m_buffer[segmentId].Info.m_halfWidth)
                                        );
                                }
                            }
                        }
                    }
                    var nearStops = KlyteUtils.FindNearStops(data.m_position, ItemClass.Service.PublicTransport, ItemClass.Service.PublicTransport, VehicleInfo.VehicleType.None, true, 400f, out List<float> dist, out List<Vector3> absolutePos, boundaries);
                    if (nearStops.Count > 0)
                    {
                        bbcb.m_platformToLine = new ushort[m_buildingStopsDescriptor[data.Info.name].Length][];
                        var nearStopsParsed = nearStops.Select((x, i) => new { stopId = x, relPos = CalculatePositionRelative(absolutePos[i], BuildingManager.instance.m_buildings.m_buffer[buildingID].m_angle, BuildingManager.instance.m_buildings.m_buffer[buildingID].m_position) })
                         .Select((y, i) => Tuple.New(platforms.Where((x, j) =>
                         {
                             //var relOrg = CalculatePositionRelative(absolutePos[i], BuildingManager.instance.m_buildings.m_buffer[buildingID].m_angle, BuildingManager.instance.m_buildings.m_buffer[buildingID].m_position);
                             var distance = x.Value.platformLine.DistanceSqr(y.relPos, out float k);
                             doLog($"[{BuildingManager.instance.m_buildings.m_buffer[buildingID].Info.name}]x = {x.Key} ({x.Value.platformLine.a} {x.Value.platformLine.b} {x.Value.platformLine.c} {x.Value.platformLine.d}) (w= {x.Value.width}) {x.Value.vehicleType}\t| relOrg {y.relPos} \t| {distance}");
                             return Mathf.Abs(distance - x.Value.width * x.Value.width) < 0.1f;
                         }).FirstOrDefault().Key, NetManager.instance.m_nodes.m_buffer[y.stopId].m_transportLine));

                        foreach (var nearStopsParsedItem in nearStopsParsed.Select(x => x.First).Distinct())
                        {
                            bbcb.m_platformToLine[nearStopsParsedItem] = nearStopsParsed.Where(x => x.First == nearStopsParsedItem).Select(x => x.Second).ToArray();
                        }
                        var uniqueLines = nearStopsParsed.Select(x => x.Second).Distinct().ToList();
                        uniqueLines.Sort((a, b) => VehicleToPriority(TransportManager.instance.m_lines.m_buffer[a].Info.m_vehicleType).CompareTo(VehicleToPriority(TransportManager.instance.m_lines.m_buffer[b].Info.m_vehicleType)));
                        bbcb.m_ordenedLines = uniqueLines.ToArray();
                        //doLog($"updatedIdsColors {nearStops.Count} [{string.Join(",", nearStops.Select(x => x.ToString()).ToArray())}], [{string.Join(",", dist.Select(x => x.ToString()).ToArray())}], ");
                    }
                }
                bbcb.m_linesUpdateFrame = SimulationManager.instance.m_currentTickIndex;
            }
        }

        private static Vector3 CalculatePositionRelative(Vector3 position, float angle, Vector3 original)
        {
            Vector3 offset = new Vector3
            {
                y = position.y - original.y
            };

            var cos = Mathf.Cos(angle);
            var sin = Mathf.Sin(angle);

            //           position.x = original.x +              cos * offset.x +  sin   * offset.z;
            //position.z            =              original.z + sin * offset.x + (-cos) * offset.z;

            //                   cos * position.x = cos * original.x +                                  cos * cos * offset.x  + cos * sin    * offset.z;
            //sin * position.z                    =                    sin * original.z +               sin * sin * offset.x  + sin * (-cos) * offset.z;
            //==========================================================================================================================================
            //sin * position.z + cos * position.x = cos * original.x + sin * original.z + (cos * cos + sin * sin) * offset.x;

            offset.x = -(cos * original.x + sin * original.z - sin * position.z - cos * position.x);
            offset.z = (-position.x + original.x + cos * offset.x) / -sin;

            return offset;
        }


        private int VehicleToPriority(VehicleInfo.VehicleType tt)
        {
            switch (tt)
            {
                case VehicleInfo.VehicleType.Car:
                    return 99;
                case VehicleInfo.VehicleType.Metro:
                case VehicleInfo.VehicleType.Train:
                case VehicleInfo.VehicleType.Monorail:
                    return 20;
                case VehicleInfo.VehicleType.Ship:
                    return 10;
                case VehicleInfo.VehicleType.Plane:
                    return 5;
                case VehicleInfo.VehicleType.Tram:
                    return 88;
                case VehicleInfo.VehicleType.Helicopter:
                    return 7;
                case VehicleInfo.VehicleType.Ferry:
                    return 15;

                case VehicleInfo.VehicleType.CableCar:
                    return 30;
                case VehicleInfo.VehicleType.Blimp:
                    return 12;
                case VehicleInfo.VehicleType.Balloon:
                    return 11;
                default: return 9999;
            }
        }

        #endregion

        public override Color GetColor(ushort buildingID, int boardIdx, int textIdx, BoardDescriptor descriptor)
        {
            var targetPlatforms = descriptor.m_platforms;
            foreach (var platform in targetPlatforms)
            {
                if (m_boardsContainers[buildingID].m_platformToLine != null && m_boardsContainers[buildingID].m_platformToLine.ElementAtOrDefault(platform) != null)
                {
                    var line = m_boardsContainers[buildingID].m_platformToLine[platform].ElementAtOrDefault(0);
                    if (line != 0)
                    {
                        if (m_linesDescriptors[line] == null)
                        {
                            UpdateLine(line);
                        }
                        return m_linesDescriptors[line].m_lineColor;
                    }
                }
            }
            return Color.white;
        }
        public override Color GetContrastColor(ushort buildingID, int boardIdx, int textIdx, BoardDescriptor descriptor)
        {
            var targetPlatforms = descriptor.m_platforms;
            foreach (var platform in targetPlatforms)
            {
                if (m_boardsContainers[buildingID].m_platformToLine != null && m_boardsContainers[buildingID].m_platformToLine.ElementAtOrDefault(platform) != null && m_boardsContainers[buildingID].m_platformToLine[platform].Length > 0)
                {
                    var line = m_boardsContainers[buildingID].m_platformToLine[platform].ElementAtOrDefault(0);
                    if (line != 0)
                    {
                        if (m_linesDescriptors[line] == null)
                        {
                            UpdateLine(line);
                        }
                        return m_linesDescriptors[line].m_contrastColor;
                    }
                }
            }
            return Color.black;
        }

        private void UpdateLine(ushort lineId)
        {
            m_linesDescriptors[lineId] = new LineDescriptor
            {
                m_lineColor = TransportManager.instance.GetLineColor(lineId),
                m_lastUpdate = SimulationManager.instance.m_currentTickIndex
            };

            m_linesDescriptors[lineId].m_contrastColor = KlyteUtils.contrastColor(m_linesDescriptors[lineId].m_lineColor);

        }

        protected override InstanceID GetPropRenderID(ushort buildingID)
        {
            InstanceID result = default(InstanceID);
            result.Building = buildingID;
            return result;
        }

        private static Dictionary<string, List<BoardDescriptor>> GenerateDefaultDictionary()
        {

            var basicEOLTextDescriptor = new BoardTextDescriptor[]{
                             new BoardTextDescriptor{
                                m_textRelativePosition = new Vector3(0,4.3f, -0.13f) ,
                                m_textRelativeRotation = Vector3.zero,
                                m_maxWidthMeters = 15.5f
                             },
                             new BoardTextDescriptor{
                                m_textRelativePosition = new Vector3(0,4.3f,0.02f),
                                m_textRelativeRotation = new Vector3(0,180,0),
                                m_maxWidthMeters = 15.5f
                             },
                        };
            var basicWallTextDescriptor = new BoardTextDescriptor[]{
                             new BoardTextDescriptor{
                                m_textRelativePosition =new Vector3(0,0,-0.08f) ,
                                m_textRelativeRotation = Vector3.zero,
                                m_maxWidthMeters = 15.5f
                             },
                        };
            var basicTotem = new BoardTextDescriptor[]{
                             new BoardTextDescriptor{
                                m_textRelativePosition =new Vector3(0.145f,2,-0.18f) ,
                                m_textRelativeRotation = new Vector3(0,330,270),
                                m_maxWidthMeters = 2.5f,
                                m_textScale = 0.5f,
                                m_dayEmissiveMultiplier = 0f,
                                m_nightEmissiveMultiplier = 7f,
                                m_useContrastColor = false,
                                m_defaultColor = Color.white
                             },
                             new BoardTextDescriptor{
                                m_textRelativePosition =new Vector3(-0.165f,2,0f) ,
                                m_textRelativeRotation = new Vector3(0,210,270),
                                m_maxWidthMeters = 2.5f,
                                m_textScale = 0.5f,
                                m_dayEmissiveMultiplier = 0f,
                                m_nightEmissiveMultiplier = 7f,
                                m_useContrastColor = false,
                                m_defaultColor = Color.white
                             },
                             new BoardTextDescriptor{
                                m_textRelativePosition =new Vector3(0.14f,2,0.17f) ,
                                m_textRelativeRotation = new Vector3(0,90,270),
                                m_maxWidthMeters = 2.5f,
                                m_textScale = 0.5f,
                                m_dayEmissiveMultiplier = 0f,
                                m_nightEmissiveMultiplier = 7f,
                                m_useContrastColor = false,
                                m_defaultColor = Color.white
                             },
                        };

            return new Dictionary<string, List<BoardDescriptor>>
            {
                ["Train Station"] = new List<BoardDescriptor>
                {
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6.BoardV6_Data",
                        m_propPosition= new Vector3(8f,6f,0.5F),
                        m_propRotation= 0,
                        m_textDescriptors =basicWallTextDescriptor,
                        m_platforms = new int[]{ 0 }
},
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6.BoardV6_Data",
                        m_propPosition= new Vector3(-13.5f,6f,0.5F),
                        m_propRotation= 0,
                        m_textDescriptors =basicWallTextDescriptor,
                        m_platforms = new int[]{ 0 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6.BoardV6_Data",
                        m_propPosition= new Vector3(0,5f,-16),
                        m_propRotation= 180,
                        m_textDescriptors =basicWallTextDescriptor,
                        m_platforms = new int[]{ 1 }
                    },
                    new BoardDescriptor
                    {
                        m_propName = "BoardV6.BoardV6_Data",
                        m_propPosition = new Vector3(-14, 8f, 22),
                        m_propRotation = 180,
                        m_textDescriptors =basicWallTextDescriptor,

                        m_platforms = new int[]{ 0,1 }
                    },
                },
                ["End of the line Trainstation"] = new List<BoardDescriptor>
                {
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(48,-1.5f,-48),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 2 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(32,-1.5f,-48),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{3,4 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(16,-1.5f,-48),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{5,6}
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,-1.5f,-48),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 7,8 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-48,-1.5f,-48),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 13 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-32,-1.5f,-48),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 11,12 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-16,-1.5f,-48),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 9, 10 }
                    },

                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(48,-1.5f,-80),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{2 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(32,-1.5f,-80),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 3,4 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(16,-1.5f,-80),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 5,6 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,-1.5f,-80),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 7,8 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-48,-1.5f,-80),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 13 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-32,-1.5f,-80),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 11,12 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-16,-1.5f,-80),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 9, 10 }
                    },
                     new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(48,-1.5f,-106),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 2 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(32,-1.5f,-106),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 3,4 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(16,-1.5f,-106),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{5,6 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,-1.5f,-106),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 7,8 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-48,-1.5f,-106),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 13 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-32,-1.5f,-106),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 11,12 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-16,-1.5f,-106),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 9, 10 }
                    },
                     new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(48,-1.5f,-133),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{2 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(32,-1.5f,-133),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 3,4 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(16,-1.5f,-133),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 5,6 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,-1.5f,-133),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 7,8 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-48,-1.5f,-133),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{13 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-32,-1.5f,-133),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{11,12 }
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-16,-1.5f,-133),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 9,10 }
                    },
                },
                ["Large Trainstation"] = new List<BoardDescriptor>
                {
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,-1.5f,-0),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{2},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,-1.5f,-16),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{3,4},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,-1.5f,-32),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{5,6 },

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,-1.5f,-48),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{7,8},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,-1.5f,-64),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{9,10},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,-1.5f,-80),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{11,12},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,-1.5f,-95.5f),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{13},
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,-1.5f,-0),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{2},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,-1.5f,-16),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{4,3},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,-1.5f,-32),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{6,5},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,-1.5f,-48),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{8,7},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,-1.5f,-64),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{10,9},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,-1.5f,-80),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{12,11},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,-1.5f,-95.5f),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{13},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,-1.5f,-0),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{2},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,-1.5f,-16),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{3,4},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,-1.5f,-32),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{5,6},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,-1.5f,-48),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{7,8},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,-1.5f,-64),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{9,10},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,-1.5f,-80),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{11,12},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,-1.5f,-95.5f),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{13},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,-1.5f,-0),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{2},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,-1.5f,-16),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{4,3},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,-1.5f,-32),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{6,5},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,-1.5f,-48),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{8,7},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,-1.5f,-64),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{10,9},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,-1.5f,-80),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{12,11},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,-1.5f,-95.5f),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{13},

                    },
                },
                ["Metro Entrance"] = new List<BoardDescriptor>
                {
                    new BoardDescriptor
                    {
                        m_propName=    "Metro Totem.Metro Totem_Data",
                        m_propPosition= new Vector3(4,0,4),
                        m_propRotation= 0,
                        m_textDescriptors =basicTotem,
                        m_platforms = new int[]{0,1},

                    },
                },
                ["Monorail Station Standalone"] = new List<BoardDescriptor>
                {
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,6,-0.05f),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{0,1},

                    },
                },
                ["Monorail Station Avenue"] = new List<BoardDescriptor>
                {
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0.05f,6,0),
                        m_propRotation= 270,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{0,1},

                    },
                },
                ["Monorail Bus Hub"] = new List<BoardDescriptor>
                {
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0.05f,6,0),
                        m_propRotation= 270,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{0,1},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(29.5f,-1,4),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{6,8,10},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(29.5f,-1,-4),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{6,4,2},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-29.5f,-1,4),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{7,9,11},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-29.5f,-1,-4),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{7,5,3},

                    },
                },
                ["Monorail Train Metro Hub"] = new List<BoardDescriptor>
                {
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,5.5f,-3),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{0},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,5.5f,12),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{2,3},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,5.5f,27),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{5},

                    },
                    new BoardDescriptor
                    {
                        m_propName = "BoardV6.BoardV6_Data",
                        m_propPosition = new Vector3(16, 4f, -0.75f),
                        m_propRotation = 0,
                        m_textDescriptors =basicWallTextDescriptor,
                        m_platforms = new int[]{6},

                    },
                    new BoardDescriptor
                    {
                        m_propName = "BoardV6.BoardV6_Data",
                        m_propPosition = new Vector3(-16, 4f, -0.75f),
                        m_propRotation = 0,
                        m_textDescriptors =basicWallTextDescriptor,
                        m_platforms = new int[]{6},

                    },
                    new BoardDescriptor
                    {
                        m_propName = "BoardV6.BoardV6_Data",
                        m_propPosition = new Vector3(44, 4f, -0.75f),
                        m_propRotation = 0,
                        m_textDescriptors =basicWallTextDescriptor,
                        m_platforms = new int[]{6},

                    },
                    new BoardDescriptor
                    {
                        m_propName = "BoardV6.BoardV6_Data",
                        m_propPosition = new Vector3(-44, 4f, -0.75f),
                        m_propRotation = 0,
                        m_textDescriptors =basicWallTextDescriptor,
                        m_platforms = new int[]{6},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(52,-2.5f-16),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{7,8},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-52,-2.5f-16),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{8,7},
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,-2.5f-16),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{7,8},
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(52,-2.5f-31.5f),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{9},

                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-52,-2.5f-31.5f),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{9}
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,-2.5f-31.5f),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{9}

                    }
                }
            };
        }

        private class LineDescriptor
        {
            public Color m_lineColor;
            public Color m_contrastColor;
            public BasicRenderInformation m_lineName;
            public BasicRenderInformation m_lineNumber;
            public uint m_lastUpdate;
        }

        private struct UpdateFlagsBuildings
        {
            public bool m_nameMesh;
        }

        public class CacheControlTransportBuilding : CacheControl
        {
        }

        public class BoardBunchContainerBuilding : IBoardBunchContainer<CacheControlTransportBuilding, BasicRenderInformation>
        {
            public ushort[][] m_platformToLine;
            public ushort[] m_ordenedLines;
            public uint m_linesUpdateFrame;
        }
    }

}
