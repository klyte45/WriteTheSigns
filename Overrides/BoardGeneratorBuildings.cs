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

    public class BoardGeneratorBuildings : BoardGeneratorParent<BoardGeneratorBuildings, BoardBunchContainer, CacheControl, BasicRenderInformation, BoardDescriptor, BoardTextDescriptor>
    {


        private Dictionary<String, List<BoardDescriptor>> loadedDescriptors;

        private UpdateFlagsBuildings[] m_updateData;
        public bool[] m_updatedIdsColorsLines;

        public override int ObjArraySize => BuildingManager.MAX_BUILDING_COUNT;

        #region Initialize
        public override void Initialize()
        {
            m_updateData = new UpdateFlagsBuildings[BuildingManager.MAX_BUILDING_COUNT];
            m_updatedIdsColorsLines = new bool[BuildingManager.MAX_BUILDING_COUNT];
            loadedDescriptors = GenerateDefaultDictionary();


            TransportManagerOverrides.eventOnLineUpdated += onLineUpdated;
            TransportManager.instance.eventLineColorChanged += (x) => onLineUpdated();
            BuildingManagerOverrides.eventOnBuildingRenamed += onBuildingNameChanged;

            #region Hooks
            //var postRenderMeshs = GetType().GetMethod("AfterRenderMeshes", allFlags);
            //doLog($"Patching=> {postRenderMeshs}");
            //AddRedirect(typeof(BuildingAI).GetMethod("RenderMeshes", allFlags), null, postRenderMeshs);
            #endregion
        }

        protected override void OnTextureRebuilt()
        {
            m_updateData = new UpdateFlagsBuildings[BuildingManager.MAX_BUILDING_COUNT];
        }

        protected void Reset()
        {
            m_boardsContainers = new BoardBunchContainer[BuildingManager.MAX_BUILDING_COUNT];
            m_updateData = new UpdateFlagsBuildings[BuildingManager.MAX_BUILDING_COUNT];
            m_updatedIdsColorsLines = new bool[BuildingManager.MAX_BUILDING_COUNT];
        }

        private void onLineUpdated()
        {
            doLog("onLineUpdated");
            m_updatedIdsColorsLines = new bool[BuildingManager.MAX_BUILDING_COUNT];
        }
        private void onBuildingNameChanged(ushort id)
        {
            doLog("onBuildingNameChanged");
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
                m_boardsContainers[buildingID] = new BoardBunchContainer();
            }
            if (m_boardsContainers[buildingID]?.m_boardsData?.Count() != loadedDescriptors[data.Info.name].Count)
            {
                m_boardsContainers[buildingID].m_boardsData = new CacheControl[loadedDescriptors[data.Info.name].Count];
                m_updateData[buildingID].m_nameMesh = false;
                m_updatedIdsColorsLines[buildingID] = false;
            }

            var updatedColors = m_updatedIdsColorsLines[buildingID];
            for (var i = 0; i < loadedDescriptors[data.Info.name].Count; i++)
            {
                var descriptor = loadedDescriptors[data.Info.name][i];
                UpdateSubparams(ref m_boardsContainers[buildingID].m_boardsData[i], buildingID, ref data, cameraInfo, ref renderInstance, descriptor, updatedColors, i);

                RenderPropMesh(cameraInfo, buildingID, data.m_angle, layerMask, renderInstance.m_dataMatrix1.MultiplyPoint(descriptor.m_propPosition), renderInstance.m_dataVector3, i, 0, ref descriptor.m_propName, descriptor.m_propRotation, out Matrix4x4 propMatrix);

                for (int j = 0; j < descriptor.m_textDescriptors.Length; j++)
                {
                    RenderTextMesh(cameraInfo, buildingID, 0, ref descriptor, propMatrix, ref descriptor.m_textDescriptors[j], ref m_boardsContainers[buildingID].m_boardsData[i]);
                }
            }
            m_updatedIdsColorsLines[buildingID] = true;
        }



        #region Upadate Data
        protected override BasicRenderInformation GetOwnNameMesh(ushort buildingID, int secIdx)
        {
            if (m_boardsContainers[buildingID].m_nameSubInfo == null || !m_updateData[buildingID].m_nameMesh)
            {
                RefreshNameData(ref m_boardsContainers[buildingID].m_nameSubInfo, BuildingManager.instance.GetBuildingName(buildingID, new InstanceID()) ?? "DUMMY!!!!!");
                m_updateData[buildingID].m_nameMesh = true;
            }
            return m_boardsContainers[buildingID].m_nameSubInfo;

        }

        protected void UpdateSubparams(ref CacheControl ctrl, ushort buildingID, ref Building data, RenderManager.CameraInfo cameraInfo, ref RenderManager.Instance instanceData, BoardDescriptor descriptor, bool updatedIdsColors, int idx)
        {
            BuildingManager instance = Singleton<BuildingManager>.instance;
            if (ctrl == null) ctrl = new CacheControl();
            if (!updatedIdsColors)
            {
                Color color;
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
                var nearStops = KlyteUtils.FindNearStops(instanceData.m_dataMatrix1.MultiplyPoint(descriptor.m_propPosition), data.Info.m_class.m_service, data.Info.m_class.m_service, descriptor.m_targetVehicle ?? VehicleInfo.VehicleType.None, true, 400f, out List<float> dist, boundaries);
                //doLog($"updatedIdsColors {nearStops.Count} [{string.Join(",", nearStops.Select(x => x.ToString()).ToArray())}], [{string.Join(",", dist.Select(x => x.ToString()).ToArray())}], ");
                if (nearStops.Count > 0)
                {
                    var effNearStopId = nearStops[dist.IndexOf(dist.Min(x => x))];
                    var stopPos = NetManager.instance.m_nodes.m_buffer[effNearStopId].m_position;
                    color = TransportManager.instance.GetLineColor(NetManager.instance.m_nodes.m_buffer[effNearStopId].m_transportLine);
                }
                else
                {
                    color = Color.white;
                }
                ctrl.m_cachedColor = color;
                ctrl.m_cachedContrastColor = DTBUtils.contrastColor(color);
            }
            return;
        }
        #endregion

        public override Color GetColor(ushort buildingID, int idx, int secIdx)
        {
            return m_boardsContainers[buildingID].m_boardsData[idx]?.m_cachedColor ?? Color.white;
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
                                m_textRelativePosition = new Vector3(0,4.3f, -0.11f) ,
                                m_textRelativeRotation = Vector3.zero,
                                m_maxWidthMeters = 15.5f
                             },
                             new BoardTextDescriptor{
                                m_textRelativePosition = new Vector3(0,4.3f,0),
                                m_textRelativeRotation = new Vector3(0,180,0),
                                m_maxWidthMeters = 15.5f
                             },
                        };
            var basicWallTextDescriptor = new BoardTextDescriptor[]{
                             new BoardTextDescriptor{
                                m_textRelativePosition =new Vector3(0,0,-0.05f) ,
                                m_textRelativeRotation = Vector3.zero,
                                m_maxWidthMeters = 15.5f
                             },
                        };

            return new Dictionary<string, List<BoardDescriptor>>
            {
                ["Train Station"] = new List<BoardDescriptor>
                {
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6.BoardV6_Data",
                        m_propPosition= new Vector3(11.5f,3.75f,0),
                        m_propRotation= 0,
                        m_textDescriptors =basicWallTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6.BoardV6_Data",
                        m_propPosition= new Vector3(-11.5f,3.75f,0),
                        m_propRotation= 0,
                        m_textDescriptors =basicWallTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6.BoardV6_Data",
                        m_propPosition= new Vector3(0,5f,-16),
                        m_propRotation= 180,
                        m_textDescriptors =basicWallTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName = "BoardV6.BoardV6_Data",
                        m_propPosition = new Vector3(-14, 8f, 22),
                        m_propRotation = 180,
                        m_textDescriptors =basicWallTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                },
                ["End of the line Trainstation"] = new List<BoardDescriptor>
                {
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(48,1,-48),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(32,1,-48),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(16,1,-48),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,1,-48),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-48,1,-48),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-32,1,-48),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-16,1,-48),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },

                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(48,1,-80),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(32,1,-80),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(16,1,-80),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,1,-80),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-48,1,-80),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-32,1,-80),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-16,1,-80),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                     new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(48,1,-106),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(32,1,-106),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(16,1,-106),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,1,-106),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-48,1,-106),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-32,1,-106),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-16,1,-106),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                     new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(48,1,-133),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(32,1,-133),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(16,1,-133),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,1,-133),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-48,1,-133),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-32,1,-133),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-16,1,-133),
                        m_propRotation= 90,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                },
                ["Large Trainstation"] = new List<BoardDescriptor>
                {
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,1,-0),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,1,-16),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,1,-32),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,1,-48),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,1,-64),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,1,-80),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,1,-95.5f),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,1,-0),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,1,-16),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,1,-32),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,1,-48),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,1,-64),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,1,-80),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,1,-95.5f),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,1,-0),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,1,-16),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,1,-32),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,1,-48),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,1,-64),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,1,-80),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,1,-95.5f),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,1,-0),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,1,-16),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,1,-32),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,1,-48),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,1,-64),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,1,-80),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,1,-95.5f),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                },
                ["Metro Entrance"] = new List<BoardDescriptor>
                {
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,0,-0),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Metro
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
                        m_targetVehicle = VehicleInfo.VehicleType.Monorail
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
                        m_targetVehicle = VehicleInfo.VehicleType.Monorail
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
                        m_targetVehicle = VehicleInfo.VehicleType.Monorail
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(29.5f,-1,4),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Car
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(29.5f,-1,-4),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Car
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-29.5f,-1,4),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Car
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-29.5f,-1,-4),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Car
                    },
                },
                ["Monorail Train Metro Hub"] = new List<BoardDescriptor>
                {
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,8,-3),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Monorail
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,8,12),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Monorail
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,8,27),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Monorail
                    },
                    new BoardDescriptor
                    {
                        m_propName = "BoardV6.BoardV6_Data",
                        m_propPosition = new Vector3(16, 4f, -0.75f),
                        m_propRotation = 0,
                        m_textDescriptors =basicWallTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName = "BoardV6.BoardV6_Data",
                        m_propPosition = new Vector3(-16, 4f, -0.75f),
                        m_propRotation = 0,
                        m_textDescriptors =basicWallTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName = "BoardV6.BoardV6_Data",
                        m_propPosition = new Vector3(44, 4f, -0.75f),
                        m_propRotation = 0,
                        m_textDescriptors =basicWallTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName = "BoardV6.BoardV6_Data",
                        m_propPosition = new Vector3(-44, 4f, -0.75f),
                        m_propRotation = 0,
                        m_textDescriptors =basicWallTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(52,0,-16),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-52,0,-16),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,0,-16),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(52,0,-31.5f),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(-52,0,-31.5f),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    },
                    new BoardDescriptor
                    {
                        m_propName=    "BoardV6plat.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,0,-31.5f),
                        m_propRotation= 0,
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_targetVehicle = VehicleInfo.VehicleType.Train
                    }
                }
            };
        }



        private struct UpdateFlagsBuildings
        {
            public bool m_nameMesh;
            public bool m_streetMesh;
            public bool m_streetPrefixMesh;
            public bool m_streetSuffixMesh;
            public bool m_streetNumberMesh;
        }
    }

}
