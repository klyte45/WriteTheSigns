using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Overrides;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.UI;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using SpriteFontPlus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using static Klyte.Commons.Utils.StopSearchUtils;
namespace Klyte.WriteTheSigns.Singleton
{
    public class WTSBuildingPropsSingleton : MonoBehaviour
    {
        public DynamicSpriteFont DrawFont => FontServer.instance[Data.DefaultFont] ?? FontServer.instance[WTSController.DEFAULT_FONT_KEY];
        private readonly Dictionary<string, StopPointDescriptorLanes[]> m_buildingStopsDescriptor = new Dictionary<string, StopPointDescriptorLanes[]>();
        public WTSBuildingsData Data => WTSBuildingsData.Instance;
        public SimpleXmlDictionary<string, BuildingGroupDescriptorXml> CityDescriptors => Data.CityDescriptors;
        public SimpleXmlDictionary<string, ExportableBuildingGroupDescriptorXml> GlobalDescriptors => Data.GlobalDescriptors;
        public SimpleXmlDictionary<string, ExportableBuildingGroupDescriptorXml> AssetsDescriptors => Data.AssetsDescriptors;

        internal StopInformation[][][] m_platformToLine = new StopInformation[BuildingManager.MAX_BUILDING_COUNT][][];
        internal StopInformation[] m_stopInformation = new StopInformation[NetManager.MAX_NODE_COUNT];
        private ulong m_lastUpdateLines = SimulationManager.instance.m_currentTickIndex;
        private readonly ulong[] m_buildingLastUpdateLines = new ulong[BuildingManager.MAX_BUILDING_COUNT];

        public void ResetLines()
        {
            m_platformToLine = new StopInformation[BuildingManager.MAX_BUILDING_COUNT][][];
            m_stopInformation = new StopInformation[NetManager.MAX_NODE_COUNT];
        }

        #region Initialize
        public void Awake() => LoadAllBuildingConfigurations();

        public void Start()
        {
            TransportManagerOverrides.EventOnLineUpdated += OnLineUpdated;
            NetManagerOverrides.EventNodeChanged += OnNodeChanged;
            TransportManager.instance.eventLineColorChanged += OnLineUpdated;
            TransportManagerOverrides.EventOnLineBuildingUpdated += OnBuildingLineChanged;
        }

        private void OnLineUpdated(ushort obj) => m_lastUpdateLines = SimulationManager.instance.m_currentTickIndex;

        private void OnNodeChanged(ushort id)
        {
            ushort buildingId = NetNode.FindOwnerBuilding(id, 56f);
            if (buildingId > 0 && Data.BoardsContainers[buildingId, 0, 0] != null)
            {
                m_buildingLastUpdateLines[buildingId] = 0;
            }
        }


        private void OnBuildingLineChanged(ushort id)
        {
            ushort parentId = id;
            int count = 16;
            while (parentId > 0 && count > 0)
            {
                m_buildingLastUpdateLines[parentId] = 0;

                parentId = BuildingManager.instance.m_buildings.m_buffer[parentId].m_parentBuilding;
                count--;
            }
            if (count == 0)
            {
                LogUtils.DoErrorLog($"INFINITELOOP! {id} {parentId} ");
            }
        }
        #endregion

        public bool CalculateGroupData(ushort buildingID, int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays)
        {
            if (Data.BoardsContainers[buildingID, 0, 0] == null)
            {
                return false;
            }
            bool result = false;
            for (int i = 0; i < Data.BoardsContainers[buildingID, 0, 0].Length; i++)
            {
                BoardBunchContainerBuilding item = Data.BoardsContainers[buildingID, 0, 0][i];

                for (int j = 0; j <= item?.m_cachedArrayRepeatTimes; j++)
                {
                    if (item?.m_cachedProp != null)
                    {
                        if (PropInstance.CalculateGroupData(item.m_cachedProp, layer, ref vertexCount, ref triangleCount, ref objectCount, ref vertexArrays))
                        {
                            result = true;
                        }
                    }
                }
            }
            return result;
        }
        public bool PopulateGroupData(ushort buildingID, int layer, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance)
        {
            if (Data.BoardsContainers[buildingID, 0, 0] == null)
            {
                return false;
            }
            bool result = false;
            float buildingAngle = BuildingManager.instance.m_buildings.m_buffer[buildingID].m_angle * Mathf.Rad2Deg;
            for (int i = 0; i < Data.BoardsContainers[buildingID, 0, 0].Length; i++)
            {
                ref BoardBunchContainerBuilding item = ref Data.BoardsContainers[buildingID, 0, 0][i];
                if (item?.m_cachedProp != null)
                {
                    var targetPosition = item.m_cachedPosition ?? default;
                    for (int j = 0; j <= item.m_cachedArrayRepeatTimes; j++)
                    {
                        if (j > 0)
                        {
                            targetPosition = item.m_cachedMatrix.MultiplyPoint(item.m_cachedOriginalPosition + (j * item.m_cachedArrayItemPace));
                        }
                        WTSDynamicTextRenderingRules.PropInstancePopulateGroupData(item.m_cachedProp, layer, new InstanceID { Building = buildingID }, targetPosition, item.m_cachedScale ?? Vector3.one, (item.m_cachedRotation ?? default) + new Vector3(0, buildingAngle), ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
                    }
                }
            }
            return result;
        }

        public void AfterRenderInstanceImpl(RenderManager.CameraInfo cameraInfo, ushort sourceBuildingId, int layerMask, ref RenderManager.Instance renderInstance)
        {
            ref Building[] bArray = ref BuildingManager.instance.m_buildings.m_buffer;
            ushort parentBuildingId = sourceBuildingId;
            ushort subBuildingIdx = 0;
            while (bArray[parentBuildingId].m_parentBuilding != 0)
            {
                parentBuildingId = bArray[parentBuildingId].m_parentBuilding;
                subBuildingIdx++;
            }

            ref Building data = ref bArray[parentBuildingId];


            string refName = GetReferenceModelName(ref data);
            if ((WTSBuildingLayoutEditor.Instance?.MainContainer?.isVisible ?? false) && (WTSBuildingLayoutEditor.Instance?.IsEditing(refName) ?? false))
            {
                if (!m_buildingStopsDescriptor.ContainsKey(refName))
                {
                    m_buildingStopsDescriptor[refName] = MapStopPoints(data.Info, WTSBuildingLayoutEditor.Instance.GetCurrentMappingThresold());
                }
                for (int i = 0; i < m_buildingStopsDescriptor[refName].Length; i++)
                {
                    m_onOverlayRenderQueue.Add(Tuple.New(renderInstance.m_dataMatrix1.MultiplyPoint(m_buildingStopsDescriptor[refName][i].platformLine.Position(0.5f)),
                           m_buildingStopsDescriptor[refName][i].width / 2, m_colorOrder[i % m_colorOrder.Length]));
                }
            }
            GetTargetDescriptor(refName, out _, out BuildingGroupDescriptorXml targetDescriptor);
            if (subBuildingIdx == 0)
            {
                UpdateLinesBuilding(parentBuildingId, refName, targetDescriptor, ref data, ref renderInstance.m_dataMatrix1);
            }

            if ((targetDescriptor?.PropInstances?.Length ?? 0) == 0)
            {
                return;
            }

            for (int i = 0; i < targetDescriptor.PropInstances.Length; i++)
            {
                if (targetDescriptor.PropInstances[i].SubBuildingPivotReference == subBuildingIdx - 1)
                {
                    RenderDescriptor(cameraInfo, parentBuildingId, ref data, layerMask, ref renderInstance, ref targetDescriptor, i);
                }
            }
        }

        internal static void GetTargetDescriptor(string building, out ConfigurationSource source, out BuildingGroupDescriptorXml target)
        {
            if (building == null)
            {
                source = ConfigurationSource.NONE;
                target = null;
                return;
            }

            if (WTSBuildingsData.Instance.CityDescriptors.ContainsKey(building))
            {
                source = ConfigurationSource.CITY;
                target = WTSBuildingsData.Instance.CityDescriptors[building];
                return;
            }

            if (WTSBuildingsData.Instance.GlobalDescriptors.ContainsKey(building))
            {
                source = ConfigurationSource.GLOBAL;
                target = WTSBuildingsData.Instance.GlobalDescriptors[building];
                return;
            }

            if (WTSBuildingsData.Instance.AssetsDescriptors.ContainsKey(building))
            {
                source = ConfigurationSource.ASSET;
                target = WTSBuildingsData.Instance.AssetsDescriptors[building];
                return;
            }

            source = ConfigurationSource.NONE;
            target = null;

        }

        public static void AfterEndOverlayImpl(RenderManager.CameraInfo cameraInfo)
        {
            if (WTSBuildingLayoutEditor.Instance.MainContainer.isVisible)
            {
                foreach (Tuple<Vector3, float, Color> tuple in WriteTheSignsMod.Controller.BuildingPropsSingleton.m_onOverlayRenderQueue)
                {
                    Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo,
                       tuple.Third,
                       tuple.First,
                       tuple.Second * 2,
                       -1, 1280f, false, true);
                }
                WriteTheSignsMod.Controller.BuildingPropsSingleton.m_onOverlayRenderQueue.Clear();
            }
        }

        private readonly List<Tuple<Vector3, float, Color>> m_onOverlayRenderQueue = new List<Tuple<Vector3, float, Color>>();

        internal static readonly Color[] m_colorOrder = new Color[]
        {
            Color.red,
            Color.Lerp(Color.red, Color.yellow,0.5f),
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.blue,
            Color.Lerp(Color.blue, Color.magenta,0.5f),
            Color.magenta,
            Color.white,
            Color.black,
            Color.Lerp( Color.red,                                    Color.black,0.5f),
            Color.Lerp( Color.Lerp(Color.red, Color.yellow,0.5f),     Color.black,0.5f),
            Color.Lerp( Color.yellow,                                 Color.black,0.5f),
            Color.Lerp( Color.green,                                  Color.black,0.5f),
            Color.Lerp( Color.cyan,                                   Color.black,0.5f),
            Color.Lerp( Color.blue,                                   Color.black,0.5f),
            Color.Lerp( Color.Lerp(Color.blue, Color.magenta,0.5f),   Color.black,0.5f),
            Color.Lerp( Color.magenta,                                Color.black,0.5f),
            Color.Lerp( Color.white,                                  Color.black,0.25f),
            Color.Lerp( Color.white,                                  Color.black,0.75f)
        };

        private void RenderDescriptor(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance renderInstance, ref BuildingGroupDescriptorXml parentDescriptor, int idx)
        {
            ref BoardInstanceBuildingXml targetDescriptor = ref parentDescriptor.PropInstances[idx];
            if (targetDescriptor?.Descriptor == null && targetDescriptor.SimpleProp == null)
            {
                return;
            }
            var targetPropName = targetDescriptor?.Descriptor?.SaveName ?? targetDescriptor.SimpleProp.name;

            if (Data.BoardsContainers[buildingID, 0, 0] == null || Data.BoardsContainers[buildingID, 0, 0].Length != parentDescriptor.PropInstances.Length)
            {
                Data.BoardsContainers[buildingID, 0, 0] = new BoardBunchContainerBuilding[parentDescriptor.PropInstances.Length];
            }
            ref BoardBunchContainerBuilding item = ref Data.BoardsContainers[buildingID, 0, 0][idx];

            if (item == null)
            {
                item = new BoardBunchContainerBuilding();
            }
            if (item.m_cachedProp?.name != targetPropName)
            {
                item.m_cachedProp = null;
            }
            if (item.m_cachedPosition == null || item.m_cachedRotation == null || item.m_cachedScale == null)
            {
                if (targetDescriptor.SubBuildingPivotReference >= 0 && targetDescriptor.SubBuildingPivotReference < data.Info.m_subBuildings.Length)
                {
                    BuildingManager inst = BuildingManager.instance;
                    uint targetBuildingId = buildingID;
                    for (int i = 0; i <= targetDescriptor.SubBuildingPivotReference; i++)
                    {
                        targetBuildingId = inst.m_buildings.m_buffer[targetBuildingId].m_subBuilding;
                    }
                    if (RenderManager.instance.RequireInstance(targetBuildingId, 1u, out uint num))
                    {
                        item.m_cachedMatrix = RenderManager.instance.m_instances[num].m_dataMatrix1;
                    }
                    else
                    {
                        item.m_cachedMatrix = renderInstance.m_dataMatrix1;
                    }
                }
                else
                {
                    item.m_cachedMatrix = renderInstance.m_dataMatrix1;
                }
                item.m_cachedPosition = item.m_cachedMatrix.MultiplyPoint(targetDescriptor.PropPosition);
                item.m_cachedOriginalPosition = targetDescriptor.PropPosition;
                item.m_cachedRotation = targetDescriptor.PropRotation;
                item.m_cachedScale = targetDescriptor.PropScale;
                item.m_cachedArrayRepeatTimes = targetDescriptor.ArrayRepeatTimes;
                item.m_cachedArrayItemPace = targetDescriptor.ArrayRepeat;
                targetDescriptor.OnChangeMatrixData();
            }
            Vector3 targetPostion = item.m_cachedPosition ?? default;
            for (int i = 0; i <= targetDescriptor.ArrayRepeatTimes; i++)
            {
                if (i > 0)
                {
                    targetPostion = item.m_cachedMatrix.MultiplyPoint(targetDescriptor.PropPosition + (i * (Vector3)targetDescriptor.ArrayRepeat));
                }
                RenderSign(ref data, cameraInfo, buildingID, idx, targetPostion, item.m_cachedRotation ?? default, layerMask, ref targetDescriptor, ref item.m_cachedProp);
                if (i == 0 && (WTSBuildingLayoutEditor.Instance?.MainContainer?.isVisible ?? false) && WTSBuildingLayoutEditor.Instance.LockSelection && (WTSBuildingLayoutEditor.Instance?.CurrentBuildingId == buildingID) && WTSBuildingLayoutEditor.Instance.LayoutList.SelectedIndex == idx)
                {
                    ToolsModifierControl.cameraController.m_targetPosition = targetPostion;
                }
            }
        }

        private void RenderSign(ref Building data, RenderManager.CameraInfo cameraInfo, ushort buildingId, int boardIdx, Vector3 position, Vector3 rotation, int layerMask, ref BoardInstanceBuildingXml targetDescriptor, ref PropInfo cachedProp)
        {
            var isSimple = targetDescriptor.Descriptor == null;

            var propname = isSimple ? targetDescriptor.m_simplePropName : targetDescriptor.Descriptor.m_propName;

            Color parentColor = WTSDynamicTextRenderingRules.RenderPropMesh(ref cachedProp, cameraInfo, buildingId, boardIdx, 0, layerMask, data.m_angle, position, Vector4.zero, ref propname, rotation, targetDescriptor.PropScale, targetDescriptor.Descriptor, targetDescriptor, out Matrix4x4 propMatrix, out bool rendered, new InstanceID { Building = buildingId });

            (isSimple ? ref targetDescriptor.m_simplePropName : ref targetDescriptor.Descriptor.m_propName) = propname;

            if (rendered && !isSimple)
            {
                var propLayout = targetDescriptor.Descriptor;
                for (int j = 0; j < propLayout.TextDescriptors.Length; j++)
                {
                    if (cameraInfo.CheckRenderDistance(position, 200 * propLayout.TextDescriptors[j].m_textScale * targetDescriptor.PropScale.magnitude * (propLayout.TextDescriptors[j].IlluminationConfig.IlluminationType == FontStashSharp.MaterialType.OPAQUE ? 1 : 2)))
                    {
                        MaterialPropertyBlock properties = PropManager.instance.m_materialBlock;
                        properties.Clear();
                        WTSDynamicTextRenderingRules.RenderTextMesh(buildingId, boardIdx, 0, targetDescriptor, propMatrix, propLayout, ref propLayout.TextDescriptors[j], properties, (int)data.m_flags, parentColor, data.Info);
                    }
                }
            }
        }
        internal void UpdateLinesBuilding(ushort buildingID, ref Building data, ref Matrix4x4 refMatrix)
        {
            if (m_platformToLine[buildingID] == null || (m_buildingLastUpdateLines[buildingID] != m_lastUpdateLines && m_platformToLine[buildingID].Length > 0))
            {
                var refName = GetReferenceModelName(ref data);
                UpdateLinesBuilding(buildingID, refName, null, ref data, ref refMatrix);
            }
        }
        private void UpdateLinesBuilding(ushort buildingID, string refName, BuildingGroupDescriptorXml propDescriptor, ref Building data, ref Matrix4x4 refMatrix)
        {
            if ((data.Info.m_buildingAI is TransportStationAI || data.Info.m_buildingAI is OutsideConnectionAI) && (m_platformToLine[buildingID] == null || (m_buildingLastUpdateLines[buildingID] != m_lastUpdateLines && m_platformToLine[buildingID].Length > 0)))
            {

                NetManager nmInstance = NetManager.instance;
                LogUtils.DoLog($"--------------- UpdateLinesBuilding {buildingID}");

                m_platformToLine[buildingID]?.ForEach(x => x?.ForEach(x => m_stopInformation[x.m_stopId] = default));
                m_platformToLine[buildingID] = null;
                if (!m_buildingStopsDescriptor.ContainsKey(refName))
                {
                    m_buildingStopsDescriptor[refName] = MapStopPoints(data.Info, propDescriptor?.StopMappingThresold ?? 1f);

                }

                var platforms = m_buildingStopsDescriptor[refName].Select((v, i) => new { Key = i, Value = v }).ToDictionary(o => o.Key, o => o.Value);

                if (platforms.Count == 0)
                {
                    m_platformToLine[buildingID] = new StopInformation[0][];
                }
                else
                {

                    var boundaries = new List<Quad2>();
                    ushort subBuilding = buildingID;
                    var allnodes = new List<ushort>();
                    while (subBuilding > 0)
                    {
                        boundaries.Add(GetBounds(ref BuildingManager.instance.m_buildings.m_buffer[subBuilding]));
                        ushort node = BuildingManager.instance.m_buildings.m_buffer[subBuilding].m_netNode;
                        while (node > 0)
                        {
                            allnodes.Add(node);
                            node = nmInstance.m_nodes.m_buffer[node].m_nextBuildingNode;
                        }
                        subBuilding = BuildingManager.instance.m_buildings.m_buffer[subBuilding].m_subBuilding;
                    }
                    foreach (ushort node in allnodes)
                    {
                        if (!boundaries.Any(x => x.Intersect(nmInstance.m_nodes.m_buffer[node].m_position)))
                        {
                            for (int segIdx = 0; segIdx < 8; segIdx++)
                            {
                                ushort segmentId = nmInstance.m_nodes.m_buffer[node].GetSegment(segIdx);
                                if (segmentId != 0 && allnodes.Contains(nmInstance.m_segments.m_buffer[segmentId].GetOtherNode(node)))
                                {


                                    boundaries.Add(GetBounds(
                                        nmInstance.m_nodes.m_buffer[nmInstance.m_segments.m_buffer[segmentId].m_startNode].m_position,
                                        nmInstance.m_nodes.m_buffer[nmInstance.m_segments.m_buffer[segmentId].m_endNode].m_position,
                                        nmInstance.m_segments.m_buffer[segmentId].Info.m_halfWidth)
                                        );
                                }
                            }
                        }
                    }
                    List<ushort> nearStops = StopSearchUtils.FindNearStops(data.m_position, ItemClass.Service.PublicTransport, ItemClass.Service.PublicTransport, VehicleInfo.VehicleType.None, true, 400f, out _, out _, boundaries);

                    m_platformToLine[buildingID] = new StopInformation[m_buildingStopsDescriptor[refName].Length][];
                    if (nearStops.Count > 0)
                    {

                        if (CommonProperties.DebugMode)
                        {
                            LogUtils.DoLog($"[{InstanceManager.instance.GetName(new InstanceID { Building = buildingID })}] nearStops = [\n\t\t{string.Join(",\n\t\t", nearStops.Select(x => $"[{x} => {nmInstance.m_nodes.m_buffer[x].m_position} (TL { nmInstance.m_nodes.m_buffer[x].m_transportLine} => [{TransportManager.instance.m_lines.m_buffer[nmInstance.m_nodes.m_buffer[x].m_transportLine].Info.m_transportType}-{TransportManager.instance.m_lines.m_buffer[nmInstance.m_nodes.m_buffer[x].m_transportLine].m_lineNumber}] {TransportManager.instance.GetLineName(nmInstance.m_nodes.m_buffer[x].m_transportLine)} )]").ToArray())}\n\t] ");
                        }
                        string buildingName = refName;
                        for (int i = 0; i < m_buildingStopsDescriptor[buildingName].Length; i++)
                        {
                            Matrix4x4 inverseMatrix = refMatrix.inverse;
                            float maxDist = m_buildingStopsDescriptor[buildingName][i].width;
                            float maxHeightDiff = m_buildingStopsDescriptor[buildingName][i].width;
                            if (CommonProperties.DebugMode)
                            {
                                LogUtils.DoLog($"platLine ({i}) = {m_buildingStopsDescriptor[buildingName][i].platformLine.a} {m_buildingStopsDescriptor[buildingName][i].platformLine.b} {m_buildingStopsDescriptor[buildingName][i].platformLine.c} {m_buildingStopsDescriptor[buildingName][i].platformLine.d}");
                                LogUtils.DoLog($"maxDist ({i}) = {maxDist}");
                                LogUtils.DoLog($"maxHeightDiff ({i}) = {maxHeightDiff}");
                                LogUtils.DoLog($"refMatrix ({i}) = {refMatrix}");
                                LogUtils.DoLog($"inverseMatrix ({i}) = {inverseMatrix}");
                            }
                            if (inverseMatrix == default)
                            {
                                LogUtils.DoWarnLog($"--------------- end UpdateLinesBuilding - inverseMatrix is zero (ID: {buildingID}) \n{Environment.StackTrace}");
                                return;
                            }
                            float angleBuilding = data.m_angle * Mathf.Rad2Deg;
                            m_platformToLine[buildingID][i] = nearStops
                                .Where(stopId =>
                                {
                                    Vector3 relPos = inverseMatrix.MultiplyPoint(nmInstance.m_nodes.m_buffer[stopId].m_position);
                                    float dist = m_buildingStopsDescriptor[buildingName][i].platformLine.DistanceSqr(relPos, out _);
                                    float diffY = Mathf.Abs(relPos.y - m_buildingStopsDescriptor[buildingName][i].platformLine.Position(0.5f).y);
                                    if (CommonProperties.DebugMode)
                                    {
                                        LogUtils.DoLog($"stop {stopId} => relPos = {relPos}; dist = {dist}; diffY = {diffY}");
                                    }

                                    return dist < maxDist && diffY < maxHeightDiff;
                                })
                                .Select(stopId =>
                                {
                                    float anglePlat = (m_buildingStopsDescriptor[buildingName][i].directionPath.GetAngleXZ() + 360 + angleBuilding) % 360;
                                    return UpdateStopInformation(stopId, buildingName, i, anglePlat, nmInstance);
                                }).ToArray();
                            if (CommonProperties.DebugMode)
                            {
                                LogUtils.DoLog($"NearLines ({i}) = [{string.Join(",", m_platformToLine[buildingID][i].Select(x => x.ToString()).ToArray())}]");
                            }
                        }
                    }
                }
                if (m_platformToLine[buildingID]?.Length > 0)
                {
                    m_platformToLine[buildingID]?.ForEach(x => x?.ForEach(x => m_stopInformation[x.m_stopId] = x));
                }

                m_buildingLastUpdateLines[buildingID] = m_lastUpdateLines;
                LogUtils.DoLog($"--------------- end UpdateLinesBuilding {buildingID}");
            }
        }

        private StopInformation UpdateStopInformation(ushort stopId, string buildingName = null, int i = 0, float anglePlat = 0, NetManager nmInstance = null)
        {
            nmInstance ??= NetManager.instance;
            var result = new StopInformation
            {
                m_lineId = nmInstance.m_nodes.m_buffer[stopId].m_transportLine,
                m_stopId = stopId
            };
            result.m_destinationId = FindDestinationStop(stopId, result.m_lineId);

            ref NetSegment[] segBuffer = ref nmInstance.m_segments.m_buffer;

            ushort segmentPrev = nmInstance.m_nodes.m_buffer[stopId].GetSegment(0);
            ushort segmentNext = nmInstance.m_nodes.m_buffer[stopId].GetSegment(1);
            if (segmentPrev != 0 && segmentNext != 0)
            {
                ushort nextStop = TransportLine.GetNextStop(stopId);
                if (segBuffer[segmentPrev].GetOtherNode(stopId) == nextStop)
                {
                    ushort next = segmentPrev;
                    segmentPrev = segmentNext;
                    segmentNext = next;
                }
                float angleDirPrev, angleDirNext;
                PathUnit.Position pathPrev;
                PathUnit.Position pathnext;
                if (segBuffer[segmentPrev].m_startNode == stopId)
                {
                    pathPrev = PathManager.instance.m_pathUnits.m_buffer[nmInstance.m_segments.m_buffer[segmentPrev].m_path].m_position01;
                }
                else
                {
                    PathManager.instance.m_pathUnits.m_buffer[nmInstance.m_segments.m_buffer[segmentPrev].m_path].GetLast2Positions(out pathPrev, out _);
                }
                if (segBuffer[segmentNext].m_startNode == stopId)
                {
                    pathnext = PathManager.instance.m_pathUnits.m_buffer[nmInstance.m_segments.m_buffer[segmentNext].m_path].m_position01;
                }
                else
                {
                    PathManager.instance.m_pathUnits.m_buffer[nmInstance.m_segments.m_buffer[segmentPrev].m_path].GetLast2Positions(out pathnext, out _);
                }
                angleDirPrev = ((segBuffer[pathPrev.m_segment].GetBezier().Position(pathPrev.m_offset / 255f) - nmInstance.m_nodes.m_buffer[stopId].m_position).GetAngleXZ() + 360) % 360;
                angleDirNext = ((segBuffer[pathnext.m_segment].GetBezier().Position(pathnext.m_offset / 255f) - nmInstance.m_nodes.m_buffer[stopId].m_position).GetAngleXZ() + 360) % 360;
                float diff = Mathf.Abs(angleDirPrev - angleDirNext);
                float diffPlat = Mathf.Abs(anglePlat - ((angleDirPrev + angleDirNext) / 2)) % 180;
                if (CommonProperties.DebugMode)
                {
                    LogUtils.DoLog($"ANGLE COMPARISON: diff = {diff} | diffPlat = {diffPlat} | PLAT = {anglePlat} | DIR IN = {angleDirPrev} | DIR OUT = {angleDirNext} | ({buildingName} =>  P[{i}] | L = {nmInstance.m_nodes.m_buffer[stopId].m_transportLine} )");
                }

                switch (GetPathType(angleDirPrev, anglePlat, angleDirNext))
                {
                    case PathType.FORWARD:
                        result.m_nextStopId = nextStop;
                        result.m_previousStopId = TransportLine.GetPrevStop(stopId);
                        break;
                    case PathType.BACKWARD_FORWARD:
                        result.m_nextStopId = nextStop;
                        break;
                    case PathType.FORWARD_BACKWARD:
                        result.m_previousStopId = nextStop;
                        break;
                    case PathType.BACKWARD:
                        result.m_previousStopId = nextStop;
                        result.m_nextStopId = TransportLine.GetPrevStop(stopId);
                        break;
                }

                if (result.m_destinationId == 0)
                {
                    result.m_destinationId = nextStop;
                }
            }
            return result;
        }

        protected PathType GetPathType(float angleIn, float anglePass, float angleOut)
        {
            float Δin = (anglePass - angleIn + 720) % 360;
            float Δout = (angleOut - anglePass + 720) % 360;
            bool inFw = (Δin < 180);
            bool outFw = (Δout < 180);
            LogUtils.DoLog($"GetPathType [{angleIn}|{anglePass}|{angleOut}]  Δin = {Δin}, Δout ={Δout}, infw = {inFw}, outFw = {outFw}");
            return (PathType)((inFw ? 0 : 1) + (outFw ? 0 : 2));
        }

        protected enum PathType
        {
            FORWARD,
            BACKWARD_FORWARD,
            FORWARD_BACKWARD,
            BACKWARD,
        }


        protected Quad2 GetBounds(ref Building data)
        {
            int width = data.Width;
            int length = data.Length;
            var vector = new Vector2(Mathf.Cos(data.m_angle), Mathf.Sin(data.m_angle));
            var vector2 = new Vector2(vector.y, -vector.x);
            vector *= width * 4f;
            vector2 *= length * 4f;
            Vector2 a = VectorUtils.XZ(data.m_position);
            var quad = default(Quad2);
            quad.a = a - vector - vector2;
            quad.b = a + vector - vector2;
            quad.c = a + vector + vector2;
            quad.d = a - vector + vector2;
            return quad;
        }
        protected Quad2 GetBounds(Vector3 ref1, Vector3 ref2, float halfWidth)
        {
            Vector2 ref1v2 = VectorUtils.XZ(ref1);
            Vector2 ref2v2 = VectorUtils.XZ(ref2);
            float halfLength = (ref1v2 - ref2v2).magnitude / 2;
            Vector2 center = (ref1v2 + ref2v2) / 2;
            float angle = Vector2.Angle(ref1v2, ref2v2);


            var vector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var vector2 = new Vector2(vector.y, -vector.x);
            vector *= halfWidth;
            vector2 *= halfLength;
            var quad = default(Quad2);
            quad.a = center - vector - vector2;
            quad.b = center + vector - vector2;
            quad.c = center + vector + vector2;
            quad.d = center - vector + vector2;
            return quad;
        }
        private ushort FindDestinationStop(ushort stopId, ushort lineId)
        {
            if (m_allowedTypesNextPreviousStations.Contains(TransportManager.instance.m_lines.m_buffer[NetManager.instance.m_nodes.m_buffer[stopId].m_transportLine].Info.m_transportType))
            {
                ushort prevStop;
                ushort curStop = stopId;
                ushort nextStop = TransportLine.GetNextStop(curStop);
                int stopCount = 0;
                do
                {
                    prevStop = curStop;
                    curStop = nextStop;
                    nextStop = TransportLine.GetNextStop(curStop);
                    if (WTSBuildingDataCaches.GetStopBuilding(nextStop, lineId) == WTSBuildingDataCaches.GetStopBuilding(prevStop, lineId))
                    {
                        return curStop;
                    }
                    if (nextStop == 0)
                    {
                        LogUtils.DoLog($"broken line: { NetManager.instance.m_nodes.m_buffer[nextStop].m_transportLine}");
                        return 0;
                    }
                    if (nextStop == stopId)
                    {
                        LogUtils.DoLog($"Thats a loop line: { NetManager.instance.m_nodes.m_buffer[nextStop].m_transportLine}");
                        return 0;
                    }
                    stopCount++;
                } while (stopCount < 9999);

            }
            return 0;
        }
        private readonly TransportInfo.TransportType[] m_allowedTypesNextPreviousStations =
    {
            TransportInfo.TransportType.Metro,
            TransportInfo.TransportType.Monorail,
            TransportInfo.TransportType.Train,
            TransportInfo.TransportType.Ship,
        };

        public static string GetReferenceModelName(ref Building data)
        {
            string refName = data.Info.name;
            if (refName.Contains("_XANALOGX_"))
            {
                refName = refName.Substring(0, refName.LastIndexOf("_XANALOGX_"));
            }

            return refName;
        }


        private static string DefaultFilename { get; } = $"{WTSController.m_defaultFileNameBuildingsXml}.xml";

        public void LoadAllBuildingConfigurations()
        {
            LogUtils.DoLog("LOADING BUILDING CONFIG START -----------------------------");
            FileUtils.ScanPrefabsFolders<BuildingInfo>(DefaultFilename, LoadDescriptorsFromXmlAsset);
            var errorList = new List<string>();
            Data.GlobalDescriptors.Clear();
            Data.AssetsDescriptors.Clear();
            LogUtils.DoLog($"DefaultBuildingsConfigurationFolder = {WTSController.DefaultBuildingsConfigurationFolder}");
            foreach (string filename in Directory.GetFiles(WTSController.DefaultBuildingsConfigurationFolder, "*.xml"))
            {
                try
                {
                    if (CommonProperties.DebugMode)
                    {
                        LogUtils.DoLog($"Trying deserialize {filename}:\n{File.ReadAllText(filename)}");
                    }
                    using FileStream stream = File.OpenRead(filename);
                    LoadDescriptorsFromXmlCommon(stream, null);
                }
                catch (Exception e)
                {
                    LogUtils.DoWarnLog($"Error Loading file \"{filename}\" ({e.GetType()}): {e.Message}\n{e}");
                    errorList.Add($"Error Loading file \"{filename}\" ({e.GetType()}): {e.Message}");
                }
            }

            if (errorList.Count > 0)
            {
                K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                {
                    title = "WTS - Errors loading Files",
                    message = string.Join("\r\n", errorList.ToArray()),
                    useFullWindowWidth = true,
                    showButton1 = true,
                    textButton1 = "Okay...",
                    showClose = true

                }, (x) => true);

            }

            Data.CleanCache();

            LogUtils.DoLog("LOADING BUILDING CONFIG END -----------------------------");
        }

        private void LoadDescriptorsFromXmlCommon(FileStream stream, BuildingInfo info) => LoadDescriptorsFromXml(stream, info, ref Data.GlobalDescriptors);
        private void LoadDescriptorsFromXmlAsset(FileStream stream, BuildingInfo info) => LoadDescriptorsFromXml(stream, info, ref Data.AssetsDescriptors);
        private void LoadDescriptorsFromXml(FileStream stream, BuildingInfo info, ref SimpleXmlDictionary<string, ExportableBuildingGroupDescriptorXml> referenceDic)
        {
            var serializer = new XmlSerializer(typeof(ExportableBuildingGroupDescriptorXml));

            LogUtils.DoLog($"trying deserialize: {info}");

            if (serializer.Deserialize(stream) is ExportableBuildingGroupDescriptorXml config)
            {
                if (info != null)
                {
                    string[] propEffName = info.name.Split(".".ToCharArray(), 2);
                    string[] xmlEffName = config.BuildingName.Split(".".ToCharArray(), 2);
                    if (propEffName.Length == 2 && xmlEffName.Length == 2 && xmlEffName[1] == propEffName[1])
                    {
                        config.BuildingName = info.name;
                    }
                }
                else if (config.BuildingName == null)
                {
                    throw new Exception("Building name not set at file!!!!");
                }
                referenceDic[config.BuildingName] = config;
            }
            else
            {
                throw new Exception("The file wasn't recognized as a valid descriptor!");
            }
        }
        public void MarkLinesDirty() => m_lastUpdateLines = SimulationManager.instance.m_currentTickIndex;
        public static StopPointDescriptorLanes[] GetStopPointsDescriptorFor(string building)
        {
            var instance = WriteTheSignsMod.Controller.BuildingPropsSingleton;
            if (!instance.m_buildingStopsDescriptor.ContainsKey(building))
            {
                GetTargetDescriptor(building, out _, out BuildingGroupDescriptorXml target);
                instance.m_buildingStopsDescriptor[building] = MapStopPoints(PrefabCollection<BuildingInfo>.FindLoaded(building), target?.StopMappingThresold ?? 1f);
            }
            return instance.m_buildingStopsDescriptor[building];
        }
    }
}
