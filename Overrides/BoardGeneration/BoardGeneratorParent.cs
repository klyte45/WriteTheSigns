using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Data;
using Klyte.DynamicTextProps.Utils;
using SpriteFontPlus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{

    public abstract class BoardGeneratorParent<BG> : Redirector, IRedirectable where BG : BoardGeneratorParent<BG>
    {

        public abstract UIDynamicFont DrawFont { get; }
        protected uint lastFontUpdateFrame = SimulationManager.instance.m_currentTickIndex;
        protected static Shader TextShader = Shader.Find("Custom/Props/Prop/Default") ?? DistrictManager.instance.m_properties.m_areaNameShader;

        public static BG Instance { get; protected set; }

        protected void BuildSurfaceFont(out UIDynamicFont font, string fontName) => font = null;//font = ScriptableObject.CreateInstance<UIDynamicFont>();//var fontList = new List<string> { fontName };//fontList.AddRange(DistrictManager.instance.m_properties?.m_areaNameFont?.baseFont?.fontNames?.ToList() ?? new List<string>());//font.baseFont = Font.CreateDynamicFontFromOSFont(fontList.ToArray(), 64);//font.lineHeight = 70;//font.baseline = 66;//font.size = 64;

        public void ChangeFont(string newFont)
        {

            //var fontList = new List<string>();
            //if (newFont != null)
            //{
            //    fontList.Add(newFont);
            //}
            //fontList.AddRange(DistrictManager.instance.m_properties.m_areaNameFont.baseFont.fontNames.ToList());
            //DrawFont.baseFont = Font.CreateDynamicFontFromOSFont(fontList.ToArray(), 64);
            //lastFontUpdateFrame = SimulationManager.instance.m_currentTickIndex;
            //OnChangeFont(DrawFont.baseFont.name != newFont ? null : newFont);
            //Reset();
        }
        protected virtual void OnChangeFont(string fontName) { }

        public virtual void Reset()
        {
            lastFontUpdateFrame = SimulationManager.instance.m_currentTickIndex;

            ResetImpl();
        }
        protected abstract void ResetImpl();

        public virtual void Awake() => Instance = this as BG;
    }

    public abstract class BoardGeneratorParent<BG, BBC, D, BD, BTD> : BoardGeneratorParent<BG>
        where BG : BoardGeneratorParent<BG, BBC, D, BD, BTD>
        where BBC : IBoardBunchContainer
        where D : DTPBaseData<D, BBC>, new()
        where BD : BoardDescriptorParentXml<BD, BTD>
        where BTD : BoardTextDescriptorParentXml<BTD>
    {

        public sealed override void Reset()
        {
            base.Reset();
            m_cachedStreetNameInformation_Full = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cachedStreetNameInformation_End = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cachedStreetNameInformation_Start = new string[DistrictManager.MAX_DISTRICT_COUNT];
            m_cachedDistrictsNames = new string[DistrictManager.MAX_DISTRICT_COUNT];
            m_defaultCacheForStrings = new Dictionary<string, BasicRenderInformation>();
        }

        public D Data => DTPBaseData<D, BBC>.Instance;

        public static readonly int m_shaderPropColor = Shader.PropertyToID("_Color");
        public static readonly int m_shaderPropColor0 = Shader.PropertyToID("_ColorV0");
        public static readonly int m_shaderPropColor1 = Shader.PropertyToID("_ColorV1");
        public static readonly int m_shaderPropColor2 = Shader.PropertyToID("_ColorV2");
        public static readonly int m_shaderPropColor3 = Shader.PropertyToID("_ColorV3");
        public static readonly int m_shaderPropEmissive = Shader.PropertyToID("_SpecColor");
        public abstract void Initialize();

        private const float m_pixelRatio = 2;
        //private const float m_scaleY = 1.2f;
        private const float m_textScale = 0.75f;
        private readonly Vector2 m_scalingMatrix = new Vector2(0.005f, 0.005f);

        public override void Awake()
        {
            base.Awake();
            Initialize();

            _font.CurrentAtlasFull += Reset;

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
            m_cachedStreetNameInformation_Full = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cachedStreetNameInformation_End = new string[NetManager.MAX_SEGMENT_COUNT];
        }

        #region events
        private void OnNameSeedChanged(ushort segmentId)
        {
            m_cachedStreetNameInformation_Full[segmentId] = null;
            m_cachedStreetNameInformation_End[segmentId] = null;
        }
        private void OnDistrictChanged()
        {
            LogUtils.DoLog("onDistrictChanged");
            m_cachedDistrictsNames = new string[DistrictManager.MAX_DISTRICT_COUNT];
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

        protected void RenderTextMesh(RenderManager.CameraInfo cameraInfo, ushort refID, int boardIdx, int secIdx, ref BD descriptor, Matrix4x4 propMatrix, ref BTD textDescriptor, MaterialPropertyBlock materialPropertyBlock)
        {
            BasicRenderInformation renderInfo = null;
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
                case TextType.District:
                    renderInfo = GetMeshDistrict(refID, boardIdx, secIdx, ref descriptor);
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
                case TextType.LinesSymbols:
                    renderInfo = GetMeshLinesSymbols(refID, boardIdx, secIdx, ref descriptor);
                    break;
                case TextType.Custom4:
                    renderInfo = GetMeshCustom4(refID, boardIdx, secIdx, ref descriptor);
                    break;
                case TextType.Custom5:
                    renderInfo = GetMeshCustom5(refID, boardIdx, secIdx, ref descriptor);
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


            materialPropertyBlock.SetColor(m_shaderPropEmissive, Color.white * (SimulationManager.instance.m_isNightTime ? textDescriptor.m_nightEmissiveMultiplier : textDescriptor.m_dayEmissiveMultiplier));
            renderInfo.m_generatedMaterial.shader = TextShader;
            Graphics.DrawMesh(renderInfo.m_mesh, matrix, renderInfo.m_generatedMaterial, 10, cameraInfo.m_camera, 0, materialPropertyBlock, false, true, true);

        }

        private string[] m_cachedStreetNameInformation_Full = new string[NetManager.MAX_SEGMENT_COUNT];
        private string[] m_cachedStreetNameInformation_End = new string[NetManager.MAX_SEGMENT_COUNT];
        private string[] m_cachedStreetNameInformation_Start = new string[NetManager.MAX_SEGMENT_COUNT];
        private string[] m_cachedDistrictsNames = new string[DistrictManager.MAX_DISTRICT_COUNT];
        private Dictionary<string, BasicRenderInformation> m_defaultCacheForStrings = new Dictionary<string, BasicRenderInformation>();
        private uint m_lastTickTextureGen = 0;

        public void ClearCacheStreetName() => m_cachedStreetNameInformation_End = new string[NetManager.MAX_SEGMENT_COUNT];
        public void ClearCacheStreetQualifier() => m_cachedStreetNameInformation_Start = new string[NetManager.MAX_SEGMENT_COUNT];
        public void ClearCacheFullStreetName() => m_cachedStreetNameInformation_Full = new string[NetManager.MAX_SEGMENT_COUNT];
        public void ClearCacheDefault() => m_defaultCacheForStrings = new Dictionary<string, BasicRenderInformation>();

        protected enum CacheArrayTypes
        {
            FullStreetName,
            SuffixStreetName,
            StreetQualifier,
            District
        }


        protected BasicRenderInformation GetFromCacheArray(ushort refId, CacheArrayTypes type)
        {
            switch (type)
            {
                case CacheArrayTypes.SuffixStreetName:
                    return GetFromCacheArray(refId, type, ref m_cachedStreetNameInformation_End);
                case CacheArrayTypes.StreetQualifier:
                    return GetFromCacheArray(refId, type, ref m_cachedStreetNameInformation_Start);
                case CacheArrayTypes.FullStreetName:
                    return GetFromCacheArray(refId, type, ref m_cachedStreetNameInformation_Full);
                case CacheArrayTypes.District:
                    return GetFromCacheArray(refId, type, ref m_cachedDistrictsNames);

            }
            return null;
        }

        private BasicRenderInformation GetFromCacheArray(ushort refId, CacheArrayTypes type, ref string[] cacheArray)
        {
            if (m_lastTickTextureGen < SimulationManager.instance.m_currentTickIndex && (cacheArray[refId] == null))
            {
                LogUtils.DoLog($"!nameUpdated segmentId {refId}");
                switch (type)
                {
                    case CacheArrayTypes.SuffixStreetName:
                        UpdateMeshStreetSuffix(refId, ref cacheArray[refId], m_defaultCacheForStrings);
                        break;
                    case CacheArrayTypes.FullStreetName:
                        UpdateMeshFullNameStreet(refId, ref cacheArray[refId], m_defaultCacheForStrings);
                        break;
                    case CacheArrayTypes.StreetQualifier:
                        UpdateMeshStreetQualifier(refId, ref cacheArray[refId], m_defaultCacheForStrings);
                        break;
                    case CacheArrayTypes.District:
                        UpdateMeshDistrict(refId, ref cacheArray[refId], m_defaultCacheForStrings);
                        break;
                }
                m_lastTickTextureGen = SimulationManager.instance.m_currentTickIndex;
            }
            m_defaultCacheForStrings.TryGetValue(cacheArray[refId] ?? "", out BasicRenderInformation result);
            return result;


        }

        protected void UpdateMeshFullNameStreet(ushort idx, ref string name, Dictionary<string, BasicRenderInformation> bris)
        {
            name = DTPHookable.GetStreetFullName(idx);
            LogUtils.DoLog($"!GenName {name} for {idx}");
            UpdateTextIfNecessary(name, bris);
        }
        protected void UpdateMeshStreetQualifier(ushort idx, ref string name, Dictionary<string, BasicRenderInformation> bris)
        {
            name = DTPHookable.GetStreetFullName(idx);
            if ((NetManager.instance.m_segments.m_buffer[idx].m_flags & NetSegment.Flags.CustomName) == 0)
            {
                name = ApplyAbbreviations(name.Replace(DTPHookable.GetStreetSuffix(idx), ""));
            }
            else
            {
                name = ApplyAbbreviations(name.Replace(DTPHookable.GetStreetSuffixCustom(idx), ""));
            }
            LogUtils.DoLog($"!GenName {name} for {idx}");
            UpdateTextIfNecessary(name, bris);
        }
        protected void UpdateMeshDistrict(ushort districtId, ref string name, Dictionary<string, BasicRenderInformation> bris)
        {
            if (districtId == 0)
            {
                name = SimulationManager.instance.m_metaData.m_CityName;
            }
            else
            {
                name = DistrictManager.instance.GetDistrictName(districtId);
            }
            UpdateTextIfNecessary(name, bris);
        }
        protected void UpdateMeshStreetSuffix(ushort idx, ref string name, Dictionary<string, BasicRenderInformation> bris)
        {
            LogUtils.DoLog($"!UpdateMeshStreetSuffix {idx}");
            if ((NetManager.instance.m_segments.m_buffer[idx].m_flags & NetSegment.Flags.CustomName) == 0)
            {
                name = ApplyAbbreviations(DTPHookable.GetStreetSuffix(idx));
            }
            else
            {
                name = ApplyAbbreviations(DTPHookable.GetStreetSuffixCustom(idx));
            }
            UpdateTextIfNecessary(name, bris);
        }

        protected string ApplyAbbreviations(string name)
        {
            if (DynamicTextPropsMod.Controller.AbbreviationFiles.TryGetValue(BoardGeneratorRoadNodes.Instance.LoadedStreetSignDescriptor.AbbreviationFile ?? "", out Dictionary<string, string> translations))
            {
                foreach (string key in translations.Keys.Where(x => x.Contains(" ")))
                {
                    name = ReplaceCaseInsensitive(name, key, translations[key], StringComparison.OrdinalIgnoreCase);

                }
                string[] parts = name.Split(' ');
                for (int i = 0; i < parts.Length; i++)
                {
                    if ((i == 0 && translations.TryGetValue($"^{parts[i]}", out string replacement))
                        || (i == parts.Length - 1 && translations.TryGetValue($"{parts[i]}$", out replacement))
                        || (i > 0 && i < parts.Length - 1 && translations.TryGetValue($"={parts[i]}=", out replacement))
                        || translations.TryGetValue(parts[i], out replacement))
                    {
                        parts[i] = replacement;
                    }
                }
                return string.Join(" ", parts.Where(x => !x.IsNullOrWhiteSpace()).ToArray());

            }
            else
            {
                return name;
            }
        }
        /// <summary>
        /// Returns a new string in which all occurrences of a specified string in the current instance are replaced with another 
        /// specified string according the type of search to use for the specified string.
        /// </summary>
        /// <param name="str">The string performing the replace method.</param>
        /// <param name="oldValue">The string to be replaced.</param>
        /// <param name="newValue">The string replace all occurrences of <paramref name="oldValue"/>. 
        /// If value is equal to <c>null</c>, than all occurrences of <paramref name="oldValue"/> will be removed from the <paramref name="str"/>.</param>
        /// <param name="comparisonType">One of the enumeration values that specifies the rules for the search.</param>
        /// <returns>A string that is equivalent to the current string except that all instances of <paramref name="oldValue"/> are replaced with <paramref name="newValue"/>. 
        /// If <paramref name="oldValue"/> is not found in the current instance, the method returns the current instance unchanged.</returns>
        [DebuggerStepThrough]
        public static string ReplaceCaseInsensitive(string str,
            string oldValue, string @newValue,
            StringComparison comparisonType)
        {

            // Check inputs.
            if (str == null)
            {
                // Same as original .NET C# string.Replace behavior.
                throw new ArgumentNullException(nameof(str));
            }
            if (str.Length == 0)
            {
                // Same as original .NET C# string.Replace behavior.
                return str;
            }
            if (oldValue == null)
            {
                // Same as original .NET C# string.Replace behavior.
                throw new ArgumentNullException(nameof(oldValue));
            }
            if (oldValue.Length == 0)
            {
                // Same as original .NET C# string.Replace behavior.
                throw new ArgumentException("String cannot be of zero length.");
            }


            //if (oldValue.Equals(newValue, comparisonType))
            //{
            //This condition has no sense
            //It will prevent method from replacesing: "Example", "ExAmPlE", "EXAMPLE" to "example"
            //return str;
            //}



            // Prepare string builder for storing the processed string.
            // Note: StringBuilder has a better performance than String by 30-40%.
            var resultStringBuilder = new StringBuilder(str.Length);



            // Analyze the replacement: replace or remove.
            bool isReplacementNullOrEmpty = string.IsNullOrEmpty(@newValue);



            // Replace all values.
            const int valueNotFound = -1;
            int foundAt;
            int startSearchFromIndex = 0;
            while ((foundAt = str.IndexOf(oldValue, startSearchFromIndex, comparisonType)) != valueNotFound)
            {

                // Append all characters until the found replacement.
                int @charsUntilReplacment = foundAt - startSearchFromIndex;
                bool isNothingToAppend = @charsUntilReplacment == 0;
                if (!isNothingToAppend)
                {
                    resultStringBuilder.Append(str, startSearchFromIndex, @charsUntilReplacment);
                }



                // Process the replacement.
                if (!isReplacementNullOrEmpty)
                {
                    resultStringBuilder.Append(@newValue);
                }


                // Prepare start index for the next search.
                // This needed to prevent infinite loop, otherwise method always start search 
                // from the start of the string. For example: if an oldValue == "EXAMPLE", newValue == "example"
                // and comparisonType == "any ignore case" will conquer to replacing:
                // "EXAMPLE" to "example" to "example" to "example" … infinite loop.
                startSearchFromIndex = foundAt + oldValue.Length;
                if (startSearchFromIndex == str.Length)
                {
                    // It is end of the input string: no more space for the next search.
                    // The input string ends with a value that has already been replaced. 
                    // Therefore, the string builder with the result is complete and no further action is required.
                    return resultStringBuilder.ToString();
                }
            }


            // Append the last part to the result.
            int @charsUntilStringEnd = str.Length - startSearchFromIndex;
            resultStringBuilder.Append(str, startSearchFromIndex, @charsUntilStringEnd);


            return resultStringBuilder.ToString();

        }
        private void UpdateTextIfNecessary(string name, Dictionary<string, BasicRenderInformation> bris)
        {
            if (name != null && (!bris.ContainsKey(name) || lastFontUpdateFrame > bris[name].m_frameDrawTime))
            {
                BasicRenderInformation bri = bris.ContainsKey(name) ? bris[name] : default;
                RefreshTextData(ref bri, name);
                bris[name] = bri;
            }
        }

        protected BasicRenderInformation RefreshTextData(string text, UIFont overrideFont = null)
        {
            var result = new BasicRenderInformation();
            RefreshTextData(ref result, text, overrideFont);
            return result;
        }

        private static DynamicSpriteFont _font = DynamicSpriteFont.FromTtf(KlyteResourceLoader.LoadResourceData("UI.DefaultFont.SourceSansPro-Regular.ttf"), 160);
        protected void RefreshTextData(ref BasicRenderInformation result, string text, UIFont overrideFont = null)
        {
            if (result == null)
            {
                result = new BasicRenderInformation();
            }
            if (text.IsNullOrWhiteSpace())
            {
                result.m_frameDrawTime = uint.MaxValue;
                return;
            }
            _font.DrawString(result, text, default, Color.white, Vector2.one * .75F);
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
        protected virtual BasicRenderInformation GetOwnNameMesh(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BasicRenderInformation GetMeshCurrentNumber(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BasicRenderInformation GetMeshFullStreetName(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BasicRenderInformation GetMeshStreetSuffix(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BasicRenderInformation GetMeshStreetPrefix(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BasicRenderInformation GetMeshDistrict(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BasicRenderInformation GetMeshCustom1(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BasicRenderInformation GetMeshCustom2(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BasicRenderInformation GetMeshCustom3(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BasicRenderInformation GetMeshCustom4(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BasicRenderInformation GetMeshCustom5(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BasicRenderInformation GetMeshLinesSymbols(ushort refID, int boardIdx, int secIdx, ref BD descriptor) => null;
        protected virtual BasicRenderInformation GetFixedTextMesh(ref BTD textDescriptor, ushort refID, int boardIdx, int secIdx, ref BD descriptor)
        {

            if (textDescriptor.GeneratedFixedTextRenderInfo == null || textDescriptor.GeneratedFixedTextRenderInfoTick < lastFontUpdateFrame)
            {
                var result = textDescriptor.GeneratedFixedTextRenderInfo as BasicRenderInformation;
                RefreshTextData(ref result, (textDescriptor.m_isFixedTextLocalized ? Locale.Get(textDescriptor.m_fixedText, textDescriptor.m_fixedTextLocaleKey) : textDescriptor.m_fixedText) ?? "");
                textDescriptor.GeneratedFixedTextRenderInfo = result;
            }
            return textDescriptor.GeneratedFixedTextRenderInfo as BasicRenderInformation;
        }
        #endregion



    }


}
