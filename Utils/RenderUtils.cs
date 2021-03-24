using ColossalFramework.Math;
using Klyte.Commons.Utils;
using SpriteFontPlus;
using SpriteFontPlus.Utility;
using System;
using System.Linq;
using UnityEngine;
using static Klyte.WriteTheSigns.Utils.RenderUtils.CacheArrayTypes;

namespace Klyte.WriteTheSigns.Utils
{
    internal static class RenderUtils
    {
        private static string[][][] m_generalCache = new string[Enum.GetValues(typeof(CacheTransformTypes)).Length][][].Select(x => new string[Enum.GetValues(typeof(CacheArrayTypes)).Length][]).ToArray();

        static RenderUtils()
        {
            foreach (string[][] m_cache in m_generalCache)
            {
                m_cache[(int)FullStreetName] = new string[NetManager.MAX_SEGMENT_COUNT];
                m_cache[(int)StreetQualifier] = new string[NetManager.MAX_SEGMENT_COUNT];
                m_cache[(int)SuffixStreetName] = new string[NetManager.MAX_SEGMENT_COUNT];
                m_cache[(int)PostalCode] = new string[NetManager.MAX_SEGMENT_COUNT];
                m_cache[(int)Districts] = new string[DistrictManager.MAX_DISTRICT_COUNT];
                m_cache[(int)Parks] = new string[DistrictManager.MAX_DISTRICT_COUNT];
                m_cache[(int)BuildingName] = new string[BuildingManager.MAX_BUILDING_COUNT];
                m_cache[(int)VehicleNumber] = new string[ushort.MaxValue];
                m_cache[(int)LineIdentifier] = new string[TransportManager.MAX_LINE_COUNT];
                m_cache[(int)LineFullName] = new string[TransportManager.MAX_LINE_COUNT];
            }
        }

        public static void ClearCachePostalCode()
        {
            foreach (string[][] m_cache in m_generalCache)
            {
                m_cache[(int)PostalCode] = new string[NetManager.MAX_SEGMENT_COUNT];
            }
        }

        public static void ClearCacheStreetName()
        {
            foreach (string[][] m_cache in m_generalCache)
            {
                m_cache[(int)SuffixStreetName] = new string[NetManager.MAX_SEGMENT_COUNT];
            }
        }

        public static void ClearCacheStreetQualifier()
        {
            foreach (string[][] m_cache in m_generalCache)
            {
                m_cache[(int)StreetQualifier] = new string[NetManager.MAX_SEGMENT_COUNT];
            }
        }

        public static void ClearCacheFullStreetName()
        {
            foreach (string[][] m_cache in m_generalCache)
            {
                m_cache[(int)FullStreetName] = new string[NetManager.MAX_SEGMENT_COUNT];
            }

        }

        public static void ClearCacheDistrictName()
        {

            foreach (string[][] m_cache in m_generalCache)
            {
                m_cache[(int)Districts] = new string[DistrictManager.MAX_DISTRICT_COUNT];
            }
            ClearCacheLineName();
        }

        public static void ClearCacheParkName()
        {
            foreach (string[][] m_cache in m_generalCache)
            {
                m_cache[(int)Parks] = new string[DistrictManager.MAX_DISTRICT_COUNT];
            }
            ClearCacheLineName();
        }

        public static void ClearCacheBuildingName()
        {
            foreach (string[][] m_cache in m_generalCache)
            {
                m_cache[(int)BuildingName] = new string[BuildingManager.MAX_BUILDING_COUNT];
            }
            ClearCacheLineName();
        }

        public static void ClearCacheBuildingName(ushort buildingID)
        {
            foreach (string[][] m_cache in m_generalCache)
            {
                m_cache[(int)BuildingName][buildingID] = null;
            }
            ClearCacheLineName();
        }
        public static void ClearCacheVehicleNumber(ushort vehicleID)
        {

            foreach (string[][] m_cache in m_generalCache)
            {
                m_cache[(int)VehicleNumber][vehicleID] = null;
            }
        }

        public static void ClearCacheVehicleNumber()
        {

            foreach (string[][] m_cache in m_generalCache)
            {
                m_cache[(int)VehicleNumber] = new string[ushort.MaxValue];
            }
        }

        public static void ClearCacheLineId()
        {
            foreach (string[][] m_cache in m_generalCache)
            {
                m_cache[(int)LineIdentifier] = new string[TransportManager.MAX_LINE_COUNT];
            }
            ClearCacheLineName();
        }
        public static void ClearCacheLineName()
        {
            foreach (string[][] m_cache in m_generalCache)
            {
                m_cache[(int)LineFullName] = new string[TransportManager.MAX_LINE_COUNT];
            }
        }

        public enum CacheArrayTypes
        {
            FullStreetName,
            SuffixStreetName,
            StreetQualifier,
            Districts,
            Parks,
            BuildingName,
            VehicleNumber,
            PostalCode,
            LineIdentifier,
            LineFullName
        }
        public enum CacheTransformTypes
        {
            NORMAL,
            CAPS,
            ABBREV,
            CAPS_ABBREV
        }



        public static BasicRenderInformation GetFromCacheArray2(ushort refId, string prefix, string suffix, bool allCaps, bool applyAbbreviations, CacheArrayTypes type, DynamicSpriteFont primaryFont, string overrideFont = null)
        {
            ref string[][] cache = ref m_generalCache[(allCaps ? 1 : 0) + (applyAbbreviations ? 1 : 2)];
            switch (type)
            {
                case CacheArrayTypes.Districts: return refId > 256 ? UpdateMeshBuildingName((ushort)(refId - 256), ref cache[(int)BuildingName][refId - 256], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont) : UpdateMeshDistrict(refId, ref cache[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont);
                case CacheArrayTypes.Parks: return UpdateMeshPark(refId, ref cache[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont);
                case CacheArrayTypes.SuffixStreetName: return UpdateMeshStreetSuffix(refId, ref cache[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont);
                case CacheArrayTypes.FullStreetName: return UpdateMeshFullNameStreet(refId, ref cache[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont);
                case CacheArrayTypes.StreetQualifier: return UpdateMeshStreetQualifier(refId, ref cache[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont);
                case CacheArrayTypes.BuildingName: return UpdateMeshBuildingName(refId, ref cache[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont);
                case CacheArrayTypes.PostalCode: return UpdateMeshPostalCode(refId, ref cache[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont);
                case CacheArrayTypes.VehicleNumber: return UpdateMeshVehicleNumber(refId, ref cache[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont);
                case CacheArrayTypes.LineIdentifier: return UpdateMeshLineIdentifier(refId, ref cache[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont);
                case CacheArrayTypes.LineFullName: return UpdateMeshLineFullName(refId, ref cache[(int)type][refId], prefix, suffix, allCaps, applyAbbreviations, primaryFont, overrideFont);
                default: return null;
            };
        }
        private static string ApplyTransforms(string name, bool allCaps, bool applyAbbreviations)
        {
            if (applyAbbreviations)
            {
                name = WTSUtils.ApplyAbbreviations(name);
            }
            if (allCaps)
            {
                name = name.ToUpper();
            }

            return name;
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
                    name = ApplyTransforms(name, allCaps, applyAbbreviations);
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
                    name = ApplyTransforms(name, allCaps, applyAbbreviations);
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
                name = ApplyTransforms(name, allCaps, applyAbbreviations);
            }
            return GetTextData(name, prefix, suffix, primaryFont, overrideFont);
        }
        public static BasicRenderInformation UpdateMeshBuildingName(ushort buildingId, ref string name, string prefix, string suffix, bool allCaps, bool applyAbbreviations, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                name = BuildingManager.instance.GetBuildingName(buildingId, InstanceID.Empty) ?? "";
                name = ApplyTransforms(name, allCaps, applyAbbreviations);
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
        public static BasicRenderInformation UpdateMeshLineFullName(ushort lineId, ref string name, string prefix, string suffix, bool allCaps, bool applyAbbreviations, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                if (lineId == 0)
                {
                    name = "";
                }
                else
                {
                    name = TransportManager.instance.GetLineName(lineId);
                    name = ApplyTransforms(name, allCaps, applyAbbreviations);
                }
            }
            return GetTextData(name, prefix, suffix, primaryFont, overrideFont);
        }
        public static BasicRenderInformation UpdateMeshPark(ushort parkId, ref string name, string prefix, string suffix, bool allCaps, bool applyAbbreviations, DynamicSpriteFont primaryFont, string overrideFont)
        {
            if (name == null)
            {
                name = DistrictManager.instance.GetParkName(parkId);
                name = ApplyTransforms(name, allCaps, applyAbbreviations);
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
                        name = WriteTheSignsMod.Controller.ConnectorADR.GetStreetSuffix(idx);
                    }
                    else
                    {
                        name = WriteTheSignsMod.Controller.ConnectorADR.GetStreetSuffixCustom(idx);
                    }
                    name = ApplyTransforms(name, allCaps, applyAbbreviations);
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

