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
    public class BoardGenerator : Redirector<BoardGenerator>
    {
        public override void doLog(string format, params object[] args)
        {
            DTBUtils.doLog(format, args);
        }

        private const float m_pixelRatio = 0.5f;
        private const float m_scaleY = 1.2f;
        private const float m_textScale = 4;
        private readonly Vector2 scalingMatrix = new Vector2(0.015f, 0.015f);

        private Dictionary<String, List<BoardDescriptor>> loadedDescriptors;

        public BuildingBoardContainer[] m_subBuildingObjs;
        private UpdateFlagsBuildings[] m_updateData;
        public bool[] m_updatedIdsColorsLines;

        public override void AwakeBody()
        {
            m_subBuildingObjs = new BuildingBoardContainer[BuildingManager.MAX_BUILDING_COUNT];
            m_updateData = new UpdateFlagsBuildings[BuildingManager.MAX_BUILDING_COUNT];
            m_updatedIdsColorsLines = new bool[BuildingManager.MAX_BUILDING_COUNT];
            loadedDescriptors = GenerateDefaultDictionary();


            TransportManagerOverrides.eventOnLineUpdated += onLineUpdated;
            TransportManager.instance.eventLineColorChanged += (x) => onLineUpdated();
            BuildingManagerOverrides.eventOnBuildingRenamed += onBuildingNameChanged;


            doLog("Loading Boarding Generator");
            #region Release Line Hooks

            var postRenderMeshs = GetType().GetMethod("AfterRenderMeshes", allFlags);
            var origMeth = typeof(BuildingAI).GetMethod("RenderMeshes", allFlags);
            AddRedirect(origMeth, null, postRenderMeshs);
            #endregion

        }
        protected void Reset()
        {
            m_subBuildingObjs = new BuildingBoardContainer[BuildingManager.MAX_BUILDING_COUNT];
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
            m_updateData[id].m_nameNormal = false;
            m_updateData[id].m_nameOutline = false;
            m_updateData[id].m_fixedStrings = false;
        }

        private Quad2 GetBounds(ref Building data)
        {
            int width = data.Width;
            int length = data.Length;
            Vector2 vector = new Vector2(Mathf.Cos(data.m_angle), Mathf.Sin(data.m_angle));
            Vector2 vector2 = new Vector2(vector.y, -vector.x);
            vector *= width * 4f;
            vector2 *= length * 4f;
            Vector2 a = VectorUtils.XZ(data.m_position);
            Quad2 quad = default(Quad2);
            quad.a = a - vector - vector2;
            quad.b = a + vector - vector2;
            quad.c = a + vector + vector2;
            quad.d = a - vector + vector2;
            return quad;
        }
        private Quad2 GetBounds(Vector3 ref1, Vector3 ref2, float halfWidth)
        {
            var ref1v2 = VectorUtils.XZ(ref1);
            var ref2v2 = VectorUtils.XZ(ref2);
            var halfLength = (ref1v2 - ref2v2).magnitude / 2;
            var center = (ref1v2 + ref2v2) / 2;
            var angle = Vector2.Angle(ref1v2, ref2v2);


            Vector2 vector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 vector2 = new Vector2(vector.y, -vector.x);
            vector *= halfWidth;
            vector2 *= halfLength;
            Quad2 quad = default(Quad2);
            quad.a = center - vector - vector2;
            quad.b = center + vector - vector2;
            quad.c = center + vector + vector2;
            quad.d = center - vector + vector2;
            return quad;
        }

        public static void AfterRenderMeshes(BuildingAI __instance, RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance)
        {
            var boardGeneratorInstance = BoardGenerator.instance;
            if (!boardGeneratorInstance.loadedDescriptors.ContainsKey(data.Info.name) || boardGeneratorInstance.loadedDescriptors[data.Info.name].Count == 0)
            {
                return;
            }
            if (boardGeneratorInstance.m_subBuildingObjs[buildingID] == null)
            {
                boardGeneratorInstance.m_subBuildingObjs[buildingID] = new BuildingBoardContainer();
            }
            if (boardGeneratorInstance.m_subBuildingObjs[buildingID]?.m_boardsData?.Count() != boardGeneratorInstance.loadedDescriptors[data.Info.name].Count)
            {
                boardGeneratorInstance.m_subBuildingObjs[buildingID].m_boardsData = new SubBuildingControl[boardGeneratorInstance.loadedDescriptors[data.Info.name].Count];
                boardGeneratorInstance.m_updateData[buildingID].m_nameOutline = false;
                boardGeneratorInstance.m_updateData[buildingID].m_nameNormal = false;
                boardGeneratorInstance.m_updatedIdsColorsLines[buildingID] = false;
            }

            var updatedColors = boardGeneratorInstance.m_updatedIdsColorsLines[buildingID];
            for (var i = 0; i < boardGeneratorInstance.loadedDescriptors[data.Info.name].Count; i++)
            {
                var descriptor = boardGeneratorInstance.loadedDescriptors[data.Info.name][i];
                boardGeneratorInstance.UpdateSubparams(ref boardGeneratorInstance.m_subBuildingObjs[buildingID].m_boardsData[i], buildingID, ref data, cameraInfo, ref instance, descriptor, updatedColors, i);

                boardGeneratorInstance.RenderPropMesh(cameraInfo, buildingID, ref data, layerMask, ref instance, i, descriptor, out Matrix4x4 propMatrix);

                for (int j = 0; j < descriptor.m_textDescriptors.Length; j++)
                {
                    boardGeneratorInstance.RenderTextMesh(cameraInfo, buildingID, ref descriptor, propMatrix, ref descriptor.m_textDescriptors[j], ref boardGeneratorInstance.m_subBuildingObjs[buildingID].m_boardsData[i]);
                }
            }
            boardGeneratorInstance.m_updatedIdsColorsLines[buildingID] = true;
        }

        #region Rendering
        private void RenderPropMesh(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance, int idx, BoardDescriptor descriptor, out Matrix4x4 propMatrix)
        {
            if (!string.IsNullOrEmpty(descriptor.m_propName))
            {
                if (m_subBuildingObjs[buildingID].m_boardsData[idx].m_cachedProp == null)
                {
                    m_subBuildingObjs[buildingID].m_boardsData[idx].m_cachedProp = PrefabCollection<PropInfo>.FindLoaded(descriptor.m_propName);
                    if (m_subBuildingObjs[buildingID].m_boardsData[idx].m_cachedProp == null)
                    {
                        DTBUtils.doErrorLog($"PREFAB NOT FOUND: {descriptor.m_propName}");
                        descriptor.m_propName = null;
                    }
                }
                m_subBuildingObjs[buildingID].m_boardsData[idx].m_cachedProp.m_color0 = GetColor(buildingID, idx);
            }
            propMatrix = RenderProp(buildingID, ref data, cameraInfo, m_subBuildingObjs[buildingID].m_boardsData[idx].m_cachedProp, ref instance, idx, layerMask, descriptor.m_propPosition, Mathf.Deg2Rad * descriptor.m_propRotation);
        }

        private void RenderTextMesh(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref BoardDescriptor descriptor, Matrix4x4 propMatrix, ref BoardTextDescriptor textDescriptor, ref SubBuildingControl ctrl)
        {
            BasicRenderInformation renderInfo = null;
            switch (textDescriptor.m_textType)
            {
                case TextType.BuildingName:
                    renderInfo = GetBuildingNameMesh(buildingID, textDescriptor.m_useOutline);
                    break;
                case TextType.Fixed:
                    renderInfo = GetFixedTextMesh(ref textDescriptor, buildingID);
                    break;
                case TextType.StreetPrefix:
                    break;
                case TextType.StreetSuffix:
                    break;
                case TextType.StreetNameComplete:
                    break;
                case TextType.BuildingNumber:
                    break;
            }
            if (renderInfo == null) return;
            var overflowScaleX = 1f;
            var overflowScaleY = 1f;
            var defaultMultiplierX = textDescriptor.m_textScale * scalingMatrix.x;
            var defaultMultiplierY = textDescriptor.m_textScale * scalingMatrix.y;
            var realWidth = defaultMultiplierX * renderInfo.m_sizeMetersUnscaled.x;
            if (textDescriptor.m_maxWidthMeters > 0 && textDescriptor.m_maxWidthMeters < realWidth)
            {
                overflowScaleX = textDescriptor.m_maxWidthMeters / realWidth;
                if (textDescriptor.m_applyOverflowResizingOnY)
                {
                    overflowScaleY = overflowScaleX;
                }
            }

            var matrix = propMatrix * Matrix4x4.TRS(
                textDescriptor.m_textRelativePosition,
                Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.x, Vector3.left) * Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.y, Vector3.down) * Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.z, Vector3.back),
                new Vector3(defaultMultiplierX * overflowScaleX, defaultMultiplierY * overflowScaleY, 1));
            if (cameraInfo.CheckRenderDistance(matrix.MultiplyPoint(Vector3.zero), Math.Min(3000, 500 * textDescriptor.m_textScale)))
            {
                Material material;
                if (textDescriptor.m_forceColor != Color.clear)
                {
                    if (textDescriptor.m_forcedColorMaterial == null)
                    {
                        textDescriptor.m_forcedColorMaterial = new Material(renderInfo.m_material) { color = textDescriptor.m_forceColor };
                    }
                    material = textDescriptor.m_forcedColorMaterial;
                }
                else if (textDescriptor.m_useContrastColor)
                {
                    if (ctrl.m_cachedContrastColor == Color.black)
                    {
                        if (renderInfo.m_blackMaterial == null)
                        {
                            renderInfo.m_blackMaterial = new Material(renderInfo.m_material) { color = Color.black };
                        }
                        material = renderInfo.m_blackMaterial;
                    }
                    else
                    {
                        material = renderInfo.m_material;
                    }
                }
                else
                {
                    material = renderInfo.m_material;
                }
                Graphics.DrawMesh(renderInfo.m_mesh, matrix, material, 0);
            }
        }


        private Matrix4x4 RenderProp(ushort buildingID, ref Building data, RenderManager.CameraInfo cameraInfo, PropInfo propInfo, ref RenderManager.Instance instance, int idx, int layerMask, Vector3 propPosition, float radAngle)
        {
            DistrictManager instance2 = Singleton<DistrictManager>.instance;
            Randomizer randomizer = new Randomizer((int)buildingID << 6 | (idx + 32));
            Vector3 position = instance.m_dataMatrix1.MultiplyPoint(propPosition);
            float scale = 1;
            if (propInfo != null)
            {
                scale = propInfo.m_minScale + (float)randomizer.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f;
                byte district = instance2.GetDistrict(data.m_position);
                propInfo = propInfo.GetVariation(ref randomizer, ref instance2.m_districts.m_buffer[(int)district]);
                Color color = propInfo.m_color0;
                if ((layerMask & 1 << propInfo.m_prefabDataLayer) != 0 || propInfo.m_hasEffects)
                {
                    Vector4 dataVector = instance.m_dataVector3;
                    if (cameraInfo.CheckRenderDistance(position, propInfo.m_maxRenderDistance))
                    {
                        InstanceID propRenderID2 = this.GetPropRenderID(buildingID, idx, ref data);

                        PropInstance.RenderInstance(cameraInfo, propInfo, propRenderID2, position, scale, data.m_angle + radAngle, color, dataVector, true);

                    }
                }
            }
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(position, Quaternion.AngleAxis((data.m_angle + radAngle) * Mathf.Rad2Deg, Vector3.down), new Vector3(scale, scale, scale));
            return matrix;
        }
        #endregion

        #region Upadate Data
        private BasicRenderInformation GetBuildingNameMesh(ushort buildingID, bool useOutline)
        {
            doLog($"!nameUpdated {buildingID}");


            if (useOutline)
            {
                if (m_subBuildingObjs[buildingID].m_nameSubInfoOutline == null || !m_updateData[buildingID].m_nameOutline)
                {
                    RefreshNameData(ref m_subBuildingObjs[buildingID].m_nameSubInfoOutline, BuildingManager.instance.GetBuildingName(buildingID, new InstanceID()) ?? "DUMMY!!!!!", useOutline);
                    m_updateData[buildingID].m_nameOutline = true;
                }
                return m_subBuildingObjs[buildingID].m_nameSubInfoOutline;
            }
            else
            {
                if (m_subBuildingObjs[buildingID].m_nameSubInfoNormal == null || !m_updateData[buildingID].m_nameNormal)
                {
                    RefreshNameData(ref m_subBuildingObjs[buildingID].m_nameSubInfoNormal, BuildingManager.instance.GetBuildingName(buildingID, new InstanceID()) ?? "DUMMY!!!!!", useOutline);
                    m_updateData[buildingID].m_nameNormal = true;
                }
                return m_subBuildingObjs[buildingID].m_nameSubInfoNormal;
            }
        }
        private BasicRenderInformation GetFixedTextMesh(ref BoardTextDescriptor textDescriptor, ushort buildingID)
        {
            if (textDescriptor.m_generatedFixedTextRenderInfo == null || !m_updateData[buildingID].m_fixedStrings)
            {
                RefreshNameData(ref textDescriptor.m_generatedFixedTextRenderInfo, textDescriptor.m_isFixedTextLocalized ? Locale.Get(textDescriptor.m_fixedText, textDescriptor.m_fixedTextLocaleKey) : textDescriptor.m_fixedText, textDescriptor.m_useOutline);
                m_updateData[buildingID].m_fixedStrings = true;
            }
            return textDescriptor.m_generatedFixedTextRenderInfo;

        }


        private void RefreshNameData(ref BasicRenderInformation result, string name, bool useOutline)
        {
            if (result == null) result = new BasicRenderInformation();
            doLog($"RefreshNameData {name}");
            var font = Singleton<DistrictManager>.instance.m_properties.m_areaNameFont;
            UIFontManager.Invalidate(font);
            UIRenderData uirenderData = UIRenderData.Obtain();
            try
            {
                uirenderData.Clear();
                PoolList<Vector3> vertices = uirenderData.vertices;
                PoolList<Color32> colors = uirenderData.colors;
                PoolList<Vector2> uvs = uirenderData.uvs;
                PoolList<int> triangles = uirenderData.triangles;
                using (UIFontRenderer uifontRenderer = font.ObtainRenderer())
                {

                    float num = 10000f;
                    uifontRenderer.defaultColor = Color.white;
                    uifontRenderer.textScale = m_textScale;
                    uifontRenderer.pixelRatio = m_pixelRatio;
                    uifontRenderer.processMarkup = true;
                    uifontRenderer.multiLine = false;
                    uifontRenderer.wordWrap = false;
                    uifontRenderer.textAlign = UIHorizontalAlignment.Center;
                    uifontRenderer.maxSize = new Vector2(num, 900f);
                    uifontRenderer.multiLine = false;
                    uifontRenderer.opacity = 1;
                    uifontRenderer.shadow = false;
                    uifontRenderer.shadowColor = Color.black;
                    uifontRenderer.shadowOffset = Vector2.zero;
                    uifontRenderer.outline = useOutline;
                    uifontRenderer.outlineSize = (int)(2.5 / m_pixelRatio);
                    uifontRenderer.outlineColor = Color.black;
                    var sizeMeters = uifontRenderer.MeasureString(name) * m_pixelRatio;
                    uifontRenderer.vectorOffset = new Vector3(num * m_pixelRatio * -0.5f, sizeMeters.y * m_scaleY, 0f);
                    uifontRenderer.Render(name, uirenderData);
                    result.m_sizeMetersUnscaled = sizeMeters;
                }
                if (result.m_mesh == null)
                {
                    result.m_mesh = new Mesh();
                }
                result.m_mesh.Clear();
                result.m_mesh.vertices = vertices.ToArray();
                result.m_mesh.colors32 = colors.ToArray();
                result.m_mesh.uv = uvs.ToArray();
                result.m_mesh.triangles = triangles.ToArray();
                result.m_material = new Material(font.material)
                {
                    renderQueue = 0
                };
            }
            finally
            {
                uirenderData.Release();
            }

        }

        private void UpdateSubparams(ref SubBuildingControl ctrl, ushort buildingID, ref Building data, RenderManager.CameraInfo cameraInfo, ref RenderManager.Instance instanceData, BoardDescriptor descriptor, bool updatedIdsColors, int idx)
        {
            BuildingManager instance = Singleton<BuildingManager>.instance;
            if (ctrl == null) ctrl = new SubBuildingControl();
            if (!updatedIdsColors)
            {
                doLog($"!colorUpdated {buildingID}");
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
                doLog($"updatedIdsColors {nearStops.Count} [{string.Join(",", nearStops.Select(x => x.ToString()).ToArray())}], [{string.Join(",", dist.Select(x => x.ToString()).ToArray())}], ");
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

        public Color GetColor(ushort buildingID, int idx)
        {
            return m_subBuildingObjs[buildingID].m_boardsData[idx]?.m_cachedColor ?? Color.white;
        }

        protected virtual InstanceID GetPropRenderID(ushort buildingID, int propIndex, ref Building data)
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
                                m_maxWidthMeters = 15.5f,
                                m_useOutline = false,
                                m_useContrastColor =true
                             },
                             new BoardTextDescriptor{
                                m_textRelativePosition = new Vector3(0,4.3f,0),
                                m_textRelativeRotation = new Vector3(0,180,0),
                                m_maxWidthMeters = 15.5f,
                                m_useOutline = false,
                                m_useContrastColor =true
                             },
                        };
            var basicWallTextDescriptor = new BoardTextDescriptor[]{
                             new BoardTextDescriptor{
                                m_textRelativePosition =new Vector3(0,0,-0.05f) ,
                                m_textRelativeRotation = Vector3.zero,
                                m_maxWidthMeters = 15.5f,
                                m_useOutline = false,
                                m_useContrastColor =true
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


        public class BoardDescriptor
        {
            [XmlAttribute("propName")]
            public string m_propName;
            [XmlAttribute("propPosition")]
            public Vector3 m_propPosition;
            [XmlAttribute("propRotation")]
            public float m_propRotation;
            [XmlArrayItem("textEntry")]
            public BoardTextDescriptor[] m_textDescriptors;
            [XmlAttribute("targetVehicleType")]
            public VehicleInfo.VehicleType? m_targetVehicle = null;

            public Matrix4x4 m_textMatrixTranslation(int idx) => Matrix4x4.Translate(m_textDescriptors[idx].m_textRelativePosition);
            public Matrix4x4 m_textMatrixRotation(int idx) => Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(m_textDescriptors[idx].m_textRelativeRotation), Vector3.one);
        }
        public class BoardTextDescriptor
        {
            [XmlAttribute("relativePosition")]
            public Vector3 m_textRelativePosition;
            [XmlAttribute("relativeRotation")]
            public Vector3 m_textRelativeRotation;
            [XmlAttribute("textScale")]
            public float m_textScale = 1f;
            [XmlAttribute("maxWidth")]
            public float m_maxWidthMeters = 0;
            [XmlAttribute("applyOverflowResizingOnY")]
            public bool m_applyOverflowResizingOnY = false;
            [XmlAttribute("useOutline")]
            public bool m_useOutline = true;
            [XmlAttribute("useContrastColor")]
            public bool m_useContrastColor = false;
            [XmlAttribute("forceColor")]
            public Color m_forceColor = Color.clear;
            [XmlAttribute("textType")]
            public TextType m_textType = TextType.BuildingName;
            [XmlAttribute("fixedText")]
            public string m_fixedText = null;
            [XmlAttribute("fixedTextLocaleCategory")]
            public string m_fixedTextLocaleKey = null;
            [XmlAttribute("fixedTextLocalized")]
            public bool m_isFixedTextLocalized = false;
            [XmlIgnore]
            public BasicRenderInformation m_generatedFixedTextRenderInfo = null;
            [XmlIgnore]
            public Material m_forcedColorMaterial = null;
        }

        public enum TextType
        {
            BuildingName,
            Fixed,
            StreetPrefix,
            StreetSuffix,
            StreetNameComplete,
            BuildingNumber,
        }

        public class BuildingBoardContainer
        {
            public BasicRenderInformation m_nameSubInfoOutline;
            public BasicRenderInformation m_nameSubInfoNormal;
            public SubBuildingControl[] m_boardsData;
        }
        public class SubBuildingControl
        {
            public PropInfo m_cachedProp;
            public Color m_cachedColor = Color.white;
            public Color m_cachedContrastColor = Color.black;
        }
        public class BasicRenderInformation
        {
            public Mesh m_mesh;
            public Material m_material;
            public Material m_blackMaterial;
            public Vector2 m_sizeMetersUnscaled;
        }
        private struct UpdateFlagsBuildings
        {
            public bool m_nameNormal;
            public bool m_nameOutline;
            public bool m_fixedStrings;
            public bool m_streetNormal;
            public bool m_streetOutline;
            public bool m_streetPrefixNormal;
            public bool m_streetPrefixOutline;
            public bool m_streetSuffixNormal;
            public bool m_streetSuffixOutline;
            public bool m_streetNumberNormal;
            public bool m_streetNumberOutline;
        }

    }
}
