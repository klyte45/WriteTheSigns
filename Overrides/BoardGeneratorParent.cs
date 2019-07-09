using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.DynamicTextBoards.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using static BuildingInfo;

namespace Klyte.DynamicTextBoards.Overrides
{

    public abstract class BoardGeneratorParent<BG> : MonoBehaviour, IRedirectable where BG : BoardGeneratorParent<BG>
    {
        public abstract UIDynamicFont DrawFont { get; }
        protected uint lastFontUpdateFrame = SimulationManager.instance.m_currentTickIndex;
        protected static Shader TextShader => DTBResourceLoader.instance.GetLoadedShader("Klyte/DynamicTextBoards/klytetextboards") ?? DistrictManager.instance.m_properties.m_areaNameShader;

        public static BG Instance { get; protected set; }
        public Redirector RedirectorInstance { get ; set; }

        protected void BuildSurfaceFont(out UIDynamicFont font, string fontName)
        {
            font = ScriptableObject.CreateInstance<UIDynamicFont>();

            font.material = new Material(Singleton<DistrictManager>.instance.m_properties.m_areaNameFont.material);
            font.shader = TextShader;
            font.baseline = (Singleton<DistrictManager>.instance.m_properties.m_areaNameFont as UIDynamicFont).baseline;
            font.size = (Singleton<DistrictManager>.instance.m_properties.m_areaNameFont as UIDynamicFont).size;
            font.lineHeight = (Singleton<DistrictManager>.instance.m_properties.m_areaNameFont as UIDynamicFont).lineHeight;
            font.baseFont = Font.CreateDynamicFontFromOSFont(fontName, 16);


            font.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack | MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        public void ChangeFont(string newFont)
        {
            var fontList = new List<String> { newFont };
            fontList.AddRange(DistrictManager.instance.m_properties.m_areaNameFont.baseFont.fontNames.ToList());
            DrawFont.baseFont = Font.CreateDynamicFontFromOSFont(fontList.ToArray(), 16);
            lastFontUpdateFrame = SimulationManager.instance.m_currentTickIndex;
            OnTextureRebuilt(DrawFont.baseFont);
        }


        protected void OnTextureRebuilt(Font obj)
        {
            if (obj == DrawFont.baseFont)
            {
                lastFontUpdateFrame = SimulationManager.instance.m_currentTickIndex;
            }
            OnTextureRebuiltImpl(obj);
        }
        protected abstract void OnTextureRebuiltImpl(Font obj);

        public void Awake()
        {
            Instance = this as BG;
            RedirectorInstance = KlyteMonoUtils.CreateElement<Redirector>(transform);
        }
    }

    public abstract class BoardGeneratorParent<BG, BBC, CC, BRI, BD, BTD, MRT> : BoardGeneratorParent<BG>
        where BG : BoardGeneratorParent<BG, BBC, CC, BRI, BD, BTD, MRT>
        where BBC : IBoardBunchContainer<CC, BRI>
        where BD : BoardDescriptorParent<BD, BTD>
        where BTD : BoardTextDescriptorParent<BTD>
        where CC : CacheControl
        where BRI : BasicRenderInformation, new()
    {
        public abstract int ObjArraySize { get; }




        public static readonly int m_shaderPropColor = Shader.PropertyToID("_Color");
        public static readonly int m_shaderPropEmissive = Shader.PropertyToID("_Emission");
        public abstract void Initialize();


        public static BBC[] m_boardsContainers;


        private const float m_pixelRatio = 0.5f;
        //private const float m_scaleY = 1.2f;
        private const float m_textScale = 4;
        private readonly Vector2 scalingMatrix = new Vector2(0.015f, 0.015f);

        public  void Awake()
        {
            Font.textureRebuilt += OnTextureRebuilt;
            Initialize();
            m_boardsContainers = new BBC[ObjArraySize];

            LogUtils.DoLog($"Loading Boards Generator {typeof(BG)}");


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



        protected void RenderPropMesh(ref PropInfo propInfo, RenderManager.CameraInfo cameraInfo, ushort refId, int boardIdx, int secIdx, int layerMask, float refAngleRad, Vector3 position, Vector4 dataVector, ref string propName, Vector3 propAngle, Vector3 propScale, ref BD descriptor, out Matrix4x4 propMatrix, out bool rendered)
        {
            if (!string.IsNullOrEmpty(propName))
            {
                if (propInfo == null)
                {
                    propInfo = PrefabCollection<PropInfo>.FindLoaded(propName);
                    if (propInfo == null)
                    {
                        LogUtils.DoErrorLog($"PREFAB NOT FOUND: {propName}");
                        propName = null;
                    }
                }
                propInfo.m_color0 = GetColor(refId, boardIdx, secIdx, descriptor);
            }
            propMatrix = RenderProp(refId, refAngleRad, cameraInfo, propInfo, position, dataVector, boardIdx, layerMask, propAngle, propScale, out rendered);
        }



        #region Rendering
        private Matrix4x4 RenderProp(ushort refId, float refAngleRad, RenderManager.CameraInfo cameraInfo, PropInfo propInfo, Vector3 position, Vector4 dataVector, int idx, int layerMask, Vector3 rotation, Vector3 scale, out bool rendered)
        {
            rendered = false;
            DistrictManager instance2 = Singleton<DistrictManager>.instance;
            Randomizer randomizer = new Randomizer((int)refId << 6 | (idx + 32));
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(position, Quaternion.AngleAxis(rotation.y + refAngleRad * Mathf.Rad2Deg, Vector3.down) * Quaternion.AngleAxis(rotation.x, Vector3.left) * Quaternion.AngleAxis(rotation.z, Vector3.back), scale);
            if (propInfo != null)
            {
                //scale = propInfo.m_minScale + (float)randomizer.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f;
                byte district = instance2.GetDistrict(position);
                byte park = instance2.GetPark(position);
                propInfo = propInfo.GetVariation(ref randomizer);//, park, ref instance2.m_districts.m_buffer[(int)district]);
                Color color = propInfo.m_color0;
                var magn = scale.magnitude;
                //if ((layerMask & 1 << propInfo.m_prefabDataLayer) != 0 || propInfo.m_hasEffects)
                //{
                if (cameraInfo.CheckRenderDistance(position, propInfo.m_maxRenderDistance * scale.sqrMagnitude))
                {
                    InstanceID propRenderID2 = this.GetPropRenderID(refId);
                    var oldLayerMask = cameraInfo.m_layerMask;
                    var oldRenderDist = propInfo.m_lodRenderDistance;
                    propInfo.m_lodRenderDistance *= scale.sqrMagnitude;
                    cameraInfo.m_layerMask = 0x7FFFFFFF;
                    try
                    {
                        PropInstance.RenderInstance(cameraInfo, propInfo, propRenderID2, matrix, position, scale.y, refAngleRad + rotation.y * Mathf.Deg2Rad, color, dataVector, true);
                    }
                    finally
                    {
                        propInfo.m_lodRenderDistance = oldRenderDist;
                        cameraInfo.m_layerMask = oldLayerMask;
                    }
                    rendered = true;
                }
                //}
            }
            return matrix;
        }

        protected abstract InstanceID GetPropRenderID(ushort refID);

        protected void RenderTextMesh(RenderManager.CameraInfo cameraInfo, MRT refID, int boardIdx, int secIdx, ref BD descriptor, Matrix4x4 propMatrix, ref BTD textDescriptor, ref CC ctrl, MaterialPropertyBlock materialPropertyBlock)
        {
            BRI renderInfo = null;
            UIFont targetFont = null;
            switch (textDescriptor.m_textType)
            {
                case TextType.OwnName:
                    renderInfo = GetOwnNameMesh(refID, boardIdx, secIdx, out targetFont);
                    break;
                case TextType.Fixed:
                    renderInfo = GetFixedTextMesh(ref textDescriptor, refID, out targetFont);
                    break;
                case TextType.StreetPrefix:
                    renderInfo = GetMeshStreetPrefix(refID, boardIdx, secIdx, out targetFont);
                    break;
                case TextType.StreetSuffix:
                    renderInfo = GetMeshStreetSuffix(refID, boardIdx, secIdx, out targetFont);
                    break;
                case TextType.StreetNameComplete:
                    renderInfo = GetMeshFullStreetName(refID, boardIdx, secIdx, out targetFont);
                    break;
                case TextType.BuildingNumber:
                    renderInfo = GetMeshCurrentNumber(refID, boardIdx, secIdx, out targetFont);
                    break;
                case TextType.Custom1:
                    renderInfo = GetMeshCustom1(refID, boardIdx, secIdx, out targetFont);
                    break;
                case TextType.Custom2:
                    renderInfo = GetMeshCustom2(refID, boardIdx, secIdx, out targetFont);
                    break;
                case TextType.Custom3:
                    renderInfo = GetMeshCustom3(refID, boardIdx, secIdx, out targetFont);
                    break;
            }
            if (renderInfo == null || targetFont == null) return;
            var overflowScaleX = 1f;
            var overflowScaleY = 1f;
            var defaultMultiplierX = textDescriptor.m_textScale * scalingMatrix.x;
            var defaultMultiplierY = textDescriptor.m_textScale * scalingMatrix.y;
            var realWidth = defaultMultiplierX * renderInfo.m_sizeMetersUnscaled.x;
            var realHeight = defaultMultiplierY * renderInfo.m_sizeMetersUnscaled.y;
            //doLog($"[{GetType().Name},{refID},{boardIdx},{secIdx}] realWidth = {realWidth}; realHeight = {realHeight}");
            var targetRelativePosition = textDescriptor.m_textRelativePosition;
            if (textDescriptor.m_maxWidthMeters > 0 && textDescriptor.m_maxWidthMeters < realWidth)
            {
                overflowScaleX = textDescriptor.m_maxWidthMeters / realWidth;
                if (textDescriptor.m_applyOverflowResizingOnY)
                {
                    overflowScaleY = overflowScaleX;
                }
            }
            else
            {
                if (textDescriptor.m_maxWidthMeters > 0 && textDescriptor.m_textAlign != UIHorizontalAlignment.Center)
                {
                    var factor = (textDescriptor.m_textAlign == UIHorizontalAlignment.Left) == (((textDescriptor.m_textRelativeRotation.y) % 360 + 810) % 360 > 180) ? 0.5f : -0.5f;
                    targetRelativePosition += new Vector3((textDescriptor.m_maxWidthMeters - realWidth) * factor / descriptor.ScaleX, 0, 0);
                }
            }
            if (textDescriptor.m_verticalAlign != UIVerticalAlignment.Middle)
            {
                var factor = (textDescriptor.m_verticalAlign == UIVerticalAlignment.Bottom) != (((textDescriptor.m_textRelativeRotation.x) % 360 + 810) % 360 > 180) ? 0.5f : -0.5f;
                targetRelativePosition += new Vector3(0, (realHeight) * factor, 0);
            }




            var matrix = propMatrix * Matrix4x4.TRS(
                targetRelativePosition,
                Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.x, Vector3.left) * Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.y, Vector3.down) * Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.z, Vector3.back),
                new Vector3(defaultMultiplierX * overflowScaleX / descriptor.ScaleX, defaultMultiplierY * overflowScaleY / descriptor.PropScale.y, 1));
            if (cameraInfo.CheckRenderDistance(matrix.MultiplyPoint(Vector3.zero), Math.Min(3000, 200 * textDescriptor.m_textScale)))
            {
                if (textDescriptor.m_defaultColor != Color.clear)
                {
                    materialPropertyBlock.SetColor(m_shaderPropColor, textDescriptor.m_defaultColor);
                }
                else if (textDescriptor.m_useContrastColor)
                {
                    materialPropertyBlock.SetColor(m_shaderPropColor, GetContrastColor(refID, boardIdx, secIdx, descriptor));
                }
                else
                {
                    materialPropertyBlock.SetColor(m_shaderPropColor, Color.white);
                }

                materialPropertyBlock.SetFloat(m_shaderPropEmissive, 1.4f * (SimulationManager.instance.m_isNightTime ? textDescriptor.m_nightEmissiveMultiplier : textDescriptor.m_dayEmissiveMultiplier));
                targetFont.material.shader = textDescriptor.ShaderOverride ?? TextShader;
                Graphics.DrawMesh(renderInfo.m_mesh, matrix, targetFont.material, ctrl?.m_cachedProp?.m_prefabDataLayer ?? 10, cameraInfo.m_camera, 0, materialPropertyBlock, false, true, true);
            }
        }

        protected void UpdateMeshStreetSuffix(ushort idx, ref BRI bri)
        {
            LogUtils.DoLog($"!UpdateMeshStreetSuffix {idx}");
            string result = "";
            result = DTBHookable.GetStreetSuffix(idx);
            RefreshNameData(ref bri, result);
        }


        protected void UpdateMeshFullNameStreet(ushort idx, ref BRI bri)
        {
            //(ushort segmentID, ref string __result, ref List<ushort> usedQueue, bool defaultPrefix, bool removePrefix = false)
            string name = DTBHookable.GetStreetFullName(idx);
            LogUtils.DoLog($"!GenName {name} for {idx}");
            RefreshNameData(ref bri, name);
        }



        protected void RefreshNameData(ref BRI result, string name, UIFont overrideFont = null)
        {
            if (result == null) result = new BRI();
            UIFontManager.Invalidate(overrideFont ?? DrawFont);
            UIRenderData uirenderData = UIRenderData.Obtain();
            try
            {
                uirenderData.Clear();
                PoolList<Vector3> vertices = uirenderData.vertices;
                PoolList<Color32> colors = uirenderData.colors;
                PoolList<Vector2> uvs = uirenderData.uvs;
                PoolList<int> triangles = uirenderData.triangles;
                using (UIFontRenderer uifontRenderer = (overrideFont ?? DrawFont).ObtainRenderer())
                {

                    float width = 10000f;
                    float height = 900f;
                    uifontRenderer.colorizeSprites = true;
                    uifontRenderer.defaultColor = Color.white;
                    uifontRenderer.textScale = m_textScale;
                    uifontRenderer.pixelRatio = m_pixelRatio;
                    uifontRenderer.processMarkup = true;
                    uifontRenderer.multiLine = false;
                    uifontRenderer.wordWrap = false;
                    uifontRenderer.textAlign = UIHorizontalAlignment.Center;
                    uifontRenderer.maxSize = new Vector2(width, height);
                    uifontRenderer.multiLine = false;
                    uifontRenderer.opacity = 1;
                    uifontRenderer.shadow = false;
                    uifontRenderer.shadowColor = Color.black;
                    uifontRenderer.shadowOffset = Vector2.zero;
                    uifontRenderer.outline = false;
                    var sizeMeters = uifontRenderer.MeasureString(name) * m_pixelRatio;
                    uifontRenderer.vectorOffset = new Vector3(width * m_pixelRatio * -0.5f, sizeMeters.y * 0.5f, 0f);
                    uifontRenderer.Render(name, uirenderData);
                    result.m_sizeMetersUnscaled = sizeMeters;
                }
                if (result.m_mesh == null)
                {
                    result.m_mesh = new Mesh();
                }
                LogUtils.DoLog(uirenderData.ToString());
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
        public abstract Color GetColor(ushort buildingID, int idx, int secIdx, BD descriptor);
        public abstract Color GetContrastColor(MRT refID, int boardIdx, int secIdx, BD descriptor);

        #region UpdateData
        protected virtual BRI GetOwnNameMesh(MRT refID, int boardIdx, int secIdx, out UIFont targetFont) { targetFont = DrawFont; return null; }
        protected virtual BRI GetMeshCurrentNumber(MRT refID, int boardIdx, int secIdx, out UIFont targetFont) { targetFont = DrawFont; return null; }
        protected virtual BRI GetMeshFullStreetName(MRT refID, int boardIdx, int secIdx, out UIFont targetFont) { targetFont = DrawFont; return null; }
        protected virtual BRI GetMeshStreetSuffix(MRT refID, int boardIdx, int secIdx, out UIFont targetFont) { targetFont = DrawFont; return null; }
        protected virtual BRI GetMeshStreetPrefix(MRT refID, int boardIdx, int secIdx, out UIFont targetFont) { targetFont = DrawFont; return null; }
        protected virtual BRI GetMeshCustom1(MRT refID, int boardIdx, int secIdx, out UIFont targetFont) { targetFont = DrawFont; return null; }
        protected virtual BRI GetMeshCustom2(MRT refID, int boardIdx, int secIdx, out UIFont targetFont) { targetFont = DrawFont; return null; }
        protected virtual BRI GetMeshCustom3(MRT refID, int boardIdx, int secIdx, out UIFont targetFont) { targetFont = DrawFont; return null; }
        protected virtual BRI GetFixedTextMesh(ref BTD textDescriptor, MRT refID, out UIFont targetFont)
        {
            targetFont = DrawFont;
            if (textDescriptor.GeneratedFixedTextRenderInfo == null || textDescriptor.GeneratedFixedTextRenderInfoTick < lastFontUpdateFrame)
            {
                var result = textDescriptor.GeneratedFixedTextRenderInfo as BRI;
                RefreshNameData(ref result, textDescriptor.m_isFixedTextLocalized ? Locale.Get(textDescriptor.m_fixedText, textDescriptor.m_fixedTextLocaleKey) : textDescriptor.m_fixedText);
                textDescriptor.GeneratedFixedTextRenderInfo = result;
            }
            return textDescriptor.GeneratedFixedTextRenderInfo as BRI;
        }
        #endregion

        protected static string A_ShaderNameTest = "Klyte/DynamicTextBoards/klytetextboards";
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

    public class IBoardBunchContainer<CC, BRI> where CC : CacheControl where BRI : BasicRenderInformation
    {
        [XmlIgnore]
        internal BRI m_nameSubInfo;
        [XmlIgnore]
        internal CC[] m_boardsData;
    }

    public class CacheControl
    {
        [XmlIgnore]
        public PropInfo m_cachedProp;


    }

    public abstract class CacheControlSerializer<CCS, CC> where CC : CacheControl where CCS : CacheControlSerializer<CCS, CC>, new()
    {
        protected CC cc;


        public virtual void Deserialize(string input)
        {
        }
        public virtual string Serialize()
        {
            return null;
        }

        public static CCS New(CC sign)
        {
            return new CCS
            {
                cc = sign
            };
        }

    }

    public class BasicRenderInformation
    {
        public Mesh m_mesh;
        public Vector2 m_sizeMetersUnscaled;
        public uint m_frameDrawTime;
    }
    [XmlRoot("buildingConfig")]
    public class BuildingConfigurationSerializer<BD, BTD>
        where BD : BoardDescriptorParent<BD, BTD>
        where BTD : BoardTextDescriptorParent<BTD>
    {
        [XmlAttribute("buildingName")]
        public string m_buildingName;
        [XmlElement("boardDescriptor")]
        public BD[] m_boardDescriptors;
    }

    /*
     *    public string Serialize()
            {
                XmlSerializer xmlser = new XmlSerializer(typeof(BoardDescriptorHigwaySign));
                XmlWriterSettings settings = new XmlWriterSettings { Indent = false };
                using (StringWriter textWriter = new StringWriter())
                {
                    using (XmlWriter xw = XmlWriter.Create(textWriter, settings))
                    {
                        XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                        ns.Add("", "");
                        xmlser.Serialize(xw, descriptor, ns);
                        return textWriter.ToString();
                    }
                }
            }

            public void Deserialize(String s)
            {
                XmlSerializer xmlser = new XmlSerializer(typeof(BoardDescriptorHigwaySign));
                try
                {
                    using (TextReader tr = new StringReader(s))
                    {
                        using (XmlReader reader = XmlReader.Create(tr))
                        {
                            if (xmlser.CanDeserialize(reader))
                            {
                                descriptor = (BoardDescriptorHigwaySign)xmlser.Deserialize(reader);
                            }
                            else
                            {
                                DTBUtils.doErrorLog($"CAN'T DESERIALIZE BOARD DESCRIPTOR!\nText : {s}");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    DTBUtils.doErrorLog($"CAN'T DESERIALIZE BOARD DESCRIPTOR!\nText : {s}\n{e.Message}\n{e.StackTrace}");
                }

            }
     * */

    public abstract class BoardDescriptorParent<BD, BTD>
        where BD : BoardDescriptorParent<BD, BTD>
        where BTD : BoardTextDescriptorParent<BTD>
    {
        [XmlAttribute("propName")]
        public string m_propName;
        [XmlIgnore]
        public Vector3 m_propPosition;
        [XmlIgnore]
        public Vector3 PropScale
        {
            get => new Vector3(ScaleX, ScaleY ?? ScaleX, ScaleZ ?? ScaleX);
            set {
                ScaleX = value.x;
                ScaleY = value.y;
                ScaleZ = value.z;
            }
        }
        [XmlIgnore]
        public Vector3 m_propRotation;
        [XmlElement("textDescriptor")]
        public BTD[] m_textDescriptors;


        [XmlAttribute("positionX")]
        public float PropPositionX { get => m_propPosition.x; set => m_propPosition.x = value; }
        [XmlAttribute("positionY")]
        public float PropPositionY { get => m_propPosition.y; set => m_propPosition.y = value; }
        [XmlAttribute("positionZ")]
        public float PropPositionZ { get => m_propPosition.z; set => m_propPosition.z = value; }


        [XmlAttribute("rotationX")]
        public float PropRotationX { get => m_propRotation.x; set => m_propRotation.x = value; }
        [XmlAttribute("rotationY")]
        public float PropRotationY { get => m_propRotation.y; set => m_propRotation.y = value; }
        [XmlAttribute("rotationZ")]
        public float PropRotationZ { get => m_propRotation.z; set => m_propRotation.z = value; }

        [XmlAttribute("scaleX")]
        public float ScaleX = 1;
        [XmlAttribute("scaleY")]
        public float? ScaleY;
        [XmlAttribute("scaleZ")]
        public float? ScaleZ;


        public Matrix4x4 m_textMatrixTranslation(int idx) => Matrix4x4.Translate(m_textDescriptors[idx].m_textRelativePosition);
        public Matrix4x4 m_textMatrixRotation(int idx) => Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(m_textDescriptors[idx].m_textRelativeRotation), Vector3.one);


        public string Serialize()
        {
            XmlSerializer xmlser = new XmlSerializer(typeof(BD));
            XmlWriterSettings settings = new XmlWriterSettings { Indent = false };
            using (StringWriter textWriter = new StringWriter())
            {
                using (XmlWriter xw = XmlWriter.Create(textWriter, settings))
                {
                    XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                    ns.Add("", "");
                    xmlser.Serialize(xw, this, ns);
                    return textWriter.ToString();
                }
            }
        }

        public static BD Deserialize(String s)
        {
            XmlSerializer xmlser = new XmlSerializer(typeof(BD));
            try
            {
                using (TextReader tr = new StringReader(s))
                {
                    using (XmlReader reader = XmlReader.Create(tr))
                    {
                        if (xmlser.CanDeserialize(reader))
                        {
                            return (BD)xmlser.Deserialize(reader);
                        }
                        else
                        {
                            LogUtils.DoErrorLog($"CAN'T DESERIALIZE BOARD DESCRIPTOR!\nText : {s}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogUtils.DoErrorLog($"CAN'T DESERIALIZE BOARD DESCRIPTOR!\nText : {s}\n{e.Message}\n{e.StackTrace}");
            }
            return null;
        }
    }

    public class BoardDescriptor : BoardDescriptorParent<BoardDescriptor, BoardTextDescriptor> { }

    public abstract class BoardTextDescriptorParent<T> where T : BoardTextDescriptorParent<T>
    {
        [XmlIgnore]
        public Vector3 m_textRelativePosition;
        [XmlIgnore]
        public Vector3 m_textRelativeRotation;
        [XmlAttribute("textScale")]
        public float m_textScale = 1f;
        [XmlAttribute("maxWidth")]
        public float m_maxWidthMeters = 0;
        [XmlAttribute("applyOverflowResizingOnY")]
        public bool m_applyOverflowResizingOnY = false;
        [XmlAttribute("useContrastColor")]
        public bool m_useContrastColor = true;
        [XmlIgnore]
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
        public float m_nightEmissiveMultiplier = 0f;
        [XmlAttribute("dayEmissiveMultiplier")]
        public float m_dayEmissiveMultiplier = 0f;
        [XmlAttribute("textAlign")]
        public UIHorizontalAlignment m_textAlign = UIHorizontalAlignment.Center;
        [XmlAttribute("verticalAlign")]
        public UIVerticalAlignment m_verticalAlign = UIVerticalAlignment.Middle;
        [XmlAttribute("shader")]
        public string m_shader = null;



        [XmlAttribute("relativePositionX")]
        public float RelPositionX { get => m_textRelativePosition.x; set => m_textRelativePosition.x = value; }
        [XmlAttribute("relativePositionY")]
        public float RelPositionY { get => m_textRelativePosition.y; set => m_textRelativePosition.y = value; }
        [XmlAttribute("relativePositionZ")]
        public float RelPositionZ { get => m_textRelativePosition.z; set => m_textRelativePosition.z = value; }

        [XmlAttribute("relativeRotationX")]
        public float RotationX { get => m_textRelativeRotation.x; set => m_textRelativeRotation.x = value; }
        [XmlAttribute("relativeRotationY")]
        public float RotationY { get => m_textRelativeRotation.y; set => m_textRelativeRotation.y = value; }
        [XmlAttribute("relativeRotationZ")]
        public float RotationZ { get => m_textRelativeRotation.z; set => m_textRelativeRotation.z = value; }

        [XmlAttribute("forceColor")]
        public string ForceColor { get => m_defaultColor == Color.clear ? null : ColorExtensions.ToRGB(m_defaultColor); set => m_defaultColor = value.IsNullOrWhiteSpace() ? Color.clear : (Color)ColorExtensions.FromRGB(value); }

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
        private Shader m_shaderOverride;
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
        public uint GeneratedFixedTextRenderInfoTick { get; private set; }

        public string Serialize()
        {
            XmlSerializer xmlser = new XmlSerializer(typeof(T));
            XmlWriterSettings settings = new XmlWriterSettings { Indent = false };
            using (StringWriter textWriter = new StringWriter())
            {
                using (XmlWriter xw = XmlWriter.Create(textWriter, settings))
                {
                    XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                    ns.Add("", "");
                    xmlser.Serialize(xw, this, ns);
                    return textWriter.ToString();
                }
            }
        }

        public static T Deserialize(String s)
        {
            XmlSerializer xmlser = new XmlSerializer(typeof(T));
            try
            {
                using (TextReader tr = new StringReader(s))
                {
                    using (XmlReader reader = XmlReader.Create(tr))
                    {
                        if (xmlser.CanDeserialize(reader))
                        {
                            return (T)xmlser.Deserialize(reader);
                        }
                        else
                        {
                            LogUtils.DoErrorLog($"CAN'T DESERIALIZE BOARD DESCRIPTOR!\nText : {s}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogUtils.DoErrorLog($"CAN'T DESERIALIZE BOARD DESCRIPTOR!\nText : {s}\n{e.Message}\n{e.StackTrace}");
            }
            return null;
        }
    }

    public class BoardTextDescriptor : BoardTextDescriptorParent<BoardTextDescriptor> { }



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
