using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.UI;
using ICities;
using Klyte.Commons.Extensors;
using Klyte.Commons.Redirectors;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.Redirectors.UIDynamicFontRendererRedirector;

namespace Klyte.DynamicTextProps.Overrides
{

    public abstract class BoardGeneratorParent<BG> : MonoBehaviour, IRedirectable where BG : BoardGeneratorParent<BG>
    {
        public abstract UIDynamicFont DrawFont { get; }
        protected uint lastFontUpdateFrame = SimulationManager.instance.m_currentTickIndex;
        protected static Shader TextShader = Shader.Find("Custom/Props/Prop/Default") ?? DistrictManager.instance.m_properties.m_areaNameShader;

        public static BG Instance { get; protected set; }
        public Redirector RedirectorInstance { get; set; }

        protected void BuildSurfaceFont(out UIDynamicFont font, string fontName)
        {
            font = ScriptableObject.CreateInstance<UIDynamicFont>();

            var fontList = new List<string> { fontName };
            fontList.AddRange(DistrictManager.instance.m_properties?.m_areaNameFont?.baseFont?.fontNames?.ToList());
            font.baseFont = Font.CreateDynamicFontFromOSFont(fontList.ToArray(), 64);
            font.lineHeight = 70;
            font.baseline = 66;
            font.size = 64;
        }

        public void ChangeFont(string newFont)
        {
            var fontList = new List<string>();
            if (newFont != null)
            {
                fontList.Add(newFont);
            }
            fontList.AddRange(DistrictManager.instance.m_properties.m_areaNameFont.baseFont.fontNames.ToList());
            DrawFont.baseFont = Font.CreateDynamicFontFromOSFont(fontList.ToArray(), 64);
            lastFontUpdateFrame = SimulationManager.instance.m_currentTickIndex;
            OnChangeFont(DrawFont.baseFont.name != newFont ? null : newFont);
            Reset();
        }
        protected virtual void OnChangeFont(string fontName) { }

        public void Reset()
        {
            lastFontUpdateFrame = SimulationManager.instance.m_currentTickIndex;

            ResetImpl();
        }
        protected abstract void ResetImpl();

        public virtual void Awake()
        {
            Instance = this as BG;
            RedirectorInstance = KlyteMonoUtils.CreateElement<Redirector>(transform);
        }
    }

    public abstract class BoardGeneratorParent<BG, BBC, CC, BRI, BD, BTD> : BoardGeneratorParent<BG>, ISerializableDataExtension
        where BG : BoardGeneratorParent<BG, BBC, CC, BRI, BD, BTD>
        where BBC : IBoardBunchContainer<CC, BRI>
        where BD : BoardDescriptorParentXml<BD, BTD>
        where BTD : BoardTextDescriptorParentXml<BTD>
        where CC : CacheControl
        where BRI : BasicRenderInformation, new()
    {
        public abstract int ObjArraySize { get; }




        public static readonly int m_shaderPropColor = Shader.PropertyToID("_Color");
        public static readonly int m_shaderPropColor0 = Shader.PropertyToID("_ColorV0");
        public static readonly int m_shaderPropColor1 = Shader.PropertyToID("_ColorV1");
        public static readonly int m_shaderPropColor2 = Shader.PropertyToID("_ColorV2");
        public static readonly int m_shaderPropColor3 = Shader.PropertyToID("_ColorV3");
        public static readonly int m_shaderPropEmissive = Shader.PropertyToID("_SpecColor");
        public abstract void Initialize();


        public static BBC[] m_boardsContainers;


        private const float m_pixelRatio = 2;
        //private const float m_scaleY = 1.2f;
        private const float m_textScale = 0.75f;
        private readonly Vector2 m_scalingMatrix = new Vector2(0.005f, 0.005f);

        public override void Awake()
        {
            base.Awake();
            Initialize();
            m_boardsContainers = new BBC[ObjArraySize];

            LogUtils.DoLog($"Loading Boards Generator {typeof(BG)}");


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
            Quad2 quad = default;
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
            Quad2 quad = default;
            quad.a = center - vector - vector2;
            quad.b = center + vector - vector2;
            quad.c = center + vector + vector2;
            quad.d = center - vector + vector2;
            return quad;
        }



        protected void RenderPropMesh(ref PropInfo propInfo, RenderManager.CameraInfo cameraInfo, ushort refId, int boardIdx, int secIdx, int layerMask, float refAngleRad, Vector3 position, Vector4 dataVector, ref string propName, Vector3 propAngle, Vector3 propScale, ref BD descriptor, out Matrix4x4 propMatrix, out bool rendered)
        {
            Color? propColor = GetColor(refId, boardIdx, secIdx, descriptor);
            if (propColor == null)
            {
                rendered = false;
                propMatrix = new Matrix4x4();
                return;
            }

            if (!string.IsNullOrEmpty(propName))
            {
                if (propInfo == null || propInfo.name != propName)
                {
                    propInfo = PrefabCollection<PropInfo>.FindLoaded(propName);
                    if (propInfo == null)
                    {
                        LogUtils.DoErrorLog($"PREFAB NOT FOUND: {propName}");
                        propName = null;
                    }
                }
                propInfo.m_color0 = propColor.GetValueOrDefault();
            }
            else
            {
                propInfo = null;
            }
            propMatrix = RenderProp(refId, refAngleRad, cameraInfo, propInfo, position, dataVector, boardIdx, layerMask, propAngle, propScale, out rendered);
        }



        #region Rendering
        private Matrix4x4 RenderProp(ushort refId, float refAngleRad, RenderManager.CameraInfo cameraInfo,
#pragma warning disable IDE0060 // Remover o parâmetro não utilizado
                                     PropInfo propInfo, Vector3 position, Vector4 dataVector, int idx, int layerMask,
#pragma warning restore IDE0060 // Remover o parâmetro não utilizado
                                     Vector3 rotation, Vector3 scale, out bool rendered)
        {
            rendered = false;
            //     DistrictManager instance2 = Singleton<DistrictManager>.instance;
            var randomizer = new Randomizer((refId << 6) | (idx + 32));
            Matrix4x4 matrix = default;
            matrix.SetTRS(position, Quaternion.AngleAxis(rotation.y + (refAngleRad * Mathf.Rad2Deg), Vector3.down) * Quaternion.AngleAxis(rotation.x, Vector3.left) * Quaternion.AngleAxis(rotation.z, Vector3.back), scale);
            if (propInfo != null)
            {
                //scale = propInfo.m_minScale + (float)randomizer.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f;
                // byte district = instance2.GetDistrict(position);
                //   byte park = instance2.GetPark(position);
                propInfo = propInfo.GetVariation(ref randomizer);//, park, ref instance2.m_districts.m_buffer[(int)district]);
                Color color = propInfo.m_color0;
                //      float magn = scale.magnitude;
                //if ((layerMask & 1 << propInfo.m_prefabDataLayer) != 0 || propInfo.m_hasEffects)
                //{
                if (cameraInfo.CheckRenderDistance(position, propInfo.m_maxRenderDistance * scale.sqrMagnitude))
                {
                    InstanceID propRenderID2 = GetPropRenderID(refId);
                    int oldLayerMask = cameraInfo.m_layerMask;
                    float oldRenderDist = propInfo.m_lodRenderDistance;
                    propInfo.m_lodRenderDistance *= scale.sqrMagnitude;
                    cameraInfo.m_layerMask = 0x7FFFFFFF;
                    try
                    {
                        PropInstance.RenderInstance(cameraInfo, propInfo, propRenderID2, matrix, position, scale.y, refAngleRad + (rotation.y * Mathf.Deg2Rad), color, dataVector, true);
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

        protected void RenderTextMesh(RenderManager.CameraInfo cameraInfo, ushort refID, int boardIdx, int secIdx, ref BD descriptor, Matrix4x4 propMatrix, ref BTD textDescriptor, ref CC ctrl, MaterialPropertyBlock materialPropertyBlock)
        {
            BRI renderInfo = null;
            switch (textDescriptor.m_textType)
            {
                case TextType.OwnName:
                    renderInfo = GetOwnNameMesh(refID, boardIdx, secIdx, ref descriptor);
                    break;
                case TextType.Fixed:
                    renderInfo = GetFixedTextMesh(ref textDescriptor, refID, boardIdx, secIdx, ref descriptor);
                    break;
                case TextType.StreetPrefix:
                    renderInfo = GetMeshStreetPrefix(refID, boardIdx, secIdx, ref descriptor);
                    break;
                case TextType.StreetSuffix:
                    renderInfo = GetMeshStreetSuffix(refID, boardIdx, secIdx, ref descriptor);
                    break;
                case TextType.StreetNameComplete:
                    renderInfo = GetMeshFullStreetName(refID, boardIdx, secIdx, ref descriptor);
                    break;
                case TextType.BuildingNumber:
                    renderInfo = GetMeshCurrentNumber(refID, boardIdx, secIdx, ref descriptor);
                    break;
                case TextType.Custom1:
                    renderInfo = GetMeshCustom1(refID, boardIdx, secIdx, ref descriptor);
                    break;
                case TextType.Custom2:
                    renderInfo = GetMeshCustom2(refID, boardIdx, secIdx, ref descriptor);
                    break;
                case TextType.Custom3:
                    renderInfo = GetMeshCustom3(refID, boardIdx, secIdx, ref descriptor);
                    break;
            }
            if (renderInfo == null)
            {
                return;
            }

            float overflowScaleX = 1f;
            float overflowScaleY = 1f;
            float defaultMultiplierX = textDescriptor.m_textScale * m_scalingMatrix.x;
            float defaultMultiplierY = textDescriptor.m_textScale * m_scalingMatrix.y;
            float realWidth = defaultMultiplierX * renderInfo.m_sizeMetersUnscaled.x;
            float realHeight = defaultMultiplierY * renderInfo.m_sizeMetersUnscaled.y;
            Vector3 targetRelativePosition = textDescriptor.m_textRelativePosition;
            //    LogUtils.DoLog($"[{GetType().Name},{refID},{boardIdx},{secIdx}] realWidth = {realWidth}; realHeight = {realHeight}; renderInfo.m_mesh.bounds = {renderInfo.m_mesh.bounds};");
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
                    float factor = textDescriptor.m_textAlign == UIHorizontalAlignment.Left == (((textDescriptor.m_textRelativeRotation.y % 360) + 810) % 360 > 180) ? 0.5f : -0.5f;
                    targetRelativePosition += new Vector3((textDescriptor.m_maxWidthMeters - realWidth) * factor / descriptor.ScaleX, 0, 0);
                }
            }


            if (textDescriptor.m_verticalAlign != UIVerticalAlignment.Middle)
            {
                float factor = textDescriptor.m_verticalAlign == UIVerticalAlignment.Bottom == (((textDescriptor.m_textRelativeRotation.x % 360) + 810) % 360 > 180) ? -1f : 1f;
                targetRelativePosition += new Vector3(0, realHeight * factor, 0);
            }





            Matrix4x4 matrix = propMatrix * Matrix4x4.TRS(
                targetRelativePosition,
                Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.x, Vector3.left) * Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.y, Vector3.down) * Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.z, Vector3.back),
                new Vector3(defaultMultiplierX * overflowScaleX / descriptor.ScaleX, defaultMultiplierY * overflowScaleY / descriptor.PropScale.y, 1));

            Color colorToSet = Color.white;
            if (textDescriptor.m_useContrastColor)
            {
                colorToSet = GetContrastColor(refID, boardIdx, secIdx, descriptor);
            }
            else if (textDescriptor.m_defaultColor != Color.clear)
            {
                colorToSet = textDescriptor.m_defaultColor;
            }
            materialPropertyBlock.Clear();
            materialPropertyBlock.SetColor(m_shaderPropColor, colorToSet);
            materialPropertyBlock.SetColor(m_shaderPropColor0, colorToSet);
            materialPropertyBlock.SetColor(m_shaderPropColor1, colorToSet);
            materialPropertyBlock.SetColor(m_shaderPropColor2, colorToSet);
            materialPropertyBlock.SetColor(m_shaderPropColor3, colorToSet);


            materialPropertyBlock.SetColor(m_shaderPropEmissive, Color.white * (SimulationManager.instance.m_isNightTime ? textDescriptor.m_nightEmissiveMultiplier : textDescriptor.m_dayEmissiveMultiplier));
            renderInfo.m_generatedMaterial.shader = TextShader;
            Graphics.DrawMesh(renderInfo.m_mesh, matrix, renderInfo.m_generatedMaterial, A_layer, cameraInfo.m_camera, 0, materialPropertyBlock, A_castShadows, A_receiveShadows, A_useLightProbes);

        }

        private static int A_layer = 10;
        private static bool A_castShadows = false;
        private static bool A_receiveShadows = true;
        private static bool A_useLightProbes = true;

        protected void UpdateMeshStreetSuffix(ushort idx, ref BRI bri)
        {
            LogUtils.DoLog($"!UpdateMeshStreetSuffix {idx}");
            string result = "";
            result = DTPHookable.GetStreetSuffix(idx);
            RefreshTextData(ref bri, result);
        }


        protected void UpdateMeshFullNameStreet(ushort idx, ref BRI bri)
        {
            //(ushort segmentID, ref string __result, ref List<ushort> usedQueue, bool defaultPrefix, bool removePrefix = false)
            string name = DTPHookable.GetStreetFullName(idx);
            LogUtils.DoLog($"!GenName {name} for {idx}");
            RefreshTextData(ref bri, name);
        }


        protected BRI RefreshTextData(string text, UIFont overrideFont = null)
        {
            var result = new BRI();
            RefreshTextData(ref result, text, overrideFont);
            return result;
        }

        protected void RefreshTextData(ref BRI result, string text, UIFont overrideFont = null)
        {
            if (result == null)
            {
                result = new BRI();
            }

            UIFontManager.Invalidate(overrideFont ?? DrawFont);
            var uirenderData = UIRenderData.Obtain();
            try
            {
                uirenderData.Clear();
                PoolList<Vector3> vertices = uirenderData.vertices;
                PoolList<Color32> colors = uirenderData.colors;
                PoolList<Vector2> uvs = uirenderData.uvs;
                PoolList<int> triangles = uirenderData.triangles;

                Texture2D tex = TextureRenderUtils.RenderTokenizedText((UIDynamicFont) (overrideFont ?? DrawFont), 2, text.IsNullOrWhiteSpace() ? " " : text, Color.white, out Vector2 realSize);

                var options = new RenderOptions
                {
                    color = Color.white,
                    fillAmount = 1f,
                    flip = UISpriteFlip.None,
                    offset = Vector3.zero,
                    pixelsToUnits = 1,
                    size = realSize,
                    spriteInfo = new UITextureAtlas.SpriteInfo()
                    {
                        region = new Rect(0, 0, 1, 1)
                    }
                };
                UIDynamicFontRendererRedirector.RenderSprite(uirenderData, options);


                if (result.m_mesh == null)
                {
                    result.m_mesh = new Mesh();
                }
                result.m_mesh.Clear();
                result.m_mesh.vertices = CenterVertices(vertices).ToArray();
                result.m_mesh.colors32 = colors.ToArray();
                result.m_mesh.uv = uvs.ToArray();
                result.m_mesh.triangles = triangles.ToArray();
                result.m_mesh.RecalculateBounds();
                result.m_mesh.RecalculateNormals();
                result.m_mesh.RecalculateTangents();
                result.m_frameDrawTime = lastFontUpdateFrame;
                result.m_sizeMetersUnscaled = new Vector2(realSize.x, result.m_mesh.bounds.extents.y);
                var mainTex = new Texture2D(tex.width, tex.height);
                mainTex.SetPixels(new Color[tex.width * tex.height].Select(x => Color.white).ToArray());
                mainTex.Apply();
                if (result.m_generatedMaterial == null)
                {
                    result.m_generatedMaterial = new Material(Shader.Find("Custom/Buildings/Building/NoBase"))
                    {
                        mainTexture = tex
                    };
                }
                else
                {
                    result.m_generatedMaterial.mainTexture = tex;
                }
                //LogUtils.DoErrorLog($"TEX SIZE= {tex.width},{tex.height} text = {text}");
                result.m_generatedMaterial.SetTexture("_MainTex", tex);
                var aciTex = new Texture2D(tex.width, tex.height);
                aciTex.SetPixels(tex.GetPixels().Select(x => new Color(1 - x.a, 0, 0, 1)).ToArray());
                aciTex.Apply();
                result.m_generatedMaterial.SetTexture("_ACIMap", aciTex);
                var xysTex = new Texture2D(tex.width, tex.height);
                xysTex.SetPixels(tex.GetPixels().Select(x => new Color(0.732F, 0.732F, 1, 1)).ToArray());
                xysTex.Apply();
                result.m_generatedMaterial.SetTexture("_XYSMap", xysTex);
            }
            finally
            {
                uirenderData.Release();
            }

        }

        private Vector3[] CenterVertices(PoolList<Vector3> points)
        {
            if (points.Count == 0)
            {
                return points.ToArray();
            }

            var max = new Vector3(points.Select(x => x.x).Max(), points.Select(x => x.y).Max(), points.Select(x => x.z).Max());
            var min = new Vector3(points.Select(x => x.x).Min(), points.Select(x => x.y).Min(), points.Select(x => x.z).Min());
            Vector3 center = (max + min) / 2;

            return points.Select(x => x - center).ToArray();
        }

        #endregion
        public abstract Color? GetColor(ushort buildingID, int idx, int secIdx, BD descriptor);
        public abstract Color GetContrastColor(ushort refID, int boardIdx, int secIdx, BD descriptor);

        #region UpdateData
        protected virtual BRI GetOwnNameMesh(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BRI GetMeshCurrentNumber(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BRI GetMeshFullStreetName(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BRI GetMeshStreetSuffix(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BRI GetMeshStreetPrefix(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BRI GetMeshCustom1(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BRI GetMeshCustom2(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BRI GetMeshCustom3(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BRI GetFixedTextMesh(ref BTD textDescriptor, ushort refID, int boardIdx, int secIdx, ref BD descriptor)
        {

            if (textDescriptor.GeneratedFixedTextRenderInfo == null || textDescriptor.GeneratedFixedTextRenderInfoTick < lastFontUpdateFrame)
            {
                var result = textDescriptor.GeneratedFixedTextRenderInfo as BRI;
                RefreshTextData(ref result, (textDescriptor.m_isFixedTextLocalized ? Locale.Get(textDescriptor.m_fixedText, textDescriptor.m_fixedTextLocaleKey) : textDescriptor.m_fixedText) ?? "");
                textDescriptor.GeneratedFixedTextRenderInfo = result;
            }
            return textDescriptor.GeneratedFixedTextRenderInfo as BRI;
        }
        #endregion

        #region Serialization
        protected abstract string ID { get; }
        public IManagers Managers => SerializableDataManager?.managers;

        public ISerializableData SerializableDataManager { get; private set; }

        public void OnCreated(ISerializableData serializableData) => SerializableDataManager = serializableData;
        public void OnLoadData()
        {
            if (ID == null || Singleton<ToolManager>.instance.m_properties.m_mode != ItemClass.Availability.Game)
            {
                return;
            }
            if (!SerializableDataManager.EnumerateData().Contains(ID))
            {
                return;
            }
            using var memoryStream = new MemoryStream(SerializableDataManager.LoadData(ID));
            byte[] storage = memoryStream.ToArray();
            Deserialize(System.Text.Encoding.UTF8.GetString(storage));
        }

        // Token: 0x0600003B RID: 59 RVA: 0x00004020 File Offset: 0x00002220
        public void OnSaveData()
        {
            if (ID == null || Singleton<ToolManager>.instance.m_properties.m_mode != ItemClass.Availability.Game)
            {
                return;
            }

            string serialData = Serialize();
            LogUtils.DoLog($"serialData: {serialData ?? "<NULL>"}");
            if (serialData == null)
            {
                return;
            }

            byte[] data = System.Text.Encoding.UTF8.GetBytes(serialData);
            SerializableDataManager.SaveData(ID, data);
        }

        public abstract void Deserialize(string data);
        public abstract string Serialize();
        public void OnReleased() { }
        #endregion

    }


}
