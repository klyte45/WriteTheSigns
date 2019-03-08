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
    public abstract class BoardGeneratorParent<BG, BBC, CC, BRI, BD, BTD, MRT> : Redirector<BG>
        where BG : BoardGeneratorParent<BG, BBC, CC, BRI, BD, BTD, MRT>
        where BBC : IBoardBunchContainer<CC, BRI>
        where BD : BoardDescriptor
        where BTD : BoardTextDescriptor
        where CC : CacheControl
        where BRI : BasicRenderInformation, new()
    {
        public abstract int ObjArraySize { get; }
        public abstract UIDynamicFont DrawFont { get; }

        protected static Shader TextShader => DTBResourceLoader.instance.GetLoadedShader("Klyte/DynamicTextBoards/klytetextboardsfrag") ?? DistrictManager.instance.m_properties.m_areaNameShader;
        protected static Shader TextShaderIlum => DTBResourceLoader.instance.GetLoadedShader("Klyte/DynamicTextBoards/klytetextboards") ?? DistrictManager.instance.m_properties.m_areaNameShader;



        public static readonly int m_shaderPropColor = Shader.PropertyToID("_Color");
        public static readonly int m_shaderPropEmissive = Shader.PropertyToID("_Emission");
        public static readonly int m_shaderPropDepth = Shader.PropertyToID("_Depth");
        protected uint lastFontUpdateFrame = SimulationManager.instance.m_currentTickIndex;
        public abstract void Initialize();


        public BBC[] m_boardsContainers;


        private const float m_pixelRatio = 0.5f;
        private const float m_scaleY = 1.2f;
        private const float m_textScale = 4;
        private readonly Vector2 scalingMatrix = new Vector2(0.015f, 0.015f);

        public override void AwakeBody()
        {
            Font.textureRebuilt += OnTextureRebuilt;
            Initialize();
            m_boardsContainers = new BBC[ObjArraySize];

            doLog($"Loading Boards Generator {typeof(BG)}");


        }

        protected void BuildSurfaceFont(out UIDynamicFont font, string fontName)
        {
            font = ScriptableObject.CreateInstance<UIDynamicFont>();

            font.shader = TextShader;
            font.material = new Material(Singleton<DistrictManager>.instance.m_properties.m_areaNameFont.material);
            font.baseline = (Singleton<DistrictManager>.instance.m_properties.m_areaNameFont as UIDynamicFont).baseline;
            font.size = (Singleton<DistrictManager>.instance.m_properties.m_areaNameFont as UIDynamicFont).size;
            font.lineHeight = (Singleton<DistrictManager>.instance.m_properties.m_areaNameFont as UIDynamicFont).lineHeight;
            font.baseFont = Font.CreateDynamicFontFromOSFont(fontName, 16);


            font.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack | MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }


        private void OnTextureRebuilt(Font obj)
        {
            if (obj == DrawFont.baseFont)
            {
                lastFontUpdateFrame = SimulationManager.instance.m_currentTickIndex;
                OnTextureRebuilt();
            }
        }
        protected abstract void OnTextureRebuilt();

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

        protected void RenderPropMesh(ref PropInfo propInfo, RenderManager.CameraInfo cameraInfo, ushort refId, int boardIdx, int secIdx, int layerMask, float refAngleRad, Vector3 position, Vector4 dataVector, ref string propName, float propAngle, out Matrix4x4 propMatrix, out bool rendered)
        {
            if (!string.IsNullOrEmpty(propName))
            {
                if (propInfo == null)
                {
                    propInfo = PrefabCollection<PropInfo>.FindLoaded(propName);
                    if (propInfo == null)
                    {
                        DTBUtils.doErrorLog($"PREFAB NOT FOUND: {propName}");
                        propName = null;
                    }
                }
                propInfo.m_color0 = GetColor(refId, boardIdx, secIdx);
            }
            propMatrix = RenderProp(refId, refAngleRad, cameraInfo, propInfo, position, dataVector, boardIdx, layerMask, Mathf.Deg2Rad * propAngle, out rendered);
        }



        #region Rendering
        private Matrix4x4 RenderProp(ushort refId, float refAngleRad, RenderManager.CameraInfo cameraInfo, PropInfo propInfo, Vector3 position, Vector4 dataVector, int idx, int layerMask, float radAngle, out bool rendered)
        {
            rendered = false;
            DistrictManager instance2 = Singleton<DistrictManager>.instance;
            Randomizer randomizer = new Randomizer((int)refId << 6 | (idx + 32));
            float scale = 1;
            if (propInfo != null)
            {
                scale = propInfo.m_minScale + (float)randomizer.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f;
                byte district = instance2.GetDistrict(position);
                propInfo = propInfo.GetVariation(ref randomizer, ref instance2.m_districts.m_buffer[(int)district]);
                Color color = propInfo.m_color0;
                if ((layerMask & 1 << propInfo.m_prefabDataLayer) != 0 || propInfo.m_hasEffects)
                {
                    if (cameraInfo.CheckRenderDistance(position, propInfo.m_maxRenderDistance))
                    {
                        InstanceID propRenderID2 = this.GetPropRenderID(refId);
                        PropInstance.RenderInstance(cameraInfo, propInfo, propRenderID2, position, scale, refAngleRad + radAngle, color, dataVector, true);
                        rendered = true;
                    }
                }
            }
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(position, Quaternion.AngleAxis((refAngleRad + radAngle) * Mathf.Rad2Deg, Vector3.down), new Vector3(scale, scale, scale));
            return matrix;
        }

        protected abstract InstanceID GetPropRenderID(ushort refID);

        protected void RenderTextMesh(RenderManager.CameraInfo cameraInfo, MRT refID, int boardIdx, int secIdx, ref BoardDescriptor descriptor, Matrix4x4 propMatrix, ref BTD textDescriptor, ref CC ctrl, MaterialPropertyBlock materialPropertyBlock)
        {
            BRI renderInfo = null;
            switch (textDescriptor.m_textType)
            {
                case TextType.OwnName:
                    renderInfo = GetOwnNameMesh(refID, boardIdx, secIdx);
                    break;
                case TextType.Fixed:
                    renderInfo = GetFixedTextMesh(ref textDescriptor, refID);
                    break;
                case TextType.StreetPrefix:
                    renderInfo = GetMeshStreetPrefix(refID, boardIdx, secIdx);
                    break;
                case TextType.StreetSuffix:
                    renderInfo = GetMeshStreetSuffix(refID, boardIdx, secIdx);
                    break;
                case TextType.StreetNameComplete:
                    renderInfo = GetMeshFullStreetName(refID, boardIdx, secIdx);
                    break;
                case TextType.BuildingNumber:
                    renderInfo = GetMeshCurrentNumber(refID, boardIdx, secIdx);
                    break;
                case TextType.Custom1:
                    renderInfo = GetMeshCustom1(refID, boardIdx, secIdx);
                    break;
                case TextType.Custom2:
                    renderInfo = GetMeshCustom2(refID, boardIdx, secIdx);
                    break;
                case TextType.Custom3:
                    renderInfo = GetMeshCustom3(refID, boardIdx, secIdx);
                    break;
            }
            if (renderInfo == null) return;
            var overflowScaleX = 1f;
            var overflowScaleY = 1f;
            var defaultMultiplierX = textDescriptor.m_textScale * scalingMatrix.x;
            var defaultMultiplierY = textDescriptor.m_textScale * scalingMatrix.y;
            var realWidth = defaultMultiplierX * renderInfo.m_sizeMetersUnscaled.x;
            var targetRelativePosition = textDescriptor.m_textRelativePosition;
            if (textDescriptor.m_maxWidthMeters > 0 && textDescriptor.m_maxWidthMeters < realWidth)
            {
                overflowScaleX = textDescriptor.m_maxWidthMeters / realWidth;
                if (textDescriptor.m_applyOverflowResizingOnY)
                {
                    overflowScaleY = overflowScaleX;
                }
            }
            else if (textDescriptor.m_maxWidthMeters > 0 && textDescriptor.m_textAlign != UIHorizontalAlignment.Center)
            {
                var factor = (textDescriptor.m_textAlign == UIHorizontalAlignment.Left) == (((textDescriptor.m_textRelativeRotation.y) % 360 + 810) % 360 > 180) ? 0.5f : -0.5f;
                targetRelativePosition += new Vector3((textDescriptor.m_maxWidthMeters - realWidth) * factor, 0, 0);
            }

            var matrix = propMatrix * Matrix4x4.TRS(
                targetRelativePosition,
                Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.x, Vector3.left) * Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.y, Vector3.down) * Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.z, Vector3.back),
                new Vector3(defaultMultiplierX * overflowScaleX, defaultMultiplierY * overflowScaleY, 1));
            if (cameraInfo.CheckRenderDistance(matrix.MultiplyPoint(Vector3.zero), Math.Min(3000, 200 * textDescriptor.m_textScale)))
            {
                if (textDescriptor.m_useContrastColor)
                {
                    materialPropertyBlock.SetColor(m_shaderPropColor, GetContrastColor(refID, boardIdx, secIdx));
                }
                else if (textDescriptor.m_defaultColor != Color.clear)
                {
                    materialPropertyBlock.SetColor(m_shaderPropColor, textDescriptor.m_defaultColor);
                }
                else
                {
                    materialPropertyBlock.SetColor(m_shaderPropColor, Color.white);
                }

                materialPropertyBlock.SetFloat(m_shaderPropEmissive, 1.4f * (SimulationManager.instance.m_isNightTime ? textDescriptor.m_nightEmissiveMultiplier : textDescriptor.m_dayEmissiveMultiplier));
                materialPropertyBlock.SetFloat(m_shaderPropDepth, 2);
                DrawFont.material.shader = textDescriptor.ShaderOverride ?? TextShader;
                Graphics.DrawMesh(renderInfo.m_mesh, matrix, DrawFont.material, ctrl?.m_cachedProp?.m_prefabDataLayer ?? 10, cameraInfo.m_camera, 0, materialPropertyBlock, false, true, true);
            }
        }

        protected void UpdateMeshStreetSuffix(ushort idx, ref BRI bri)
        {
            doLog($"!UpdateMeshStreetSuffix {idx}");
            Type t = Type.GetType("Klyte.Addresses.Overrides.NetManagerOverrides, Addresses");
            string result = "";
            if (t != null)
            {
                List<ushort> usedQueue = new List<ushort>();
                result = DTBUtils.RunPrivateStaticAction(t, "GenerateSegmentNameInternal", idx, result, usedQueue, true)[1].ToString();

            }
            else
            {
                if ((NetManager.instance.m_segments.m_buffer[idx].m_flags & NetSegment.Flags.CustomName) != 0)
                {
                    doLog($"!UpdateMeshStreetSuffix Custom");
                    InstanceID id = default(InstanceID);
                    id.NetSegment = idx;
                    result = Singleton<InstanceManager>.instance.GetName(id);
                }
                else
                {
                    doLog($"!UpdateMeshStreetSuffix NonCustom {NetManager.instance.m_segments.m_buffer[idx].m_nameSeed}");
                    if (NetManager.instance.m_segments.m_buffer[idx].Info.m_netAI is RoadBaseAI ai)
                    {
                        Randomizer randomizer = new Randomizer((int)NetManager.instance.m_segments.m_buffer[idx].m_nameSeed);
                        randomizer.Int32(12);
                        result = DTBUtils.RunPrivateMethod<string>(ai, "GenerateStreetName", randomizer);
                        //}
                    }
                    else
                    {
                        result = "???";
                    }
                }
            }
            //(ushort segmentID, ref string __result, ref List<ushort> usedQueue, bool defaultPrefix, bool removePrefix = false)
            RefreshNameData(ref bri, result);
        }

        protected void UpdateMeshFullNameStreet(ushort idx, ref BRI bri)
        {
            //(ushort segmentID, ref string __result, ref List<ushort> usedQueue, bool defaultPrefix, bool removePrefix = false)
            var name = NetManager.instance.GetDefaultSegmentName(idx);
            doLog($"!GenName {name} for {idx}");
            RefreshNameData(ref bri, name);
        }

        protected void RefreshNameData(ref BRI result, string name)
        {
            if (result == null) result = new BRI();
            UIFontManager.Invalidate(DrawFont);
            UIRenderData uirenderData = UIRenderData.Obtain();
            try
            {
                uirenderData.Clear();
                PoolList<Vector3> vertices = uirenderData.vertices;
                PoolList<Color32> colors = uirenderData.colors;
                PoolList<Vector2> uvs = uirenderData.uvs;
                PoolList<int> triangles = uirenderData.triangles;
                using (UIFontRenderer uifontRenderer = DrawFont.ObtainRenderer())
                {

                    float num = 10000f;
                    uifontRenderer.colorizeSprites = true;
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
                    uifontRenderer.outline = false;
                    var sizeMeters = uifontRenderer.MeasureString(name) * m_pixelRatio;
                    uifontRenderer.vectorOffset = new Vector3(num * m_pixelRatio * -0.5f, sizeMeters.y * m_scaleY, 0f);
                    uifontRenderer.Render(name, uirenderData);
                    result.m_sizeMetersUnscaled = sizeMeters;
                }
                if (result.m_mesh == null)
                {
                    result.m_mesh = new Mesh();
                }
                doLog(uirenderData.ToString());
                result.m_mesh.Clear();
                result.m_mesh.vertices = vertices.ToArray();
                result.m_mesh.colors32 = colors.Select(x => new Color32(x.a, x.a, x.a, x.a)).ToArray();
                result.m_mesh.uv = uvs.ToArray();
                result.m_mesh.triangles = triangles.ToArray();
                result.m_frameDrawTime = lastFontUpdateFrame;
            }
            finally
            {
                uirenderData.Release();
            }

        }

        #endregion
        public abstract Color GetColor(ushort buildingID, int idx, int secIdx);
        public abstract Color GetContrastColor(MRT refID, int boardIdx, int secIdx);

        #region UpdateData
        protected virtual BRI GetOwnNameMesh(MRT refID, int boardIdx, int secIdx) => null;
        protected virtual BRI GetMeshCurrentNumber(MRT refID, int boardIdx, int secIdx) => null;
        protected virtual BRI GetMeshFullStreetName(MRT refID, int boardIdx, int secIdx) => null;
        protected virtual BRI GetMeshStreetSuffix(MRT refID, int boardIdx, int secIdx) => null;
        protected virtual BRI GetMeshStreetPrefix(MRT refID, int boardIdx, int secIdx) => null;
        protected virtual BRI GetMeshCustom1(MRT refID, int boardIdx, int secIdx) => null;
        protected virtual BRI GetMeshCustom2(MRT refID, int boardIdx, int secIdx) => null;
        protected virtual BRI GetMeshCustom3(MRT refID, int boardIdx, int secIdx) => null;
        protected virtual BRI GetFixedTextMesh(ref BTD textDescriptor, MRT refID)
        {
            if (textDescriptor.GeneratedFixedTextRenderInfo == null || textDescriptor.GeneratedFixedTextRenderInfoTick < lastFontUpdateFrame)
            {
                var result = textDescriptor.GeneratedFixedTextRenderInfo as BRI;
                RefreshNameData(ref result, textDescriptor.m_isFixedTextLocalized ? Locale.Get(textDescriptor.m_fixedText, textDescriptor.m_fixedTextLocaleKey) : textDescriptor.m_fixedText);
                textDescriptor.GeneratedFixedTextRenderInfo = result;
            }
            return textDescriptor.GeneratedFixedTextRenderInfo as BRI;
        }
        #endregion

        protected static string A_ShaderNameTest = "Klyte/DynamicTextBoards/klytetextboardsfrag";
        protected static IEnumerable<string> A_Shaders => DTBShaderLibrary.m_loadedShaders.Keys;

        protected void A_ReloadFromDisk()
        {
            DTBShaderLibrary.ReloadFromDisk();
        }
        protected void A_CopyToFont()
        {
            DrawFont.shader = DTBResourceLoader.instance.GetLoadedShader(A_ShaderNameTest);
        }

    }

    public abstract class IBoardBunchContainer<CC, BRI> where CC : CacheControl where BRI : BasicRenderInformation
    {
        internal BRI m_nameSubInfo;
        internal CC[] m_boardsData;
    }
    public class BoardBunchContainer : IBoardBunchContainer<CacheControl, BasicRenderInformation> { }

    public class CacheControl
    {
        public PropInfo m_cachedProp;
        public Color m_cachedColor = Color.white;
        public Color m_cachedContrastColor = Color.black;
    }
    public class BasicRenderInformation
    {
        public Mesh m_mesh;
        public Vector2 m_sizeMetersUnscaled;
        public uint m_frameDrawTime;
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
        [XmlAttribute("useContrastColor")]
        public bool m_useContrastColor = true;
        [XmlAttribute("forceColor")]
        public Color m_defaultColor = Color.clear;
        [XmlAttribute("textType")]
        public TextType m_textType = TextType.OwnName;
        [XmlAttribute("fixedText")]
        public string m_fixedText = null;
        [XmlAttribute("fixedTextLocaleCategory")]
        public string m_fixedTextLocaleKey = null;
        [XmlAttribute("fixedTextLocalized")]
        public bool m_isFixedTextLocalized = false;
        [XmlAttribute("nightEmissiveMultiplier")]
        public float m_nightEmissiveMultiplier = -0.1f;
        [XmlAttribute("dayEmissiveMultiplier")]
        public float m_dayEmissiveMultiplier = 0.6f;
        [XmlAttribute("textAlign")]
        public UIHorizontalAlignment m_textAlign = UIHorizontalAlignment.Center;
        [XmlAttribute("shader")]
        public string m_shader;
        [XmlIgnore]
        public Shader ShaderOverride
        {
            get {
                if (m_shader == null) return null;
                if (m_shaderOverride == null)
                {
                    m_shaderOverride = Shader.Find(m_shader) ?? DTBResourceLoader.instance.GetLoadedShader(m_shader);
                }
                return m_shaderOverride;
            }
        }
        [XmlIgnore]
        internal Shader m_shaderOverride;
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
        Custom1,
        Custom2,
        Custom3,
    }


}
