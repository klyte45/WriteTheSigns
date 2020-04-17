using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Data;
using Klyte.DynamicTextProps.Utils;
using SpriteFontPlus;
using SpriteFontPlus.Utility;
using System;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{

    public abstract class BoardGeneratorParent<BG> : Redirector, IRedirectable where BG : BoardGeneratorParent<BG>
    {
        public virtual string FontName { get; set; }
        public DynamicSpriteFont DrawFont => FontServer.instance[FontName] ?? FontServer.instance[DTPController.DEFAULT_FONT_KEY];
        protected long LastFontUpdateFrame => DrawFont.LastUpdate;
        protected static Shader TextShader = Shader.Find("Custom/Props/Prop/Default") ?? DistrictManager.instance.m_properties.m_areaNameShader;

        public static BG Instance { get; protected set; }

        public void ChangeFont(string newFont) => FontName = newFont ?? DTPController.DEFAULT_FONT_KEY;

        public void Reset() => ResetImpl();
        protected abstract void ResetImpl();

        public virtual void Awake() => Instance = this as BG;

    }

    public abstract class BoardGeneratorParent<BG, BBC, D> : BoardGeneratorParent<BG>
        where BG : BoardGeneratorParent<BG, BBC, D>
        where BBC : IBoardBunchContainer
        where D : DTPBaseData<D, BBC>, new()
    {

        public sealed override string FontName { get => DTPBaseData<D, BBC>.Instance.DefaultFont; set => DTPBaseData<D, BBC>.Instance.DefaultFont = value; }


        public D Data => DTPBaseData<D, BBC>.Instance;

        public static readonly int m_shaderPropColor = Shader.PropertyToID("_Color");
        public static readonly int m_shaderPropColor0 = Shader.PropertyToID("_ColorV0");
        public static readonly int m_shaderPropColor1 = Shader.PropertyToID("_ColorV1");
        public static readonly int m_shaderPropColor2 = Shader.PropertyToID("_ColorV2");
        public static readonly int m_shaderPropColor3 = Shader.PropertyToID("_ColorV3");
        public static readonly int m_shaderPropEmissive = Shader.PropertyToID("_SpecColor");
        public abstract void Initialize();

        private readonly Vector2 m_scalingMatrix = new Vector2(0.005f, 0.005f);

        public override void Awake()
        {
            base.Awake();
            Initialize();

            NetManagerOverrides.EventSegmentNameChanged += OnNameSeedChanged;
            DistrictManagerOverrides.EventOnDistrictChanged += OnDistrictChanged;

            var adrEventsType = Type.GetType("Klyte.Addresses.ModShared.AdrEvents, KlyteAddresses");
            if (adrEventsType != null)
            {
                static void RegisterEvent(string eventName, Type adrEventsType, Action action) => adrEventsType.GetEvent(eventName)?.AddEventHandler(null, action);
                RegisterEvent("EventRoadNamingChange", adrEventsType, new Action(OnNameSeedChanged));
                RegisterEvent("EventDistrictColorChanged", adrEventsType, new Action(OnDistrictChanged));
            }

            LogUtils.DoLog($"Loading Boards Generator {typeof(BG)}");


        }
        private void OnNameSeedChanged()
        {
            RenderUtils.ClearCacheFullStreetName();
            RenderUtils.ClearCacheStreetName();
            RenderUtils.ClearCacheStreetQualifier();
        }

        #region events
        private void OnNameSeedChanged(ushort segmentId)
        {
            RenderUtils.ClearCacheFullStreetName();
            RenderUtils.ClearCacheStreetName();
            RenderUtils.ClearCacheStreetQualifier();
        }
        private void OnDistrictChanged()
        {
            LogUtils.DoLog("onDistrictChanged");
            RenderUtils.ClearCacheDistrictName();
        }
        #endregion

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



        protected void RenderPropMesh<B>(ref PropInfo propInfo, RenderManager.CameraInfo cameraInfo, ushort refId, int boardIdx, int secIdx, int layerMask, float refAngleRad, Vector3 position, Vector4 dataVector, ref string propName, Vector3 propAngle, Vector3 propScale, BoardInstanceXml<B> descriptor, out Matrix4x4 propMatrix, out bool rendered) where B : IBoardDescriptor, new()
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
            propMatrix = RenderUtils.RenderProp(refId, refAngleRad, cameraInfo, propInfo, position, dataVector, boardIdx, propAngle, propScale, out rendered, GetPropRenderID(refId));
        }



        #region Rendering

        protected abstract InstanceID GetPropRenderID(ushort refID);

        protected void RenderTextMesh<B>(RenderManager.CameraInfo cameraInfo, ushort refID, int boardIdx, int secIdx, BoardInstanceXml<B> descriptor, Matrix4x4 propMatrix, BoardTextDescriptorGeneralXml textDescriptor, MaterialPropertyBlock materialPropertyBlock) where B : IBoardDescriptor, new()
        {
            BasicRenderInformation renderInfo = null;
            switch (textDescriptor.m_textType)
            {
                case TextType.OwnName:
                    renderInfo = GetOwnNameMesh(refID, boardIdx, secIdx, descriptor);
                    break;
                case TextType.Fixed:
                    renderInfo = GetFixedTextMesh(textDescriptor, refID, boardIdx, secIdx, descriptor);
                    break;
                case TextType.StreetPrefix:
                    renderInfo = GetMeshStreetPrefix(refID, boardIdx, secIdx, descriptor);
                    break;
                case TextType.StreetSuffix:
                    renderInfo = GetMeshStreetSuffix(refID, boardIdx, secIdx, descriptor);
                    break;
                case TextType.StreetNameComplete:
                    renderInfo = GetMeshFullStreetName(refID, boardIdx, secIdx, descriptor);
                    break;
                case TextType.BuildingNumber:
                    renderInfo = GetMeshCurrentNumber(refID, boardIdx, secIdx, descriptor);
                    break;
                case TextType.District:
                    renderInfo = GetMeshDistrict(refID, boardIdx, secIdx, descriptor);
                    break;
                case TextType.Custom1:
                    renderInfo = GetMeshCustom1(refID, boardIdx, secIdx, descriptor);
                    break;
                case TextType.Custom2:
                    renderInfo = GetMeshCustom2(refID, boardIdx, secIdx, descriptor);
                    break;
                case TextType.Custom3:
                    renderInfo = GetMeshCustom3(refID, boardIdx, secIdx, descriptor);
                    break;
                case TextType.LinesSymbols:
                    renderInfo = GetMeshLinesSymbols(refID, boardIdx, secIdx, descriptor);
                    break;
                case TextType.Custom4:
                    renderInfo = GetMeshCustom4(refID, boardIdx, secIdx, descriptor);
                    break;
                case TextType.Custom5:
                    renderInfo = GetMeshCustom5(refID, boardIdx, secIdx, descriptor);
                    break;
            }
            if (renderInfo?.m_mesh == null || renderInfo?.m_generatedMaterial == null)
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
            //LogUtils.DoLog($"[{GetType().Name},{refID},{boardIdx},{secIdx}] realWidth = {realWidth}; realHeight = {realHeight}; renderInfo.m_mesh.bounds = {renderInfo.m_mesh.bounds};");
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


            //materialPropertyBlock.SetColor(m_shaderPropEmissive, Color.white * (SimulationManager.instance.m_isNightTime ? textDescriptor.m_nightEmissiveMultiplier : textDescriptor.m_dayEmissiveMultiplier));
            renderInfo.m_generatedMaterial.shader = TextShader;
            Graphics.DrawMesh(renderInfo.m_mesh, matrix, renderInfo.m_generatedMaterial, 10, null, 0, materialPropertyBlock, false);

        }
        #endregion
        public abstract Color? GetColor<B>(ushort buildingID, int idx, int secIdx, BoardInstanceXml<B> descriptor) where B : IBoardDescriptor, new();
        public abstract Color GetContrastColor<B>(ushort refID, int boardIdx, int secIdx, BoardInstanceXml<B> descriptor) where B : IBoardDescriptor, new();

        #region UpdateData
        protected virtual BasicRenderInformation GetOwnNameMesh<B>(ushort refID, int boardIdx, int secIdx, BoardInstanceXml<B> descriptor) where B : IBoardDescriptor, new() => null;
        protected virtual BasicRenderInformation GetMeshCurrentNumber<B>(ushort refID, int boardIdx, int secIdx, BoardInstanceXml<B> descriptor) where B : IBoardDescriptor, new() => null;
        protected virtual BasicRenderInformation GetMeshFullStreetName<B>(ushort refID, int boardIdx, int secIdx, BoardInstanceXml<B> descriptor) where B : IBoardDescriptor, new() => null;
        protected virtual BasicRenderInformation GetMeshStreetSuffix<B>(ushort refID, int boardIdx, int secIdx, BoardInstanceXml<B> descriptor) where B : IBoardDescriptor, new() => null;
        protected virtual BasicRenderInformation GetMeshStreetPrefix<B>(ushort refID, int boardIdx, int secIdx, BoardInstanceXml<B> descriptor) where B : IBoardDescriptor, new() => null;
        protected virtual BasicRenderInformation GetMeshDistrict<B>(ushort refID, int boardIdx, int secIdx, BoardInstanceXml<B> descriptor) where B : IBoardDescriptor, new() => null;
        protected virtual BasicRenderInformation GetMeshCustom1<B>(ushort refID, int boardIdx, int secIdx, BoardInstanceXml<B> descriptor) where B : IBoardDescriptor, new() => null;
        protected virtual BasicRenderInformation GetMeshCustom2<B>(ushort refID, int boardIdx, int secIdx, BoardInstanceXml<B> descriptor) where B : IBoardDescriptor, new() => null;
        protected virtual BasicRenderInformation GetMeshCustom3<B>(ushort refID, int boardIdx, int secIdx, BoardInstanceXml<B> descriptor) where B : IBoardDescriptor, new() => null;
        protected virtual BasicRenderInformation GetMeshCustom4<B>(ushort refID, int boardIdx, int secIdx, BoardInstanceXml<B> descriptor) where B : IBoardDescriptor, new() => null;
        protected virtual BasicRenderInformation GetMeshCustom5<B>(ushort refID, int boardIdx, int secIdx, BoardInstanceXml<B> descriptor) where B : IBoardDescriptor, new() => null;
        protected virtual BasicRenderInformation GetMeshLinesSymbols<B>(ushort refID, int boardIdx, int secIdx, BoardInstanceXml<B> descriptor) where B : IBoardDescriptor, new() => null;
        protected virtual BasicRenderInformation GetFixedTextMesh<B>(BoardTextDescriptorGeneralXml textDescriptor, ushort refID, int boardIdx, int secIdx, BoardInstanceXml<B> descriptor) where B : IBoardDescriptor, new() => RenderUtils.GetTextData(textDescriptor.m_fixedText ?? "", DrawFont);
        #endregion



    }


}
