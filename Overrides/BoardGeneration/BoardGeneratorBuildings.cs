using ColossalFramework;
using ColossalFramework.DataBinding;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.Packaging;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.UI;
using Klyte.DynamicTextProps.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using static Klyte.Commons.Utils.StopSearchUtils;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorBuildings;

namespace Klyte.DynamicTextProps.Overrides
{

    public partial class BoardGeneratorBuildings : BoardGeneratorParent<BoardGeneratorBuildings, BoardBunchContainerBuilding, CacheControl, BasicRenderInformation, BoardDescriptorBuildingXml, BoardTextDescriptorBuildingsXml>
    {

        public Dictionary<string, UIFont> m_fontCache = new Dictionary<string, UIFont>();
        private static Dictionary<string, BuildingGroupDescriptorXml> m_loadedDescriptors;
        internal static Dictionary<string, BuildingGroupDescriptorXml> LoadedDescriptors
        {
            get {

                if (m_loadedDescriptors == null)
                {
                    m_loadedDescriptors = new Dictionary<string, BuildingGroupDescriptorXml>();
                }
                return m_loadedDescriptors;
            }
        }

        private DTPBuildingEditorTab2 EditorInstance => DTPBuildingEditorTab2.Instance as DTPBuildingEditorTab2;

        private LineDescriptor[] m_linesDescriptors;
        private ulong[] m_lineLastUpdate;
        private ulong[] m_lastDrawBuilding;
        private DistrictDescriptor[] m_districtDescriptors;
        private readonly Dictionary<string, StopPointDescriptorLanes[]> m_buildingStopsDescriptor = new Dictionary<string, StopPointDescriptorLanes[]>();
        private readonly Dictionary<string, BasicRenderInformation> m_textCache = new Dictionary<string, BasicRenderInformation>();

        public override int ObjArraySize => BuildingManager.MAX_BUILDING_COUNT;

        private UIDynamicFont m_font;

        public override UIDynamicFont DrawFont => m_font;

        public static readonly TextType[] AVAILABLE_TEXT_TYPES = new TextType[]
        {
            TextType.OwnName,
            TextType.Fixed,
            //TextType.StreetPrefix,
            //TextType.StreetSuffix,
            //TextType.StreetNameComplete,
            //TextType.LinesSymbols,
            TextType.Custom1, // Next Station Line
            TextType.Custom2, // Previous Station Line
            TextType.Custom3  // Line Destination (Last stop before get back)
        };

        #region Initialize
        public override void Initialize()
        {
            BuildSurfaceFont(out m_font, LoadedConfig.DefaultFont);
            LoadAllBuildingConfigurations();


            TransportManagerOverrides.EventOnLineUpdated += OnLineUpdated;
            NetManagerOverrides.EventNodeChanged += OnNodeChanged;
            TransportManager.instance.eventLineColorChanged += OnLineUpdated;
            InstanceManagerOverrides.EventOnBuildingRenamed += OnBuildingNameChanged;
            TransportManagerOverrides.EventOnLineBuildingUpdated += OnBuildingLineChanged;
            DistrictManagerOverrides.EventOnDistrictChanged += () => m_districtDescriptors = new DistrictDescriptor[DistrictManager.MAX_DISTRICT_COUNT];

            #region Hooks
            System.Reflection.MethodInfo postRenderMeshs = GetType().GetMethod("AfterRenderMeshes", RedirectorUtils.allFlags);
            LogUtils.DoLog($"Patching=> {postRenderMeshs}");
            RedirectorInstance.AddRedirect(typeof(BuildingAI).GetMethod("RenderMeshes", RedirectorUtils.allFlags), null, postRenderMeshs);
            System.Reflection.MethodInfo afterEndOverlayImpl = GetType().GetMethod("AfterEndOverlayImpl", RedirectorUtils.allFlags);
            RedirectorInstance.AddRedirect(typeof(ToolManager).GetMethod("EndOverlayImpl", RedirectorUtils.allFlags), null, afterEndOverlayImpl);

            var adrEventsType = Type.GetType("Klyte.Addresses.ModShared.AdrEvents, KlyteAddresses");
            if (adrEventsType != null)
            {
                static void RegisterEvent(string eventName, Type adrEventsType, Action action) => adrEventsType.GetEvent(eventName)?.AddEventHandler(null, action);
                RegisterEvent("EventBuildingNameStrategyChanged", adrEventsType, ResetImpl);
                RegisterEvent("EventRoadNamingChange", adrEventsType, ResetImpl);
            }

            #endregion
        }

        protected override void OnChangeFont(string fontName) => LoadedConfig.DefaultFont = fontName;

        private static string DefaultFilename { get; } = $"{DynamicTextPropsMod.m_defaultFileNameXml}.xml";
        public void LoadAllBuildingConfigurations()
        {
            FileUtils.ScanPrefabsFolders<BuildingInfo>(DefaultFilename, LoadDescriptorsFromXml);
            var errorList = new List<string>();
            foreach (string filename in Directory.GetFiles(DynamicTextPropsMod.DefaultBuildingsConfigurationFolder, "*.xml"))
            {
                try
                {
                    using FileStream stream = File.OpenRead(filename);
                    LoadDescriptorsFromXml(stream, null);
                }
                catch (Exception e)
                {
                    errorList.Add($"Error Loading file \"{filename}\" ({e.GetType()}): {e.Message}");
                }
            }

            m_linesDescriptors = new LineDescriptor[TransportManager.MAX_LINE_COUNT];
            m_lineLastUpdate = new ulong[TransportManager.MAX_LINE_COUNT];
            m_districtDescriptors = new DistrictDescriptor[DistrictManager.MAX_DISTRICT_COUNT];
            m_boardsContainers = new BoardBunchContainerBuilding[BuildingManager.MAX_BUILDING_COUNT];
            m_lastDrawBuilding = new ulong[BuildingManager.MAX_BUILDING_COUNT];
            if (errorList.Count > 0)
            {
                UIComponent uIComponent = UIView.library.ShowModal("ExceptionPanel");
                if (uIComponent != null)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    BindPropertyByKey component = uIComponent.GetComponent<BindPropertyByKey>();
                    if (component != null)
                    {
                        string title = $"Errors loading Files";
                        string text = string.Join("\r\n", errorList.ToArray());
                        string img = "IconError";
                        component.SetProperties(TooltipHelper.Format(new string[]
                        {
                            "title",
                            title,
                            "message",
                            text,
                            "img",
                            img
                        }));
                    }
                }
                else
                {
                    LogUtils.DoErrorLog("PANEL NOT FOUND!!!!");
                }
            }
        }

        private void LoadDescriptorsFromXml(FileStream stream, BuildingInfo info)
        {
            var serializer = new XmlSerializer(typeof(BuildingGroupDescriptorXml));


            if (serializer.Deserialize(stream) is BuildingGroupDescriptorXml config)
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
                LoadedDescriptors[config.BuildingName] = config;
            }
        }

        private void OnNodeChanged(ushort id)
        {
            ushort buildingId = NetNode.FindOwnerBuilding(id, 56f);
            if (buildingId > 0 && m_boardsContainers[buildingId] != null)
            {
                m_boardsContainers[buildingId].m_linesUpdateFrame = 0;
            }
        }

        protected override void ResetImpl()
        {
            m_boardsContainers.ForEach(x =>
            {
                if (x != null)
                {
                    x.m_nameSubInfo = null;
                }
            });
            m_textCache.Clear();

        }


        private void OnLineUpdated(ushort lineId) => m_linesDescriptors[lineId] = default;
        private void OnBuildingNameChanged(ushort id)
        {
            if (m_boardsContainers[id] != null)
            {
                m_boardsContainers[id].m_nameSubInfo = null;
            }
        }

        private void OnBuildingLineChanged(ushort id)
        {
            ushort parentId = id;
            int count = 10;
            while (parentId > 0 && count > 0)
            {
                if (m_boardsContainers[parentId] != null)
                {
                    m_boardsContainers[parentId].m_platformToLine = null;
                }

                parentId = BuildingManager.instance.m_buildings.m_buffer[parentId].m_parentBuilding;
                count--;
            }
            if (count == 0)
            {
                LogUtils.DoErrorLog($"INFINITELOOP! {id} {parentId} ");
            }
        }

        public void OnDescriptorChanged() => m_boardsContainers = new BoardBunchContainerBuilding[BuildingManager.MAX_BUILDING_COUNT];
        #endregion

        public static void AfterEndOverlayImpl(RenderManager.CameraInfo cameraInfo)
        {
            if (Instance.EditorInstance.component.isVisible)
            {
                foreach (Tuple<Vector3, float, Color> tuple in Instance.m_onOverlayRenderQueue)
                {
                    Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo,
                       tuple.Third,
                       tuple.First,
                       tuple.Second * 2,
                       -1, 1280f, false, true);
                }
                Instance.m_onOverlayRenderQueue.Clear();
            }
        }

        private readonly List<Tuple<Vector3, float, Color>> m_onOverlayRenderQueue = new List<Tuple<Vector3, float, Color>>();

        public static void AfterRenderMeshes(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance) => Instance.AfterRenderMeshesImpl(cameraInfo, buildingID, ref data, layerMask, ref instance);

        public static StopPointDescriptorLanes[] GetStopPointsDescriptorFor(string building)
        {
            if (!Instance.m_buildingStopsDescriptor.ContainsKey(building))
            {
                Instance.m_buildingStopsDescriptor[building] = MapStopPoints(PrefabCollection<BuildingInfo>.FindLoaded(building), LoadedDescriptors.ContainsKey(building) ? LoadedDescriptors[building].StopMappingThresold : 1f);
            }
            return Instance.m_buildingStopsDescriptor[building];
        }

        public static void ClearStopMapping(string building) => Instance.m_buildingStopsDescriptor.Remove(building);

        public void AfterRenderMeshesImpl(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance renderInstance)
        {
            if (data.m_parentBuilding != 0)
            {
                RenderManager instance = Singleton<RenderManager>.instance;
                if (instance.RequireInstance(data.m_parentBuilding, 1u, out uint num))
                {
                    AfterRenderMeshesImpl(cameraInfo, data.m_parentBuilding, ref BuildingManager.instance.m_buildings.m_buffer[data.m_parentBuilding], layerMask, ref instance.m_instances[num]);
                }
                return;
            }
            if (m_lastDrawBuilding[buildingID] >= SimulationManager.instance.m_currentTickIndex)
            {
                return;
            }
            m_lastDrawBuilding[buildingID] = SimulationManager.instance.m_currentTickIndex;

            string refName = GetReferenceModelName(ref data);
            if (EditorInstance.component.isVisible && EditorInstance.m_currentBuildingName == refName)
            {
                if (!m_buildingStopsDescriptor.ContainsKey(refName))
                {
                    m_buildingStopsDescriptor[refName] = MapStopPoints(data.Info, LoadedDescriptors.ContainsKey(refName) ? LoadedDescriptors[refName].StopMappingThresold : 1f);
                }
                for (int i = 0; i < m_buildingStopsDescriptor[refName].Length; i++)
                {
                    m_onOverlayRenderQueue.Add(Tuple.New(renderInstance.m_dataMatrix1.MultiplyPoint(m_buildingStopsDescriptor[refName][i].platformLine.Position(0.5f)),
                           m_buildingStopsDescriptor[refName][i].width / 2, m_colorOrder[i % m_colorOrder.Length]));
                }
            }
            if (!LoadedDescriptors.ContainsKey(refName) || (LoadedDescriptors[refName]?.BoardDescriptors?.Length ?? 0) == 0)
            {
                return;
            }
            if (m_boardsContainers[buildingID] == null)
            {
                m_boardsContainers[buildingID] = new BoardBunchContainerBuilding();
            }
            if (m_boardsContainers[buildingID]?.m_boardsData?.Count() != LoadedDescriptors[refName].BoardDescriptors.Length)
            {
                m_boardsContainers[buildingID].m_boardsData = new CacheControl[LoadedDescriptors[refName].BoardDescriptors.Length];
                m_boardsContainers[buildingID].m_nameSubInfo = null;
            }

            UpdateLinesBuilding(buildingID, ref data, m_boardsContainers[buildingID], ref renderInstance.m_dataMatrix1);
            for (int i = 0; i < LoadedDescriptors[refName].BoardDescriptors.Length; i++)
            {
                renderInstance = RenderDescriptor(cameraInfo, buildingID, data, layerMask, renderInstance, refName, i);
            }
        }

        private RenderManager.Instance RenderDescriptor(RenderManager.CameraInfo cameraInfo, ushort buildingID, Building data, int layerMask, RenderManager.Instance renderInstance, string refName, int i)
        {
            BoardDescriptorBuildingXml descriptor = LoadedDescriptors[refName].BoardDescriptors[i];
            if (m_boardsContainers[buildingID].m_boardsData[i] == null)
            {
                m_boardsContainers[buildingID].m_boardsData[i] = new CacheControl();
            }
            for (int k = 0; k <= descriptor.m_arrayRepeatTimes; k++)
            {
                RenderPropConfig(cameraInfo, buildingID, data, layerMask, ref renderInstance, i, ref descriptor, k);
                if (descriptor.ArrayRepeat == Vector3.zero)
                {
                    break;
                }
            }

            return renderInstance;
        }

        private void RenderPropConfig(RenderManager.CameraInfo cameraInfo, ushort buildingID, Building data, int layerMask, ref RenderManager.Instance renderInstance, int i, ref BoardDescriptorBuildingXml descriptor, int k)
        {
            RenderPropMesh(ref m_boardsContainers[buildingID].m_boardsData[i].m_cachedProp, cameraInfo, buildingID, i, 0, layerMask, data.m_angle, renderInstance.m_dataMatrix1.MultiplyPoint(descriptor.m_propPosition + (descriptor.ArrayRepeat * k)), renderInstance.m_dataVector3, ref descriptor.m_propName, descriptor.m_propRotation, descriptor.PropScale, ref descriptor, out Matrix4x4 propMatrix, out bool rendered);
            if (rendered && descriptor.m_textDescriptors != null)
            {
                for (int j = 0; j < descriptor.m_textDescriptors?.Length; j++)
                {
                    MaterialPropertyBlock materialBlock = Singleton<PropManager>.instance.m_materialBlock;
                    materialBlock.Clear();

                    RenderTextMesh(cameraInfo, buildingID, i, j, ref descriptor, propMatrix, ref descriptor.m_textDescriptors[j], ref m_boardsContainers[buildingID].m_boardsData[i], materialBlock);
                }
            }
        }

        public static string GetReferenceModelName(ref Building data)
        {
            string refName = data.Info.name;
            if (refName.Contains("_XANALOGX_"))
            {
                refName = refName.Substring(0, refName.LastIndexOf("_XANALOGX_"));
            }

            return refName;
        }



        #region Upadate Data
        protected override BasicRenderInformation GetOwnNameMesh(ushort buildingID, int boardIdx, int secIdx, ref BoardDescriptorBuildingXml descriptor) => GetOwnNameMesh(buildingID, secIdx, ref descriptor);
        private BasicRenderInformation GetOwnNameMesh(ushort buildingID, int secIdx, ref BoardDescriptorBuildingXml descriptor)
        {
            string cacheKey = $"{descriptor.m_textDescriptors[secIdx].m_prefix}{BuildingManager.instance.GetBuildingName(buildingID, new InstanceID()) ?? "DUMMY!!!!!"}{descriptor.m_textDescriptors[secIdx].m_suffix}";
            if (descriptor.m_textDescriptors[secIdx].m_allCaps)
            {
                cacheKey = cacheKey.ToUpper();
            }
            return GetCachedText(cacheKey);
        }
        protected override BasicRenderInformation GetMeshCustom1(ushort buildingID, int boardIdx, int secIdx, ref BoardDescriptorBuildingXml descriptor)
        {
            if (descriptor.m_platforms.Length > 0)
            {
                StopInformation stop = GetTargetStopInfo(buildingID, descriptor);
                if (stop.m_nextStopId > 0)
                {
                    return GetOwnNameMesh(DTPLineUtils.GetStopBuilding(stop.m_nextStopId, stop.m_lineId), secIdx, ref descriptor);
                }
            }

            return null;
        }
        protected override BasicRenderInformation GetMeshCustom2(ushort buildingID, int boardIdx, int secIdx, ref BoardDescriptorBuildingXml descriptor)
        {
            if (descriptor.m_platforms.Length > 0)
            {
                StopInformation stop = GetTargetStopInfo(buildingID, descriptor);
                if (stop.m_previousStopId > 0)
                {
                    return GetOwnNameMesh(DTPLineUtils.GetStopBuilding(stop.m_previousStopId, stop.m_lineId), secIdx, ref descriptor);
                }
            }

            return null;
        }
        protected override BasicRenderInformation GetMeshCustom3(ushort buildingID, int boardIdx, int secIdx, ref BoardDescriptorBuildingXml descriptor)
        {
            if (descriptor.m_platforms.Length > 0)
            {
                StopInformation stop = GetTargetStopInfo(buildingID, descriptor);
                if (stop.m_destinationId > 0)
                {
                    return GetOwnNameMesh(DTPLineUtils.GetStopBuilding(stop.m_destinationId, stop.m_lineId), secIdx, ref descriptor);
                }
            }

            return null;
        }
        protected override BasicRenderInformation GetFixedTextMesh(ref BoardTextDescriptorBuildingsXml textDescriptor, ushort refID, int boardIdx, int secIdx, ref BoardDescriptorBuildingXml descriptor)
        {
            string txt = (textDescriptor.m_isFixedTextLocalized ? Locale.Get(textDescriptor.m_fixedText, textDescriptor.m_fixedTextLocaleKey) : textDescriptor.m_fixedText) ?? "";
            if (descriptor.m_textDescriptors[secIdx].m_cachedType != TextType.Fixed)
            {
                descriptor.m_textDescriptors[secIdx].GeneratedFixedTextRenderInfo = null;
                descriptor.m_textDescriptors[secIdx].m_cachedType = TextType.Fixed;
            }
            return GetTextRendered(secIdx, descriptor, txt);
        }

        private BasicRenderInformation GetTextRendered(int secIdx, BoardDescriptorBuildingXml descriptor, string txt)
        {
            if (!descriptor.m_textDescriptors[secIdx].m_overrideFont.IsNullOrWhiteSpace())
            {

                if (!m_fontCache.ContainsKey(descriptor.m_textDescriptors[secIdx].m_overrideFont))
                {
                    BuildSurfaceFont(out UIDynamicFont surfaceFont, descriptor.m_textDescriptors[secIdx].m_overrideFont);
                    if (surfaceFont.baseFont == null)
                    {
                        descriptor.m_textDescriptors[secIdx].m_overrideFont = null;

                        return GetCachedText(txt);
                    }
                    else
                    {
                        m_fontCache[descriptor.m_textDescriptors[secIdx].m_overrideFont] = surfaceFont;
                    }
                }
                if (descriptor.m_textDescriptors[secIdx].GeneratedFixedTextRenderInfo == null)
                {
                    descriptor.m_textDescriptors[secIdx].GeneratedFixedTextRenderInfo = RefreshTextData(txt, m_fontCache[descriptor.m_textDescriptors[secIdx].m_overrideFont]);
                }
                return descriptor.m_textDescriptors[secIdx].GeneratedFixedTextRenderInfo;
            }
            else
            {

                return GetCachedText(txt);
            }
        }

        private BasicRenderInformation GetCachedText(string txt)
        {
            if (!m_textCache.ContainsKey(txt))
            {
                m_textCache[txt] = RefreshTextData(txt);
            }
            return m_textCache[txt];
        }

        protected void UpdateLinesBuilding(ushort buildingID, ref Building data, BoardBunchContainerBuilding bbcb, ref Matrix4x4 refMatrix)
        {
            NetManager nmInstance = NetManager.instance;
            string refName = GetReferenceModelName(ref data);
            if (bbcb.m_platformToLine == null || (bbcb.m_platformToLine?.Length > 0 && bbcb.m_platformToLine.SelectMany((x) => x?.Select(y => bbcb.m_linesUpdateFrame < m_lineLastUpdate[y.m_lineId])).Any(x => x)))
            {
                LogUtils.DoLog("--------------- UpdateLinesBuilding");
                bbcb.m_platformToLine = null;
                if (!m_buildingStopsDescriptor.ContainsKey(refName))
                {
                    m_buildingStopsDescriptor[refName] = MapStopPoints(data.Info, LoadedDescriptors.ContainsKey(refName) ? LoadedDescriptors[refName].StopMappingThresold : 1f);

                }

                var platforms = m_buildingStopsDescriptor[refName].Select((v, i) => new { Key = i, Value = v }).ToDictionary(o => o.Key, o => o.Value);

                if (platforms.Count == 0)
                {
                    bbcb.m_platformToLine = new StopInformation[0][];
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

                    if (nearStops.Count > 0)
                    {
                        bbcb.m_platformToLine = new StopInformation[m_buildingStopsDescriptor[refName].Length][];

                        if (DynamicTextPropsMod.DebugMode)
                        {
                            LogUtils.DoLog($"[{InstanceManager.instance.GetName(new InstanceID { Building = buildingID })}] nearStops = [\n\t\t{string.Join(",\n\t\t", nearStops.Select(x => $"[{x} => {nmInstance.m_nodes.m_buffer[x].m_position} (TL { nmInstance.m_nodes.m_buffer[x].m_transportLine} => [{TransportManager.instance.m_lines.m_buffer[nmInstance.m_nodes.m_buffer[x].m_transportLine].Info.m_transportType}-{TransportManager.instance.m_lines.m_buffer[nmInstance.m_nodes.m_buffer[x].m_transportLine].m_lineNumber}] {InstanceManager.instance.GetName(new InstanceID { TransportLine = nmInstance.m_nodes.m_buffer[x].m_transportLine })} )]").ToArray())}\n\t] ");
                        }
                        string buildingName = refName;
                        for (int i = 0; i < m_buildingStopsDescriptor[buildingName].Length; i++)
                        {
                            Matrix4x4 inverseMatrix = refMatrix.inverse;
                            if (inverseMatrix == default)
                            {
                                bbcb.m_platformToLine = null;
                                LogUtils.DoLog("--------------- end UpdateLinesBuilding - inverseMatrix is zero");
                                return;
                            }
                            float maxDist = m_buildingStopsDescriptor[buildingName][i].width;
                            float maxHeightDiff = m_buildingStopsDescriptor[buildingName][i].width;
                            if (DynamicTextPropsMod.DebugMode)
                            {
                                LogUtils.DoLog($"platLine ({i}) = {m_buildingStopsDescriptor[buildingName][i].platformLine.a} {m_buildingStopsDescriptor[buildingName][i].platformLine.b} {m_buildingStopsDescriptor[buildingName][i].platformLine.c} {m_buildingStopsDescriptor[buildingName][i].platformLine.d}");
                                LogUtils.DoLog($"maxDist ({i}) = {maxDist}");
                                LogUtils.DoLog($"maxHeightDiff ({i}) = {maxHeightDiff}");
                                LogUtils.DoLog($"refMatrix ({i}) = {refMatrix}");
                                LogUtils.DoLog($"inverseMatrix ({i}) = {inverseMatrix}");
                            }
                            float angleBuilding = data.m_angle * Mathf.Rad2Deg;
                            bbcb.m_platformToLine[i] = nearStops
                                .Where(x =>
                                {
                                    Vector3 relPos = inverseMatrix.MultiplyPoint(nmInstance.m_nodes.m_buffer[x].m_position);
                                    float dist = m_buildingStopsDescriptor[buildingName][i].platformLine.DistanceSqr(relPos, out _);
                                    float diffY = Mathf.Abs(relPos.y - m_buildingStopsDescriptor[buildingName][i].platformLine.Position(0.5f).y);
                                    if (DynamicTextPropsMod.DebugMode)
                                    {
                                        LogUtils.DoLog($"stop {x} => relPos = {relPos}; dist = {dist}; diffY = {diffY}");
                                    }

                                    return dist < maxDist && diffY < maxHeightDiff;
                                })
                                .Select(x =>
                                {
                                    var result = new StopInformation
                                    {
                                        m_lineId = nmInstance.m_nodes.m_buffer[x].m_transportLine,
                                        m_stopId = x
                                    };
                                    result.m_destinationId = FindDestinationStop(x, result.m_lineId);
                                    float anglePlat = (m_buildingStopsDescriptor[buildingName][i].directionPath.GetAngleXZ() + 360 + angleBuilding) % 360;

                                    ref NetSegment[] segBuffer = ref nmInstance.m_segments.m_buffer;
                                    for (int j = 0; j < 8; j++)
                                    {
                                        ushort segment = nmInstance.m_nodes.m_buffer[x].GetSegment(j);
                                        if (segment == 0)
                                        {
                                            continue;
                                        }
                                        float angleDir;
                                        PathUnit.Position path1;
                                        PathUnit.Position path2;
                                        if (segBuffer[segment].m_startNode == x)
                                        {
                                            path1 = PathManager.instance.m_pathUnits.m_buffer[nmInstance.m_segments.m_buffer[segment].m_path].m_position00;
                                            path2 = PathManager.instance.m_pathUnits.m_buffer[nmInstance.m_segments.m_buffer[segment].m_path].m_position01;
                                        }
                                        else
                                        {
                                            PathManager.instance.m_pathUnits.m_buffer[nmInstance.m_segments.m_buffer[segment].m_path].GetLast2Positions(out path2, out path1);

                                        }
                                        angleDir = ((segBuffer[path2.m_segment].GetBezier().Position(path2.m_offset / 255f) - segBuffer[path1.m_segment].GetBezier().Position(path1.m_offset / 255f)).GetAngleXZ() + 360) % 360;
                                        float diff = Mathf.Abs(angleDir - anglePlat);
                                        LogUtils.DoLog($"ANGLE COMPARISON: diff = {diff} | PLAT = {anglePlat} | SEG = {nmInstance.m_segments.m_buffer[segment].m_startDirection} ({angleDir}) ({buildingName}=>  P[{i}] | L = {nmInstance.m_nodes.m_buffer[x].m_transportLine} )");
                                        if (diff > 90 && diff < 270)
                                        {
                                            result.m_previousStopId = segBuffer[segment].GetOtherNode(x);
                                        }
                                        else
                                        {
                                            result.m_nextStopId = segBuffer[segment].GetOtherNode(x);
                                        }
                                    }

                                    if (result.m_destinationId == 0)
                                    {
                                        result.m_destinationId = result.m_nextStopId;
                                    }
                                    return result;
                                }).ToArray();
                            if (DynamicTextPropsMod.DebugMode)
                            {
                                LogUtils.DoLog($"NearLines ({i}) = [{string.Join(",", bbcb.m_platformToLine[i].Select(x => x.ToString()).ToArray())}]");
                            }
                        }
                    }
                    else
                    {
                        bbcb.m_platformToLine = null;
                    }
                }
                bbcb.m_linesUpdateFrame = SimulationManager.instance.m_currentTickIndex;
                LogUtils.DoLog("--------------- end UpdateLinesBuilding");
            }
        }

        private ushort FindDestinationStop(ushort stopId, ushort lineId)
        {
            if (m_allowedTypesNextPreviousStations.Contains(TransportManager.instance.m_lines.m_buffer[NetManager.instance.m_nodes.m_buffer[stopId].m_transportLine].Info.m_transportType))
            {
                ushort prevStop = 0;
                ushort curStop = stopId;
                ushort nextStop = TransportLine.GetNextStop(curStop);
                int stopCount = 0;
                do
                {
                    prevStop = curStop;
                    curStop = nextStop;
                    nextStop = TransportLine.GetNextStop(curStop);
                    if (DTPLineUtils.GetStopBuilding(nextStop, lineId) == DTPLineUtils.GetStopBuilding(prevStop, lineId))
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

        #endregion

        public override Color? GetColor(ushort buildingID, int boardIdx, int textIdx, BoardDescriptorBuildingXml descriptor)
        {
            switch (descriptor.ColorModeProp)
            {
                case ColoringMode.Fixed:
                    return descriptor.FixedColor;
                case ColoringMode.ByPlatform:
                    StopInformation stop = GetTargetStopInfo(buildingID, descriptor);
                    if (stop.m_lineId != 0)
                    {
                        if (m_linesDescriptors[stop.m_lineId].m_lineColor == default)
                        {
                            UpdateLine(stop.m_lineId);
                        }
                        return m_linesDescriptors[stop.m_lineId].m_lineColor;
                    }
                    if (!descriptor.m_showIfNoLine)
                    {
                        return null;
                    }
                    break;
                case ColoringMode.ByDistrict:
                    byte districtId = DistrictManager.instance.GetDistrict(BuildingManager.instance.m_buildings.m_buffer[buildingID].m_position);
                    if (m_districtDescriptors[districtId] == null)
                    {
                        UpdateDistrict(districtId);
                    }
                    return m_districtDescriptors[districtId].m_districtColor;
                case ColoringMode.FromBuilding:
                    return BuildingManager.instance.m_buildings.m_buffer[buildingID].Info.m_buildingAI.GetColor(buildingID, ref BuildingManager.instance.m_buildings.m_buffer[buildingID], InfoManager.InfoMode.None);
            }
            return Color.white;
        }

        private StopInformation GetTargetStopInfo(ushort buildingID, BoardDescriptorBuildingXml descriptor)
        {
            foreach (int platform in descriptor.m_platforms)
            {
                if (m_boardsContainers[buildingID].m_platformToLine != null && m_boardsContainers[buildingID].m_platformToLine.ElementAtOrDefault(platform) != null)
                {
                    StopInformation line = m_boardsContainers[buildingID].m_platformToLine[platform].ElementAtOrDefault(0);
                    if (line.m_lineId != 0)
                    {
                        return line;
                    }
                }
            }
            return default;
        }

        public override Color GetContrastColor(ushort buildingID, int boardIdx, int textIdx, BoardDescriptorBuildingXml descriptor)
        {
            switch (descriptor.ColorModeProp)
            {
                case ColoringMode.Fixed:
                    return KlyteMonoUtils.ContrastColor(descriptor.FixedColor);
                case ColoringMode.ByPlatform:
                    int[] targetPlatforms = descriptor.m_platforms;
                    foreach (int platform in targetPlatforms)
                    {
                        if (m_boardsContainers[buildingID].m_platformToLine != null && m_boardsContainers[buildingID].m_platformToLine.ElementAtOrDefault(platform) != null && m_boardsContainers[buildingID].m_platformToLine[platform].Length > 0)
                        {
                            StopInformation line = m_boardsContainers[buildingID].m_platformToLine[platform].ElementAtOrDefault(0);
                            if (line.m_lineId != 0)
                            {
                                if (m_linesDescriptors[line.m_lineId].m_lineColor == default)
                                {
                                    UpdateLine(line.m_lineId);
                                }
                                return m_linesDescriptors[line.m_lineId].m_contrastColor;
                            }
                        }
                    }
                    break;
                case ColoringMode.ByDistrict:
                    byte districtId = DistrictManager.instance.GetDistrict(BuildingManager.instance.m_buildings.m_buffer[buildingID].m_position);
                    if (m_districtDescriptors[districtId] == null)
                    {
                        UpdateDistrict(districtId);
                    }
                    return m_districtDescriptors[districtId].m_contrastColor;
                case ColoringMode.FromBuilding:
                    return KlyteMonoUtils.ContrastColor(BuildingManager.instance.m_buildings.m_buffer[buildingID].Info.m_buildingAI.GetColor(buildingID, ref BuildingManager.instance.m_buildings.m_buffer[buildingID], InfoManager.InfoMode.None));

            }
            return Color.black;
        }

        private void UpdateLine(ushort lineId)
        {
            m_linesDescriptors[lineId] = new LineDescriptor
            {
                m_lineColor = TransportManager.instance.GetLineColor(lineId)
            };
            m_lineLastUpdate[lineId] = SimulationManager.instance.m_currentTickIndex;
            m_linesDescriptors[lineId].m_contrastColor = KlyteMonoUtils.ContrastColor(m_linesDescriptors[lineId].m_lineColor);
        }

        private void UpdateDistrict(ushort districtId)
        {
            m_districtDescriptors[districtId] = new DistrictDescriptor
            {
                m_districtColor = DTPHookable.GetDistrictColor(districtId),
                m_lastUpdate = SimulationManager.instance.m_currentTickIndex
            };

            m_districtDescriptors[districtId].m_contrastColor = KlyteMonoUtils.ContrastColor(m_districtDescriptors[districtId].m_districtColor);

        }

        protected override InstanceID GetPropRenderID(ushort buildingID)
        {
            InstanceID result = default;
            result.Building = buildingID;
            return result;
        }

        private struct LineDescriptor
        {
            public Color m_lineColor;
            public Color m_contrastColor;
        }
        private class DistrictDescriptor
        {
            public Color m_districtColor;
            public Color m_contrastColor;
            public uint m_lastUpdate;
        }


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

        public static bool SaveInCommonFolder(string buildingName)
        {
            if (LoadedDescriptors.ContainsKey(buildingName))
            {
                BuildingGroupDescriptorXml item = LoadedDescriptors[buildingName];
                item.BuildingName = buildingName;
                SaveInCommonFolder(new XmlSerializer(typeof(BuildingGroupDescriptorXml)), item, true);
                return true;
            }
            return false;
        }

        private static void SaveInCommonFolder(XmlSerializer serializer, BuildingGroupDescriptorXml item, bool force = false)
        {
            string filePath = DynamicTextPropsMod.DefaultBuildingsConfigurationFolder + Path.DirectorySeparatorChar + $"{DynamicTextPropsMod.m_defaultFileNameXml}_{item.BuildingName}.xml";
            SaveInPath(serializer, item, force, filePath);
        }
        public static bool SaveInAssetFolder(string buildingName)
        {
            if (LoadedDescriptors.ContainsKey(buildingName))
            {
                BuildingGroupDescriptorXml item = LoadedDescriptors[buildingName];
                Package.Asset asset = PackageManager.FindAssetByName(buildingName);
                if (!(asset == null) && !(asset.package == null))
                {
                    string packagePath = asset.package.packagePath;
                    if (packagePath != null)
                    {
                        string filePath = Path.Combine(Path.GetDirectoryName(packagePath), DefaultFilename);
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

        #region Serialization
        protected override string ID => "K45_DTP_BGB";


        public override void Deserialize(string data)
        {
            LogUtils.DoLog($"{GetType()} STR: \"{data}\"");
            if (data.IsNullOrWhiteSpace())
            {
                return;
            }
            try
            {
                LoadedConfig = XmlUtils.DefaultXmlDeserialize<BoardGeneratorBuildingConfigXml>(data);
            }
            catch (Exception e)
            {
                LogUtils.DoErrorLog($"Error deserializing: {e.Message}\n{e.StackTrace}");
            }
        }

        public override string Serialize() => XmlUtils.DefaultXmlSerialize(LoadedConfig, false);

        public static BoardGeneratorBuildingConfigXml LoadedConfig = new BoardGeneratorBuildingConfigXml();
        #endregion

    }
}