using ColossalFramework.Math;
using Klyte.Commons.Utils;
using SpriteFontPlus;
using SpriteFontPlus.Utility;
using UnityEngine;

namespace Klyte.DynamicTextProps.Utils
{
    internal static class RenderUtils
    {

        private static string[] m_cachedStreetNameInformation_Full = new string[NetManager.MAX_SEGMENT_COUNT];
        private static string[] m_cachedStreetNameInformation_End = new string[NetManager.MAX_SEGMENT_COUNT];
        private static string[] m_cachedStreetNameInformation_Start = new string[NetManager.MAX_SEGMENT_COUNT];
        private static string[] m_cachedDistrictsNames = new string[DistrictManager.MAX_DISTRICT_COUNT];

        public static void ClearCacheStreetName() => m_cachedStreetNameInformation_End = new string[NetManager.MAX_SEGMENT_COUNT];
        public static void ClearCacheStreetQualifier() => m_cachedStreetNameInformation_Start = new string[NetManager.MAX_SEGMENT_COUNT];
        public static void ClearCacheFullStreetName() => m_cachedStreetNameInformation_Full = new string[NetManager.MAX_SEGMENT_COUNT];
        public static void ClearCacheDistrictName() => m_cachedDistrictsNames = new string[DistrictManager.MAX_DISTRICT_COUNT];

        public enum CacheArrayTypes
        {
            FullStreetName,
            SuffixStreetName,
            StreetQualifier,
            District
        }


        public static BasicRenderInformation GetFromCacheArray(ushort refId, string prefix, string suffix, bool allCaps, CacheArrayTypes type, DynamicSpriteFont primaryFont, string overrideFont = null)
        {
            return type switch
            {
                CacheArrayTypes.SuffixStreetName => UpdateMeshStreetSuffix(refId, ref m_cachedStreetNameInformation_End[refId], prefix, suffix, allCaps, primaryFont, overrideFont),
                CacheArrayTypes.FullStreetName => UpdateMeshFullNameStreet(refId, ref m_cachedStreetNameInformation_Full[refId], prefix, suffix, allCaps, primaryFont, overrideFont),
                CacheArrayTypes.StreetQualifier => UpdateMeshStreetQualifier(refId, ref m_cachedStreetNameInformation_Start[refId], prefix, suffix, allCaps, primaryFont, overrideFont),
                CacheArrayTypes.District => UpdateMeshDistrict(refId, ref m_cachedDistrictsNames[refId], prefix, suffix, allCaps, primaryFont, overrideFont),
                _ => null,
            };
        }

        public static BasicRenderInformation UpdateMeshFullNameStreet(ushort idx, ref string name, string prefix, string suffix, bool allCaps, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                name = DTPHookable.GetStreetFullName(idx);
                LogUtils.DoLog($"!GenName {name} for {idx}");
            }
            return GetTextData(name, prefix, suffix, allCaps, primaryFont, overrideFont);
        }
        public static BasicRenderInformation UpdateMeshStreetQualifier(ushort idx, ref string name, string prefix, string suffix, bool allCaps, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                name = DTPHookable.GetStreetFullName(idx);
                if ((NetManager.instance.m_segments.m_buffer[idx].m_flags & NetSegment.Flags.CustomName) == 0)
                {
                    name = DTPUtils.ApplyAbbreviations(name.Replace(DTPHookable.GetStreetSuffix(idx), ""));
                }
                else
                {
                    name = DTPUtils.ApplyAbbreviations(name.Replace(DTPHookable.GetStreetSuffixCustom(idx), ""));
                }
                LogUtils.DoLog($"!GenName {name} for {idx}");
            }
            return GetTextData(name, prefix, suffix, allCaps, primaryFont, overrideFont);
        }
        public static BasicRenderInformation UpdateMeshDistrict(ushort districtId, ref string name, string prefix, string suffix, bool allCaps, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                if (districtId == 0)
                {
                    name = SimulationManager.instance.m_metaData.m_CityName;
                }
                else
                {
                    name = DistrictManager.instance.GetDistrictName(districtId);
                }
            }
            return GetTextData(name, prefix, suffix, allCaps, primaryFont, overrideFont);
        }
        public static BasicRenderInformation UpdateMeshStreetSuffix(ushort idx, ref string name, string prefix, string suffix, bool allCaps, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                LogUtils.DoLog($"!UpdateMeshStreetSuffix {idx}");
                if ((NetManager.instance.m_segments.m_buffer[idx].m_flags & NetSegment.Flags.CustomName) == 0)
                {
                    name = DTPUtils.ApplyAbbreviations(DTPHookable.GetStreetSuffix(idx));
                }
                else
                {
                    name = DTPUtils.ApplyAbbreviations(DTPHookable.GetStreetSuffixCustom(idx));
                }
            }
            return GetTextData(name, prefix, suffix, allCaps, primaryFont, overrideFont);
        }


        public static BasicRenderInformation GetTextData(string text, string prefix, string suffix, bool allCaps, DynamicSpriteFont primaryFont, string overrideFont = null)
        {
            string str = $"{prefix}{text}{suffix}";
            if (allCaps)
            {
                str = str.ToUpper();
            }
            return (FontServer.instance[overrideFont] ?? primaryFont).DrawString(str, default, Color.white, FontServer.instance.ScaleEffective);
        }

        public static Matrix4x4 RenderProp(ushort refId, float refAngleRad, RenderManager.CameraInfo cameraInfo,
                                    PropInfo propInfo, Vector3 position, Vector4 dataVector, int idx,
                                    Vector3 rotation, Vector3 scale, out bool rendered, InstanceID propRenderID2)
        {
            rendered = false;
            var randomizer = new Randomizer((refId << 6) | (idx + 32));
            Matrix4x4 matrix = default;
            matrix.SetTRS(position, Quaternion.AngleAxis(rotation.y + (refAngleRad * Mathf.Rad2Deg), Vector3.down) * Quaternion.AngleAxis(rotation.x, Vector3.left) * Quaternion.AngleAxis(rotation.z, Vector3.back), scale);
            if (propInfo != null)
            {
                propInfo = propInfo.GetVariation(ref randomizer);
                Color color = propInfo.m_color0;
                if (cameraInfo.CheckRenderDistance(position, propInfo.m_maxRenderDistance * scale.sqrMagnitude))
                {
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
            }
            return matrix;
        }
    }
}

