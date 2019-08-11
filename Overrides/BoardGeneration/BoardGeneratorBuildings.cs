﻿using ColossalFramework;
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

        internal static Dictionary<string, BuildingGroupDescriptorXml> m_loadedDescriptors;

        private DTPBuildingEditorTab EditorInstance => DTPBuildingEditorTab.Instance;

        private LineDescriptor[] m_linesDescriptors;
        private ulong[] m_lineLastUpdate;
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
                RegisterEvent("EventBuildingNameStrategyChanged", adrEventsType, () => OnTextureRebuiltImpl(DrawFont.baseFont));
            }

            #endregion
        }
        protected override void OnChangeFont(string fontName) => LoadedConfig.DefaultFont = fontName;

        private static string DefaultFilename { get; } = $"{DynamicTextPropsMod.m_defaultFileNameXml}.xml";
        public void LoadAllBuildingConfigurations()
        {
            FileUtils.ScanPrefabsFolders(DefaultFilename, LoadDescriptorsFromXml);
            var errorList = new List<string>();
            foreach (var filename in Directory.GetFiles(DynamicTextPropsMod.DefaultBuildingsConfigurationFolder, "*.xml"))
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
                        var title = $"Errors loading Files";
                        var text = string.Join("\r\n", errorList.ToArray());
                        var img = "IconError";
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
                if (m_loadedDescriptors == null)
                {
                    m_loadedDescriptors = new Dictionary<string, BuildingGroupDescriptorXml>();
                }
                if (info != null)
                {
                    var propEffName = info.name.Split(".".ToCharArray(), 2);
                    var xmlEffName = config.BuildingName.Split(".".ToCharArray(), 2);
                    if (propEffName.Length == 2 && xmlEffName.Length == 2 && xmlEffName[1] == propEffName[1])
                    {
                        config.BuildingName = info.name;
                    }
                }
                m_loadedDescriptors[config.BuildingName] = config;
            }
        }

        private void OnNodeChanged(ushort id)
        {
            var buildingId = NetNode.FindOwnerBuilding(id, 56f);
            if (buildingId > 0 && m_boardsContainers[buildingId] != null)
            {
                m_boardsContainers[buildingId].m_linesUpdateFrame = 0;
            }
        }

        protected override void OnTextureRebuiltImpl(Font obj)
        {
            if (obj == DrawFont.baseFont)
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
            if (m_boardsContainers[id] != null)
            {
                m_boardsContainers[id].m_platformToLine = null;
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
                       tuple.Second,
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
                Instance.m_buildingStopsDescriptor[building] = MapStopPoints(PrefabCollection<BuildingInfo>.FindLoaded(building));
            }
            return Instance.m_buildingStopsDescriptor[building];
        }

        public void AfterRenderMeshesImpl(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance renderInstance)
        {
            if (EditorInstance.component.isVisible && EditorInstance.m_currentBuildingName == data.Info.name)
            {
                if (!m_buildingStopsDescriptor.ContainsKey(data.Info.name))
                {
                    m_buildingStopsDescriptor[data.Info.name] = MapStopPoints(data.Info);
                }
                for (var i = 0; i < m_buildingStopsDescriptor[data.Info.name].Length; i++)
                {
                    m_onOverlayRenderQueue.Add(Tuple.New(renderInstance.m_dataMatrix1.MultiplyPoint(m_buildingStopsDescriptor[data.Info.name][i].platformLine.Position(0.5f)),
                           m_buildingStopsDescriptor[data.Info.name][i].width / 2, m_colorOrder[i % m_colorOrder.Length]));
                }
            }
            if (!m_loadedDescriptors.ContainsKey(data.Info.name) || (m_loadedDescriptors[data.Info.name]?.BoardDescriptors?.Length ?? 0) == 0)
            {
                return;
            }
            if (m_boardsContainers[buildingID] == null)
            {
                m_boardsContainers[buildingID] = new BoardBunchContainerBuilding();
            }
            if (m_boardsContainers[buildingID]?.m_boardsData?.Count() != m_loadedDescriptors[data.Info.name].BoardDescriptors.Length)
            {
                m_boardsContainers[buildingID].m_boardsData = new CacheControl[m_loadedDescriptors[data.Info.name].BoardDescriptors.Length];
                m_boardsContainers[buildingID].m_nameSubInfo = null;
            }

            UpdateLinesBuilding(buildingID, ref data, m_boardsContainers[buildingID], ref renderInstance.m_dataMatrix1);
            for (var i = 0; i < m_loadedDescriptors[data.Info.name].BoardDescriptors.Length; i++)
            {
                BoardDescriptorBuildingXml descriptor = m_loadedDescriptors[data.Info.name].BoardDescriptors[i];
                if (m_boardsContainers[buildingID].m_boardsData[i] == null)
                {
                    m_boardsContainers[buildingID].m_boardsData[i] = new CacheControl();
                }

                RenderPropMesh(ref m_boardsContainers[buildingID].m_boardsData[i].m_cachedProp, cameraInfo, buildingID, i, 0, layerMask, data.m_angle, renderInstance.m_dataMatrix1.MultiplyPoint(descriptor.m_propPosition), renderInstance.m_dataVector3, ref descriptor.m_propName, descriptor.m_propRotation, descriptor.PropScale, ref descriptor, out Matrix4x4 propMatrix, out var rendered);
                if (rendered && descriptor.m_textDescriptors != null)
                {
                    for (var j = 0; j < descriptor.m_textDescriptors?.Length; j++)
                    {
                        MaterialPropertyBlock materialBlock = Singleton<PropManager>.instance.m_materialBlock;
                        materialBlock.Clear();

                        RenderTextMesh(cameraInfo, buildingID, i, j, ref descriptor, propMatrix, ref descriptor.m_textDescriptors[j], ref m_boardsContainers[buildingID].m_boardsData[i], materialBlock);
                    }
                }
            }
        }



        #region Upadate Data
        protected override BasicRenderInformation GetOwnNameMesh(ushort buildingID, int boardIdx, int secIdx, out UIFont font, ref BoardDescriptorBuildingXml descriptor) => GetOwnNameMesh(buildingID, out font, ref descriptor, false);
        private  BasicRenderInformation GetOwnNameMesh(ushort buildingID, out UIFont font, ref BoardDescriptorBuildingXml descriptor, bool getFromGlobalCache)
        {
            font = DrawFont;
            if (getFromGlobalCache)
            {
                return GetCachedText(BuildingManager.instance.GetBuildingName(buildingID, new InstanceID()) ?? "DUMMY!!!!!");
            }
            if (m_boardsContainers[buildingID] == null)
            {
                m_boardsContainers[buildingID] = new BoardBunchContainerBuilding();
            }

            if (m_boardsContainers[buildingID].m_nameSubInfo == null)
            {
                RefreshTextData(ref m_boardsContainers[buildingID].m_nameSubInfo, BuildingManager.instance.GetBuildingName(buildingID, new InstanceID()) ?? "DUMMY!!!!!");
            }
            return m_boardsContainers[buildingID].m_nameSubInfo;

        }
        protected override BasicRenderInformation GetMeshCustom1(ushort buildingID, int boardIdx, int secIdx, out UIFont targetFont, ref BoardDescriptorBuildingXml descriptor)
        {
            if (descriptor.m_platforms.Length > 0)
            {
                StopInformation stop = GetTargetStopInfo(buildingID, descriptor);
                if (stop.m_nextStopBuilding > 0)
                {
                    return GetOwnNameMesh(stop.m_nextStopBuilding, out targetFont, ref descriptor, true);
                }
            }
            targetFont = DrawFont;
            return null;
        }
        protected override BasicRenderInformation GetMeshCustom2(ushort buildingID, int boardIdx, int secIdx, out UIFont targetFont, ref BoardDescriptorBuildingXml descriptor)
        {
            if (descriptor.m_platforms.Length > 0)
            {
                StopInformation stop = GetTargetStopInfo(buildingID, descriptor);
                if (stop.m_nextStopBuilding > 0)
                {
                    return GetOwnNameMesh(stop.m_previousStopBuilding, out targetFont, ref descriptor, true);
                }
            }
            targetFont = DrawFont;
            return null;
        }
        protected override BasicRenderInformation GetMeshCustom3(ushort buildingID, int boardIdx, int secIdx, out UIFont targetFont, ref BoardDescriptorBuildingXml descriptor)
        {
            if (descriptor.m_platforms.Length > 0)
            {
                StopInformation stop = GetTargetStopInfo(buildingID, descriptor);
                if (stop.m_destinationBuilding > 0)
                {
                    return GetOwnNameMesh(stop.m_destinationBuilding, out targetFont, ref descriptor, true);
                }
            }
            targetFont = DrawFont;
            return null;
        }
        protected override BasicRenderInformation GetFixedTextMesh(ref BoardTextDescriptorBuildingsXml textDescriptor, ushort refID, out UIFont targetFont, ref BoardDescriptorBuildingXml descriptor)
        {
            targetFont = DrawFont;
            var txt = (textDescriptor.m_isFixedTextLocalized ? Locale.Get(textDescriptor.m_fixedText, textDescriptor.m_fixedTextLocaleKey) : textDescriptor.m_fixedText) ?? "";
            return GetCachedText(txt);
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
            if (bbcb.m_platformToLine == null || (bbcb.m_platformToLine?.Length > 0 && bbcb.m_platformToLine.SelectMany((x) => x?.Select(y => bbcb.m_linesUpdateFrame < m_lineLastUpdate[y.m_lineId])).Any(x => x)))
            {
                LogUtils.DoLog("--------------- UpdateLinesBuilding");
                bbcb.m_platformToLine = null;
                if (!m_buildingStopsDescriptor.ContainsKey(data.Info.name))
                {
                    m_buildingStopsDescriptor[data.Info.name] = MapStopPoints(data.Info);

                }

                var platforms = m_buildingStopsDescriptor[data.Info.name].Select((v, i) => new { Key = i, Value = v }).ToDictionary(o => o.Key, o => o.Value);

                if (platforms.Count == 0)
                {
                    bbcb.m_platformToLine = new StopInformation[0][];
                }
                else
                {

                    var boundaries = new List<Quad2>();
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
                    foreach (var node in allnodes)
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
                    List<ushort> nearStops = StopSearchUtils.FindNearStops(data.m_position, ItemClass.Service.PublicTransport, ItemClass.Service.PublicTransport, VehicleInfo.VehicleType.None, true, 400f, out _, out _, boundaries);

                    if (nearStops.Count > 0)
                    {
                        bbcb.m_platformToLine = new StopInformation[m_buildingStopsDescriptor[data.Info.name].Length][];

                        if (DynamicTextPropsMod.DebugMode)
                        {
                            LogUtils.DoLog($"[{InstanceManager.instance.GetName(new InstanceID { Building = buildingID })}] nearStops = [\n\t\t{string.Join(",\n\t\t", nearStops.Select(x => $"[{x} => {NetManager.instance.m_nodes.m_buffer[x].m_position} (TL { NetManager.instance.m_nodes.m_buffer[x].m_transportLine} => [{TransportManager.instance.m_lines.m_buffer[NetManager.instance.m_nodes.m_buffer[x].m_transportLine].Info.m_transportType}-{TransportManager.instance.m_lines.m_buffer[NetManager.instance.m_nodes.m_buffer[x].m_transportLine].m_lineNumber}] {InstanceManager.instance.GetName(new InstanceID { TransportLine = NetManager.instance.m_nodes.m_buffer[x].m_transportLine })} )]").ToArray())}\n\t] ");
                        }
                        var buildingName = data.Info.name;
                        for (var i = 0; i < m_buildingStopsDescriptor[buildingName].Length; i++)
                        {
                            Matrix4x4 inverseMatrix = refMatrix.inverse;
                            var maxDist = (m_buildingStopsDescriptor[buildingName][i].width * m_buildingStopsDescriptor[buildingName][i].width) + 1f;
                            var maxHeightDiff = m_buildingStopsDescriptor[buildingName][i].width / 2;
                            if (DynamicTextPropsMod.DebugMode)
                            {
                                LogUtils.DoLog($"platLine ({i}) = {m_buildingStopsDescriptor[buildingName][i].platformLine.a} {m_buildingStopsDescriptor[buildingName][i].platformLine.b} {m_buildingStopsDescriptor[buildingName][i].platformLine.c} {m_buildingStopsDescriptor[buildingName][i].platformLine.d}");
                                LogUtils.DoLog($"maxDist ({i}) = {maxDist}");
                                LogUtils.DoLog($"maxHeightDiff ({i}) = {maxHeightDiff}");
                            }
                            bbcb.m_platformToLine[i] = nearStops
                                .Where(x =>
                                {
                                    Vector3 relPos = inverseMatrix.MultiplyPoint(NetManager.instance.m_nodes.m_buffer[x].m_position);
                                    var dist = m_buildingStopsDescriptor[buildingName][i].platformLine.DistanceSqr(relPos, out _);
                                    var diffY = Mathf.Abs(relPos.y - m_buildingStopsDescriptor[buildingName][i].platformLine.Position(0.5f).y);
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
                                        m_lineId = NetManager.instance.m_nodes.m_buffer[x].m_transportLine,
                                        m_stopId = x
                                    };
                                    result.m_previousStopBuilding = GetPrevStop(x);
                                    result.m_nextStopBuilding = GetNextStop(x);
                                    if (result.m_previousStopBuilding == result.m_nextStopBuilding)
                                    {
                                        result.m_nextStopBuilding = 0;
                                    }
                                    result.m_destinationBuilding = FindDestinationBuilding(x);

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

        private readonly TransportInfo.TransportType[] m_allowedTypesNextPreviousStations =
        {
            TransportInfo.TransportType.Metro,
            TransportInfo.TransportType.Monorail,
            TransportInfo.TransportType.Train,
        };

        private ushort GetNextStop(ushort stopId)
        {
            if (m_allowedTypesNextPreviousStations.Contains(TransportManager.instance.m_lines.m_buffer[NetManager.instance.m_nodes.m_buffer[stopId].m_transportLine].Info.m_transportType))
            {
                return BuildingManager.instance.FindBuilding(NetManager.instance.m_nodes.m_buffer[TransportLine.GetNextStop(stopId)].m_position, 100f, ItemClass.Service.None, ItemClass.SubService.None, Building.Flags.None, Building.Flags.None);
            }
            return 0;
        }
        private ushort GetPrevStop(ushort stopId)
        {
            if (m_allowedTypesNextPreviousStations.Contains(TransportManager.instance.m_lines.m_buffer[NetManager.instance.m_nodes.m_buffer[stopId].m_transportLine].Info.m_transportType))
            {
                return BuildingManager.instance.FindBuilding(NetManager.instance.m_nodes.m_buffer[TransportLine.GetPrevStop(stopId)].m_position, 100f, ItemClass.Service.None, ItemClass.SubService.None, Building.Flags.None, Building.Flags.None);
            }
            return 0;
        }
        private ushort FindDestinationBuilding(ushort stopId)
        {
            if (m_allowedTypesNextPreviousStations.Contains(TransportManager.instance.m_lines.m_buffer[NetManager.instance.m_nodes.m_buffer[stopId].m_transportLine].Info.m_transportType))
            {
                var currentStop = stopId;
                float diff;
                do
                {
                    currentStop = TransportLine.GetNextStop(currentStop);
                    var segment0 = NetManager.instance.m_nodes.m_buffer[currentStop].m_segment0;
                    var segment1 = NetManager.instance.m_nodes.m_buffer[currentStop].m_segment1;

                    Vector3 dir0 = NetManager.instance.m_segments.m_buffer[segment0].m_endNode == currentStop ? NetManager.instance.m_segments.m_buffer[segment0].m_endDirection : NetManager.instance.m_segments.m_buffer[segment0].m_startDirection;
                    Vector3 dir1 = NetManager.instance.m_segments.m_buffer[segment1].m_endNode == currentStop ? NetManager.instance.m_segments.m_buffer[segment1].m_endDirection : NetManager.instance.m_segments.m_buffer[segment1].m_startDirection;

                    diff = Math.Abs(dir0.GetAngleXZ() - dir1.GetAngleXZ());
                    LogUtils.DoLog($"{currentStop}=> {dir0.GetAngleXZ()} x {dir1.GetAngleXZ()}");


                    if (currentStop == stopId || currentStop == 0)
                    {
                        LogUtils.DoLog($"Thats a loop line: { NetManager.instance.m_nodes.m_buffer[currentStop].m_transportLine}");
                        return 0;
                    }
                } while (diff > 90);

                return BuildingManager.instance.FindBuilding(NetManager.instance.m_nodes.m_buffer[currentStop].m_position, 100f, ItemClass.Service.None, ItemClass.SubService.None, Building.Flags.None, Building.Flags.None);

            }
            return 0;
        }

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
                    var districtId = DistrictManager.instance.GetDistrict(BuildingManager.instance.m_buildings.m_buffer[buildingID].m_position);
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
            foreach (var platform in descriptor.m_platforms)
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
                    var targetPlatforms = descriptor.m_platforms;
                    foreach (var platform in targetPlatforms)
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
                    var districtId = DistrictManager.instance.GetDistrict(BuildingManager.instance.m_buildings.m_buffer[buildingID].m_position);
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

        public static void GenerateDefaultBuildingsConfiguration()
        {
            BuildingGroupDescriptorXml[] fileContent = GenerateDefaultDictionary().Select(x => new BuildingGroupDescriptorXml { BuildingName = x.Key, BoardDescriptors = x.Value.ToArray() }).ToArray();
            var serializer = new XmlSerializer(typeof(BuildingGroupDescriptorXml));
            foreach (BuildingGroupDescriptorXml item in fileContent)
            {
                SaveInCommonFolder(serializer, item);
            }
        }
        public static bool SaveInCommonFolder(string buildingName)
        {
            if (m_loadedDescriptors.ContainsKey(buildingName))
            {
                BuildingGroupDescriptorXml item = m_loadedDescriptors[buildingName];
                item.BuildingName = buildingName;
                SaveInCommonFolder(new XmlSerializer(typeof(BuildingGroupDescriptorXml)), item, true);
                return true;
            }
            return false;
        }

        private static void SaveInCommonFolder(XmlSerializer serializer, BuildingGroupDescriptorXml item, bool force = false)
        {
            var filePath = DynamicTextPropsMod.DefaultBuildingsConfigurationFolder + Path.DirectorySeparatorChar + $"{DynamicTextPropsMod.m_defaultFileNameXml}_{item.BuildingName}.xml";
            SaveInPath(serializer, item, force, filePath);
        }
        public static bool SaveInAssetFolder(string buildingName)
        {
            if (m_loadedDescriptors.ContainsKey(buildingName))
            {
                BuildingGroupDescriptorXml item = m_loadedDescriptors[buildingName];
                Package.Asset asset = PackageManager.FindAssetByName(buildingName);
                if (!(asset == null) && !(asset.package == null))
                {
                    var packagePath = asset.package.packagePath;
                    if (packagePath != null)
                    {
                        var filePath = Path.Combine(Path.GetDirectoryName(packagePath), DefaultFilename);
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

        private static Dictionary<string, List<BoardDescriptorBuildingXml>> GenerateDefaultDictionary()
        {

            var basicEOLTextDescriptor = new BoardTextDescriptorBuildingsXml[]{
                             new BoardTextDescriptorBuildingsXml{
                                m_textRelativePosition = new Vector3(0,4.7f, -0.13f) ,
                                m_textRelativeRotation = Vector3.zero,
                                m_maxWidthMeters = 15.5f
                             },
                             new BoardTextDescriptorBuildingsXml{
                                m_textRelativePosition = new Vector3(0,4.7f,0.02f),
                                m_textRelativeRotation = new Vector3(0,180,0),
                                m_maxWidthMeters = 15.5f
                             },
                        };
            var basicWallTextDescriptor = new BoardTextDescriptorBuildingsXml[]{
                             new BoardTextDescriptorBuildingsXml{
                                m_textRelativePosition =new Vector3(0,0.4F,-0.08f) ,
                                m_textRelativeRotation = Vector3.zero,
                                m_maxWidthMeters = 15.5f
                             },
                        };
            var basicTotem = new BoardTextDescriptorBuildingsXml[]{
                             new BoardTextDescriptorBuildingsXml{
                                m_textRelativePosition =new Vector3(-0.01f,2.2f,-0.09f) ,
                                m_textRelativeRotation = new Vector3(0,330,270),
                                m_maxWidthMeters = 2.5f,
                                m_textScale = 0.5f,
                                m_dayEmissiveMultiplier = 0f,
                                m_nightEmissiveMultiplier = 7f,
                                m_useContrastColor = false,
                                m_defaultColor = Color.white
                             },
                             new BoardTextDescriptorBuildingsXml{
                                m_textRelativePosition =new Vector3(-0.01f,2.2f,0.09f) ,
                                m_textRelativeRotation = new Vector3(0,210,270),
                                m_maxWidthMeters = 2.5f,
                                m_textScale = 0.5f,
                                m_dayEmissiveMultiplier = 0f,
                                m_nightEmissiveMultiplier = 7f,
                                m_useContrastColor = false,
                                m_defaultColor = Color.white
                             },
                             new BoardTextDescriptorBuildingsXml{
                                m_textRelativePosition =new Vector3(0.14f,2.2f,0f) ,
                                m_textRelativeRotation = new Vector3(0,90,270),
                                m_maxWidthMeters = 2.5f,
                                m_textScale = 0.5f,
                                m_dayEmissiveMultiplier = 0f,
                                m_nightEmissiveMultiplier = 7f,
                                m_useContrastColor = false,
                                m_defaultColor = Color.white
                             },
                        };

            return new Dictionary<string, List<BoardDescriptorBuildingXml>>
            {
                ["Train Station"] = new List<BoardDescriptorBuildingXml>
                {
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679674753.BoardV6_Data",
                        m_propPosition= new Vector3(8f,6f,0.5F),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicWallTextDescriptor,
                        m_platforms = new int[]{ 0 }
},
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679674753.BoardV6_Data",
                        m_propPosition= new Vector3(-13.5f,6f,0.5F),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicWallTextDescriptor,
                        m_platforms = new int[]{ 0 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679674753.BoardV6_Data",
                        m_propPosition= new Vector3(0,5f,-16),
                        m_propRotation= new Vector3(0,180,0),
                        m_textDescriptors =basicWallTextDescriptor,
                        m_platforms = new int[]{ 1 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName = "1679674753.BoardV6_Data",
                        m_propPosition = new Vector3(-14, 8f, 22),
                        m_propRotation= new Vector3(0,180,0),
                        m_textDescriptors =basicWallTextDescriptor,

                        m_platforms = new int[]{ 0,1 }
                    },
                },
                ["End of the line Trainstation"] = new List<BoardDescriptorBuildingXml>
                {
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(48,-1.5f,-48),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 2 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(32,-1.5f,-48),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{3,4 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(16,-1.5f,-48),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{5,6}
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,-1.5f,-48),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 7,8 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-48,-1.5f,-48),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 13 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-32,-1.5f,-48),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 11,12 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-16,-1.5f,-48),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 9, 10 }
                    },

                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(48,-1.5f,-80),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{2 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(32,-1.5f,-80),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 3,4 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(16,-1.5f,-80),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 5,6 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,-1.5f,-80),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 7,8 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-48,-1.5f,-80),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 13 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-32,-1.5f,-80),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 11,12 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-16,-1.5f,-80),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 9, 10 }
                    },
                     new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(48,-1.5f,-106),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 2 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(32,-1.5f,-106),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 3,4 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(16,-1.5f,-106),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{5,6 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,-1.5f,-106),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 7,8 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-48,-1.5f,-106),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 13 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-32,-1.5f,-106),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 11,12 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-16,-1.5f,-106),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 9, 10 }
                    },
                     new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(48,-1.5f,-133),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{2 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(32,-1.5f,-133),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 3,4 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(16,-1.5f,-133),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 5,6 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,-1.5f,-133),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 7,8 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-48,-1.5f,-133),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{13 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-32,-1.5f,-133),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{11,12 }
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-16,-1.5f,-133),
                        m_propRotation= new Vector3(0,90,0),
                        m_textDescriptors =basicEOLTextDescriptor,

                        m_platforms = new int[]{ 9,10 }
                    },
                },
                ["Large Trainstation"] = new List<BoardDescriptorBuildingXml>
                {
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,-1.5f,-0),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{2},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,-1.5f,-16),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{3,4},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,-1.5f,-32),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{5,6 },

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,-1.5f,-48),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{7,8},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,-1.5f,-64),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{9,10},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,-1.5f,-80),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{11,12},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(41,-1.5f,-95.5f),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{13},
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,-1.5f,-0),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{2},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,-1.5f,-16),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{4,3},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,-1.5f,-32),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{6,5},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,-1.5f,-48),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{8,7},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,-1.5f,-64),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{10,9},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,-1.5f,-80),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{12,11},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(17,-1.5f,-95.5f),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{13},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,-1.5f,-0),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{2},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,-1.5f,-16),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{3,4},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,-1.5f,-32),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{5,6},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,-1.5f,-48),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{7,8},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,-1.5f,-64),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{9,10},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,-1.5f,-80),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{11,12},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-41,-1.5f,-95.5f),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{13},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,-1.5f,-0),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{2},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,-1.5f,-16),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{4,3},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,-1.5f,-32),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{6,5},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,-1.5f,-48),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{8,7},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,-1.5f,-64),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{10,9},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,-1.5f,-80),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{12,11},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-17,-1.5f,-95.5f),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{13},

                    },
                },
                ["Metro Entrance"] = new List<BoardDescriptorBuildingXml>
                {
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679686240.Metro Totem_Data",
                        m_propPosition= new Vector3(4,0,4),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicTotem,
                        m_platforms = new int[]{0,1},

                    },
                },
                ["Monorail Station Standalone"] = new List<BoardDescriptorBuildingXml>
                {
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,6,-0.05f),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{0,1},

                    },
                },
                ["Monorail Station Avenue"] = new List<BoardDescriptorBuildingXml>
                {
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(0.05f,6,0),
                        m_propRotation= new Vector3(0,270,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{0,1},

                    },
                },
                ["Monorail Bus Hub"] = new List<BoardDescriptorBuildingXml>
                {
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(0.05f,6,0),
                        m_propRotation= new Vector3(0,270,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{0,1},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(29.5f,-1,4),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{6,8,10},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(29.5f,-1,-4),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{6,4,2},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-29.5f,-1,4),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{7,9,11},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-29.5f,-1,-4),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{7,5,3},

                    },
                },
                ["Monorail Train Metro Hub"] = new List<BoardDescriptorBuildingXml>
                {
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,8f,-3),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{5},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,5.5f,12),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{2,3},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,8f,27),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{0},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName = "1679674753.BoardV6_Data",
                        m_propPosition = new Vector3(16, 4f, -0.75f),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicWallTextDescriptor,
                        m_platforms = new int[]{6},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName = "1679674753.BoardV6_Data",
                        m_propPosition = new Vector3(-16, 4f, -0.75f),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicWallTextDescriptor,
                        m_platforms = new int[]{6},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName = "1679674753.BoardV6_Data",
                        m_propPosition = new Vector3(44, 4f, -0.75f),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicWallTextDescriptor,
                        m_platforms = new int[]{6},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName = "1679674753.BoardV6_Data",
                        m_propPosition = new Vector3(-44, 4f, -0.75f),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicWallTextDescriptor,
                        m_platforms = new int[]{6},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(52,-2.5f,-16),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{7,8},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-52,-2.5f,-16),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{8,7},
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,-2.5f,-16),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{7,8},
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(52,-2.5f,-31.5f),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{9},

                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(-52,-2.5f,-31.5f),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{9}
                    },
                    new BoardDescriptorBuildingXml

                    {
                        m_propName=    "1679676810.BoardV6plat_Data",
                        m_propPosition= new Vector3(0,-2.5f,-31.5f),
                        m_propRotation= new Vector3(0,0,0),
                        m_textDescriptors =basicEOLTextDescriptor,
                        m_platforms = new int[]{9}

                    }
                }
            };
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