using ColossalFramework.Math;
using Klyte.Commons.Utils;
using SpriteFontPlus;
using SpriteFontPlus.Utility;
using System;
using UnityEngine;
using static Klyte.WriteTheSigns.Utils.RenderUtils.CacheArrayTypes;

namespace Klyte.WriteTheSigns.Utils
{
    internal static class RenderUtils
    {

        private static string[][] m_cache = new string[Enum.GetValues(typeof(CacheArrayTypes)).Length][];
        private static string[][] m_cacheUpper = new string[Enum.GetValues(typeof(CacheArrayTypes)).Length][];

        static RenderUtils()
        {
            m_cache[(int)FullStreetName] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cache[(int)StreetQualifier] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cache[(int)SuffixStreetName] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cache[(int)Districts] = new string[DistrictManager.MAX_DISTRICT_COUNT];
            m_cache[(int)Parks] = new string[DistrictManager.MAX_DISTRICT_COUNT];
            m_cache[(int)FullStreetNameAbbreviation] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cache[(int)SuffixStreetNameAbbreviation] = new string[NetManager.MAX_SEGMENT_COUNT];

            m_cacheUpper[(int)FullStreetName] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cacheUpper[(int)StreetQualifier] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cacheUpper[(int)SuffixStreetName] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cacheUpper[(int)Districts] = new string[DistrictManager.MAX_DISTRICT_COUNT];
            m_cacheUpper[(int)Parks] = new string[DistrictManager.MAX_DISTRICT_COUNT];
            m_cacheUpper[(int)FullStreetNameAbbreviation] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cacheUpper[(int)SuffixStreetNameAbbreviation] = new string[NetManager.MAX_SEGMENT_COUNT];

        }

        public static void ClearCacheStreetName()
        {
            m_cache[(int)SuffixStreetName] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cache[(int)SuffixStreetNameAbbreviation] = new string[NetManager.MAX_SEGMENT_COUNT];

            m_cacheUpper[(int)SuffixStreetName] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cacheUpper[(int)SuffixStreetNameAbbreviation] = new string[NetManager.MAX_SEGMENT_COUNT];
        }

        public static void ClearCacheStreetQualifier()
        {
            m_cache[(int)StreetQualifier] = new string[NetManager.MAX_SEGMENT_COUNT];

            m_cacheUpper[(int)StreetQualifier] = new string[NetManager.MAX_SEGMENT_COUNT];
        }

        public static void ClearCacheFullStreetName()
        {
            m_cache[(int)FullStreetName] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cache[(int)FullStreetNameAbbreviation] = new string[NetManager.MAX_SEGMENT_COUNT];

            m_cacheUpper[(int)FullStreetName] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cacheUpper[(int)FullStreetNameAbbreviation] = new string[NetManager.MAX_SEGMENT_COUNT];
        }

        public static void ClearCacheDistrictName()
        {
            m_cache[(int)Districts] = new string[DistrictManager.MAX_DISTRICT_COUNT];

            m_cacheUpper[(int)Districts] = new string[DistrictManager.MAX_DISTRICT_COUNT];
        }

        public static void ClearCacheParkName()
        {
            m_cache[(int)Parks] = new string[DistrictManager.MAX_DISTRICT_COUNT];

            m_cacheUpper[(int)Parks] = new string[DistrictManager.MAX_DISTRICT_COUNT];
        }

        public enum CacheArrayTypes
        {
            FullStreetName,
            SuffixStreetName,
            StreetQualifier,
            Districts,
            Parks,
            FullStreetNameAbbreviation,
            SuffixStreetNameAbbreviation,
        }


        public static BasicRenderInformation GetFromCacheArray(ushort refId, string prefix, string suffix, bool allCaps, CacheArrayTypes type, DynamicSpriteFont primaryFont, string overrideFont = null)
        {
            return type switch
            {
                CacheArrayTypes.Districts => UpdateMeshDistrict(refId, ref (allCaps ? m_cacheUpper : m_cache)[(int)type][refId], prefix, suffix, allCaps, false, primaryFont, overrideFont),
                CacheArrayTypes.Parks => UpdateMeshPark(refId, ref (allCaps ? m_cacheUpper : m_cache)[(int)type][refId], prefix, suffix, allCaps, false, primaryFont, overrideFont),
                CacheArrayTypes.SuffixStreetName => UpdateMeshStreetSuffix(refId, ref (allCaps ? m_cacheUpper : m_cache)[(int)type][refId], prefix, suffix, allCaps, false, primaryFont, overrideFont),
                CacheArrayTypes.FullStreetName => UpdateMeshFullNameStreet(refId, ref (allCaps ? m_cacheUpper : m_cache)[(int)type][refId], prefix, suffix, allCaps, false, primaryFont, overrideFont),
                CacheArrayTypes.StreetQualifier => UpdateMeshStreetQualifier(refId, ref (allCaps ? m_cacheUpper : m_cache)[(int)type][refId], prefix, suffix, allCaps, false, primaryFont, overrideFont),
                CacheArrayTypes.SuffixStreetNameAbbreviation => UpdateMeshStreetSuffix(refId, ref (allCaps ? m_cacheUpper : m_cache)[(int)type][refId], prefix, suffix, allCaps, true, primaryFont, overrideFont),
                CacheArrayTypes.FullStreetNameAbbreviation => UpdateMeshFullNameStreet(refId, ref (allCaps ? m_cacheUpper : m_cache)[(int)type][refId], prefix, suffix, allCaps, true, primaryFont, overrideFont),
                _ => null

            };
        }

        public static BasicRenderInformation UpdateMeshFullNameStreet(ushort idx, ref string name, string prefix, string suffix, bool allCaps, bool applyAbbreviations, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                name = WTSHookable.GetStreetFullName(idx);
                if (applyAbbreviations)
                {
                    name = WTSUtils.ApplyAbbreviations(name);
                }
                if (allCaps)
                {
                    name = name.ToUpper();
                }
                LogUtils.DoLog($"!GenName {name} for {idx} (UC={allCaps})");
            }
            return GetTextData(name, prefix, suffix, primaryFont, overrideFont);
        }
        public static BasicRenderInformation UpdateMeshStreetQualifier(ushort idx, ref string name, string prefix, string suffix, bool allCaps, bool applyAbbreviations, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                name = WTSHookable.GetStreetFullName(idx);
                if ((NetManager.instance.m_segments.m_buffer[idx].m_flags & NetSegment.Flags.CustomName) == 0)
                {
                    name = applyAbbreviations ? WTSUtils.ApplyAbbreviations(name.Replace(WTSHookable.GetStreetSuffix(idx), "")) : name.Replace(WTSHookable.GetStreetSuffix(idx), "");
                }
                else
                {
                    name = applyAbbreviations ? WTSUtils.ApplyAbbreviations(name.Replace(WTSHookable.GetStreetSuffixCustom(idx), "")) : name.Replace(WTSHookable.GetStreetSuffixCustom(idx), "");
                }
                if (allCaps)
                {
                    name = name.ToUpper();
                }
                LogUtils.DoLog($"!GenName {name} for {idx} (UC={allCaps})");
            }
            return GetTextData(name, prefix, suffix, primaryFont, overrideFont);
        }
        public static BasicRenderInformation UpdateMeshDistrict(ushort districtId, ref string name, string prefix, string suffix, bool allCaps, bool applyAbbreviations, DynamicSpriteFont primaryFont, string overrideFont)
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
                if (allCaps)
                {
                    name = name.ToUpper();
                }
            }
            return GetTextData(name, prefix, suffix, primaryFont, overrideFont);
        }
        public static BasicRenderInformation UpdateMeshPark(ushort parkId, ref string name, string prefix, string suffix, bool allCaps, bool applyAbbreviations, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                name = DistrictManager.instance.GetParkName(parkId);
                if (allCaps)
                {
                    name = name.ToUpper();
                }
            }
            return GetTextData(name, prefix, suffix, primaryFont, overrideFont);
        }
        public static BasicRenderInformation UpdateMeshStreetSuffix(ushort idx, ref string name, string prefix, string suffix, bool allCaps, bool applyAbbreviations, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                LogUtils.DoLog($"!UpdateMeshStreetSuffix {idx} (UC={allCaps})");
                if ((NetManager.instance.m_segments.m_buffer[idx].m_flags & NetSegment.Flags.CustomName) == 0)
                {
                    name = applyAbbreviations ? WTSUtils.ApplyAbbreviations(WTSHookable.GetStreetSuffix(idx)) : WTSHookable.GetStreetSuffix(idx);
                }
                else
                {
                    name = applyAbbreviations ? WTSUtils.ApplyAbbreviations(WTSHookable.GetStreetSuffixCustom(idx)) : WTSHookable.GetStreetSuffixCustom(idx);
                }
                if (allCaps)
                {
                    name = name.ToUpper();
                }
            }
            return GetTextData(name, prefix, suffix, primaryFont, overrideFont);
        }


        public static BasicRenderInformation GetTextData(string text, string prefix, string suffix, DynamicSpriteFont primaryFont, string overrideFont = null)
        {
            string str = $"{prefix}{text}{suffix}";
            return (FontServer.instance[overrideFont] ?? primaryFont)?.DrawString(str, default, Color.white, FontServer.instance.ScaleEffective);
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

