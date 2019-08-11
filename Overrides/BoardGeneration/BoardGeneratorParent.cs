using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.UI;
using ICities;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{

    public abstract class BoardGeneratorParent<BG> : MonoBehaviour, IRedirectable where BG : BoardGeneratorParent<BG>
    {
        public abstract UIDynamicFont DrawFont { get; }
        protected uint lastFontUpdateFrame = SimulationManager.instance.m_currentTickIndex;
        protected static Shader TextShader => DTPResourceLoader.instance.GetLoadedShader("Klyte/DynamicTextProps/klytetextboards") ?? DistrictManager.instance.m_properties.m_areaNameShader;

        public static BG Instance { get; protected set; }
        public Redirector RedirectorInstance { get; set; }

        protected void BuildSurfaceFont(out UIDynamicFont font, string fontName)
        {
            font = ScriptableObject.CreateInstance<UIDynamicFont>();

            font.material = new Material(Singleton<DistrictManager>.instance.m_properties.m_areaNameFont.material);
            font.shader = TextShader;
            font.baseline = (Singleton<DistrictManager>.instance.m_properties.m_areaNameFont as UIDynamicFont).baseline;
            font.size = (Singleton<DistrictManager>.instance.m_properties.m_areaNameFont as UIDynamicFont).size * 4;
            font.lineHeight = (Singleton<DistrictManager>.instance.m_properties.m_areaNameFont as UIDynamicFont).lineHeight;
            var fontList = new List<string> { fontName };
            fontList.AddRange(DistrictManager.instance.m_properties?.m_areaNameFont?.baseFont?.fontNames?.ToList());
            font.baseFont = Font.CreateDynamicFontFromOSFont(fontList.ToArray(), 64);
            font.lineHeight = 70;
            font.baseline = 66;

            font.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack | MaterialGlobalIlluminationFlags.RealtimeEmissive;
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
            OnTextureRebuilt(DrawFont.baseFont);
        }
        protected virtual void OnChangeFont(string fontName) { }

        protected void OnTextureRebuilt(Font obj)
        {
            if (obj == DrawFont.baseFont)
            {
                lastFontUpdateFrame = SimulationManager.instance.m_currentTickIndex;
            }
            OnTextureRebuiltImpl(obj);
        }
        protected abstract void OnTextureRebuiltImpl(Font obj);

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
        public static readonly int m_shaderPropEmissive = Shader.PropertyToID("_Emission");
        public abstract void Initialize();


        public static BBC[] m_boardsContainers;


        private const float m_pixelRatio = 2;
        //private const float m_scaleY = 1.2f;
        private const float m_textScale = 0.75f;
        private readonly Vector2 m_scalingMatrix = new Vector2(0.005f, 0.005f);

        public override void Awake()
        {
            base.Awake();
            Font.textureRebuilt += OnTextureRebuilt;
            Initialize();
            m_boardsContainers = new BBC[ObjArraySize];

            LogUtils.DoLog($"Loading Boards Generator {typeof(BG)}");


        }


        protected Quad2 GetBounds(ref Building data)
        {
            var width = data.Width;
            var length = data.Length;
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
            var halfLength = (ref1v2 - ref2v2).magnitude / 2;
            Vector2 center = (ref1v2 + ref2v2) / 2;
            var angle = Vector2.Angle(ref1v2, ref2v2);


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
                    var oldLayerMask = cameraInfo.m_layerMask;
                    var oldRenderDist = propInfo.m_lodRenderDistance;
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
            UIFont targetFont = null;
            switch (textDescriptor.m_textType)
            {
                case TextType.OwnName:
                    renderInfo = GetOwnNameMesh(refID, boardIdx, secIdx, out targetFont, ref descriptor);
                    break;
                case TextType.Fixed:
                    renderInfo = GetFixedTextMesh(ref textDescriptor, refID, boardIdx, secIdx, out targetFont, ref descriptor);
                    break;
                case TextType.StreetPrefix:
                    renderInfo = GetMeshStreetPrefix(refID, boardIdx, secIdx, out targetFont, ref descriptor);
                    break;
                case TextType.StreetSuffix:
                    renderInfo = GetMeshStreetSuffix(refID, boardIdx, secIdx, out targetFont, ref descriptor);
                    break;
                case TextType.StreetNameComplete:
                    renderInfo = GetMeshFullStreetName(refID, boardIdx, secIdx, out targetFont, ref descriptor);
                    break;
                case TextType.BuildingNumber:
                    renderInfo = GetMeshCurrentNumber(refID, boardIdx, secIdx, out targetFont, ref descriptor);
                    break;
                case TextType.Custom1:
                    renderInfo = GetMeshCustom1(refID, boardIdx, secIdx, out targetFont, ref descriptor);
                    break;
                case TextType.Custom2:
                    renderInfo = GetMeshCustom2(refID, boardIdx, secIdx, out targetFont, ref descriptor);
                    break;
                case TextType.Custom3:
                    renderInfo = GetMeshCustom3(refID, boardIdx, secIdx, out targetFont, ref descriptor);
                    break;
            }
            if (renderInfo == null || targetFont == null)
            {
                return;
            }

            var overflowScaleX = 1f;
            var overflowScaleY = 1f;
            var defaultMultiplierX = textDescriptor.m_textScale * m_scalingMatrix.x;
            var defaultMultiplierY = textDescriptor.m_textScale * m_scalingMatrix.y;
            var realWidth = defaultMultiplierX * renderInfo.m_sizeMetersUnscaled.x;
            var realHeight = defaultMultiplierY * renderInfo.m_sizeMetersUnscaled.y;
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
                    var factor = textDescriptor.m_textAlign == UIHorizontalAlignment.Left == (((textDescriptor.m_textRelativeRotation.y % 360) + 810) % 360 > 180) ? 0.5f : -0.5f;
                    targetRelativePosition += new Vector3((textDescriptor.m_maxWidthMeters - realWidth) * factor / descriptor.ScaleX, 0, 0);
                }
            }


            if (textDescriptor.m_verticalAlign != UIVerticalAlignment.Middle)
            {
                var factor = textDescriptor.m_verticalAlign == UIVerticalAlignment.Bottom == (((textDescriptor.m_textRelativeRotation.x % 360) + 810) % 360 > 180) ? -1f : 1f;
                targetRelativePosition += new Vector3(0, realHeight * factor, 0);
            }





            Matrix4x4 matrix = propMatrix * Matrix4x4.TRS(
                targetRelativePosition,
                Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.x, Vector3.left) * Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.y, Vector3.down) * Quaternion.AngleAxis(textDescriptor.m_textRelativeRotation.z, Vector3.back),
                new Vector3(defaultMultiplierX * overflowScaleX / descriptor.ScaleX, defaultMultiplierY * overflowScaleY / descriptor.PropScale.y, 1));
            if (cameraInfo.CheckRenderDistance(matrix.MultiplyPoint(Vector3.zero), Math.Min(3000, 200 * textDescriptor.m_textScale)))
            {
                if (textDescriptor.m_useContrastColor)
                {
                    materialPropertyBlock.SetColor(m_shaderPropColor, GetContrastColor(refID, boardIdx, secIdx, descriptor));
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
                targetFont.material.shader = textDescriptor.ShaderOverride ?? TextShader;
                Graphics.DrawMesh(renderInfo.m_mesh, matrix, targetFont.material, ctrl?.m_cachedProp?.m_prefabDataLayer ?? 10, cameraInfo.m_camera, 0, materialPropertyBlock, false, true, true);
            }
        }

        protected void UpdateMeshStreetSuffix(ushort idx, ref BRI bri)
        {
            LogUtils.DoLog($"!UpdateMeshStreetSuffix {idx}");
            var result = "";
            result = DTPHookable.GetStreetSuffix(idx);
            RefreshTextData(ref bri, result);
        }


        protected void UpdateMeshFullNameStreet(ushort idx, ref BRI bri)
        {
            //(ushort segmentID, ref string __result, ref List<ushort> usedQueue, bool defaultPrefix, bool removePrefix = false)
            var name = DTPHookable.GetStreetFullName(idx);
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
                Vector2 sizeMeters;

                using (UIFontRenderer uifontRenderer = (overrideFont ?? DrawFont).ObtainRenderer())
                {

                    var width = 10000f;
                    var height = 900f;
                    uifontRenderer.colorizeSprites = true;
                    uifontRenderer.defaultColor = Color.white;
                    uifontRenderer.textScale = m_textScale;
                    uifontRenderer.pixelRatio = m_pixelRatio;
                    uifontRenderer.processMarkup = true;
                    uifontRenderer.wordWrap = false;
                    uifontRenderer.textAlign = UIHorizontalAlignment.Center;
                    uifontRenderer.maxSize = new Vector2(width, height);
                    uifontRenderer.multiLine = true;
                    uifontRenderer.opacity = 1;
                    uifontRenderer.shadow = false;
                    uifontRenderer.shadowColor = Color.black;
                    uifontRenderer.shadowOffset = Vector2.zero;
                    uifontRenderer.outline = false;
                    if(uifontRenderer is UIDynamicFont.DynamicFontRenderer dynamicRenderer)
                    {
                        dynamicRenderer.spriteAtlas = UIView.GetAView().defaultAtlas;                    
                    }
                    sizeMeters = uifontRenderer.MeasureString(text);
                    uifontRenderer.Render(text, uirenderData);
                }
                if (result.m_mesh == null)
                {
                    result.m_mesh = new Mesh();
                }
                LogUtils.DoLog(uirenderData.ToString());
                result.m_mesh.Clear();
                result.m_mesh.vertices = CenterVertices(vertices);
                result.m_mesh.colors32 = colors.Select(x => new Color32(x.a, x.a, x.a, x.a)).ToArray();
                result.m_mesh.uv = uvs.ToArray();
                result.m_mesh.triangles = triangles.ToArray();
                result.m_frameDrawTime = lastFontUpdateFrame;
                result.m_sizeMetersUnscaled = new Vector2(sizeMeters.x * m_pixelRatio, result.m_mesh.bounds.extents.y);
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
        protected virtual BRI GetOwnNameMesh(ushort refID, int boardIdx, int secIdx, out UIFont targetFont, ref BD descriptor) { targetFont = DrawFont; return null; }
        protected virtual BRI GetMeshCurrentNumber(ushort refID, int boardIdx, int secIdx, out UIFont targetFont, ref BD descriptor) { targetFont = DrawFont; return null; }
        protected virtual BRI GetMeshFullStreetName(ushort refID, int boardIdx, int secIdx, out UIFont targetFont, ref BD descriptor) { targetFont = DrawFont; return null; }
        protected virtual BRI GetMeshStreetSuffix(ushort refID, int boardIdx, int secIdx, out UIFont targetFont, ref BD descriptor) { targetFont = DrawFont; return null; }
        protected virtual BRI GetMeshStreetPrefix(ushort refID, int boardIdx, int secIdx, out UIFont targetFont, ref BD descriptor) { targetFont = DrawFont; return null; }
        protected virtual BRI GetMeshCustom1(ushort refID, int boardIdx, int secIdx, out UIFont targetFont, ref BD descriptor) { targetFont = DrawFont; return null; }
        protected virtual BRI GetMeshCustom2(ushort refID, int boardIdx, int secIdx, out UIFont targetFont, ref BD descriptor) { targetFont = DrawFont; return null; }
        protected virtual BRI GetMeshCustom3(ushort refID, int boardIdx, int secIdx, out UIFont targetFont, ref BD descriptor) { targetFont = DrawFont; return null; }
        protected virtual BRI GetFixedTextMesh(ref BTD textDescriptor, ushort refID, int boardIdx, int secIdx, out UIFont targetFont, ref BD descriptor)
        {
            targetFont = DrawFont;
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
            var storage = memoryStream.ToArray();
            Deserialize(System.Text.Encoding.UTF8.GetString(storage));
        }

        // Token: 0x0600003B RID: 59 RVA: 0x00004020 File Offset: 0x00002220
        public void OnSaveData()
        {
            if (ID == null || Singleton<ToolManager>.instance.m_properties.m_mode != ItemClass.Availability.Game)
            {
                return;
            }

            var serialData = Serialize();
            LogUtils.DoLog($"serialData: {serialData ?? "<NULL>"}");
            if (serialData == null)
            {
                return;
            }

            var data = System.Text.Encoding.UTF8.GetBytes(serialData);
            SerializableDataManager.SaveData(ID, data);
        }

        public abstract void Deserialize(string data);
        public abstract string Serialize();
        public void OnReleased() { }
        #endregion


        protected static string A_ShaderNameTest = "Klyte/DynamicTextProps/klytetextboards";
        protected static IEnumerable<string> A_Shaders => DTPShaderLibrary.m_loadedShaders.Keys;

        protected void A_ReloadFromDisk() => DTPShaderLibrary.ReloadFromDisk();
        protected void A_CopyToFont() => DrawFont.shader = DTPResourceLoader.instance.GetLoadedShader(A_ShaderNameTest);

    }


}
