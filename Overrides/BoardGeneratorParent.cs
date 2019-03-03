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
    public abstract class BoardGeneratorParent<BG, BBC, SBC, BD, BTD> : Redirector<BG>
        where BG : BoardGeneratorParent<BG, BBC, SBC, BD, BTD>
        where BBC : BuildingBoardContainer
        where BD : BoardDescriptor
        where BTD : BoardTextDescriptor
        where SBC : SubBuildingControl
    {
        public abstract Type TypeToOverride { get; }
        public abstract string MethodNameToOverride { get; }
        public abstract int ObjArraySize { get; }

        public abstract void Initialize();


        public BBC[] m_subBuildingObjs;


        private const float m_pixelRatio = 0.5f;
        private const float m_scaleY = 1.2f;
        private const float m_textScale = 4;
        private readonly Vector2 scalingMatrix = new Vector2(0.015f, 0.015f);

        public override void AwakeBody()
        {
            Initialize();
            m_subBuildingObjs = new BBC[ObjArraySize];

            doLog($"Loading Boarding Generator {typeof(BG)} {GetType()}");
            #region Hooks

            var postRenderMeshs = typeof(BG).GetMethod(MethodNameToOverride, allFlags);
            var origMeth = TypeToOverride.GetMethod(MethodNameToOverride, allFlags);
            doLog($"Patching {origMeth} => {postRenderMeshs}");
            AddRedirect(origMeth, null, postRenderMeshs);
            #endregion

        }

        protected Quad2 GetBounds(ref Building data)
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
        protected Quad2 GetBounds(Vector3 ref1, Vector3 ref2, float halfWidth)
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


        public override void doLog(string format, params object[] args)
        {
            DTBUtils.doLog(format, args);
        }

        protected void RenderPropMesh(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance, int idx, BD descriptor, out Matrix4x4 propMatrix)
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



        #region Rendering
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

        protected virtual InstanceID GetPropRenderID(ushort buildingID, int propIndex, ref Building data)
        {
            InstanceID result = default(InstanceID);
            result.Building = buildingID;
            return result;
        }

        protected void RenderTextMesh(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref BoardDescriptor descriptor, Matrix4x4 propMatrix, ref BTD textDescriptor, ref SBC ctrl)
        {
            BasicRenderInformation renderInfo = null;
            switch (textDescriptor.m_textType)
            {
                case TextType.OwnName:
                    renderInfo = GetOwnNameMesh(buildingID, textDescriptor.m_useOutline);
                    break;
                case TextType.Fixed:
                    renderInfo = GetFixedTextMesh(ref textDescriptor, buildingID);
                    break;
                case TextType.StreetPrefix:
                    renderInfo = GetMeshStreetPrefix(buildingID, textDescriptor.m_useOutline);
                    break;
                case TextType.StreetSuffix:
                    renderInfo = GetMeshStreetSuffix(buildingID, textDescriptor.m_useOutline);
                    break;
                case TextType.StreetNameComplete:
                    renderInfo = GetMeshFullStreetName(buildingID, textDescriptor.m_useOutline);
                    break;
                case TextType.BuildingNumber:
                    renderInfo = GetMeshCurrentNumber(buildingID, textDescriptor.m_useOutline);
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


        protected void RefreshNameData(ref BasicRenderInformation result, string name, bool useOutline)
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

        #endregion
        public abstract Color GetColor(ushort buildingID, int idx);
        #region UpdateData
        protected virtual BasicRenderInformation GetOwnNameMesh(ushort refID, bool useOutline) => null;
        protected virtual BasicRenderInformation GetFixedTextMesh(ref BTD textDescriptor, ushort buildingID) => null;
        protected virtual BasicRenderInformation GetMeshCurrentNumber(ushort refID, bool m_useOutline) => null;
        protected virtual BasicRenderInformation GetMeshFullStreetName(ushort refID, bool m_useOutline) => null;
        protected virtual BasicRenderInformation GetMeshStreetSuffix(ushort refID, bool m_useOutline) => null;
        protected virtual BasicRenderInformation GetMeshStreetPrefix(ushort refID, bool m_useOutline) => null;
        protected abstract void UpdateSubparams(ref SBC ctrl, ushort refID, ref Building data, RenderManager.CameraInfo cameraInfo, ref RenderManager.Instance instanceData, BD descriptor, bool updatedIdsColors, int idx);
        #endregion

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
        public TextType m_textType = TextType.OwnName;
        [XmlAttribute("fixedText")]
        public string m_fixedText = null;
        [XmlAttribute("fixedTextLocaleCategory")]
        public string m_fixedTextLocaleKey = null;
        [XmlAttribute("fixedTextLocalized")]
        public bool m_isFixedTextLocalized = false;
        [XmlIgnore]
        private BasicRenderInformation m_generatedFixedTextRenderInfo;
        [XmlIgnore]
        public BasicRenderInformation GeneratedFixedTextRenderInfo
        {
            get {
                return m_generatedFixedTextRenderInfo;
            }
            set {
                m_generatedFixedTextRenderInfo = value;
                GeneratedFixedTextRenderInfoTick = SimulationManager.instance.m_currentTickIndex;
            }
        }
        [XmlIgnore]
        public Material m_forcedColorMaterial = null;
        [XmlIgnore]
        public uint GeneratedFixedTextRenderInfoTick { get; private set; }
    }

    public enum TextType
    {
        OwnName,
        Fixed,
        StreetPrefix,
        StreetSuffix,
        StreetNameComplete,
        BuildingNumber,
    }

}
