using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using Klyte.DynamicTextBoards.Managers;
using Klyte.DynamicTextBoards.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BuildingInfo;
using static Klyte.DynamicTextBoards.Managers.BoardManager;

namespace Klyte.DynamicTextBoards.AI
{
    public class StationBoardAI : DummyBuildingAI
    {
        private static void doLog(string format, params object[] args)
        {

            KCAIUtils.doLog(format, args);

        }

        private Quad2 GetBounds(ref Building data)
        {
            int width = data.Width;
            int length = data.Length;
            Vector2 vector = new Vector2(Mathf.Cos(data.m_angle), Mathf.Sin(data.m_angle));
            Vector2 vector2 = new Vector2(vector.y, -vector.x);
            vector *= (float)width * 4f;
            vector2 *= (float)length * 4f;
            Vector2 a = VectorUtils.XZ(data.m_position);
            Quad2 quad = default(Quad2);
            quad.a = a - vector - vector2;
            quad.b = a + vector - vector2;
            quad.c = a + vector + vector2;
            quad.d = a - vector + vector2;
            return quad;
        }
        // Token: 0x0600039A RID: 922 RVA: 0x0003A744 File Offset: 0x00038B44
        public override void RenderMeshes(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance)
        {

            ushort parent = Building.FindParentBuilding(buildingID);
            if (parent == 0 || this.m_info.m_subMeshes == null || this.m_info.m_subMeshes.Length == 0)
            {
                base.RenderMeshes(cameraInfo, buildingID, ref data, layerMask, ref instance);
                return;
            }
            var submeshIndex = UpdateSubparams(ref BoardManager.instance.m_subBuildingObjs[buildingID], buildingID, ref data, cameraInfo, ref instance, ref BuildingManager.instance.m_buildings.m_buffer[parent]);
            this.m_info.m_rendered = true;
            if (this.m_info.m_mesh != null)
            {
                BuildingAI.RenderMesh(cameraInfo, buildingID, ref data, this.m_info, ref instance);
            }

            BuildingInfo.MeshInfo meshInfo = this.m_info.m_subMeshes[submeshIndex];


            BuildingInfoSub buildingInfoSub = meshInfo.m_subInfo as BuildingInfoSub;
            buildingInfoSub.m_rendered = true;

            var matrix = instance.m_dataMatrix1 * meshInfo.m_matrix * Matrix4x4.Scale(scalingMatrix);
            Graphics.DrawMesh(buildingInfoSub.m_mesh, matrix, buildingInfoSub.m_material, buildingInfoSub.m_prefabDataLayer);
        }


        private int UpdateSubparams(ref SubBuildingControl ctrl, ushort buildingID, ref Building data, RenderManager.CameraInfo cameraInfo, ref RenderManager.Instance instanceData, ref Building parentData)
        {
            BuildingManager instance = Singleton<BuildingManager>.instance;
            ushort parentId = Building.FindParentBuilding(buildingID);
            if (ctrl == null)
            {
                ctrl = new SubBuildingControl();
                doLog($"!colorUpdated {buildingID} => {parentId}");
                Color color;
                var nearStops = KlyteUtils.FindNearStops(data.m_position, parentData.Info.m_class.m_service, true, 400f, out List<float> dist, GetBounds(ref parentData));
                doLog($"RefreshNameData {nearStops.Count} [{string.Join(",", nearStops.Select(x => x.ToString()).ToArray())}], [{string.Join(",", dist.Select(x => x.ToString()).ToArray())}], ");
                if (nearStops.Count > 0)
                {
                    var effNearStopId = nearStops[dist.IndexOf(dist.Min(x => x))];
                    var stopPos = NetManager.instance.m_nodes.m_buffer[effNearStopId].m_position;
                    color = TransportManager.instance.GetLineColor(NetManager.instance.m_nodes.m_buffer[effNearStopId].m_transportLine);
                }
                else
                {
                    color = Color.black;
                }
                ctrl.m_cachedColor = color;
                ctrl.m_cachedContrastColor = KlyteUtils.contrastColor(ctrl.m_cachedColor);

                m_info.m_color0 = color;
                m_info.m_color1 = color;
                m_info.m_color2 = color;
                m_info.m_color3 = color;
                m_info.m_useColorVariations = false;
                m_info.m_colorizeEverything = true;
                m_info.m_rendered = false;

                instance.UpdateBuildingColors(buildingID, buildingID);
            }


            if (!BoardManager.instance.m_updatedIdsName[parentId])
            {
                doLog($"!nameUpdated {buildingID}");
                if (BoardManager.instance.m_submeshIndexes[parentId] == 0)
                {
                    instance.m_buildings.m_buffer[buildingID].Info.m_subMeshes = instance.m_buildings.m_buffer[buildingID].Info.m_subMeshes.Concat(new MeshInfo[] { new MeshInfo(instance.m_buildings.m_buffer[buildingID].Info.m_subMeshes[0]) }).ToArray();
                    BoardManager.instance.m_submeshIndexes[parentId] = instance.m_buildings.m_buffer[buildingID].Info.m_subMeshes.Length - 1;
                    instance.m_buildings.m_buffer[buildingID].Info.m_subMeshes[BoardManager.instance.m_submeshIndexes[parentId]].EditorInstantiateProperties();
                    instance.m_buildings.m_buffer[buildingID].Info.m_subMeshes[BoardManager.instance.m_submeshIndexes[parentId]].m_subInfo.InitializePrefab();
                    instance.m_buildings.m_buffer[buildingID].Info.m_subMeshes[BoardManager.instance.m_submeshIndexes[parentId]].m_subInfo.gameObject.transform.SetParent(instance.m_buildings.m_buffer[buildingID].Info.gameObject.transform);
                    instance.m_buildings.m_buffer[buildingID].Info.m_subMeshes[BoardManager.instance.m_submeshIndexes[parentId]].m_subInfo.gameObject.SetActive(true);
                    instance.m_buildings.m_buffer[buildingID].Info.m_subMeshes[BoardManager.instance.m_submeshIndexes[parentId]].m_angle = instance.m_buildings.m_buffer[buildingID].Info.m_subMeshes[0].m_angle;
                    instance.m_buildings.m_buffer[buildingID].Info.m_subMeshes[BoardManager.instance.m_submeshIndexes[parentId]].m_position = instance.m_buildings.m_buffer[buildingID].Info.m_subMeshes[0].m_position;
                    instance.m_buildings.m_buffer[buildingID].Info.m_subMeshes[BoardManager.instance.m_submeshIndexes[parentId]].m_matrix = instance.m_buildings.m_buffer[buildingID].Info.m_subMeshes[0].m_matrix;
                }
                RefreshNameData(ref instance.m_buildings.m_buffer[buildingID].Info.m_subMeshes[BoardManager.instance.m_submeshIndexes[parentId]], instance.GetBuildingName(parentId, new InstanceID()) ?? "DUMMY!!!!!", ctrl.m_cachedContrastColor, out Vector2 size);


                BoardManager.instance.m_updatedIdsName[parentId] = true;
            }
            return BoardManager.instance.m_submeshIndexes[parentId];
            //RenderName(cameraInfo, ref ctrl.m_cachedMaterial, ref ctrl.m_cachedMesh, ref instanceData);
        }

        public override Color GetColor(ushort buildingID, ref Building data, InfoManager.InfoMode infoMode)
        {
            return BoardManager.instance.m_subBuildingObjs[buildingID]?.m_cachedColor ?? base.GetColor(buildingID, ref data, infoMode);
        }

        //private void RenderName(RenderManager.CameraInfo cameraInfo, ref Material nameMaterial, ref Mesh mesh, ref RenderManager.Instance instanceData)
        //{
        //    if (mesh == null || nameMaterial == null)
        //    {
        //        return;
        //    }
        //    //Matrix4x4 dataMatrix = instanceData.m_dataMatrix2;
        //    //Vector4 column = dataMatrix.GetColumn(0);
        //    //Vector4 column2 = dataMatrix.GetColumn(3);
        //    //Vector3 rhs = instanceData.m_position - cameraInfo.m_position;
        //    //Vector3 vector = Vector3.Cross(cameraInfo.m_up, rhs);
        //    //if ((column2.x - column.x) * vector.x + (column2.z - column.z) * vector.z < 0f)
        //    //{
        //    //    Vector4 column3 = dataMatrix.GetColumn(1);
        //    //    Vector4 column4 = dataMatrix.GetColumn(2);
        //    //    dataMatrix.SetColumn(0, column2);
        //    //    dataMatrix.SetColumn(1, column4);
        //    //    dataMatrix.SetColumn(2, column3);
        //    //    dataMatrix.SetColumn(3, column);
        //    //}
        //    //nameMaterial.SetMatrix(NetManager.instance.ID_LeftMatrix, dataMatrix);
        //    doLog($"instanceData.m_position = {instanceData.m_position}");
        //    //doLog($"dataMatrix = {dataMatrix}");
        //    if (nameMaterial.SetPass(0))
        //    {
        //        BuildingManager.instance.m_drawCallData.m_overlayCalls = BuildingManager.instance.m_drawCallData.m_overlayCalls + 1;
        //        Graphics.DrawMeshNow(mesh, instanceData.m_position, Quaternion.identity);
        //    }
        //}

        private float m_pixelRatio = 0.5f;
        [CustomizableProperty("DSNB_LineHeight")]
        private float m_scaleY = 1.2f;
        [CustomizableProperty("DSNB_TextSize")]
        private float m_textScale = 4;
        [CustomizableProperty("DSNB_ScaleMatrix")]
        private Vector3 scalingMatrix = new Vector3(0.015f, 0.015f, 1f);

        private void RefreshNameData(ref MeshInfo result, string name, Color targetColor, out Vector2 size)
        {
            doLog($"RefreshNameData {name} {targetColor}");
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
                    uifontRenderer.outline = true;
                    uifontRenderer.outlineSize = (int)(2.5 / m_pixelRatio);
                    uifontRenderer.outlineColor = Color.black;
                    size = uifontRenderer.MeasureString(name);
                    uifontRenderer.vectorOffset = new Vector3(num * m_pixelRatio * -0.5f, size.y * m_pixelRatio * m_scaleY, 0f);
                    uifontRenderer.Render(name, uirenderData);
                }
                if (result.m_subInfo.m_mesh == null)
                {
                    result.m_subInfo.m_mesh = new Mesh();
                }
                result.m_subInfo.m_mesh.Clear();
                result.m_subInfo.m_mesh.vertices = vertices.ToArray();
                result.m_subInfo.m_mesh.colors32 = colors.ToArray();
                result.m_subInfo.m_mesh.uv = uvs.ToArray();
                result.m_subInfo.m_mesh.triangles = triangles.ToArray();
                result.m_subInfo.m_material = new Material(font.material)
                {
                    renderQueue = 0,
                    //color = targetColor
                };
            }
            finally
            {
                uirenderData.Release();
            }
        }

    }
}
