using ColossalFramework.Math;
using Klyte.Commons.Utils;
using SpriteFontPlus;
using SpriteFontPlus.Utility;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using static Klyte.WriteTheSigns.Utils.RenderUtils.CacheArrayTypes;

namespace Klyte.WriteTheSigns.Utils
{
    internal static class RenderUtils
    {
        private static string[][][] m_generalCache = GenerateCacheArray();

        private static string[][][] GenerateCacheArray() => new string[Enum.GetValues(typeof(CacheTransformTypes)).Length][][].Select(x => new string[Enum.GetValues(typeof(CacheArrayTypes)).Length][]).ToArray();


        public static long GetGeneralTextCacheSize()
        {
            long sizeBytes = 0;
            foreach (string[][] m_cache in m_generalCache)
            {
                if (!(m_cache is null))
                {
                    foreach (string[] subCache in m_cache)
                    {
                        if (!(subCache is null))
                        {
                            foreach (string val in subCache)
                            {
                                if (val != null)
                                {
                                    sizeBytes += Encoding.Default.GetBytes(val).Length;
                                }
                            }
                        }
                    }
                }
            }
            LogUtils.DoLog($"size by for: {sizeBytes}; by marshaller: {Marshal.SizeOf(sizeBytes)}");
            return sizeBytes;
        }

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
                m_cache[(int)LineIdentifier] = new string[TransportManager.MAX_LINE_COUNT + NetManager.MAX_NODE_COUNT];
                m_cache[(int)LineFullName] = new string[TransportManager.MAX_LINE_COUNT + NetManager.MAX_NODE_COUNT];
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

        public static void ClearCacheCityName()
        {
            foreach (string[][] m_cache in m_generalCache)
            {
                m_cache[(int)Districts][0] = null;
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

        public static void ClearCacheBuildingName(ushort? buildingId)
        {
            foreach (string[][] m_cache in m_generalCache)
            {
                if (buildingId is null)
                {
                    m_cache[(int)BuildingName] = new string[BuildingManager.MAX_BUILDING_COUNT];
                }
                else
                {
                    m_cache[(int)BuildingName][(int)buildingId] = null;
                }
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
                m_cache[(int)LineIdentifier] = new string[TransportManager.MAX_LINE_COUNT + NetManager.MAX_NODE_COUNT];
            }
            ClearCacheLineName();
            ClearCacheVehicleNumber();
        }
        public static void ClearCacheLineName()
        {
            foreach (string[][] m_cache in m_generalCache)
            {
                m_cache[(int)LineFullName] = new string[TransportManager.MAX_LINE_COUNT + NetManager.MAX_NODE_COUNT];
            }
        }
        public static void ClearCacheLineName(WTSLine lineId)
        {
            foreach (string[][] m_cache in m_generalCache)
            {
                m_cache[(int)LineFullName][lineId.ToRefId()] = null;
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
            string name = GetTargetStringFor(refId, allCaps, applyAbbreviations, type);

            return name is null ? null : GetTextData(name, prefix, suffix, primaryFont, overrideFont);
        }

        public static string GetTargetStringFor(ushort refId, bool allCaps, bool applyAbbreviations, CacheArrayTypes type)
        {
            ref string[][] cache = ref m_generalCache[(allCaps ? 1 : 0) + (applyAbbreviations ? 1 : 2)];
            string name;
            switch (type)
            {
                case CacheArrayTypes.Districts:
                    name = refId > 256
                        ? UpdateMeshBuildingName((ushort)(refId - 256), ref cache[(int)BuildingName][refId - 256], allCaps, applyAbbreviations)
                        : UpdateMeshDistrict(refId, ref cache[(int)type][refId], allCaps, applyAbbreviations);
                    break;
                case CacheArrayTypes.Parks:
                    name = UpdateMeshPark(refId, ref cache[(int)type][refId], allCaps, applyAbbreviations);
                    break;
                case CacheArrayTypes.SuffixStreetName:
                    name = UpdateMeshStreetSuffix(refId, ref cache[(int)type][refId], allCaps, applyAbbreviations);
                    break;
                case CacheArrayTypes.FullStreetName:
                    name = UpdateMeshFullNameStreet(refId, ref cache[(int)type][refId], allCaps, applyAbbreviations);
                    break;
                case CacheArrayTypes.StreetQualifier:
                    name = UpdateMeshStreetQualifier(refId, ref cache[(int)type][refId], allCaps, applyAbbreviations);
                    break;
                case CacheArrayTypes.BuildingName:
                    name = UpdateMeshBuildingName(refId, ref cache[(int)type][refId], allCaps, applyAbbreviations);
                    break;
                case CacheArrayTypes.PostalCode:
                    name = UpdateMeshPostalCode(refId, ref cache[(int)type][refId], allCaps, applyAbbreviations);
                    break;
                case CacheArrayTypes.VehicleNumber:
                    name = UpdateMeshVehicleNumber(refId, ref cache[(int)type][refId], allCaps, applyAbbreviations);
                    break;
                case CacheArrayTypes.LineIdentifier:
                    name = UpdateMeshLineIdentifier(WTSLine.FromRefID(refId), ref cache[(int)type][refId], allCaps, applyAbbreviations);
                    break;
                case CacheArrayTypes.LineFullName:
                    name = UpdateMeshLineFullName(WTSLine.FromRefID(refId), ref cache[(int)type][refId], allCaps, applyAbbreviations);
                    break;
                default: name = null; break;
            };
            return name;
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

        public static string UpdateMeshFullNameStreet(ushort idx, ref string name, bool allCaps, bool applyAbbreviations)
        {
            if (name == null)
            {
                name = idx == 0
                    ? ""
                    : ApplyTransforms(WriteTheSignsMod.Controller.ConnectorADR.GetStreetFullName(idx) ?? "", allCaps, applyAbbreviations);
                // LogUtils.DoLog($"!GenName {name} for {idx} (UC={allCaps})");
            }
            return name;
        }

        public static string UpdateMeshStreetQualifier(ushort idx, ref string name, bool allCaps, bool applyAbbreviations)
        {
            if (name == null)
            {
                name = idx == 0
                    ? ""
                    : (NetManager.instance.m_segments.m_buffer[idx].m_flags & NetSegment.Flags.CustomName) == 0
                        ? ApplyTransforms(WriteTheSignsMod.Controller.ConnectorADR.GetStreetQualifier(idx), allCaps, applyAbbreviations)
                        : ApplyTransforms(WriteTheSignsMod.Controller.ConnectorADR.GetStreetQualifierCustom(idx), allCaps, applyAbbreviations);
                //    LogUtils.DoLog($"!GenName {name} for {idx} (UC={allCaps})");
            }
            return name;
        }
        public static string UpdateMeshPostalCode(ushort idx, ref string name, bool allCaps, bool applyAbbreviations)
        {
            if (name == null)
            {
                name = idx == 0
                    ? ""
                    : WriteTheSignsMod.Controller.ConnectorADR.GetStreetPostalCode(NetManager.instance.m_segments.m_buffer[idx].m_middlePosition, idx);
                //  LogUtils.DoLog($"!Gen Postal Code {name} for {idx} (UC={allCaps})");
            }
            return name;
        }
        public static string UpdateMeshDistrict(ushort districtId, ref string name, bool allCaps, bool applyAbbreviations)
        {
            if (name == null)
            {
                name = districtId == 0
                    ? ApplyTransforms(SimulationManager.instance.m_metaData.m_CityName, allCaps, applyAbbreviations)
                    : ApplyTransforms(DistrictManager.instance.GetDistrictName(districtId), allCaps, applyAbbreviations);
            }
            return name;
        }
        public static string UpdateMeshBuildingName(ushort buildingId, ref string name, bool allCaps, bool applyAbbreviations)
        {
            if (name == null)
            {
                name = ApplyTransforms(BuildingManager.instance.GetBuildingName(buildingId, InstanceID.Empty) ?? "", allCaps, applyAbbreviations);
            }
            return name;
        }
        public static string UpdateMeshVehicleNumber(ushort vehicleId, ref string name, bool allCaps, bool applyAbbreviations)
        {
            if (name == null)
            {
                name = WriteTheSignsMod.Controller.ConnectorTLM.GetVehicleIdentifier(vehicleId);
            }
            return name;
        }
        public static string UpdateMeshLineIdentifier(WTSLine line, ref string name, bool allCaps, bool applyAbbreviations)
        {
            if (name == null)
            {
                name = line.ZeroLine ? "" : WriteTheSignsMod.Controller.ConnectorTLM.GetLineIdString(line);
            }
            return name;
        }
        public static string UpdateMeshLineFullName(WTSLine line, ref string name, bool allCaps, bool applyAbbreviations)
        {
            if (name == null)
            {
                name = line.ZeroLine ? "" : ApplyTransforms(WriteTheSignsMod.Controller.ConnectorTLM.GetLineName(line), allCaps, applyAbbreviations);
            }
            return name;
        }
        public static string UpdateMeshPark(ushort parkId, ref string name, bool allCaps, bool applyAbbreviations)
        {
            if (name == null)
            {
                name = ApplyTransforms(DistrictManager.instance.GetParkName(parkId), allCaps, applyAbbreviations);
            }
            return name;
        }
        public static string UpdateMeshStreetSuffix(ushort idx, ref string name, bool allCaps, bool applyAbbreviations)
        {
            if (name == null)
            {
                name = idx == 0
                    ? ""
                    : (NetManager.instance.m_segments.m_buffer[idx].m_flags & NetSegment.Flags.CustomName) == 0
                        ? ApplyTransforms(WriteTheSignsMod.Controller.ConnectorADR.GetStreetSuffix(idx), allCaps, applyAbbreviations)
                        : ApplyTransforms(WriteTheSignsMod.Controller.ConnectorADR.GetStreetSuffixCustom(idx), allCaps, applyAbbreviations);
            }
            return name;
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

