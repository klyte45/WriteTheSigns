using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.Packaging;
using Klyte.Commons;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Overrides;
using Klyte.WriteTheSigns.Rendering;
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
        public SimpleXmlDictionary<string, ExportableBuildingGroupDescriptorXml> GlobalDescriptors => WTSBuildingsData.Instance.GlobalDescriptors;

        internal readonly StopInformation[][][] m_platformToLine = new StopInformation[BuildingManager.MAX_BUILDING_COUNT][][];
        private ulong m_lastUpdateLines = SimulationManager.instance.m_currentTickIndex;
        private readonly ulong[] m_buildingLastUpdateLines = new ulong[BuildingManager.MAX_BUILDING_COUNT];
        private ulong[] m_lastDrawBuilding = new ulong[BuildingManager.MAX_BUILDING_COUNT];

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



        public void AfterRenderInstanceImpl(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance renderInstance)
        {
            if (data.m_parentBuilding != 0)
            {
                RenderManager instance = Singleton<RenderManager>.instance;
                if (instance.RequireInstance(data.m_parentBuilding, 1u, out uint num))
                {
                    AfterRenderInstanceImpl(cameraInfo, data.m_parentBuilding, ref BuildingManager.instance.m_buildings.m_buffer[data.m_parentBuilding], layerMask, ref instance.m_instances[num]);
                }
                return;
            }
            if (m_lastDrawBuilding[buildingID] >= SimulationManager.instance.m_currentTickIndex)
            {
                return;
            }
            m_lastDrawBuilding[buildingID] = SimulationManager.instance.m_currentTickIndex;

            string refName = GetReferenceModelName(ref data);
            //if (EditorInstance.component.isVisible && EditorInstance.m_currentBuildingName == refName)
            //{
            //    if (!m_buildingStopsDescriptor.ContainsKey(refName))
            //    {
            //        m_buildingStopsDescriptor[refName] = MapStopPoints(data.Info, Data.Descriptors.ContainsKey(refName) ? Data.Descriptors[refName].StopMappingThresold : 1f);
            //    }
            //    for (int i = 0; i < m_buildingStopsDescriptor[refName].Length; i++)
            //    {
            //        m_onOverlayRenderQueue.Add(Tuple.New(renderInstance.m_dataMatrix1.MultiplyPoint(m_buildingStopsDescriptor[refName][i].platformLine.Position(0.5f)),
            //               m_buildingStopsDescriptor[refName][i].width / 2, m_colorOrder[i % m_colorOrder.Length]));
            //    }
            //}
            ExportableBuildingGroupDescriptorXml expTargetDescriptor = null;
            bool couldGetDescriptor = Data.CityDescriptors.TryGetValue(refName, out BuildingGroupDescriptorXml targetDescriptor) || Data.GlobalDescriptors.TryGetValue(refName, out expTargetDescriptor) || Data.AssetsDescriptors.TryGetValue(refName, out expTargetDescriptor);
            targetDescriptor ??= expTargetDescriptor;
            if (!couldGetDescriptor || targetDescriptor.PropInstances.Length == 0)
            {
                return;
            }

            UpdateLinesBuilding(buildingID, refName, ref targetDescriptor, ref data, ref renderInstance.m_dataMatrix1);
            for (int i = 0; i < targetDescriptor.PropInstances.Length; i++)
            {
                RenderDescriptor(cameraInfo, buildingID, ref data, layerMask, ref renderInstance, ref targetDescriptor, i);
            }
        }

        private void RenderDescriptor(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance renderInstance, ref BuildingGroupDescriptorXml parentDescriptor, int idx)
        {
            BoardDescriptorGeneralXml propLayout = parentDescriptor.GetDescriptorOf(idx);
            if (propLayout?.m_propName == null)
            {
                return;
            }
            ref BoardInstanceBuildingXml targetDescriptor = ref parentDescriptor.PropInstances[idx];
            if (Data.BoardsContainers[buildingID, 0, 0] == null || Data.BoardsContainers[buildingID, 0, 0].Length != parentDescriptor.PropInstances.Length)
            {
                Data.BoardsContainers[buildingID, 0, 0] = new BoardBunchContainerBuilding[parentDescriptor.PropInstances.Length];
            }
            ref BoardBunchContainerBuilding item = ref Data.BoardsContainers[buildingID, 0, 0][idx];

            if (item == null)
            {
                item = new BoardBunchContainerBuilding();
            }
            if (item.m_cachedProp?.name != propLayout.m_propName)
            {
                item.m_cachedProp = null;
            }
            if (item.m_cachedPosition == null || item.m_cachedRotation == null)
            {
                item.m_cachedPosition = renderInstance.m_dataMatrix1.MultiplyPoint(targetDescriptor.PropPosition);
                item.m_cachedRotation = targetDescriptor.PropRotation;
                LogUtils.DoLog($"[B{buildingID}/{idx}]Cached position: {item.m_cachedPosition} | Cached rotation: {item.m_cachedRotation}");
            }
            Vector3 targetPostion = item.m_cachedPosition ?? default;
            for (int i = 0; i <= targetDescriptor.m_arrayRepeatTimes; i++)
            {
                if (i > 0)
                {
                    targetPostion = renderInstance.m_dataMatrix1.MultiplyPoint(targetDescriptor.PropPosition + (i * (Vector3)targetDescriptor.ArrayRepeat));
                }
                RenderSign(ref data, cameraInfo, buildingID, idx, targetPostion, item.m_cachedRotation ?? default, layerMask, propLayout, ref targetDescriptor, ref item.m_cachedProp);

            }
        }

        private void RenderSign(ref Building data, RenderManager.CameraInfo cameraInfo, ushort buildingId, int boardIdx, Vector3 position, Vector3 rotation, int layerMask, BoardDescriptorGeneralXml propLayout, ref BoardInstanceBuildingXml targetDescriptor, ref PropInfo cachedProp)
        {
            WTSPropRenderingRules.RenderPropMesh(ref cachedProp, cameraInfo, buildingId, boardIdx, 0, layerMask, data.m_angle, position, Vector4.zero, ref propLayout.m_propName, rotation, targetDescriptor.PropScale, propLayout, targetDescriptor, out Matrix4x4 propMatrix, out bool rendered, new InstanceID { Building = buildingId });
            if (rendered)
            {
                for (int j = 0; j < propLayout.m_textDescriptors.Length; j++)
                {
                    if (cameraInfo.CheckRenderDistance(position, 200 * propLayout.m_textDescriptors[j].m_textScale * (propLayout.m_textDescriptors[j].ColoringConfig.MaterialType == FontStashSharp.MaterialType.OPAQUE ? 1 : 2)))
                    {
                        MaterialPropertyBlock properties = PropManager.instance.m_materialBlock;
                        properties.Clear();
                        WTSPropRenderingRules.RenderTextMesh(buildingId, boardIdx, 0, targetDescriptor, propMatrix, propLayout, ref propLayout.m_textDescriptors[j], properties, DrawFont);
                    }
                }
            }
        }


        protected void UpdateLinesBuilding(ushort buildingID, string refName, ref BuildingGroupDescriptorXml propDescriptor, ref Building data, ref Matrix4x4 refMatrix)
        {
            if (m_platformToLine[buildingID] == null || (m_buildingLastUpdateLines[buildingID] != m_lastUpdateLines && m_platformToLine[buildingID].Length > 0))
            {
                NetManager nmInstance = NetManager.instance;
                LogUtils.DoLog($"--------------- UpdateLinesBuilding {buildingID}");
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
                            if (inverseMatrix == default)
                            {
                                m_platformToLine[buildingID] = new StopInformation[0][];
                                LogUtils.DoLog("--------------- end UpdateLinesBuilding - inverseMatrix is zero");
                                return;
                            }
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
                                    var result = new StopInformation
                                    {
                                        m_lineId = nmInstance.m_nodes.m_buffer[stopId].m_transportLine,
                                        m_stopId = stopId
                                    };
                                    result.m_destinationId = FindDestinationStop(stopId, result.m_lineId);
                                    float anglePlat = (m_buildingStopsDescriptor[buildingName][i].directionPath.GetAngleXZ() + 360 + angleBuilding) % 360;

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
                                }).ToArray();
                            if (CommonProperties.DebugMode)
                            {
                                LogUtils.DoLog($"NearLines ({i}) = [{string.Join(",", m_platformToLine[buildingID][i].Select(x => x.ToString()).ToArray())}]");
                            }
                        }
                    }
                }
                m_buildingLastUpdateLines[buildingID] = m_lastUpdateLines;
                LogUtils.DoLog($"--------------- end UpdateLinesBuilding {buildingID}");
            }
        }

        protected PathType GetPathType(float angleIn, float anglePass, float angleOut)
        {
            float Δin = (anglePass - angleIn);
            float Δout = (angleOut - anglePass);
            bool inFw = (Δin > 0) == (Math.Abs(Δin) < 180);
            bool outFw = (Δout > 0) == (Math.Abs(Δout) < 180);
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


        public static bool SaveInCommonFolder(string buildingName)
        {
            if (WTSBuildingsData.Instance.CityDescriptors.ContainsKey(buildingName))
            {
                BuildingGroupDescriptorXml item = WTSBuildingsData.Instance.CityDescriptors[buildingName];
                item.BuildingName = buildingName;
                SaveInCommonFolder(new XmlSerializer(typeof(BuildingGroupDescriptorXml)), item, true);
                return true;
            }
            return false;
        }

        private static void SaveInCommonFolder(XmlSerializer serializer, BuildingGroupDescriptorXml item, bool force = false)
        {
            string filePath = WTSController.DefaultBuildingsConfigurationFolder + Path.DirectorySeparatorChar + $"{WTSController.m_defaultFileNameXml}_{item.BuildingName}.xml";
            SaveInPath(serializer, item, force, filePath);
        }
        public static bool SaveInAssetFolder(string buildingName)
        {
            if (WTSBuildingsData.Instance.CityDescriptors.ContainsKey(buildingName))
            {
                BuildingGroupDescriptorXml item = WTSBuildingsData.Instance.CityDescriptors[buildingName];
                Package.Asset asset = PackageManager.FindAssetByName(buildingName);
                if (!(asset == null) && !(asset.package == null))
                {
                    string packagePath = asset.package.packagePath;
                    if (packagePath != null)
                    {
                        string filePath = Path.Combine(Path.GetDirectoryName(packagePath), $"{WTSController.m_defaultFileNameXml}.xml");
                        item.BuildingName = buildingName;
                        SaveInPath(new XmlSerializer(typeof(BuildingGroupDescriptorXml)), item, true, filePath);

                    }
                    return true;
                }
            }
            return false;
        }


        private static void SaveInPath(XmlSerializer serializer, BuildingGroupDescriptorXml item, bool force, string filePath)
        {
            if (force || !File.Exists(filePath))
            {
                FileStream stream = File.OpenWrite(filePath);
                try
                {
                    var ns = new XmlSerializerNamespaces();
                    ns.Add("", "");
                    stream.SetLength(0);
                    serializer.Serialize(stream, item, ns);
                }
                finally
                {
                    stream.Close();
                }
            }
        }

        private static string DefaultFilename { get; } = $"{WTSController.m_defaultFileNameXml}.xml";

        public void LoadAllBuildingConfigurations()
        {
            LogUtils.DoLog("LOADING BUILDING CONFIG START -----------------------------");
            FileUtils.ScanPrefabsFolders<BuildingInfo>(DefaultFilename, LoadDescriptorsFromXmlAsset);
            var errorList = new List<string>();
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


            m_lastDrawBuilding = new ulong[BuildingManager.MAX_BUILDING_COUNT];
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
    }
}
