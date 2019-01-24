using ColossalFramework;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.CustomAI.AI
{
    public class StationBoardAI : DummyBuildingAI
    {
        private static bool debug = true;
        private Dictionary<ushort, SubmeshControl> m_buildingObjs = new Dictionary<ushort, SubmeshControl>();

        private static void doLog(string format, params object[] args)
        {

            if (debug)
            {
                try
                {
                    Debug.LogWarningFormat("KCv" + KlyteCustomAI.version + " " + format, args);
                }
                catch
                {
                    Debug.LogErrorFormat("KltUtils: Erro ao fazer log: {0} (args = {1})", format, args == null ? "[]" : string.Join(",", args.Select(x => x != null ? x.ToString() : "--NULL--").ToArray()));
                }
            }

        }

        // Token: 0x0600039A RID: 922 RVA: 0x0003A744 File Offset: 0x00038B44
        public override void RenderMeshes(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance)
        {

            ushort num = Building.FindParentBuilding(buildingID);
            if (num == 0)
            {
                base.RenderMeshes(cameraInfo, buildingID, ref data, layerMask, ref instance);
                return;
            }
            this.m_info.m_rendered = true;
            if (this.m_info.m_mesh != null)
            {
                BuildingAI.RenderMesh(cameraInfo, buildingID, ref data, this.m_info, ref instance);
            }
            m_buildingObjs.TryGetValue(buildingID, out SubmeshControl obj);
            UpdateSubparams(ref obj, buildingID);
            m_buildingObjs[buildingID] = obj;
            Building.Flags flags = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)num].m_flags;
            for (int i = 0; i < this.m_info.m_subMeshes.Length; i++)
            {
                BuildingInfo.MeshInfo meshInfo = this.m_info.m_subMeshes[i];
                if (((meshInfo.m_flagsRequired | meshInfo.m_flagsForbidden) & flags) == meshInfo.m_flagsRequired)
                {
                    BuildingInfoSub buildingInfoSub = meshInfo.m_subInfo as BuildingInfoSub;
                    buildingInfoSub.m_rendered = true;
                    if (buildingInfoSub.m_subMeshes != null && buildingInfoSub.m_subMeshes.Length != 0)
                    {
                        for (int j = 0; j < buildingInfoSub.m_subMeshes.Length; j++)
                        {
                            BuildingInfo.MeshInfo meshInfo2 = buildingInfoSub.m_subMeshes[j];
                            if (((meshInfo2.m_flagsRequired | meshInfo2.m_flagsForbidden) & flags) == meshInfo2.m_flagsRequired)
                            {
                                BuildingInfoSub buildingInfoSub2 = meshInfo2.m_subInfo as BuildingInfoSub;
                                buildingInfoSub2.m_rendered = true;
                                BuildingAI.RenderMesh(cameraInfo, this.m_info, buildingInfoSub2, meshInfo.m_matrix, ref instance);
                            }
                        }
                    }
                    else
                    {
                        BuildingAI.RenderMesh(cameraInfo, this.m_info, buildingInfoSub, meshInfo.m_matrix, ref instance);
                    }
                }
            }
        }


        private void UpdateSubparams(ref SubmeshControl ctrl, ushort buildingID)
        {
            if (ctrl == null) ctrl = new SubmeshControl();
            ushort num = Building.FindParentBuilding(buildingID);
            BuildingManager instance = Singleton<BuildingManager>.instance;
            if (!ctrl.m_colorUpdated)
            {
                doLog($"!colorUpdated {buildingID} => {num}");
                Color color;
                var nearStops = KlyteUtils.FindNearStops(instance.m_buildings.m_buffer[(int)num].m_position, instance.m_buildings.m_buffer[(int)num].Info.m_class.m_service, true, 200f, out List<float> dist);
                doLog($"RefreshNameData {nearStops.Count} [{string.Join(",", nearStops.Select(x => nearStops.ToString()).ToArray())}]");
                if (nearStops.Count > 0)
                {
                    var effNearStopId = nearStops[dist.IndexOf(dist.Min(x => x))];
                    var stopPos = NetManager.instance.m_nodes.m_buffer[effNearStopId].m_position;
                    if (instance.m_buildings.m_buffer[(int)num].RayCast(num, new ColossalFramework.Math.Segment3(new Vector3(stopPos.x, 10000, stopPos.z), new Vector3(stopPos.x, -10000, stopPos.z)), out float t))
                    {
                        color = TransportManager.instance.GetLineColor(NetManager.instance.m_nodes.m_buffer[effNearStopId].m_transportLine);
                    }
                    else
                    {
                        color = Color.gray;
                    }
                }
                else
                {
                    color = Color.black;
                }
                ctrl.m_cachedColor = color;
                ctrl.m_cachedContrastColor = KlyteUtils.contrastColor(ctrl.m_cachedColor);

                ctrl.m_colorUpdated = true;
            }


            if (!ctrl.m_nameUpdated)
            {
                doLog($"!nameUpdated {buildingID}");

                RefreshNameData(ref ctrl.m_cachedMesh, instance.GetBuildingName(num, new InstanceID()) ?? "DUMMY!!!!!", ctrl.m_cachedContrastColor, out Vector2 size, ref ctrl.m_cachedMaterial);

                ctrl.m_nameUpdated = true;
            }

            if (buildingID != 0 && ctrl.m_cachedMesh != null && ctrl.m_cachedMaterial != null && ctrl.m_cachedMaterial.SetPass(0))
            {
                Graphics.DrawMeshNow(ctrl.m_cachedMesh, instance.m_buildings.m_buffer[buildingID].m_position, Quaternion.identity);
            }
        }

        public override Color GetColor(ushort buildingID, ref Building data, InfoManager.InfoMode infoMode)
        {
            return m_buildingObjs.TryGetValue(buildingID, out SubmeshControl submesh) ? submesh.m_cachedColor : Color.clear;
        }

        private static float textScale = 5f;
        private static float pixelRatio = 10f;
        private static Vector2 maxSize = new Vector2(100f, 10f);


        private void RefreshNameData(ref Mesh result, string name, Color targetColor, out Vector2 size, ref Material fontMaterial)
        {
            doLog($"RefreshNameData {name} {targetColor}");
            var font = FindObjectOfType<UILabel>().font;
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
                    uifontRenderer.defaultColor = Color.green;
                    uifontRenderer.textScale = textScale;
                    uifontRenderer.pixelRatio = pixelRatio;
                    uifontRenderer.processMarkup = true;
                    uifontRenderer.multiLine = false;
                    uifontRenderer.wordWrap = false;
                    uifontRenderer.textAlign = UIHorizontalAlignment.Center;
                    uifontRenderer.maxSize = maxSize;
                    uifontRenderer.shadow = false;
                    uifontRenderer.shadowColor = Color.black;
                    uifontRenderer.shadowOffset = Vector2.one;
                    size = uifontRenderer.MeasureString(name);
                    uifontRenderer.vectorOffset = new Vector3(-50f, size.y * 0.5f, 0f);
                    uifontRenderer.Render(name, uirenderData);
                }
                if (result == null)
                {
                    result = new Mesh();
                }
                result.Clear();
                result.vertices = vertices.ToArray();
                result.colors32 = colors.ToArray();
                result.uv = uvs.ToArray();
                result.triangles = triangles.ToArray();
                if (fontMaterial == null)
                {
                    fontMaterial = new Material(font.material)
                    {
                        shader = Shader.Find("UI/Dynamic Font Shader")
                    };
                }
                fontMaterial.color = targetColor;
            }
            finally
            {
                uirenderData.Release();
            }
        }

        private class SubmeshControl
        {
            public Mesh m_cachedMesh;
            public Material m_cachedMaterial;
            public Color m_cachedColor = Color.white;
            public Color m_cachedContrastColor = Color.black;

            public bool m_nameUpdated;
            public bool m_colorUpdated;
        }
    }
}
