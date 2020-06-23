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
            m_cache[(int)PostalCode] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cache[(int)Districts] = new string[DistrictManager.MAX_DISTRICT_COUNT];
            m_cache[(int)Parks] = new string[DistrictManager.MAX_DISTRICT_COUNT];
            m_cache[(int)FullStreetNameAbbreviation] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cache[(int)SuffixStreetNameAbbreviation] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cache[(int)BuildingName] = new string[BuildingManager.MAX_BUILDING_COUNT];
            m_cache[(int)VehicleNumber] = new string[ushort.MaxValue];
            m_cache[(int)LineIdentifier] = new string[TransportManager.MAX_LINE_COUNT];

            m_cacheUpper[(int)FullStreetName] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cacheUpper[(int)StreetQualifier] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cacheUpper[(int)SuffixStreetName] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cacheUpper[(int)Districts] = new string[DistrictManager.MAX_DISTRICT_COUNT];
            m_cacheUpper[(int)Parks] = new string[DistrictManager.MAX_DISTRICT_COUNT];
            m_cacheUpper[(int)FullStreetNameAbbreviation] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cacheUpper[(int)SuffixStreetNameAbbreviation] = new string[NetManager.MAX_SEGMENT_COUNT];
            m_cacheUpper[(int)BuildingName] = new string[BuildingManager.MAX_BUILDING_COUNT];

        }




        public static void ClearCachePostalCode()
        {
            m_cache[(int)PostalCode] = new string[NetManager.MAX_SEGMENT_COUNT];

            m_cacheUpper[(int)PostalCode] = new string[NetManager.MAX_SEGMENT_COUNT];
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

        public static void ClearCacheBuildingName()
        {
            m_cache[(int)BuildingName] = new string[BuildingManager.MAX_BUILDING_COUNT];

            m_cacheUpper[(int)BuildingName] = new string[BuildingManager.MAX_BUILDING_COUNT];
        }

        public static void ClearCacheBuildingName(ushort buildingID)
        {
            m_cache[(int)BuildingName][buildingID] = null;
            m_cacheUpper[(int)BuildingName][buildingID] = null;
        }
        public static void ClearCacheVehicleNumber(ushort vehicleID) => m_cache[(int)VehicleNumber][vehicleID] = null;
        public static void ClearCacheVehicleNumber() => m_cache[(int)VehicleNumber] = new string[ushort.MaxValue];

        public enum CacheArrayTypes
        {
            FullStreetName,
            SuffixStreetName,
            StreetQualifier,
            Districts,
            Parks,
            FullStreetNameAbbreviation,
            SuffixStreetNameAbbreviation,
            BuildingName,
            VehicleNumber,
            PostalCode,
            LineIdentifier
        }


        public static BasicRenderInformation GetFromCacheArray2(ushort refId, string prefix, string suffix, bool allCaps, bool applyAbbreviations, CacheArrayTypes type, DynamicSpriteFont primaryFont, string overrideFont = null)
        {
            if (applyAbbreviations)
            {
                type = type switch
                {
                    CacheArrayTypes.SuffixStreetName => CacheArrayTypes.SuffixStreetNameAbbreviation,
                    CacheArrayTypes.FullStreetName => CacheArrayTypes.FullStreetNameAbbreviation,
                    _ => type
                };
            }
            return type switch
            {
                CacheArrayTypes.Districts => UpdateMeshDistrict(refId, ref (allCaps ? m_cacheUpper : m_cache)[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont),
                CacheArrayTypes.Parks => UpdateMeshPark(refId, ref (allCaps ? m_cacheUpper : m_cache)[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont),
                CacheArrayTypes.SuffixStreetName => UpdateMeshStreetSuffix(refId, ref (allCaps ? m_cacheUpper : m_cache)[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont),
                CacheArrayTypes.FullStreetName => UpdateMeshFullNameStreet(refId, ref (allCaps ? m_cacheUpper : m_cache)[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont),
                CacheArrayTypes.StreetQualifier => UpdateMeshStreetQualifier(refId, ref (allCaps ? m_cacheUpper : m_cache)[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont),
                CacheArrayTypes.SuffixStreetNameAbbreviation => UpdateMeshStreetSuffix(refId, ref (allCaps ? m_cacheUpper : m_cache)[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont),
                CacheArrayTypes.FullStreetNameAbbreviation => UpdateMeshFullNameStreet(refId, ref (allCaps ? m_cacheUpper : m_cache)[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont),
                CacheArrayTypes.BuildingName => UpdateMeshBuildingName(refId, ref (allCaps ? m_cacheUpper : m_cache)[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont),
                CacheArrayTypes.PostalCode => UpdateMeshPostalCode(refId, ref m_cache[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont),
                CacheArrayTypes.VehicleNumber => UpdateMeshVehicleNumber(refId, ref m_cache[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont),
                CacheArrayTypes.LineIdentifier => UpdateMeshLineIdentifier(refId, ref m_cache[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont),
                _ => null
            };
        }

        public static BasicRenderInformation UpdateMeshFullNameStreet(ushort idx, ref string name, string prefix, string suffix, bool allCaps, bool applyAbbreviations, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                if (idx == 0)
                {
                    name = "";
                }
                else
                {
                    name = WriteTheSignsMod.Controller.ConnectorADR.GetStreetFullName(idx) ?? "";
                    if (applyAbbreviations)
                    {
                        name = WTSUtils.ApplyAbbreviations(name);
                    }
                    if (allCaps)
                    {
                        name = name.ToUpper();
                    }
                }
                LogUtils.DoLog($"!GenName {name} for {idx} (UC={allCaps})");
            }
            return GetTextData(name, prefix, suffix, primaryFont, overrideFont);
        }
        public static BasicRenderInformation UpdateMeshStreetQualifier(ushort idx, ref string name, string prefix, string suffix, bool allCaps, bool applyAbbreviations, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                if (idx == 0)
                {
                    name = "";
                }
                else
                {
                    if ((NetManager.instance.m_segments.m_buffer[idx].m_flags & NetSegment.Flags.CustomName) == 0)
                    {
                        name = WriteTheSignsMod.Controller.ConnectorADR.GetStreetQualifier(idx);
                    }
                    else
                    {
                        name = WriteTheSignsMod.Controller.ConnectorADR.GetStreetQualifierCustom(idx);
                    }
                    if (applyAbbreviations)
                    {
                        name = WTSUtils.ApplyAbbreviations(name);
                    }
                    if (allCaps)
                    {
                        name = name.ToUpper();
                    }
                }
                LogUtils.DoLog($"!GenName {name} for {idx} (UC={allCaps})");
            }
            return GetTextData(name, prefix, suffix, primaryFont, overrideFont);
        }
        public static BasicRenderInformation UpdateMeshPostalCode(ushort idx, ref string name, string prefix, string suffix, bool allCaps, bool applyAbbreviations, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                if (idx == 0)
                {
                    name = "";
                }
                else
                {
                    name = WriteTheSignsMod.Controller.ConnectorADR.GetStreetPostalCode(NetManager.instance.m_segments.m_buffer[idx].m_middlePosition, idx);
                }
                LogUtils.DoLog($"!Gen Postal Code {name} for {idx} (UC={allCaps})");
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
        public static BasicRenderInformation UpdateMeshBuildingName(ushort buildingId, ref string name, string prefix, string suffix, bool allCaps, bool applyAbbreviations, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                name = BuildingManager.instance.GetBuildingName(buildingId, InstanceID.Empty) ?? "";
                if (allCaps)
                {
                    name = name?.ToUpper();
                }
            }
            return GetTextData(name, prefix, suffix, primaryFont, overrideFont);
        }
        public static BasicRenderInformation UpdateMeshVehicleNumber(ushort vehicleId, ref string name, string prefix, string suffix, bool allCaps, bool applyAbbreviations, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                name = WriteTheSignsMod.Controller.ConnectorTLM.GetVehicleIdentifier(vehicleId);
            }
            return GetTextData(name, prefix, suffix, primaryFont, overrideFont);
        }
        public static BasicRenderInformation UpdateMeshLineIdentifier(ushort lineId, ref string name, string prefix, string suffix, bool allCaps, bool applyAbbreviations, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                if (lineId == 0)
                {
                    name = "";
                }
                else
                {
                    name = WriteTheSignsMod.Controller.ConnectorTLM.GetLineIdString(lineId);
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
                if (idx == 0)
                {
                    name = "";
                }
                else
                {
                    LogUtils.DoLog($"!UpdateMeshStreetSuffix {idx} (UC={allCaps})");
                    if ((NetManager.instance.m_segments.m_buffer[idx].m_flags & NetSegment.Flags.CustomName) == 0)
                    {
                        name = applyAbbreviations ? WTSUtils.ApplyAbbreviations(WriteTheSignsMod.Controller.ConnectorADR.GetStreetSuffix(idx)) : WriteTheSignsMod.Controller.ConnectorADR.GetStreetSuffix(idx);
                    }
                    else
                    {
                        name = applyAbbreviations ? WTSUtils.ApplyAbbreviations(WriteTheSignsMod.Controller.ConnectorADR.GetStreetSuffixCustom(idx)) : WriteTheSignsMod.Controller.ConnectorADR.GetStreetSuffixCustom(idx);
                    }
                    if (allCaps)
                    {
                        name = name.ToUpper();
                    }
                }
            }
            return GetTextData(name, prefix, suffix, primaryFont, overrideFont);
        }

        public static BasicRenderInformation GetTextData(string text, string prefix, string suffix, DynamicSpriteFont primaryFont, string overrideFont)
        {
            string str = $"{prefix}{text}{suffix}";
            return (FontServer.instance[overrideFont] ?? primaryFont ?? FontServer.instance[WTSController.DEFAULT_FONT_KEY])?.DrawString(WriteTheSignsMod.Controller, str, default, FontServer.instance.ScaleEffective);
        }

        public static Matrix4x4 RenderProp(ushort refId, float refAngleRad, RenderManager.CameraInfo cameraInfo,
                                    PropInfo propInfo, Color propColor, Vector3 position, Vector4 dataVector, int idx,
                                    Vector3 rotation, Vector3 scale, int layerMask, out bool rendered, InstanceID propRenderID2)
        {
            rendered = false;
            var randomizer = new Randomizer((refId << 6) | (idx + 32));
            Matrix4x4 matrix = default;
            matrix.SetTRS(position, Quaternion.AngleAxis(rotation.y + (refAngleRad * Mathf.Rad2Deg), Vector3.down) * Quaternion.AngleAxis(rotation.x, Vector3.left) * Quaternion.AngleAxis(rotation.z, Vector3.back), scale);
            if (propInfo != null)
            {
                propInfo = propInfo.GetVariation(ref randomizer);
                if (cameraInfo.CheckRenderDistance(position, propInfo.m_maxRenderDistance * scale.sqrMagnitude))
                {
                    int oldLayerMask = cameraInfo.m_layerMask;
                    float oldRenderDist = propInfo.m_lodRenderDistance;
                    propInfo.m_lodRenderDistance *= scale.sqrMagnitude;
                    cameraInfo.m_layerMask = 0x7FFFFFFF;
                    try
                    {
                        PropInstance.RenderInstance(cameraInfo, propInfo, propRenderID2, matrix, position, scale.y, refAngleRad + (rotation.y * Mathf.Deg2Rad), propColor, dataVector, true);
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

