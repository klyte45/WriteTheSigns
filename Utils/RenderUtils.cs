using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Overrides;
using SpriteFontPlus;
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


        public static BasicRenderInformation GetFromCacheArray(ushort refId, CacheArrayTypes type, DynamicSpriteFont primaryFont, string overrideFont = null)
        {
            return type switch
            {
                CacheArrayTypes.SuffixStreetName => UpdateMeshStreetSuffix(refId, ref m_cachedStreetNameInformation_End[refId], primaryFont, overrideFont),
                CacheArrayTypes.FullStreetName => UpdateMeshFullNameStreet(refId, ref m_cachedStreetNameInformation_Full[refId], primaryFont, overrideFont),
                CacheArrayTypes.StreetQualifier => UpdateMeshStreetQualifier(refId, ref m_cachedStreetNameInformation_Start[refId], primaryFont, overrideFont),
                CacheArrayTypes.District => UpdateMeshDistrict(refId, ref m_cachedDistrictsNames[refId], primaryFont, overrideFont),
                _ => null,
            };
        }

        public static BasicRenderInformation UpdateMeshFullNameStreet(ushort idx, ref string name, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                name = DTPHookable.GetStreetFullName(idx);
                LogUtils.DoLog($"!GenName {name} for {idx}");
            }
            return GetTextData(name, primaryFont, overrideFont);
        }
        public static BasicRenderInformation UpdateMeshStreetQualifier(ushort idx, ref string name, DynamicSpriteFont primaryFont, string overrideFont)
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
            return GetTextData(name, primaryFont, overrideFont);
        }
        public static BasicRenderInformation UpdateMeshDistrict(ushort districtId, ref string name, DynamicSpriteFont primaryFont, string overrideFont)
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
            return GetTextData(name, primaryFont, overrideFont);
        }
        public static BasicRenderInformation UpdateMeshStreetSuffix(ushort idx, ref string name, DynamicSpriteFont primaryFont, string overrideFont)
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
            return GetTextData(name, primaryFont,overrideFont);
        }


        public static BasicRenderInformation GetTextData(string text, DynamicSpriteFont primaryFont, string overrideFont = null) => (FontServer.instance[overrideFont] ?? primaryFont).DrawString(text, default, Color.white, FontServer.instance.ScaleEffective);


    }
}

